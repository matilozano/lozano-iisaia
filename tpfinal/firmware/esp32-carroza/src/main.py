import asyncio
import network
import ntptime
import time
from clock import Clock
from dispatcher import Dispatcher
from hardware import SimulatedHardware
from esp32_hardware import Esp32Hardware
from server import HttpServer
import config_local as config


async def main():
    clock = Clock()
    hardware = SimulatedHardware(clock) if config.SIMULATED_HARDWARE else Esp32Hardware(config.PIN_MAP)
    dispatcher = Dispatcher(hardware, clock, config.WATCHDOG_MS)
    wlan = network.WLAN(network.STA_IF)
    wlan.active(True)
    wlan.connect(config.SSID, config.PASSWORD)
    started = clock.now()
    while not wlan.isconnected():
        if clock.elapsed(clock.now(), started) >= config.NETWORK_TIMEOUT_MS:
            dispatcher.stop()
            raise OSError("Wi-Fi no disponible")
        await asyncio.sleep(0.1)
    # Before accepting commands, UTC must be usable. Failure leaves safe outputs.
    try:
        ntptime.settime()
        clock.utc()
    except Exception:
        dispatcher.stop()
        raise
    print("ESP32 IP", wlan.ifconfig()[0], "HAL simulated:", config.SIMULATED_HARDWARE)
    await HttpServer(dispatcher).run("0.0.0.0", config.HTTP_PORT)


asyncio.run(main())
