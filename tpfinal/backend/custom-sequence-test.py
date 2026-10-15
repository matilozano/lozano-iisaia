"""HTTP regression of custom definitions on the existing TP API. No browser timers."""
import json
import time
import uuid
import urllib.request
import urllib.error

BASE = "http://127.0.0.1:5080"


def request(path, body=None, method=None, status=200):
    req = urllib.request.Request(BASE + path, data=None if body is None else json.dumps(body).encode(),
                                 headers={"Content-Type": "application/json"}, method=method or ("GET" if body is None else "POST"))
    try:
        response = urllib.request.urlopen(req, timeout=5)
    except urllib.error.HTTPError as error:
        response = error
    with response:
        data = response.read()
        assert response.status == status, (path, response.status, data)
        if data:
            assert "application/json" in response.headers.get("Content-Type", ""), path
            return json.loads(data)


def until(predicate, seconds=5):
    deadline = time.monotonic() + seconds
    while time.monotonic() < deadline:
        value = request("/api/sequences/execution")
        if predicate(value):
            return value
        time.sleep(.03)
    raise AssertionError("No se alcanzó el estado esperado: " + str(value))


identifier = uuid.uuid4().hex
definition = {"id": identifier, "name": "Regresión personalizada", "description": "Prueba HTTP temporal", "steps": [
    {"id": uuid.uuid4().hex, "order": i + 1, "delayMs": 80, "componentId": component, "command": command}
    for i, (component, command) in enumerate([
        ("front-lights", {"action": "on"}), ("side-lights", {"action": "on"}),
        ("main-light-bank", {"action": "SWEEP_LEFT"}), ("main-motor", {"action": "start", "direction": "reverse", "speed": 20}),
        ("hydraulic-1", {"action": "EXTEND"}), ("servo-1", {"action": "SET_POSITION", "position": 120}),
        ("hydraulic-1", {"action": "STOP"}), ("main-motor", {"action": "stop"}),
        ("main-light-bank", {"action": "ALL_OFF"}), ("front-lights", {"action": "off"}), ("side-lights", {"action": "off"})])
]}
created = []
try:
    request("/api/devices/stop-all", {}, status=204)
    saved = request("/api/sequences", definition, status=201)
    created.append(identifier)
    assert request("/api/sequences/" + identifier)["revision"] == saved["revision"]
    invalid = dict(definition, id=uuid.uuid4().hex, steps=[dict(definition["steps"][0], command={"action": "SET_CHANNEL"})])
    assert request("/api/sequences", invalid, status=400)["error"] == "INVALID_PARAMETER"
    saved["name"] = "Actualizada"
    updated = request("/api/sequences/" + identifier, saved, "PUT")
    request("/api/sequences/" + identifier, saved, "PUT", status=409)
    duplicate = request("/api/sequences/" + identifier + "/duplicate", {})
    created.append(duplicate["id"])
    assert duplicate["steps"][0]["id"] != saved["steps"][0]["id"]
    all_custom = [s["id"] for s in request("/api/sequences") if s["id"] != "SHOW_FNE"]
    request("/api/sequences/order", list(reversed(all_custom)), status=204)
    request("/api/sequences/" + identifier + "/start?mode=UNKNOWN", {}, status=400)
    run = request("/api/sequences/" + identifier + "/start", {})
    finished = until(lambda e: e["status"] == "COMPLETED")
    assert finished["runId"] == run["runId"] and finished["completedCycles"] == 1
    events = [e for e in request("/api/sequences/events") if e["runId"] == run["runId"]]
    assert [e["componentId"] for e in events] == [s["componentId"] for s in definition["steps"]]
    run = request("/api/sequences/" + identifier + "/start?mode=CONTINUOUS", {})
    until(lambda e: e["completedCycles"] >= 2)
    request("/api/sequences/" + identifier + "/start", {}, status=409)
    request("/api/devices/main-motor/commands", {"action": "start"}, status=409)
    request("/api/sequences/" + identifier, updated, "PUT", status=409)
    states = request("/api/devices/stop-all?includeState=true", {})
    assert all(s["state"] in ("off", "stopped") for s in states)
    assert until(lambda e: e["status"] == "CANCELLED")["runId"] == run["runId"]
    count = len([e for e in request("/api/sequences/events") if e["runId"] == run["runId"]])
    time.sleep(1.3)
    assert len([e for e in request("/api/sequences/events") if e["runId"] == run["runId"]]) == count
    request("/api/sequences/" + identifier + "/start?mode=CONTINUOUS", {})
    until(lambda e: e["currentStep"] >= 4)
    request("/api/sequences/cancel", {})
    until(lambda e: e["status"] == "CANCELLED")
    run = request("/api/sequences/" + identifier + "/start?mode=CONTINUOUS", {})
    until(lambda e: e["currentStep"] >= 4)
    request("/api/telemetry/simulation", {"action": "SET_FAULT", "code": "COMMUNICATION_LOSS", "active": True})
    until(lambda e: e["status"] == "CANCELLED")
    request("/api/sequences/" + identifier + "/start?mode=CONTINUOUS", {}, status=409)
    request("/api/telemetry/simulation", {"action": "SET_FAULT", "code": "COMMUNICATION_LOSS", "active": False})
    request("/api/telemetry/simulation", {"action": "ACKNOWLEDGE"})
    request("/api/telemetry/simulation", {"action": "RESET"})
    time.sleep(.3)
    assert request("/api/sequences/execution")["status"] == "CANCELLED"
    schema = request("/swagger/v1/swagger.json")
    assert "put" in schema["paths"]["/api/sequences/{id}"]
    assert "delete" in schema["paths"]["/api/sequences/{id}"]
    fields = schema["components"]["schemas"]["SequenceExecutionDto"]["properties"]
    assert "mode" in fields and "completedCycles" in fields
    print("OK: CRUD HTTP, revisión, orden, duplicación, validación, único/continuo, conflictos, STOP ALL/cancelación sin reactivación y OpenAPI.")
finally:
    request("/api/devices/stop-all", {}, status=204)
    for identifier in created:
        item = request("/api/sequences/" + identifier)
        request("/api/sequences/" + identifier + "?revision=" + item["revision"], method="DELETE", status=204)
