"""GPIO API double: validates adapter logic, not electrical or on-board behavior."""
import sys
import types
import unittest
from pathlib import Path
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "src"))
from esp32_hardware import Esp32Hardware


class Pin:
    OUT = 1
    def __init__(self, number, mode=None, value=0):
        self.number, self.level = number, value
    def value(self, value=None):
        if value is not None:
            self.level = value
        return self.level


class PWM:
    def __init__(self, pin, freq, duty_u16):
        self.pin, self.freq, self.duty = pin, freq, duty_u16
        self.writes = []
    def duty_u16(self, value):
        self.writes.append(value)
        self.duty = value


class ADC:
    ATTN_11DB = 3
    def __init__(self, pin):
        self.raw = 0
    def atten(self, value):
        pass
    def read_u16(self):
        return self.raw


class GpioTests(unittest.TestCase):
    def setUp(self):
        # Arbitrary test-only identifiers, never a recommended ESP32 wiring map.
        self.mapping = {"front-lights": {"pins": [1]}, "side-lights": {"pins": [2]},
                        "main-light-bank": {"pins": list(range(3, 11))},
                        "main-motor": {"direction": 11, "enable": 12},
                        "hydraulic-1": {"extend": 13, "retract": 14, "feedback": 15, "adc_min": 100, "adc_max": 60100},
                        "servo-1": {"pin": 16, "min_us": 500, "max_us": 2500}}
        self.fake = patch.dict(sys.modules, {"machine": types.SimpleNamespace(Pin=Pin, PWM=PWM, ADC=ADC)})
        self.fake.start()
        self.addCleanup(self.fake.stop)
        self.hardware = Esp32Hardware(self.mapping)

    def test_initial_outputs_are_disabled(self):
        self.assertEqual(self.hardware.motor_enable.duty, 0)
        self.assertEqual(self.hardware.extend.level, 0)
        self.assertEqual(self.hardware.retract.level, 0)

    def test_motor_disable_before_direction_and_speed(self):
        self.hardware.apply({"id": "main-motor", "state": "running", "direction": "reverse", "speed": 40})
        self.assertEqual(self.hardware.motor_enable.writes, [0, 26214])
        self.assertEqual(self.hardware.motor_direction.level, 1)
        self.hardware.apply({"id": "main-motor", "state": "stopped", "direction": "reverse", "speed": 0})
        self.assertEqual(self.hardware.motor_enable.duty, 0)

    def test_hydraulic_mutual_exclusion_and_feedback(self):
        for movement, expected in (("EXTENDING", (1, 0)), ("RETRACTING", (0, 1)), ("STOPPED", (0, 0))):
            self.hardware.apply({"id": "hydraulic-1", "movement": movement})
            self.assertEqual((self.hardware.extend.level, self.hardware.retract.level), expected)
        for raw, expected in ((100, 0), (30100, 50), (60100, 100)):
            self.hardware.feedback.raw = raw
            self.assertEqual(self.hardware.position(), expected)

    def test_lights_channels_and_servo_pwm(self):
        self.hardware.apply({"id": "front-lights", "state": "on"})
        self.assertEqual(self.hardware.devices["front-lights"][0].level, 1)
        self.hardware.apply({"id": "main-light-bank", "state": "running", "channels": [True] + [False] * 7})
        self.assertEqual([p.level for p in self.hardware.devices["main-light-bank"]], [1] + [0] * 7)
        for angle, pulse in ((0, 500), (90, 1500), (180, 2500)):
            self.hardware.apply({"id": "servo-1", "position": angle})
            self.assertEqual(self.hardware.servo.duty, int(pulse * 65535 / 20000))

    def test_invalid_configuration_rejected(self):
        with self.assertRaises(ValueError):
            Esp32Hardware({})
        self.mapping["servo-1"]["pin"] = 1
        with self.assertRaises(ValueError):
            Esp32Hardware(self.mapping)
