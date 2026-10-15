import json
import unittest
from test_firmware import FakeClock, request
from dispatcher import Dispatcher
from hardware import Hardware, SimulatedHardware


class TelemetryTests(unittest.TestCase):
    def setUp(self):
        self.clock = FakeClock()
        self.hardware = SimulatedHardware(self.clock)
        self.dispatcher = Dispatcher(self.hardware, self.clock, watchdog_ms=60000)

    def send(self, operation="GET_TELEMETRY", simulation=None):
        message = request(operation, None)
        if simulation is not None:
            message["simulation"] = simulation
        return self.dispatcher.handle(json.dumps(message))

    def control(self, action, **kwargs):
        return self.send("SIMULATION_CONTROL", dict(action=action, **kwargs))

    def execute(self, token=None):
        return self.dispatcher.handle(json.dumps(request("EXECUTE", "main-motor",
            {"action": "start", "speed": 50}, token or self.dispatcher.token)))

    def test_deterministic_values_follow_confirmed_motor(self):
        self.assertTrue(self.execute()["success"])
        result = self.send()
        self.assertTrue(result["success"], result)
        self.assertEqual(result["states"], [])
        t = result["telemetry"]
        self.assertEqual((t["voltage"], t["current"], t["temperature"], t["motorSpeed"]), (23.5, 5, 40, 50))
        self.assertEqual((t["source"], t["quality"], t["ageMs"]), ("SIMULATED", "VALID", 0))

    def test_faults_stop_immediately_latch_and_reject_delayed_commands(self):
        for code in ("OVER_TEMPERATURE", "OVER_CURRENT", "VOLTAGE_RANGE", "COMMUNICATION_LOSS", "INVALID_SENSOR", "STALE_SENSOR"):
            with self.subTest(code=code):
                self.setUp()
                self.execute()
                old = self.dispatcher.token
                result = self.control("SET_FAULT", code=code, active=True)
                self.assertTrue(result["success"], result)
                t = result["telemetry"]
                self.assertTrue(t["protectionLatched"])
                self.assertTrue(t["stopConfirmed"])
                self.assertEqual(self.hardware.outputs["main-motor"]["speed"], 0)
                self.assertNotEqual(old, self.dispatcher.token)
                self.assertFalse(self.execute()["success"])
                self.assertEqual(self.execute(old)["error"]["code"], "TIMEOUT")
                self.assertFalse(self.control("RESET")["success"])
                self.control("ACKNOWLEDGE", code=code)
                self.assertFalse(self.control("RESET")["success"])
                self.control("SET_FAULT", code=code, active=False)
                self.assertTrue(self.send()["telemetry"]["protectionLatched"])
                self.assertTrue(self.control("RESET")["success"])
                self.assertEqual(self.hardware.outputs["main-motor"]["speed"], 0)
                self.assertFalse(self.execute(old)["success"])
                self.assertTrue(self.execute()["success"])

    def test_quality_fault_values_and_alert_history(self):
        for code, quality, key, value in (("OVER_TEMPERATURE", "VALID", "temperature", 85),
                ("OVER_CURRENT", "VALID", "current", 15), ("VOLTAGE_RANGE", "VALID", "voltage", 16),
                ("COMMUNICATION_LOSS", "OFFLINE", "voltage", None),
                ("INVALID_SENSOR", "INVALID", "temperature", None), ("STALE_SENSOR", "STALE", "ageMs", 5000)):
            self.setUp()
            result = self.control("SET_FAULT", code=code, active=True)
            self.assertTrue(result["success"], result)
            t = result["telemetry"]
            self.assertEqual((t["quality"], t[key]), (quality, value))
            self.assertEqual(t["alerts"][0]["activatedAt"], self.clock.utc())
            self.control("SET_FAULT", code=code, active=False)
            self.assertEqual(self.send()["telemetry"]["alerts"][0]["resolvedAt"], self.clock.utc())
            self.assertFalse(self.control("RESET")["success"])

    def test_failed_stop_never_confirms_safety(self):
        self.execute()
        self.hardware.fail_id = "main-motor"
        self.assertFalse(self.control("SET_FAULT", code="OVER_CURRENT", active=True)["success"])
        t = self.send()["telemetry"]
        self.assertTrue(t["protectionLatched"])
        self.assertFalse(t["stopConfirmed"])
        self.assertFalse(self.execute()["success"])

    def test_stop_all_does_not_clear_latch_and_snapshot_cannot_mutate_alert(self):
        self.control("SET_FAULT", code="OVER_CURRENT", active=True)
        self.assertTrue(self.send("STOP_ALL")["success"])
        t = self.send()["telemetry"]
        t["alerts"][0]["active"] = False
        self.assertTrue(self.send()["telemetry"]["alerts"][0]["active"])
        self.assertFalse(self.execute()["success"])

    def test_real_hal_has_no_measurements_or_injection(self):
        class OutputsOnly(Hardware):
            def apply(self, state):
                pass
            def position(self):
                return 0
        self.dispatcher = Dispatcher(OutputsOnly(), self.clock)
        result = self.send()
        self.assertTrue(result["success"], result)
        t = result["telemetry"]
        self.assertEqual(t["quality"], "UNAVAILABLE")
        self.assertFalse(t["canInjectFaults"])
        for key in ("voltage", "current", "temperature", "motorSpeed", "sampledAt"):
            self.assertIsNone(t[key])
        self.assertFalse(self.control("SET_FAULT", code="OVER_CURRENT", active=True)["success"])

    def test_invalid_controls_do_not_mutate(self):
        for simulation in ({}, {"action": "WHAT"}, {"action": "SET_FAULT", "code": "WHAT", "active": True},
                {"action": "SET_FAULT", "code": "OVER_CURRENT", "active": 1},
                {"action": "RESET", "active": True}, {"action": "RESET", "code": "OVER_CURRENT"},
                {"action": "ACKNOWLEDGE", "active": False}):
            self.assertFalse(self.send("SIMULATION_CONTROL", simulation)["success"])
        result = self.send()
        self.assertTrue(result["success"], result)
        self.assertEqual(result["telemetry"]["alerts"], [])

    def test_recovery_needs_confirmed_stop_and_invalidates_fault_era_intentions(self):
        self.execute()
        self.hardware.fail_id = "main-motor"
        self.control("SET_FAULT", code="OVER_CURRENT", active=True)
        self.control("SET_FAULT", code="OVER_CURRENT", active=False)
        self.control("ACKNOWLEDGE")
        self.assertFalse(self.control("RESET")["success"])
        self.hardware.fail_id = None
        self.assertTrue(self.send("STOP_ALL")["success"])
        old = self.dispatcher.token
        self.assertTrue(self.control("RESET")["success"])
        self.assertFalse(self.execute(old)["success"])
        self.assertTrue(self.execute()["success"])


if __name__ == "__main__":
    unittest.main()
