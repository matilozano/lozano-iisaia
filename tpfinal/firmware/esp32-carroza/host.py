"""Host the exact firmware dispatcher/server with simulated outputs, without a board."""
import argparse
import asyncio
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent / "src"))
from clock import Clock
from dispatcher import Dispatcher
from hardware import SimulatedHardware
from server import HttpServer

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--port", type=int, default=5090)
    parser.add_argument("--watchdog-ms", type=int, default=5000)
    args = parser.parse_args()
    clock = Clock()
    asyncio.run(HttpServer(Dispatcher(SimulatedHardware(clock), clock, args.watchdog_ms)).run("127.0.0.1", args.port))
