import os
import binascii
from components import create_components
from protocol import parse, envelope, response, ProtocolError, ZERO_UUID, uuid


def new_token():
    return binascii.hexlify(os.urandom(16)).decode()


class Dispatcher:
    """All calls run synchronously on one event loop: no await inside mutations."""
    def __init__(self, hardware, clock, watchdog_ms=5000, token_factory=new_token):
        if type(watchdog_ms) is not int or not 500 <= watchdog_ms <= 60000:
            raise ValueError("watchdog_ms fuera de rango")
        self.clock, self.watchdog_ms = clock, watchdog_ms
        self.components = create_components(hardware, clock)
        self.by_id = {c.state["id"]: c for c in self.components}
        self.token_factory = token_factory
        self.token = token_factory()
        self.last_valid = clock.now()
        self.expired = False
        self.faulted = False

    def stop(self):
        self.token = self.token_factory()  # Invalidate even when an output fails.
        failure = None
        for component in self.components:
            try:
                component.stop()
            except Exception as error:
                component.state["online"] = False
                failure = error
        self.faulted = failure is not None
        if failure:
            raise ProtocolError("INTERNAL_ERROR", "Parada no confirmada por todos los componentes")

    def tick(self):
        if not self.expired and self.clock.elapsed(self.clock.now(), self.last_valid) >= self.watchdog_ms:
            self.expired = True
            self.stop()
        if not self.faulted:
            try:
                for component in self.components:
                    component.tick()
            except Exception:
                self.stop()
                self.faulted = True
                raise

    def handle(self, body):
        request_id = ZERO_UUID
        try:
            request = parse(body)
            if uuid(request.get("requestId")):
                request_id = request["requestId"]
            envelope(request)
            operation = request["operation"]
            self.tick()
            if operation == "STOP_ALL":
                self.stop()
                states = [c.snapshot() for c in self.components]
            else:
                component = self.by_id.get(request["componentId"])
                if component is None:
                    raise ProtocolError("INVALID_PARAMETER", "Componente desconocido")
                if operation == "EXECUTE":
                    if request.get("controlToken") != self.token:
                        raise ProtocolError("TIMEOUT", "Comando obsoleto: consultar estado antes de una nueva intencion")
                    if self.faulted:
                        raise ProtocolError("INTERNAL_ERROR", "Parada no confirmada; requiere STOP_ALL")
                    if not isinstance(request["command"], dict):
                        raise ProtocolError("INVALID_PARAMETER", "command requerido")
                    self.clock.utc()  # Do not actuate before a usable timestamp exists.
                    try:
                        component.execute(request["command"])
                    except ProtocolError as error:
                        if error.code in ("DEVICE_OFFLINE", "INTERNAL_ERROR"):
                            self.stop()
                        raise
                    except Exception:
                        self.stop()
                        raise
                states = [component.snapshot()]
            timestamp = self.clock.utc()
            self.last_valid = self.clock.now()
            self.expired = False
            return response(request_id, states=states, timestamp=timestamp, token=self.token)
        except ProtocolError as error:
            return response(request_id, error=error, token=self.token)
        except Exception:
            return response(request_id, error=ProtocolError("INTERNAL_ERROR", "Controlador/HAL no confirmo la operacion"), token=self.token)
