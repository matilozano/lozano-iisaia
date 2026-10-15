# Iteración 8 — Firmware ESP32 y continuidad

Verificación de cierre: 2026-10-10. Rama `tpfinal`. Sin commits ni push.
Alcance confirmado en PLAN: firmware independiente y frontera HTTP verificable
sin placa. No se inició Iteración 9.

## Estado encontrado y trabajo conservado

Había cambios no commiteados en .gitignore, tres archivos Infrastructure del
backend y Esp32Tests, además de todo firmware/ sin trackear. El núcleo, HAL,
servidor, compilador, host y tests estaban implementados. No se encontró código
truncado ni fue necesario reemplazar esos archivos. Faltaban README de firmware,
actualización de documentación del protocolo/arquitectura y este informe.
Los resultados de la ejecución interrumpida no se tomaron como aprobación.

En esta recuperación se completó la documentación, se revisaron los cambios y
se repitieron builds, suites y regresiones de ambos gateways. No se modificó
el comportamiento funcional que ya estaba implementado.

## Arquitectura y estado

Backend Application → IComponentGateway → Esp32ComponentGateway → HTTP →
server → protocol → Dispatcher → ComponentController → HAL.
El núcleo MicroPython también corre en CPython, con HAL simulada para integración.
La HAL ESP32 encapsula Pin/PWM/ADC; no hay dependencias de hardware en Domain,
Application, Controllers o frontend. SHOW_FNE sigue ejecutándose en backend.
No se agregó ningún endpoint público ni se modificaron DTOs `/api/devices`.
El controlador implementa POST `/v1/exchange`: GET_STATE, EXECUTE y STOP_ALL.

Las salidas se aplican antes de confirmar snapshots. El hidráulico simulado
progresa; el real usa ADC calibrado. Motor/servo confirman consignas, no medición
mecánica. Mutaciones síncronas en un event loop evitan estados intercalados;
la lectura HTTP es asíncrona y acotada. UTC se exige antes de actuar; movimientos
y watchdog usan tiempo monotónico. Configuración Wi-Fi/pines está separada e ignorada
por Git. No se definió cableado ni electrónica de potencia.

## STOP_ALL, watchdog y mensajes demorados

STOP_ALL renueva controlToken antes de apagar luces/banco y detener motor,
hidráulico y efectos. Servo conserva ángulo. Ante fallo se intentan las demás
paradas y se devuelve error; no se inventa éxito parcial. El token aleatorio se
renueva también al arrancar y vencer watchdog. EXECUTE con generación anterior
devuelve TIMEOUT sin actuar. El backend consulta token antes de nuevas intenciones,
sin reintentar automáticamente órdenes rechazadas. Clientes antiguos sin token
pueden consultar/parar; deben actualizarse para ejecutar contra este firmware.
El gateway mantiene compatibilidad con controladores anteriores sin token, sin
atribuirles garantía de barrera. No es autenticación ni deduplicación general.

Watchdog de comunicación: 5000 ms, configurable 500..60000; consultas válidas
también lo alimentan, tráfico inválido no. Revisión cooperativa cada 20 ms.
No hay reanudación automática ni protección independiente ante bloqueo de CPU.
Pruebas incluyen un cuerpo HTTP iniciado antes de STOP y completado después,
reinicio, tráfico inválido y cliente lento sin impedir watchdog.

## Archivos

Preexistentes modificados por la implementación recuperada:

- `.gitignore`: excluye bytecode, herramientas y configuración local.
- `backend/Carroza.Api/Infrastructure/Esp32/Esp32Protocol.cs`: token opcional.
- `backend/Carroza.Api/Infrastructure/Esp32/HttpEsp32Transport.cs`: Content-Length.
- `backend/Carroza.Api/Infrastructure/Gateways/Esp32ComponentGateway.cs`: consulta previa y validación de token.
- `backend/Carroza.Simulator.Tests/Esp32Tests.cs`: compatibilidad, token y transporte.

Archivos nuevos de implementación, ya presentes al retomar, bajo `firmware/esp32-carroza/`:

- `build.py`, `requirements-build.txt`, `host.py`, `integration-test.ps1`.
- `src/clock.py`, `protocol.py`, `hardware.py`, `components.py`, `dispatcher.py`.
- `src/server.py`, `esp32_hardware.py`, `config_example.py`, `main.py`.
- `tests/test_firmware.py`, `tests/test_gpio_adapter.py`.

Creados ahora: `firmware/esp32-carroza/README.md` y `docs/ITERACION-8.md`.
Modificados ahora: `docs/ARCHITECTURE.md`, `docs/COMPONENTS.md`,
`docs/DESARROLLO.md`, `docs/ESP32-PROTOCOL.md`.
PLAN se conserva porque ya define el alcance; informes 1–7 no se alteraron.

