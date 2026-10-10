from protocol import ProtocolError


def clone(state):
    result = dict(state)
    if "channels" in result:
        result["channels"] = list(result["channels"])
    return result


class Hardware:
    """Synchronous bounded HAL. apply returns only after outputs are accepted."""
    def apply(self, state):
        raise NotImplementedError

    def position(self):
        raise NotImplementedError


class SimulatedHardware(Hardware):
    def __init__(self, clock):
        self.clock = clock
        self.outputs = {}
        self.hydraulic_position = 0.0
        self.updated = clock.now()
        self.fail_id = None

    def position(self):
        now = self.clock.now()
        movement = self.outputs.get("hydraulic-1", {}).get("movement", "STOPPED")
        rate = 20 if movement == "EXTENDING" else -20 if movement == "RETRACTING" else 0
        self.hydraulic_position = min(100, max(0, self.hydraulic_position + self.clock.elapsed(now, self.updated) * rate / 1000))
        self.updated = now
        return self.hydraulic_position

    def apply(self, state):
        if state["id"] == self.fail_id:
            raise ProtocolError("DEVICE_OFFLINE", state["id"] + ": HAL no disponible")
        if state["id"] == "hydraulic-1":
            self.position()
        self.outputs[state["id"]] = clone(state)
