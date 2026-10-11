# Controlador ESP32 — protocolo v1

Firmware MicroPython independiente del backend. `src/server.py` recibe HTTP;
`protocol.py` valida mensajes; `dispatcher.py` serializa operaciones; los
controladores de `components.py` usan la HAL de `hardware.py` o `esp32_hardware.py`.
SHOW_FNE permanece en Application del backend.

## Compilar y probar sin placa

Desde la raíz, con Python 3.12 y pip:

```powershell
python -m pip install --target firmware/esp32-carroza/.tools -r firmware/esp32-carroza/requirements-build.txt
python firmware/esp32-carroza/build.py
python -m unittest discover -s firmware/esp32-carroza/tests -v
```

El build genera nueve módulos `.mpy` con mpy-cross 1.27.0.post2. Es compilación
de bytecode MicroPython, no compilación de una imagen del runtime ESP32.
Los tests ejecutan el mismo núcleo en CPython y prueban el adaptador GPIO con
un doble de `machine`; no demuestran funcionamiento eléctrico.

## Integración HTTP local

En terminales separadas, desde la raíz:

```powershell
python firmware/esp32-carroza/host.py --port 5090 --watchdog-ms 5000
dotnet run --project backend/Carroza.Api -- --ComponentGateway:Mode=ESP32 --ComponentGateway:ESP32:Endpoint=http://127.0.0.1:5090/v1/exchange
npm --prefix frontend run dev -- --host 127.0.0.1 --strictPort
```

Luego ejecutar, sin operar simultáneamente el panel:

```powershell
./firmware/esp32-carroza/integration-test.ps1
./backend/frontend-proxy-test.ps1
```

5090 es el controlador de prueba; API y frontend conservan 5080/5173.
Detener esa API y reiniciarla sin argumentos ESP32 para volver al simulador.
Las etiquetas SIMULATOR/SIMULADO del panel siguen siendo estáticas y no identifican
el modo del gateway. Consultar [protocolo](../../docs/ESP32-PROTOCOL.md).

## Configuración y despliegue en placa (no verificado físicamente)

Usar el runtime MicroPython 1.27 compatible con la placa, siguiendo las
[instrucciones oficiales de ESP32](https://www.micropython.org/download/ESP32_GENERIC/).
Copiar `src/config_example.py` como `config_local.py`, completar Wi-Fi localmente
y transferirlo a la placa junto con los módulos de `src/`. No versionar secretos.
Para arranque automático transferir **main.py como fuente**; los otros módulos
pueden transferirse como `.py` o `.mpy` compatibles con el runtime instalado.

Por defecto `SIMULATED_HARDWARE=True`. Para salidas reales se requiere cambiarlo
a false y completar PIN_MAP para los seis IDs, sin pines duplicados. El ejemplo
describe las claves: luces/banco digitales, motor dirección/PWM, hidráulico dos
salidas excluyentes y feedback ADC calibrado, servo PWM con pulsos calibrados.
No existe asignación definitiva de GPIO ni diseño de potencia en esta entrega.
No conectar cargas de potencia directamente a GPIO.

El arranque inicializa salidas seguras, conecta Wi-Fi y sincroniza UTC mediante
NTP antes de aceptar comandos. Un fallo deja salidas detenidas. `HTTP_PORT=80`,
`WATCHDOG_MS=5000` (500..60000), `NETWORK_TIMEOUT_MS=15000` son configurables.

## Parada y límites

STOP_ALL invalida comandos de la generación anterior, detiene motor/hidráulico
y efectos, apaga iluminación y conserva el ángulo confirmado del servo. Ante
fallo de una salida intenta detener las demás y devuelve error, nunca éxito parcial.
El watchdog de comunicación ejecuta esa parada si no hay operaciones válidas;
GET_STATE también mantiene viva la comunicación. No es un watchdog físico contra
bloqueos del procesador. No hay reanudación automática.

El token protege contra solicitudes anteriores a una parada, no autentica clientes.
HTTP sin TLS/autenticación requiere un entorno de prueba controlado. La posición
hidráulica real usa ADC; motor y servo confirman consignas aplicadas, no feedback
mecánico. Placa, alimentación, red real, calibración y seguridad eléctrica siguen
sin validación física.
# Telemetría y protecciones — Iteración 9

El protocolo v1 agrega `GET_TELEMETRY` y `SIMULATION_CONTROL`, ambos con
`componentId: null` y `command: null`. Devuelven el sobre habitual, `states: []`
y `telemetry`. No cambia GET_STATE, EXECUTE ni STOP_ALL.

Solo `SimulatedHardware` permite `simulation: {action: "SET_FAULT", code:
"OVER_TEMPERATURE", active: true}`. Códigos: OVER_TEMPERATURE, OVER_CURRENT,
VOLTAGE_RANGE, COMMUNICATION_LOSS, INVALID_SENSOR y STALE_SENSOR. Desactivar usa
`active: false`; ACKNOWLEDGE admite código individual u omisión para reconocer
todas las alertas. RESET requiere todas resueltas y reconocidas, además de parada
confirmada si se activó protección. Conserva historial de hasta seis causas.

Cada falla enclava la protección y aplica parada global inmediatamente dentro del
dispatcher, sin esperar consultas. Rechaza EXECUTE mientras está enclavada; STOP_ALL
continúa disponible. Protección, STOP_ALL y RESET invalidan tokens previos.
Reconocer o resolver no libera el enclavamiento; RESET no reanuda movimientos.
Una salida que falla impide `stopConfirmed`; las demás igualmente se detienen.

Con velocidad confirmada `s` en 0..100, las medidas simuladas son `24−s×0.01 V`,
`1+s×0.08 A`, `25+s×0.3 °C`. Las fallas fuerzan 16 V, 15 A o 85 °C, fuera
de los límites operativos 20..28 V, 12 A y 70 °C. OFFLINE e INVALID entregan
mediciones null. STALE impone antigüedad inicial 5000 ms que crece con reloj
monotónico (umbral 2000 ms); conserva el timestamp previo, o null si aún no hubo
lectura. Es una antigüedad inyectada, no un fallo real de transporte.

HAL física devuelve source/quality UNAVAILABLE, mediciones y timestamp null y
`canInjectFaults: false`; no se simulan sensores reales ni se habilita inyección.
Las garantías verificadas corresponden a HAL simulada, no a seguridad física.
