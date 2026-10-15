# Iteración 9 — Dashboard, telemetría y protecciones

Estado al retomar el 2026-10-11: implementación integrada; cierre automatizado
repetido en esta ejecución. Revisión visual posterior realizada en escritorio/móvil;
pendiente activación manual de movimiento reducido y caída real del transporte.
Rama `tpfinal`. Sin commit ni push. No se avanza a Iteración 10.

## Estado inicial y alcance

La Iteración 9 está definida en PLAN. Al comenzar solo había una modificación
previa de `docs/PLAN.md` que incorporaba ese alcance; se conservó íntegra. Las
iteraciones anteriores estaban implementadas. Se priorizó la reorganización del
dashboard, la telemetría simulada y la protección automática sin hardware físico.

Al retomar estaban creados los modelos, servicios, monitor, contratos, gateway,
firmware, dashboard y sus pruebas. El informe estaba parcial: no reflejaba toda
la comprobación previa y no constituía evidencia de cierre de esta ejecución.
Se inspeccionaron cambios trackeados/no trackeados y código de las tres áreas;
no se reemplazó la implementación. Se completó la cobertura del proxy agregando
`/api/telemetry` al script existente, se repitieron verificaciones y se actualizó
este informe. La modificación previa de PLAN que define Iteración 9 se conserva.

## Diseño e implementación

- Dashboard con Twin destacado, telemetría adyacente, navegación a controles y
  secuencias, controles agrupados y panel de alertas. STOP ALL ocupa una fila
  persistente independiente del área desplazable; no se superpone a los controles.
- `useDevices` sigue siendo la única fuente de polling. Una consulta de telemetría
  fallida no descarta respuestas válidas de componentes/secuencia; se conservan
  lecturas anteriores con advertencia. No se generan voltios/amperios/temperaturas
  en React. El gateway mostrado proviene de la API.
- Controllers → TelemetryService → IComponentGateway. El contrato se amplía con
  lecturas y control de simulación, sin modificar `/api/devices`. Domain contiene
  los registros; DTOs HTTP permanecen separados. Program solo registra servicios.
- Simulator produce telemetría bajo su bloqueo de estado. Firmware usa el mismo
  modelo determinista junto al dispatcher/HAL. HAL física informa UNAVAILABLE,
  valores null y no permite inyección. SHOW_FNE permanece en Application.
- TelemetryMonitor supervisa cada 250 ms aunque el navegador esté cerrado.
  Application cancela la secuencia y bloquea admisiones mediante OperationCoordinator.
  El gateway/firmware también rechaza comandos con protección enclavada.

## Contratos y política operativa

GET `/api/telemetry` devuelve gateway, source, online, sampledAt UTC, ageMs,
quality, voltage (V), current (A), temperature (°C), motorSpeed (%),
protectionLatched, stopConfirmed, canInjectFaults y alerts. Calidad:
VALID/STALE/INVALID/OFFLINE/UNAVAILABLE. Null significa sin medición, nunca cero.
Estado de API y de controlador son distintos. Una lectura anterior no confirma
condiciones actuales.

POST `/api/telemetry/simulation` acepta:

```json
{"action":"SET_FAULT","code":"OVER_TEMPERATURE","active":true}
```

`active:false` retira la causa. ACKNOWLEDGE admite code opcional (sin code reconoce
todas); RESET no admite parámetros. Código desconocido/parámetros inválidos: 400.
El acceso está limitado por capacidades del gateway/HAL simulada, no por un botón
del navegador. No se implementó autenticación, fuera de alcance.

Modelo normal, para velocidad confirmada s (0..100): V=24−0.01s, A=1+0.08s,
°C=25+0.3s; velocidad=s. No intenta modelar física calibrada. Rango normal:
23..24 V, 1..9 A y 25..55 °C. Cada consulta produce muestra del estado actual.
Fallas discretas y reproducibles:

| Código | Lectura / condición | Protección |
| --- | --- | --- |
| OVER_TEMPERATURE | 85 °C | Crítica |
| OVER_CURRENT | 15 A | Crítica |
| VOLTAGE_RANGE | 16 V | Crítica |
| COMMUNICATION_LOSS | online=false, OFFLINE, valores null | Crítica |
| INVALID_SENSOR | INVALID, valores null | Crítica |
| STALE_SENSOR | STALE, ageMs ≥5000 | Crítica |

Las seis condiciones activan parada global y enclavamiento. No se hace clasificación
numérica en frontend. Activación/resolución tienen timestamp; active y acknowledged
son independientes. Retirar o reconocer no permite reactivar. RESET exige todas
resueltas/reconocidas y parada confirmada. Restablecer nunca reanuda movimientos
ni SHOW_FNE. STOP ALL continúa disponible y no elimina la protección.

