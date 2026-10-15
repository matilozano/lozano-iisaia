from hardware import Hardware


class Esp32Hardware(Hardware):
    """Low-voltage driver signals only. No direct loads. No GPIO in domain logic."""
    def __init__(self, mapping):
        from machine import Pin, PWM, ADC
        ids = ("front-lights", "side-lights", "main-light-bank", "main-motor", "hydraulic-1", "servo-1")
        if set(mapping) != set(ids):
            raise ValueError("PIN_MAP debe definir los seis componentes")
        pins = []
        for key in ids:
            item = mapping[key]
            fields = ("direction", "enable") if key == "main-motor" else ("extend", "retract", "feedback") if key == "hydraulic-1" else ("pin",) if key == "servo-1" else ()
            values = [item[f] for f in fields] if fields else item["pins"]
            if not fields and len(values) != (8 if key == "main-light-bank" else 1):
                raise ValueError("Cantidad de canales GPIO incorrecta")
            pins.extend(values)
        if any(type(p) is not int or p < 0 for p in pins) or len(pins) != len(set(pins)):
            raise ValueError("GPIO invalido o duplicado")
        hydraulic = mapping["hydraulic-1"]
        if not 0 <= hydraulic["adc_min"] < hydraulic["adc_max"] <= 65535:
            raise ValueError("Calibracion ADC invalida")
        servo = mapping["servo-1"]
        if not 0 < servo["min_us"] < servo["max_us"] < 20000:
            raise ValueError("Calibracion servo invalida")
        self.mapping, self.devices = mapping, {}
        for key in ("front-lights", "side-lights", "main-light-bank"):
            self.devices[key] = [Pin(p, Pin.OUT, value=0) for p in mapping[key]["pins"]]
        motor = mapping["main-motor"]
        self.motor_direction = Pin(motor["direction"], Pin.OUT, value=0)
        self.motor_enable = PWM(Pin(motor["enable"], Pin.OUT, value=0), freq=1000, duty_u16=0)
        self.extend = Pin(hydraulic["extend"], Pin.OUT, value=0)
        self.retract = Pin(hydraulic["retract"], Pin.OUT, value=0)
        self.feedback = ADC(Pin(hydraulic["feedback"]))
        self.feedback.atten(ADC.ATTN_11DB)
        self.servo = PWM(Pin(servo["pin"]), freq=50, duty_u16=0)

    def apply(self, state):
        key = state["id"]
        if key in self.devices:
            values = state.get("channels", [state["state"] == "on"])
            for pin, value in zip(self.devices[key], values):
                pin.value(int(value))
        elif key == "main-motor":
            self.motor_enable.duty_u16(0)  # Never reverse with enable asserted.
            self.motor_direction.value(int(state["direction"] == "reverse"))
            self.motor_enable.duty_u16(int(state["speed"] * 65535 / 100) if state["state"] == "running" else 0)
        elif key == "hydraulic-1":
            self.extend.value(0)
            self.retract.value(0)
            if state["movement"] == "EXTENDING":
                self.extend.value(1)
            elif state["movement"] == "RETRACTING":
                self.retract.value(1)
        elif key == "servo-1":
            config = self.mapping[key]
            us = config["min_us"] + state["position"] * (config["max_us"] - config["min_us"]) / 180
            self.servo.duty_u16(int(us * 65535 / 20000))

    def position(self):
        config = self.mapping["hydraulic-1"]
        value = self.feedback.read_u16()
        return min(100, max(0, (value - config["adc_min"]) * 100 / (config["adc_max"] - config["adc_min"])))
