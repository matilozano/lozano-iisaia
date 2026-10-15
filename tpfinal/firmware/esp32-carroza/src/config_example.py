# Copy to config_local.py (ignored by Git). No real credentials in this file.
SSID = "CHANGE_ME"
PASSWORD = "CHANGE_ME"
HTTP_PORT = 80
WATCHDOG_MS = 5000
NETWORK_TIMEOUT_MS = 15000
SIMULATED_HARDWARE = True
# Explicit deployment mapping, never inferred by dispatcher. No default GPIO.
# Digital lights/bank: {"pins": [GPIO, ...]}
# Motor: {"direction": GPIO, "enable": GPIO}
# Hydraulic: {"extend": GPIO, "retract": GPIO, "feedback": GPIO,
#             "adc_min": 0, "adc_max": 65535}
# Servo: {"pin": GPIO, "min_us": 500, "max_us": 2500}
PIN_MAP = {}
