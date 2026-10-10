from hardware import clone
from protocol import command, number, ProtocolError


class Component:
    def __init__(self, component_id, hardware, clock, state="stopped", **fields):
        self.hardware, self.clock = hardware, clock
        self.state = {"id": component_id, "state": state, "online": True, "direction": None, "speed": 0}
        self.state.update(fields)
        self.commit(self.state)

    def commit(self, desired):
        try:
            self.hardware.apply(desired)
        except Exception:
            self.state["online"] = False
            raise
        self.state = clone(desired)
        self.state["online"] = True

    def snapshot(self):
        return clone(self.state)

    def tick(self):
        pass

    def stop(self):
        pass


class Light(Component):
    def execute(self, cmd):
        command(cmd, ("on", "off"))
        self.commit(dict(self.state, state=cmd["action"]))

    def stop(self):
        self.execute({"action": "off"})


class Motor(Component):
    def execute(self, cmd):
        command(cmd, ("start", "stop"), ("direction", "speed") if cmd.get("action") == "start" else ())
        if cmd["action"] == "stop":
            self.commit(dict(self.state, state="stopped", speed=0))
        else:
            direction = cmd.get("direction") if cmd.get("direction") is not None else "forward"
            if direction not in ("forward", "reverse"):
                raise ProtocolError("INVALID_PARAMETER", "Direccion invalida")
            speed = cmd.get("speed") if cmd.get("speed") is not None else 50
            number(speed, 0, 100, True)
            self.commit(dict(self.state, state="running", direction=direction, speed=int(speed)))

    def stop(self):
        self.execute({"action": "stop"})


class Servo(Component):
    def execute(self, cmd):
        command(cmd, ("SET_POSITION",), ("position",))
        self.commit(dict(self.state, position=number(cmd.get("position"), 0, 180)))

    def stop(self):
        self.commit(self.state)  # Preserve the confirmed setpoint; no new safe angle.


class Hydraulic(Component):
    def tick(self):
        position = number(self.hardware.position(), 0, 100)
        movement = self.state["movement"]
        if (position == 100 and movement == "EXTENDING") or (position == 0 and movement == "RETRACTING"):
            movement = "STOPPED"
        self.commit(dict(self.state, position=position, movement=movement,
                         state="stopped" if movement == "STOPPED" else "running",
                         limitExtended=position == 100, limitRetracted=position == 0))

    def execute(self, cmd):
        command(cmd, ("EXTEND", "RETRACT", "STOP"))
        self.tick()
        position = self.state["position"]
        movement = "EXTENDING" if cmd["action"] == "EXTEND" and position < 100 else "RETRACTING" if cmd["action"] == "RETRACT" and position > 0 else "STOPPED"
        self.commit(dict(self.state, movement=movement, state="stopped" if movement == "STOPPED" else "running"))

    def stop(self):
        # Disable drive even if feedback has failed; do not pretend feedback was read.
        desired = dict(self.state, movement="STOPPED", state="stopped")
        try:
            position = number(self.hardware.position(), 0, 100)
            desired.update(position=position, limitExtended=position == 100, limitRetracted=position == 0)
        except Exception:
            self.commit(desired)
            self.state["online"] = False
            raise
        self.commit(desired)


class Bank(Component):
    EFFECTS = ("SWEEP_RIGHT", "SWEEP_LEFT", "PING_PONG", "BLINK")

    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.started = self.clock.now()

    def tick(self):
        effect = self.state["effect"]
        if effect == "NONE":
            return
        step = self.clock.elapsed(self.clock.now(), self.started) // (1200 - 9 * self.state["effectSpeed"])
        active = step % 8 if effect == "SWEEP_RIGHT" else 7 - step % 8 if effect == "SWEEP_LEFT" else step % 14 if step % 14 <= 7 else 14 - step % 14
        channels = [step % 2 == 0] * 8 if effect == "BLINK" else [i == active for i in range(8)]
        self.commit(dict(self.state, channels=channels, state="running"))

    def execute(self, cmd):
        command(cmd, self.EFFECTS + ("ALL_ON", "ALL_OFF", "STOP_EFFECT", "SET_SPEED"), ("speed",) if cmd.get("action") == "SET_SPEED" else ())
        action = cmd["action"]
        if action == "SET_SPEED":
            speed = int(number(cmd.get("speed"), 1, 100, True))
            self.commit(dict(self.state, effectSpeed=speed))
            self.started = self.clock.now()
        elif action in self.EFFECTS:
            self.commit(dict(self.state, effect=action, state="running"))
            self.started = self.clock.now()
        else:
            self.tick()
            channels = list(self.state["channels"]) if action == "STOP_EFFECT" else [action == "ALL_ON"] * 8
            self.commit(dict(self.state, channels=channels, effect="NONE", state="on" if any(channels) else "off"))
        self.tick()

    def stop(self):
        self.commit(dict(self.state, channels=[False] * 8, effect="NONE", state="off"))


def create_components(hardware, clock):
    return [Light("front-lights", hardware, clock, state="off"),
            Light("side-lights", hardware, clock, state="off"),
            Motor("main-motor", hardware, clock),
            Bank("main-light-bank", hardware, clock, state="off", channels=[False] * 8, effect="NONE", effectSpeed=50),
            Hydraulic("hydraulic-1", hardware, clock, position=0, movement="STOPPED", limitExtended=False, limitRetracted=True),
            Servo("servo-1", hardware, clock, position=90)]