Una pérdida real del transporte también enclava la coordinación backend: conserva
la última lectura, marca OFFLINE, cancela secuencia e intenta parar sin inventar
confirmación. Al recuperar el enlace requiere reconocimiento y reset. Esta alerta
pertenece a Application y no depende de que exista en el catálogo remoto.

El protocolo v1 agrega GET_TELEMETRY/SIMULATION_CONTROL y telemetry/simulation
opcionales. Se preservan los mensajes anteriores y controlToken. Una parada crítica
rota token; RESET también lo rota en firmware. El gateway acepta un rechazo explícito
INVALID_COMMAND de GET_TELEMETRY como firmware anterior sin telemetría, UNAVAILABLE;
no inventa mediciones. Mensajes inválidos siguen rechazándose.

## Pruebas y resultados

Resultados repetidos el 2026-10-11, contra el código actual:

| Comando / comprobación | Resultado real |
| --- | --- |
| `dotnet build backend/Carroza.Api` | APROBADA; cero errores y advertencias |
| `dotnet run --no-restore --project backend/Carroza.Simulator.Tests` | APROBADA; 422 comprobaciones (34+75+52+82+63+116), además 60 operaciones concurrentes |
| `npm.cmd --prefix frontend run build` | APROBADA; TypeScript y Vite, 44 módulos |
| `npm.cmd --prefix frontend run test` | APROBADA; 17 tests, cero fallos |
| Python unittest discover de firmware | APROBADA; 32 tests |
| Python `firmware/esp32-carroza/build.py` | APROBADA; compilación bytecode MicroPython |
| Simulator: smoke-test / contract-test / iteration-3-test / iteration-4-test | APROBADAS; componentes, límites, errores, banco, posiciones y compatibilidad HTTP |
| Simulator: sequence-test | APROBADA; SHOW_FNE completo, orden/tiempos, exclusión, cancelación y STOP ALL |
| Simulator: fault-test | APROBADA; cinco errores HTTP, sin mutación y operación independiente |
| Simulator: telemetry-test | APROBADA; seis condiciones críticas, bloqueo, reconocimiento/reset y ausencia de reactivación |
| Simulator: frontend-proxy-test | APROBADA; seis consultas JSON directas y vía 5173, incluida telemetría |
| ESP32/HAL simulada: `firmware/esp32-carroza/integration-test.ps1` | APROBADA; smoke, contrato, banco, posiciones, SHOW_FNE completo y STOP ALL por HTTP real al firmware |
| ESP32/HAL simulada: telemetry-test / frontend-proxy-test | APROBADAS; las seis protecciones, recuperación explícita, ausencia de reactivación y doce respuestas JSON directas/proxy |
| Búsquedas arquitectónicas de DESARROLLO | APROBADAS; las tres búsquedas no encontraron coincidencias (exit 1 esperado) |
| Swagger/OpenAPI | APROBADA por HTTP: UI, assets, endpoints anteriores y nuevos, DTO de telemetría y comandos ejercitados por los scripts |
| Enlaces Markdown locales de README, AGENTS y docs | APROBADA; ningún destino inexistente |
| `git diff --check` | APROBADA; sin errores de whitespace (avisos LF/CRLF del entorno) |
| Revisión visual en esta ejecución | PENDIENTE; acceso rechazado por política del navegador |

Los scripts mutantes se ejecutaron secuencialmente. Se usó `-BaseUrl
http://127.0.0.1:5080` en los que lo admiten: el valor histórico localhost produjo
demoras de conexión en este entorno; no se cambiaron puertos ni contratos.
Un primer wrapper interpretó un LASTEXITCODE heredado de rg como fallo de smoke,
aunque smoke informó éxito. Se reinició el código antes de cada script y se
repitió la regresión completa; no se atribuyó ese fallo del wrapper al producto.
`python` no está en PATH: unittest y build se ejecutaron con el Python del runtime
de Codex (`C:/Users/Matias/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe`).

En el segundo modo se usó `host.py --port 5090` y API en 5080 con
`--ComponentGateway:Mode=ESP32 --ComponentGateway:ESP32:Endpoint=http://127.0.0.1:5090/v1/exchange`.
Frontend permaneció en 5173. La respuesta confirmó gateway=ESP32 y source=SIMULATED.
Al finalizar se restauró la API a Simulator y se confirmó el estado detenido.

