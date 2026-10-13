"""Owned, isolated process on temporary test port 5081. Never stops a shared API."""
import json
import pathlib
import subprocess
import tempfile
import time
import urllib.request
import urllib.error
import argparse
import sys
import socket

ROOT = pathlib.Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument("--api-dll", default=str(ROOT / "backend/Carroza.Api/bin/Debug/net9.0/Carroza.Api.dll"))
DLL = pathlib.Path(parser.parse_args().api_dll).resolve()
BASE = "http://127.0.0.1:5081"


def call(path, body=None):
    req = urllib.request.Request(BASE + path, data=None if body is None else json.dumps(body).encode(), headers={"Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=2) as response:
        return json.load(response)


def start(path, extra=()):
    process = subprocess.Popen(["dotnet", str(DLL), "--urls", BASE, "--Sequences:Path", str(path), "--Logging:LogLevel:Default=Warning", *extra], cwd=ROOT, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    try:
        for _ in range(60):
            assert process.poll() is None, "Proceso no pudo iniciar; puerto debe estar libre"
            try:
                call("/api/sequences/execution")
                return process
            except (urllib.error.URLError, TimeoutError):
                time.sleep(.05)
        raise AssertionError("API aislada no disponible")
    except BaseException:
        process.terminate()
        process.wait(timeout=5)
        raise


def stop(process):
    process.terminate()
    process.wait(timeout=5)


with tempfile.TemporaryDirectory(prefix="carroza-restart-") as directory:
    path = pathlib.Path(directory) / "sequences.json"
    process = start(path)
    try:
        call("/api/sequences", {"id": "restart-test", "name": "Reinicio", "description": "", "steps": [
            {"id": "a", "order": 1, "delayMs": 0, "componentId": "main-motor", "command": {"action": "start", "speed": 40}},
            {"id": "b", "order": 2, "delayMs": 10000, "componentId": "front-lights", "command": {"action": "on"}}]})
        call("/api/sequences/restart-test/start?mode=CONTINUOUS", {})
        time.sleep(.1)
        assert call("/api/sequences/execution")["status"] == "RUNNING"
    finally:
        stop(process)
    process = start(path)
    try:
        assert call("/api/sequences/restart-test")["name"] == "Reinicio"
        assert call("/api/sequences/execution")["status"] == "IDLE"
        assert all(d["state"] in ("off", "stopped") for d in call("/api/devices"))
        time.sleep(.2)
        assert call("/api/sequences/execution")["status"] == "IDLE"
    finally:
        stop(process)
    process = start(path, ["--Simulator:Faults:main-motor=TIMEOUT"])
    try:
        call("/api/sequences/restart-test/start?mode=CONTINUOUS", {})
        for _ in range(100):
            state = call("/api/sequences/execution")
            if state["status"] == "FAILED":
                break
            time.sleep(.02)
        assert state["status"] == "FAILED" and "TIMEOUT" in state["error"]
        assert len(call("/api/sequences/events")) == 1
        assert all(d["state"] in ("off", "stopped") for d in call("/api/devices"))
    finally:
        stop(process)
    hal = subprocess.Popen([sys.executable, str(ROOT / "firmware/esp32-carroza/host.py"), "--port", "5091", "--watchdog-ms", "5000"], cwd=ROOT, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    try:
        for _ in range(100):
            assert hal.poll() is None, "HAL aislada no pudo iniciar; 5091 debe estar libre"
            try:
                with socket.create_connection(("127.0.0.1", 5091), timeout=.1):
                    break
            except OSError:
                time.sleep(.03)
        extra = ["--ComponentGateway:Mode=ESP32", "--ComponentGateway:ESP32:Endpoint=http://127.0.0.1:5091/v1/exchange"]
        process = start(path, extra)
        try:
            call("/api/sequences/restart-test/start?mode=CONTINUOUS", {})
            time.sleep(.15)
            assert next(d for d in call("/api/devices") if d["id"] == "main-motor")["state"] == "running"
        finally:
            stop(process)  # Abrupt termination: the HAL remains alive and receives no shutdown hook.
        process = start(path, extra)
        try:
            assert call("/api/sequences/execution")["status"] == "IDLE"
            assert call("/api/sequences/restart-test")["name"] == "Reinicio"
            assert all(d["state"] in ("off", "stopped") for d in call("/api/devices"))
            time.sleep(.2)
            assert call("/api/sequences/execution")["status"] == "IDLE"
        finally:
            stop(process)
    finally:
        stop(hal)
    process = start(path, extra + ["--ComponentGateway:ESP32:TimeoutMs=100"])
    try:
        telemetry = call("/api/telemetry")
        assert telemetry["protectionLatched"] and not telemetry["stopConfirmed"] and not telemetry["online"]
        try:
            call("/api/sequences/restart-test/start?mode=CONTINUOUS", {})
            raise AssertionError("Arranque sin controlador permitió ejecución")
        except urllib.error.HTTPError as error:
            assert error.code == 409
    finally:
        stop(process)
print("OK: reinicio real conserva definición, descarta ejecución, estado seguro (Simulator y ESP32/HAL sobreviviente) y TIMEOUT continuo sin pasos posteriores.")
