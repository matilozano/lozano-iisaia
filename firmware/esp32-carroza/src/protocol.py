import json
import math

ZERO_UUID = "00000000-0000-0000-0000-000000000000"


class ProtocolError(Exception):
    def __init__(self, code, message):
        self.code = code
        self.message = message
        super().__init__(message)


def uuid(value):
    if not isinstance(value, str) or len(value) != 36 or value == ZERO_UUID:
        return False
    return all(c == "-" if i in (8, 13, 18, 23) else c in "0123456789abcdefABCDEF"
               for i, c in enumerate(value))


def parse(body):
    try:
        request = json.loads(body)
    except (ValueError, TypeError):
        raise ProtocolError("INVALID_PARAMETER", "JSON invalido")
    if not isinstance(request, dict):
        raise ProtocolError("INVALID_PARAMETER", "Envelope requerido")
    return request


def envelope(request):
    if any(k not in request for k in ("version", "requestId", "operation", "componentId", "command")):
        raise ProtocolError("INVALID_PARAMETER", "Envelope incompleto")
    if type(request["version"]) is not int or request["version"] != 1:
        raise ProtocolError("INVALID_PARAMETER", "Version incompatible")
    if not uuid(request["requestId"]):
        raise ProtocolError("INVALID_PARAMETER", "requestId debe ser UUID no nulo")
    operation = request["operation"]
    if operation not in ("GET_STATE", "EXECUTE", "STOP_ALL"):
        raise ProtocolError("INVALID_COMMAND", "Operacion desconocida")
    if operation == "STOP_ALL":
        if request["componentId"] is not None or request["command"] is not None:
            raise ProtocolError("INVALID_PARAMETER", "STOP_ALL no admite componente/comando")
    elif not isinstance(request["componentId"], str):
        raise ProtocolError("INVALID_PARAMETER", "componentId requerido")
    if operation == "GET_STATE" and request["command"] is not None:
        raise ProtocolError("INVALID_PARAMETER", "GET_STATE no admite comando")


def command(value, actions, parameters=()):
    if not isinstance(value, dict) or not isinstance(value.get("action"), str):
        raise ProtocolError("INVALID_PARAMETER", "command.action requerido")
    if value["action"] not in actions:
        raise ProtocolError("INVALID_COMMAND", "Comando no soportado")
    for key, item in value.items():
        if key != "action" and key not in parameters and item is not None:
            raise ProtocolError("INVALID_PARAMETER", "Parametro no admitido: " + key)


def number(value, low, high, integer=False):
    if type(value) not in (int, float) or not math.isfinite(value) or not low <= value <= high or (integer and int(value) != value):
        raise ProtocolError("INVALID_PARAMETER", "Valor fuera de rango/tipo")
    return value


def response(request_id, states=None, error=None, timestamp=None, token=None):
    return {"version": 1, "requestId": request_id, "success": error is None,
            "executedAt": timestamp if error is None else None,
            "states": states if error is None else None,
            "error": None if error is None else {"code": error.code, "message": error.message},
            "controlToken": token}
