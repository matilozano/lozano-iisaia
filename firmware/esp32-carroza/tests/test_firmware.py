import asyncio
import json
from pathlib import Path
import sys
import unittest
import uuid

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "src"))
from dispatcher import Dispatcher
from hardware import SimulatedHardware
from server import HttpServer


class FakeClock:
    def __init__(self):
        self.ms = 0

    def now(self):
        return self.ms

    def elapsed(self, now, before):
        return now - before

    def utc(self):
        return "2026-10-07T12:00:00Z"


def request(operation="GET_STATE", component="front-lights", command=None, token=None):
    return dict(version=1, requestId=str(uuid.uuid4()), operation=operation,
                componentId=component, command=command, controlToken=token)


class FirmwareTests(unittest.TestCase):
    def setUp(self):
        self.clock = FakeClock()
        self.hardware = SimulatedHardware(self.clock)
        self.dispatcher = Dispatcher(self.hardware, self.clock, watchdog_ms=60000)

    def send(self, operation="GET_STATE", component="front-lights", command=None, token=None):
        r = request(operation, component, command, self.dispatcher.token if token is None else token)
        response = self.dispatcher.handle(json.dumps(r))
        self.assertEqual(response["requestId"], r["requestId"])
        return response

    def execute(self, component, action, **parameters):
        result = self.send("EXECUTE", component, dict(action=action, **parameters))
        self.assertTrue(result["success"], result)
        return result["states"][0]

    def test_get_all_states_and_envelope(self):
        for component in self.dispatcher.by_id:
            result = self.send(component=component)
            self.assertTrue(result["success"])
            self.assertEqual(result["version"], 1)
            self.assertEqual(result["states"][0]["id"], component)
            self.assertTrue(result["states"][0]["online"])
            self.assertEqual(result["executedAt"], self.clock.utc())
            self.assertEqual(len(result["controlToken"]), 32)

    def test_lights(self):
        for component in ("front-lights", "side-lights"):
            for action in ("on", "off"):
                self.assertEqual(self.execute(component, action)["state"], action)
                self.assertEqual(self.hardware.outputs[component]["state"], action)

    def test_motor_directions_limits_defaults_stop(self):
        for direction in ("forward", "reverse"):
            for speed in (0, 40, 100):
                state = self.execute("main-motor", "start", direction=direction, speed=speed)
                self.assertEqual((state["state"], state["direction"], state["speed"]), ("running", direction, speed))
        self.assertEqual(self.execute("main-motor", "start")["speed"], 50)
        state = self.execute("main-motor", "stop")
        self.assertEqual((state["state"], state["speed"]), ("stopped", 0))

    def test_servo_positions_and_stop_preserves(self):
        for position in (0, 90, 120.5, 180):
            self.assertEqual(self.execute("servo-1", "SET_POSITION", position=position)["position"], position)
        self.send("STOP_ALL", None)
        self.assertEqual(self.send(component="servo-1")["states"][0]["position"], 180)

    def test_hydraulic_progress_limits_and_stop(self):
        self.execute("hydraulic-1", "EXTEND")
        self.clock.ms += 1000
        state = self.send(component="hydraulic-1")["states"][0]
        self.assertEqual(state["position"], 20)
        state = self.execute("hydraulic-1", "STOP")
        self.clock.ms += 1000
        self.assertEqual(self.send(component="hydraulic-1")["states"][0]["position"], 20)
        self.execute("hydraulic-1", "EXTEND")
        self.clock.ms += 6000
        state = self.send(component="hydraulic-1")["states"][0]
        self.assertEqual((state["position"], state["movement"], state["limitExtended"]), (100, "STOPPED", True))
        self.execute("hydraulic-1", "RETRACT")
        self.clock.ms += 6000
        state = self.send(component="hydraulic-1")["states"][0]
        self.assertEqual((state["position"], state["movement"], state["limitRetracted"]), (0, "STOPPED", True))

    def test_bank_sweeps_pingpong_blink_and_speed(self):
        for effect, expected in (("SWEEP_RIGHT", [0, 1, 2]), ("SWEEP_LEFT", [7, 6, 5]), ("PING_PONG", [0, 1, 2])):
            self.execute("main-light-bank", effect)
            for active in expected:
                channels = self.send(component="main-light-bank")["states"][0]["channels"]
                self.assertEqual(channels, [i == active for i in range(8)])
                self.clock.ms += 750
        self.execute("main-light-bank", "PING_PONG")
        self.clock.ms += 8 * 750
        self.assertEqual(self.send(component="main-light-bank")["states"][0]["channels"].index(True), 6)
        self.execute("main-light-bank", "SET_SPEED", speed=100)
        self.execute("main-light-bank", "BLINK")
        self.assertEqual(self.send(component="main-light-bank")["states"][0]["channels"], [True] * 8)
        self.clock.ms += 300
        self.assertEqual(self.send(component="main-light-bank")["states"][0]["channels"], [False] * 8)

    def test_bank_stop_effect_and_all(self):
        self.execute("main-light-bank", "SWEEP_RIGHT")
        self.clock.ms += 750
        held = self.execute("main-light-bank", "STOP_EFFECT")
        self.clock.ms += 4000
        self.assertEqual(self.send(component="main-light-bank")["states"][0], held)
        for action, on in (("ALL_ON", True), ("ALL_OFF", False)):
            state = self.execute("main-light-bank", action)
            self.assertEqual(state["channels"], [on] * 8)
            self.assertEqual(state["effect"], "NONE")

    def test_validation_no_mutation(self):
        cases = [("main-motor", {"action": "start", "speed": v}) for v in (-1, 101, True, "50", 1.5)]
        cases += [("servo-1", {"action": "SET_POSITION", "position": v}) for v in (-1, 181, None, True, float("nan"), float("inf"))]
        cases += [("main-light-bank", {"action": "SET_SPEED", "speed": v}) for v in (0, 101, None, True)]
        cases += [("front-lights", {"action": "on", "speed": 1}), ("hydraulic-1", {"action": "STOP", "position": 10}),
                  ("main-motor", {"action": "start", "direction": "sideways"}), ("servo-1", {"action": "wrong"})]
        for component, command in cases:
            with self.subTest(component=component, command=command):
                before = self.send(component=component)["states"]
                result = self.send("EXECUTE", component, command)
                self.assertFalse(result["success"])
                self.assertIn(result["error"]["code"], ("INVALID_PARAMETER", "INVALID_COMMAND"))
                self.assertEqual(self.send(component=component)["states"], before)

    def test_invalid_envelopes(self):
        for body in ("{", "null", "[]", "{}", '"string"'):
            self.assertFalse(self.dispatcher.handle(body)["success"])
        for key, value in (("version", 2), ("version", True), ("requestId", "bad"), ("requestId", "00000000-0000-0000-0000-000000000000"),
                           ("operation", "OTHER"), ("componentId", "missing"), ("command", {"action": "on"})):
            r = request()
            r[key] = value
            self.assertFalse(self.dispatcher.handle(json.dumps(r))["success"])
        for key in request():
            if key == "controlToken":
                continue
            r = request()
            del r[key]
            self.assertFalse(self.dispatcher.handle(json.dumps(r))["success"])

    def activate(self):
        self.execute("front-lights", "on")
        self.execute("side-lights", "on")
        self.execute("main-motor", "start", speed=80)
        self.execute("hydraulic-1", "EXTEND")
        self.execute("main-light-bank", "BLINK")
        self.execute("servo-1", "SET_POSITION", position=120)

    def assert_safe(self, states):
        self.assertTrue(all(s["state"] in ("off", "stopped") for s in states))
        by_id = {s["id"]: s for s in states}
        self.assertEqual(by_id["main-motor"]["speed"], 0)
        self.assertEqual(by_id["main-light-bank"]["channels"], [False] * 8)
        self.assertEqual(by_id["hydraulic-1"]["movement"], "STOPPED")
        self.assertEqual(by_id["servo-1"]["position"], 120)

    def test_stop_snapshot_and_delayed_request(self):
        self.activate()
        self.clock.ms += 1000
        old = self.dispatcher.token
        result = self.send("STOP_ALL", None)
        self.assertTrue(result["success"])
        self.assert_safe(result["states"])
        self.assertEqual(result["states"][4]["position"], 20)
        self.assertNotEqual(old, result["controlToken"])
        for action in ("start", "start"):
            result = self.send("EXECUTE", "main-motor", {"action": action}, old)
            self.assertEqual(result["error"]["code"], "TIMEOUT")
        self.execute("main-motor", "start")  # A genuinely new intention is allowed.

    def test_watchdog_rotates_token_no_auto_restart(self):
        self.dispatcher.watchdog_ms = 500
        self.activate()
        old = self.dispatcher.token
        self.clock.ms += 500
        self.dispatcher.tick()
        self.assert_safe([c.snapshot() for c in self.dispatcher.components])
        self.assertNotEqual(old, self.dispatcher.token)
        self.assertFalse(self.send("EXECUTE", "main-motor", {"action": "start"}, old)["success"])
        self.send()  # Communication returns, but outputs stay stopped.
        self.clock.ms += 100
        self.dispatcher.tick()
        self.assert_safe([c.snapshot() for c in self.dispatcher.components])

    def test_invalid_traffic_does_not_feed_watchdog(self):
        self.dispatcher.watchdog_ms = 500
        self.activate()
        self.clock.ms += 400
        self.dispatcher.handle("{")
        self.clock.ms += 100
        self.dispatcher.tick()
        self.assert_safe([c.snapshot() for c in self.dispatcher.components])

    def test_restart_invalidates_previous_boot(self):
        old = self.dispatcher.token
        restarted = Dispatcher(self.hardware, self.clock)
        r = request("EXECUTE", "front-lights", {"action": "on"}, old)
        self.assertEqual(restarted.handle(json.dumps(r))["error"]["code"], "TIMEOUT")

    def test_snapshot_does_not_expose_mutable_state(self):
        state = self.send(component="main-light-bank")["states"][0]
        state["channels"][0] = True
        self.assertFalse(self.send(component="main-light-bank")["states"][0]["channels"][0])

    def test_output_failure_no_false_success_and_best_effort_stop(self):
        self.activate()
        self.hardware.fail_id = "front-lights"
        result = self.send("STOP_ALL", None)
        self.assertFalse(result["success"])
        self.assertEqual(self.hardware.outputs["main-motor"]["state"], "stopped")
        self.assertEqual(self.hardware.outputs["main-light-bank"]["channels"], [False] * 8)
        self.assertFalse(self.send("EXECUTE", "main-motor", {"action": "start"})["success"])
        self.hardware.fail_id = None
        self.assertTrue(self.send("STOP_ALL", None)["success"])