SHOW_FNE completó los 14 pasos y confirmó retracción hidráulica hasta 0 y servo
120°. El historial incluyó 14 eventos OK en orden y con los delays definidos.
En ambos gateways se repitió cancelación manual y STOP ALL después del paso 6:
CANCELLED confirmado, motor/hidráulico detenidos, luces/canales apagados, efecto
NONE y servo conservado. Se observaron 13 segundos posteriores, superando el final
original del show, sin eventos nuevos ni cambios del snapshot. Cada falla crítica
canceló también el show; RESET no lo reanudó. Las fallas ordinarias de componentes
mantienen su política FAILED, cubierta por la suite de secuencias existente.

## Problemas encontrados

- API anterior mantenía el ejecutable bloqueado; se identificó el proceso del TP
  antes de reiniciarlo. NuGet.Config requirió ejecución autorizada fuera del sandbox;
  luego el build backend pasó con cero advertencias/errores.
- Prueba de reconocimiento de pérdida real de enlace reprodujo que el código de
  alerta local se enviaba al catálogo remoto, que lo rechazaba. Se corrigió la
  coordinación y la prueba pasó con un gateway limpio.
- Un proceso de test fallido retuvo binarios de configuración Iteration9; la suite
  final de desarrollo se ejecutó correctamente en Debug después de detener la API.
- La revisión visual observó una ejecución SHOW_FNE ajena a las acciones del
  revisor. Se solicitó liberar la instancia 5080 antes de pruebas mutantes compartidas.
- Los agentes de frontend y revisión alcanzaron límite de uso. Se inspeccionaron
  los archivos frontend y ejecutaron build/tests desde el agente principal. La
  revisión independiente no se considera aprobada por un agente que no terminó.

## Revisión visual y pendientes

Antes de la interrupción se abrió el dashboard en navegador real. Se observaron
Twin, métricas, controles y STOP persistente en 1440×900; también viewport móvil
390×844 sin overflow horizontal y STOP visible. Se operaron luces, motor a 50/100%,
reversa/STOP, barridos, BLINK, hidráulico y servo; se observó cancelación y
enclavamiento por sobretemperatura, reconocimiento/resolución/reset y lectura STALE.
Son observaciones previas, no una nueva aprobación de cierre.

El 2026-10-11 se intentó retomar el tab mediante cua.getState y cua.getTab. El
inventario respondió, pero getTab fue rechazado por la política de seguridad del
navegador (URL/protocolo bloqueado). No se intentó eludir el bloqueo. Quedan
PENDIENTES la repetición visual completa de escritorio/móvil, observación continua
de SHOW_FNE y STOP ALL, error de transporte y activación visual de movimiento
reducido. Los tests de mapping/markup/CSS y HTTP aprobados no los sustituyen.
No se probó placa ESP32 ni sensores reales; están fuera del alcance de Iteración 9.
La reducción de movimiento automatizada no sustituye su inspección visual.
La implementación y las verificaciones automatizadas están completas; no se declara
cumplimiento visual total de la Definition of Done mientras persistan estos pendientes.

### Cierre visual posterior — 2026-10-11, 16:16–16:20 ART

Se preservaron todos los cambios existentes. Esta tarea solo modifica documentación;
no cambia código, contratos, configuración ni scripts. Builds y suites de producto:
NO APLICA repetir para este cambio documental; los resultados de la ejecución
funcional anterior se conservan arriba y no se presentan como pruebas nuevas.

En este nuevo intento `cua.createBrowserTab` abrió correctamente
`http://127.0.0.1:5173/`. El rechazo anterior queda como evidencia histórica,
pero ya no bloqueó estas comprobaciones. Se revisaron capturas de viewport, DOM
accesible y medidas de layout, con API real en 5080 y gateway SIMULATOR.
El inventario de procesos no mostró scripts de regresión activos; la ejecución
estaba IDLE antes del primer comando. No se detuvieron ni reiniciaron servidores,
no se ejecutaron pruebas mutantes en paralelo ni se cambiaron puertos.

