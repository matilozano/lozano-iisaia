from hardware import SimulatedHardware
from protocol import ProtocolError

FAULTS = ("OVER_TEMPERATURE", "OVER_CURRENT", "VOLTAGE_RANGE",
          "COMMUNICATION_LOSS", "INVALID_SENSOR", "STALE_SENSOR")


class Telemetry:
    """Simulation measurements and retained protection lifecycle, bounded to six alerts."""
    def __init__(self, hardware, clock):
        self.hardware, self.clock = hardware, clock
        self.simulated = isinstance(hardware, SimulatedHardware)
        self.alerts = {}
        self.latched = False
        self.stop_confirmed = False
        self.stale_since = None
        self.sampled_at = None

    def control(self, value):
        if not self.simulated:
            raise ProtocolError("INVALID_COMMAND", "Inyeccion disponible solo en HAL simulada")
        if not isinstance(value, dict):
            raise ProtocolError("INVALID_PARAMETER", "simulation requerido")
        action, code = value.get("action"), value.get("code")
        if action not in ("SET_FAULT", "ACKNOWLEDGE", "RESET"):
            raise ProtocolError("INVALID_PARAMETER", "Accion de simulacion invalida")
        if any(key not in ("action", "code", "active") for key in value):
            raise ProtocolError("INVALID_PARAMETER", "Parametro de simulacion desconocido")
        if action != "SET_FAULT" and value.get("active") is not None or action == "RESET" and code is not None:
            raise ProtocolError("INVALID_PARAMETER", "Parametro no admitido para esta accion")
        if action == "SET_FAULT":
            if code not in FAULTS or type(value.get("active")) is not bool:
                raise ProtocolError("INVALID_PARAMETER", "Falla/active invalido")
            active = value["active"]
            previous = self.alerts.get(code)
            if active and (previous is None or not previous["active"]):
                self.alerts[code] = dict(code=code, active=True, acknowledged=False,
                                         activatedAt=self.clock.utc(), resolvedAt=None)
                if code == "STALE_SENSOR":
                    self.stale_since = self.clock.now()
            elif not active and previous and previous["active"]:
                previous["active"] = False
                previous["resolvedAt"] = self.clock.utc()
        elif action == "ACKNOWLEDGE":
            if code is not None and code not in self.alerts:
                raise ProtocolError("INVALID_PARAMETER", "Alerta desconocida")
            for alert in self.alerts.values():
                if code is None or alert["code"] == code:
                    alert["acknowledged"] = True
        else:
            if any(a["active"] or not a["acknowledged"] for a in self.alerts.values()) or (self.latched and not self.stop_confirmed):
                raise ProtocolError("INVALID_PARAMETER", "Resolver, reconocer y confirmar parada antes de restablecer")
            self.latched = False

    def active(self, code):
        return self.alerts.get(code, {}).get("active", False)

    def critical(self):
        return any(a["active"] for a in self.alerts.values())

    def snapshot(self):
        quality = "VALID" if self.simulated else "UNAVAILABLE"
        online = not self.active("COMMUNICATION_LOSS")
        age = 0
        values = dict(voltage=None, current=None, temperature=None, motorSpeed=None)
        if self.simulated:
            speed = self.hardware.outputs.get("main-motor", {}).get("speed", 0)
            values = dict(voltage=16 if self.active("VOLTAGE_RANGE") else 24 - speed * .01,
                          current=15 if self.active("OVER_CURRENT") else 1 + speed * .08,
                          temperature=85 if self.active("OVER_TEMPERATURE") else 25 + speed * .3,
                          motorSpeed=speed)
            if self.active("STALE_SENSOR"):
                quality = "STALE"
                age = 5000 + self.clock.elapsed(self.clock.now(), self.stale_since)
            else:
                self.sampled_at = self.clock.utc()
            if self.active("INVALID_SENSOR") or not online:
                quality = "INVALID" if online else "OFFLINE"
                values = dict(voltage=None, current=None, temperature=None, motorSpeed=None)
        result = dict(source="SIMULATED" if self.simulated else "UNAVAILABLE", online=online,
                      sampledAt=self.sampled_at, ageMs=age, quality=quality,
                      protectionLatched=self.latched, stopConfirmed=self.stop_confirmed,
                      canInjectFaults=self.simulated, alerts=[dict(a) for a in self.alerts.values()])
        result.update(values)
        return result