## Verificaciones de esta recuperación

Python utilizado: runtime disponible de Codex, Python 3.12; los comandos `python`
del README se ejecutaron con su ruta absoluta. mpy-cross 1.27.0.post2 ya estaba
instalado localmente en .tools. No se instaló ni probó una imagen en placa.

| Comando / comprobación | Resultado |
| --- | --- |
| `dotnet build backend/Carroza.Api` | APROBADO, 0 errores y 0 advertencias |
| `npm --prefix frontend run build` | APROBADO, 40 módulos |
| `dotnet run --project backend/Carroza.Simulator.Tests` | APROBADO: 34 + 75 + 52 + 82 + 116 comprobaciones; 60 operaciones concurrentes |
| `npm --prefix frontend run test` | APROBADO, 11 pruebas |
| `python firmware/esp32-carroza/build.py` | APROBADO, 9 módulos .mpy; no equivale a prueba del runtime en placa |
| `python -m unittest discover -s firmware/esp32-carroza/tests -v` | APROBADO, 23 tests de protocolo, componentes, HAL, parada, watchdog, HTTP y GPIO mediante doble |
| Scripts smoke, contract, iteration-3, iteration-4, fault, sequence y frontend-proxy en Simulator | APROBADOS, ejecutados secuencialmente |
| `firmware/esp32-carroza/integration-test.ps1`, API ESP32 → host.py/HAL simulada | APROBADO: smoke, contrato, banco, hidráulico/servo y secuencias |
| Proxy en modo ESP32 | APROBADO: cinco consultas por API directa y por Vite, todas JSON |
| Búsquedas arquitectónicas de DESARROLLO | APROBADO: las tres sin coincidencias, rg=1 esperado |
| Diff y enlaces locales de los seis documentos creados/modificados | APROBADO: git diff --check sin errores; enlaces resueltos |
| Swagger UI/assets/OpenAPI y operaciones HTTP | APROBADO por scripts iteration-3/4 y sequence en Simulator; iteration-3/4 también aprobados en ESP32 |

## Problemas de entorno y revisión visual

SHOW_FNE completó sus 14 pasos en ambos modos. Los scripts verificaron orden y
tiempos, conflictos 409 ante doble inicio/comandos manuales, historial, cancelación
y STOP ALL después de activar hidráulico. Observaron otros 13 segundos sin
reactivación: estado CANCELLED y componentes detenidos/apagados, servo conservado.
Fallas reproducibles pasaron en la suite .NET y fault-test; fallas de HAL y ausencia
de falso éxito pasaron en los tests del firmware. No se confundieron con fallas físicas.

El primer wrapper de scripts interpretó LASTEXITCODE nulo como fallo después de
un smoke exitoso. Se corrigió la invocación inicializando el código a cero antes
de cada script y se repitió la batería; no era un fallo del producto.
El primer intento del navegador a 127.0.0.1:5173 devolvió CONNECTION_REFUSED:
el listener existente era solo IPv6. Se inició Vite con --host 127.0.0.1
--strictPort, conservando puertos. Las consultas del proxy retornaron JSON;
no se reprodujo HTML en `/api`.

Revisión visual parcial ejecutada: navegador abrió el panel en 5173 y se observó
Twin/HUD durante SHOW_FNE vía firmware: ONLINE, RUNNING, motor forward 40%, banco
SWEEP_LEFT y estados de hidráulico/servo coherentes con el panel. Se inspeccionó
una captura del Twin. No se operó el panel durante los scripts, para no interferir.
No se aprueba por ello el checklist visual completo: responsive, reducción de
movimiento, errores y todos los pasos manuales quedan PENDIENTES de revisión visual.
Frontend no fue modificado. Sus etiquetas SIMULATOR/SIMULADO siguen siendo estáticas.
Al terminar se detuvieron el host firmware y la API ESP32 de prueba y se reinició
la API en Simulator, conservando 5080 y Vite en 5173.

## Pendientes y límites reales

- Prueba física ESP32, Wi-Fi/NTP real, ADC/PWM, calibración y seguridad eléctrica:
  NO EJECUTADAS; integración física opcional en PLAN, no aprobada por tests de HAL.
- Revisión visual completa: parcial, pendiente en los casos indicados.
- No hay autenticación/TLS en firmware, watchdog físico independiente ni electrónica
  definitiva. No se presenta este controlador de demostración como sistema de seguridad.

Referencias de uso: [firmware](../firmware/esp32-carroza/README.md),
[protocolo](ESP32-PROTOCOL.md), [desarrollo](DESARROLLO.md).