| Revisión nueva | Estado y evidencia observada |
| --- | --- |
| Escritorio 1440×900 | APROBADA: document/body scrollWidth=1440; área desplazable=1425. Twin destacado a la izquierda y telemetría a la derecha, sin overflow horizontal de página |
| Móvil 390×844 | APROBADA: document/body scrollWidth=390; área desplazable scrollWidth=clientWidth=375. Twin, métricas, controles, secuencias y alertas accesibles mediante desplazamiento vertical |
| STOP ALL persistente | APROBADA: escritorio rect top=825/bottom=900/height=75; móvil top=773/bottom=844/height=71. Visible en vista general, secuencias/historial y laboratorio/alertas, sin cubrir el contenido desplazable |
| SHOW_FNE completo | APROBADA visualmente: RUNNING, luces con halo, SWEEP_RIGHT, SWEEP_LEFT, motor 40%, elevación hidráulica, servo 120°, BLINK, retracción, STOP y luces OFF; COMPLETED 14/14 a 19,1 s |
| Coherencia Twin/telemetría | APROBADA: durante motor 40%, HUD running/forward y métricas 23,6 V / 4,2 A / 37 °C / 40%; al parar 24 V / 1 A / 25 °C / 0%. Posiciones hidráulicas y servo concordantes |
| SHOW_FNE + STOP ALL móvil | APROBADA: orden a mitad del recorrido; confirmación CANCELLED en paso 8 a 11,73 s. Ruedas y rotor detenidos, luces oscuras, banco NONE, hidráulico 94,1% STOPPED y servo conservado en 120° |
| Sin steps posteriores | APROBADA: runId `4469cf74-9d95-4477-91c5-1f517a368bc1`, 8 eventos después de STOP; seguían siendo 8 a las 16:19:19, más allá del final previsto. El Twin permaneció CANCELLED/detenido antes de iniciar una nueva orden explícita |
| Pérdida de comunicación simulada | APROBADA: laboratorio COMMUNICATION_LOSS, calidad SIN COMUNICACIÓN, cuatro métricas —, advertencia de datos no actuales, API ONLINE diferenciada del controlador y protección enclavada; no se inventaron ceros |
| Alertas y recuperación móvil | APROBADA: ACTIVA/SIN RECONOCER → ACTIVA/RECONOCIDA → RESUELTA/RECONOCIDA; retirar/reconocer mantuvo el enclavamiento. RESET explícito lo liberó sin reanudar SHOW_FNE |
| Sobretemperatura durante show en escritorio | APROBADA: 85 °C, parada confirmada, Twin CANCELLED y banco NONE; inicio/motor deshabilitados, STOP ALL habilitado. Reconocimiento, resolución y RESET no reanudaron movimientos |
| Lectura STALE en escritorio | APROBADA: DESACTUALIZADA, antigüedad 5,0 s, advertencia junto al Twin y protección enclavada. Recuperación explícita realizada |
| Caída real de API/transporte | PENDIENTE visualmente: no se apagaron procesos compartidos ni existe control documentado de red en el navegador. La pérdida controlada anterior no se presenta como desconexión física o caída de la API |
| prefers-reduced-motion activo | PENDIENTE visualmente: capacidades del navegador solo visibility/viewport y del tab pageAssets/webmcp; no hay emulación documentada de media. Se solicitó activación al usuario; no se obtuvo confirmación durante la revisión. Lectura de reglas CSS no equivale a activación visual |

Las capturas emitidas durante la revisión mostraron los estados descritos; no se
guardaron archivos de captura dentro del repositorio. El historial móvil dispone
de scroll interno de tabla, sin ensanchar la página. No se declara ausencia de
todo scroll horizontal interno, sino ausencia de overflow horizontal del dashboard.

Las fallas inyectadas quedaron retiradas/reconocidas, protección restablecida,
motor/hidráulico detenidos y luces/banco apagados; servo conservado. El viewport
temporal se restableció al finalizar. Continúan únicamente los pendientes visuales
explícitos de esta tabla; las comprobaciones nuevas no sustituyen sus evidencias.

## Archivos

Nuevos backend: Domain/Telemetry/TelemetrySnapshot.cs, Application/Services/TelemetryService.cs,
Contracts/DTOs/TelemetryDtos.cs, Controllers/TelemetryController.cs,
Infrastructure/Gateways/SimulatedTelemetry.cs, Infrastructure/Lifecycle/TelemetryMonitor.cs,
Carroza.Simulator.Tests/TelemetryTests.cs y backend/telemetry-test.ps1.
Modificados: IComponentGateway, OperationCoordinator, SequenceService, Program,
SimulatorComponentGateway, Esp32ComponentGateway, Esp32Protocol, GatewayErrorsOperationFilter
y entrada de la suite .NET. En esta continuación se modificó además
`backend/frontend-proxy-test.ps1` para cubrir la nueva lectura.

Frontend: nuevos Telemetry.tsx, pollSnapshot.ts, DevicePanel.tsx, CommandHistory.tsx
y tests dashboard/poll/telemetry. Modificados App, useDevices, Sequences, CarrozaView,
styles.css y tsconfig.test.json.

Firmware: nuevo src/telemetry.py y tests/test_telemetry.py; cambios en dispatcher,
protocol, test_firmware y README. Documentación actual: este informe y contratos
en DESARROLLO/ESP32-PROTOCOL, arquitectura/componentes/README según cierre.
Los informes 1–8 no se modifican.