class HttpTests(unittest.IsolatedAsyncioTestCase):
    async def asyncSetUp(self):
        self.clock = FakeClock()
        self.dispatcher = Dispatcher(SimulatedHardware(self.clock), self.clock, watchdog_ms=500)
        self.adapter = HttpServer(self.dispatcher, io_timeout=0.15)
        self.server = await asyncio.start_server(self.adapter.client, "127.0.0.1", 0)
        self.port = self.server.sockets[0].getsockname()[1]

    async def asyncTearDown(self):
        self.server.close()
        await self.server.wait_closed()

    async def exchange(self, r):
        reader, writer = await asyncio.open_connection("127.0.0.1", self.port)
        body = json.dumps(r).encode()
        writer.write(b"POST /v1/exchange HTTP/1.1\r\nContent-Type: application/json\r\nContent-Length: " + str(len(body)).encode() + b"\r\n\r\n" + body)
        await writer.drain()
        reply = await reader.read()
        writer.close()
        await writer.wait_closed()
        return json.loads(reply.split(b"\r\n\r\n", 1)[1])

    async def test_http_round_trip(self):
        get = await self.exchange(request())
        execute = await self.exchange(request("EXECUTE", "front-lights", {"action": "on"}, get["controlToken"]))
        self.assertTrue(execute["success"])
        self.assertEqual(execute["states"][0]["state"], "on")

    async def test_delayed_body_cannot_cross_stop(self):
        token = self.dispatcher.token
        reader, writer = await asyncio.open_connection("127.0.0.1", self.port)
        body = json.dumps(request("EXECUTE", "main-motor", {"action": "start"}, token)).encode()
        writer.write(b"POST /v1/exchange HTTP/1.1\r\nContent-Type: application/json\r\nContent-Length: " + str(len(body)).encode() + b"\r\n\r\n" + body[:10])
        await writer.drain()
        stopped = await self.exchange(request("STOP_ALL", None))
        self.assertTrue(stopped["success"])
        writer.write(body[10:])
        await writer.drain()
        result = json.loads((await reader.read()).split(b"\r\n\r\n", 1)[1])
        self.assertEqual(result["error"]["code"], "TIMEOUT")
        writer.close()
        await writer.wait_closed()
        self.assertEqual(self.dispatcher.by_id["main-motor"].state["state"], "stopped")

    async def test_slow_http_does_not_block_watchdog(self):
        await self.exchange(request("EXECUTE", "main-motor", {"action": "start"}, self.dispatcher.token))
        reader, writer = await asyncio.open_connection("127.0.0.1", self.port)
        writer.write(b"POST /v1/exchange HTTP/1.1\r\n")
        await writer.drain()
        self.clock.ms += 500
        task = asyncio.create_task(self.adapter.maintenance())
        await asyncio.sleep(0.03)
        self.assertEqual(self.dispatcher.by_id["main-motor"].state["state"], "stopped")
        self.assertIn(b"408 Request Timeout", await reader.read())
        task.cancel()
        with self.assertRaises(asyncio.CancelledError):
            await task
        writer.close()
        await writer.wait_closed()


if __name__ == "__main__":
    unittest.main()
