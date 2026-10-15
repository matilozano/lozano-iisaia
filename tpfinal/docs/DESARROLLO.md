# Desarrollo local

## Diseñador de secuencias — Iteración 11

Además de las regresiones existentes, ejecutar secuencialmente contra la API del TP:

```powershell
python backend/custom-sequence-test.py
python backend/sequence-restart-test.py
```

custom-sequence-test utiliza 5080, crea/elimina sus propias definiciones y aplica
STOP ALL al finalizar. Comprueba CRUD/revisión/orden, ambos modos, conflictos,
cancelación, pérdida de comunicación simulada, recuperación sin reinicio automático
y OpenAPI. Ejecutarlo en Simulator y ESP32 con HAL simulada, sin operar el panel.

sequence-restart-test administra exclusivamente sus procesos temporales en 5081
(debe estar libre), con JSON temporal independiente y HAL temporal en 5091.
Reinicia realmente el servidor
durante RUNNING, comprueba definición conservada/IDLE/estado seguro inicial del
Simulator y TIMEOUT continuo sin pasos posteriores. También verifica reinicio con
HAL sobreviviente: el arranque aplica STOP ALL antes de operar y conserva IDLE.
Sin confirmación del controlador conserva alerta/enclavamiento y StopConfirmed=false;
no se habilitan nuevas ejecuciones. No demuestra seguridad física de hardware real.
Acepta `--api-dll ruta/Carroza.Api.dll` para
usar otra salida compilada. fault-test acepta `-ApiDirectory ruta` por la misma razón.

El almacenamiento predeterminado es `ContentRootPath/App_Data/sequences.json`;
puede configurarse con `Sequences:Path`. Guardar copias con la API detenida.
Archivo JSON versionado, revisión optimista por definición, bloqueo entre procesos
y reemplazo seguro en el mismo directorio. Corrupción/versiones no soportadas:
500 STORAGE_CORRUPT, archivo intacto; restaurar manualmente una copia válida.
Errores de acceso/escritura: 500 STORAGE_ERROR, sin confirmar cambios.
Archivo ocupado o edición obsoleta: 409 OPERATION_CONFLICT. El orden se serializa;
entre reordenamientos válidos sucesivos prevalece el último confirmado, sin cambiar
el contenido de las definiciones. No compartir el archivo mediante sistemas de
archivos remotos cuya semántica de reemplazo/bloqueo no se haya verificado.

Contrato adicional, conservando rutas y respuestas previas:

| Método/ruta | Semántica |
| --- | --- |
| POST /api/sequences | Crear, 201; definición inválida 400; id existente 409 |
| PUT /api/sequences/{id} | Actualizar, 200; requiere revision vigente, conflicto 409 |
| DELETE /api/sequences/{id}?revision=... | Eliminar, 204; ausente 404; revisión obsoleta 409 |
| POST /api/sequences/{id}/duplicate | Copiar definición guardada con nuevos IDs, 200 o 404 |
| POST /api/sequences/order | Array de todos los IDs personalizados en orden, 204; lista cambió 409 |
| POST /api/sequences/{id}/start?mode=ONCE | Ejecución única, predeterminada y compatible |
| POST /api/sequences/{id}/start?mode=CONTINUOUS | Repetir hasta parada/falla/protección; modo desconocido 400 |

Los pasos personalizados agregan id estable y description opcional; definition
agrega revision. order consecutivo 1..N, 1..200 pasos, delayMs 0..600000 antes del
comando. Name 1..100 no vacío, description hasta 2000; description de paso hasta
500. IDs de secuencia ASCII alfanuméricos/guion/guion bajo, hasta 100, sin nombres
reservados por rutas o SHOW_FNE. Los comandos y sus rangos no cambian.
Execution agrega mode y completedCycles. Entre ciclos hay espera cancelable de
100 ms y nueva comprobación de telemetría, fuera del bloqueo de comandos.

SHOW_FNE permanece incorporada, primera y de solo lectura; puede duplicarse.
Todas las mutaciones de definiciones se rechazan durante RUNNING, protección o
parada. Consultas y STOP ALL permanecen disponibles. Una ejecución usa snapshot
capturado dentro de la admisión coordinada. No se guardan ejecuciones ni se reanudan.
Detener secuencia aplica parada global, conserva servo y sincroniza por el polling
existente. La finalización normal no introduce una parada implícita adicional:
el operador debe incluir los comandos de cierre que su definición requiera.

En Secuencias e historial: crear/seleccionar, agregar/duplicar/editar pasos,
arrastrar desde el agarre o usar Espacio/flechas/Escape; Subir/Bajar también funciona
con teclado. Guardar y ejecutar son acciones separadas: se ejecuta la última
definición guardada. Confirmar eliminación. Revisar editor, progreso/ciclos,
Twin y STOP ALL en escritorio/móvil y movimiento reducido. No aprobar revisión
visual por resultados HTTP; evidencia y pendientes en [ITERACION-11](ITERACION-11.md).

## Firmware — Iteración 8

Instalación, compilación MicroPython, tests y arranque del controlador HTTP local:
[README del firmware](../firmware/esp32-carroza/README.md).
Agregar al cierre las pruebas `python -m unittest discover -s firmware/esp32-carroza/tests -v`,
`python firmware/esp32-carroza/build.py` y `./firmware/esp32-carroza/integration-test.ps1`.
El último requiere host.py en 5090 y API en modo ESP32 en 5080; ejecutar luego
frontend-proxy-test con Vite en 5173. Reiniciar la API en Simulator para ejecutar
la regresión habitual completa, incluido fault-test. Nunca ejecutar ambas baterías
HTTP en paralelo. Resultados reales: [ITERACION-8](ITERACION-8.md).
La compilación de bytecode y las pruebas con HAL simulada no validan hardware físico.

Requisitos: SDK .NET 9, Node.js 22 con npm y PowerShell 7 para los scripts de prueba.

## Ejecución

Desde la raíz:

```powershell
dotnet run --project backend/Carroza.Api
```

En otra terminal:

```powershell
npm --prefix frontend install
npm --prefix frontend run dev
```

Frontend: http://127.0.0.1:5173

API: http://127.0.0.1:5080/api/devices

Mantener libres los puertos 5080 y 5173. El proxy de Vite conecta `/api` con la API en 5080. En Development, Swagger UI está en http://127.0.0.1:5080/swagger y OpenAPI en /swagger/v1/swagger.json. Iniciar Vite con el comando indicado para cargar frontend/vite.config.ts; un servidor estático sin proxy no sirve la API.

## Compilación

```powershell
dotnet build backend/Carroza.Api
npm --prefix frontend run build
```

## Pruebas automatizadas

Con la API iniciada:

```powershell
./backend/smoke-test.ps1
./backend/contract-test.ps1
./backend/iteration-3-test.ps1
./backend/iteration-4-test.ps1
./backend/fault-test.ps1
./backend/sequence-test.ps1
./backend/frontend-proxy-test.ps1
```

Suite del servicio y simulador, independiente del servidor HTTP:

```powershell
dotnet run --project backend/Carroza.Simulator.Tests
```

La suite .NET es un ejecutable de comprobaciones sin frameworks externos; se ejecuta con `dotnet run`, no con `dotnet test`. Los scripts HTTP comprueban catálogo, ambos sectores de luces, motor, dirección, límites 0/100, validaciones 400, componentes inexistentes 404, contrato JSON y parada general 204. Terminan con todos los componentes apagados/detenidos. La prueba iteration-3-test comprueba Swagger UI, sus assets, cuatro operaciones OpenAPI y cinco DTOs, además de comandos y evolución temporal del banco. Ejecutar los scripts secuencialmente y sin operar el panel: comparten estado. frontend-proxy-test requiere también Vite en 5173 y comprueba Content-Type y JSON de las consultas del panel en ambos puertos.

La suite .NET incluye pruebas del banco con reloj controlado: barridos, ida y vuelta, parpadeo, velocidad, congelamiento, validaciones y STOP ALL.

## Verificación manual

1. Iniciar la API y abrir el frontend. Confirmar ambas luces apagadas y motor detenido.
2. Encender/apagar luces frontales y laterales; observar el indicador y consultar `GET /api/devices`.
3. Iniciar motor adelante, seleccionar 70% y pulsar Aplicar velocidad.
4. Detener el motor y reiniciarlo en reversa. Comprobar estado, sentido y velocidad.
5. Encender ambas luces e iniciar el motor. Pulsar Detener todo y confirmar luces apagadas y motor detenido a 0%.
6. Enviar velocidad 101 a la API y confirmar HTTP 400 sin cambio de estado (cubierto por contract-test).
7. Detener temporalmente la API: debe aparecer OFFLINE y un error conservando el último estado conocido. Reiniciar la API: el panel debe recuperar la conexión y mostrar el estado reiniciado.
8. Comprobar que los controles y Detener todo sean accesibles en móvil y notebook.
9. En el banco probar barrido derecha, izquierda, ida y vuelta y parpadeo; observar ocho canales CH01..CH08 y contrastarlos con GET /api/devices/main-light-bank.
10. Probar velocidad 1 y 100; detener efecto (conserva patrón), Todos ON y Todos OFF.
11. Activar luces, motor y efecto; STOP ALL debe apagar iluminación y detener animaciones tras confirmación.
12. Verificar historial y último comando (OK/ERROR); recargar borra el historial, pero no el estado del backend.
13. Abrir /swagger, expandir cada operación y los esquemas; usar Try it out para consultar estados, ejecutar comandos de luz/motor/banco y STOP ALL. Verificar el resultado desde el panel.

## Fronteras arquitectónicas

Estos comandos no deben encontrar coincidencias (código de salida 1 de rg significa que no hubo coincidencias):

```powershell
rg 'Map(Get|Post|Put|Delete|Patch)' backend/Carroza.Api/Program.cs
rg 'SimulatorComponentGateway|Esp32ComponentGateway|IComponentGateway|Infrastructure' backend/Carroza.Api/Controllers
rg 'Infrastructure|Controllers|Contracts|Microsoft.AspNetCore' backend/Carroza.Api/Application backend/Carroza.Api/Domain
```

## Contrato HTTP

- GET `/api/devices`: lista de los seis componentes con estado actual.
- GET `/api/devices/{id}`: estado de un componente; 404 si no existe.
- POST `/api/devices/{id}/commands`: 200 con confirmación; 400 ante comando inválido; 404 si no existe.
- POST `/api/devices/stop-all`: 204 tras apagar luces y canales, detener efectos y motor.

Ejemplos de comandos:

```json
{ "action": "on" }
```

```json
{ "action": "start", "direction": "reverse", "speed": 70 }
```

```json
{ "action": "stop" }
```

El resultado de un comando conserva `deviceId`, `success`, `executedAt` y `state` como objeto completo (`id`, `state`, `online`, `direction`, `speed`). El navegador no debe inferir ejecución antes de la confirmación.

## Contrato del banco — Iteración 3

Componente `main-light-bank`, type `light_bank`. Usa las mismas rutas genéricas. Los comandos existentes de luces/motor siguen en minúsculas; el banco admite `ALL_ON`, `ALL_OFF`, `SWEEP_RIGHT`, `SWEEP_LEFT`, `PING_PONG`, `BLINK`, `STOP_EFFECT`, `SET_SPEED`.

`SET_SPEED` requiere `{ "action": "SET_SPEED", "speed": 50 }`, rango 1..100. Los otros comandos del banco no admiten speed ni direction. Un comando inválido devuelve 400 sin cambiar la configuración.

Sus estados agregan `channels` (ocho booleanos CH01..CH08), `effect` (NONE o efecto activo) y `effectSpeed` (1..100). Estos campos se omiten para los tres componentes anteriores, cuyo JSON se conserva. `speed` sigue siendo el campo del motor; para el banco queda en 0. Estado del banco: running con efecto, on si hay canales encendidos sin efecto, off si todos están apagados.

El simulador utiliza tiempo monotónico: intervalo de paso = 1200 - 9 × effectSpeed milisegundos. Los barridos vuelven al inicio; ida y vuelta recorre 0..7..0 sin repetir extremos. Cambiar velocidad reinicia la fase del efecto. STOP_EFFECT congela los canales actuales. ALL_ON/ALL_OFF cancelan el efecto; STOP ALL apaga todos los canales y conserva effectSpeed. Al reiniciar el servidor se restablece velocidad 50 y canales apagados.

El panel consulta cada 150 ms después de finalizar la consulta anterior, sin generar canales localmente. Con latencia alta puede omitir cuadros intermedios, pero cada patrón mostrado fue confirmado por API. Historial limitado a 100 entradas por sesión. Gateway SIMULATOR y controlador SIMULADO describen el único modo soportado; no representan telemetría de hardware.
## Iteración 4: posiciones y fallas

`hydraulic-1`, type `HYDRAULIC_ACTUATOR`: EXTEND, RETRACT y STOP sin parámetros. position es porcentaje 0..100; movement es STOPPED, EXTENDING o RETRACTING. limitExtended y limitRetracted indican extremos. Carrera completa: cinco segundos, 20 puntos porcentuales por segundo, calculados con reloj monotónico. STOP congela la posición alcanzada. Estado inicial: retraído.

`servo-1`, type `SERVO`: `{ "action": "SET_POSITION", "position": 90 }`. Acepta números finitos 0..180, incluidos decimales; no admite speed ni direction. Inicial: 90°. STOP ALL conserva el último ángulo (no ordena movimiento a otra posición). Esta es una convención del simulador, no una garantía de seguridad de hardware.

Se mantienen las rutas genéricas y los campos de componentes anteriores. Los campos nuevos se omiten cuando no aplican. STOP ALL conserva HTTP 204 por defecto. `POST /api/devices/stop-all?includeState=true` devuelve HTTP 200 con los seis estados tomados bajo el mismo bloqueo; el frontend utiliza esta opción y no calcula posiciones después de la parada.

Fallas reproducibles configuradas al iniciar, sin modificar appsettings:

```powershell
dotnet run --project backend/Carroza.Api -- --Simulator:Faults:servo-1=TIMEOUT --Simulator:Faults:front-lights=DEVICE_OFFLINE
```

También se puede usar una sección `Simulator:Faults` de configuración, con IDs como claves y códigos como valores. Para recuperar, reiniciar sin esos argumentos/configuración. Se reinicia también el estado virtual. No hay fallas aleatorias ni endpoints especiales de simulación.

Códigos: DEVICE_OFFLINE→503, TIMEOUT→504, INVALID_COMMAND→400, INVALID_PARAMETER→400, INTERNAL_ERROR→500. La respuesta mantiene `{status,error,message}` e identifica el componente. TIMEOUT simula el resultado de una espera agotada inmediatamente, sin demora ni ejecución tardía. Las fallas se aplican antes de ejecutar comandos válidos. Las validaciones de Domain ocurren primero. DEVICE_OFFLINE hace online=false en consultas; las otras fallas no impiden consultar. STOP ALL omite la inyección de fallas para detener el simulador, sin borrar la condición offline. Esto no modela una confirmación de parada de hardware desconectado.

`fault-test.ps1` inicia una instancia aislada en 5081 con cinco fallas, comprueba códigos HTTP/no mutación y un componente sano, y detiene su proceso al finalizar. Requiere backend compilado y puerto libre; acepta `-Port`. No modifica archivos de configuración ni variables globales.

Checklist visual adicional: extender/retraer/parar hidráulico y observar posición, aplicar servo 0/90/180, activar fallas con el comando anterior, confirmar offline y timeout sin cambio de ángulo, operar otro componente y comprobar STOP ALL. Repetir en notebook, tablet y móvil. Usar Swagger Try it out para comandos y consulta de estados.

## Secuencias — Iteración 5

- GET `/api/sequences`: catálogo y pasos.
- GET `/api/sequences/{id}`: definición; 404 si no existe.
- POST `/api/sequences/{id}/start`: 200 con ejecución RUNNING; 404 desconocida; 409 OPERATION_CONFLICT si existe una ejecución o parada en curso.
- GET `/api/sequences/execution`: ejecución actual/última; IDLE inicialmente. Incluye runId, sequenceId, status, currentStep, totalSteps, startedAt, finishedAt, elapsedSeconds, lastResult y error.
- POST `/api/sequences/cancel`: cancela y aplica parada global; 200 con ejecución. Sin ejecución activa conserva el estado terminal e igualmente detiene componentes.
- GET `/api/sequences/events?after=0`: eventos automáticos posteriores al cursor; se retienen hasta 500 en memoria, con runId, hora, origen, componente, comando y resultado.

Durante RUNNING los comandos manuales devuelven 409 OPERATION_CONFLICT. Consultas, cancelación y STOP ALL siguen disponibles. STOP ALL mantiene 204 o snapshot 200 con includeState=true. Cancela la secuencia antes de detener componentes; permite comandos nuevos posteriores a la parada confirmada. Fallas de steps producen FAILED, registran componente/código y aplican parada segura sin ejecutar pasos posteriores. Servo conserva ángulo confirmado. No hay persistencia ni reanudación automática.

SHOW_FNE: luces 0 s, barrido derecha 1 s, izquierda 3 s, motor adelante 40% 5 s, extender 7 s, servo 120° 9 s, blink 11 s, retraer 14 s, motor stop 16 s, iluminación off 17 s, hidráulico stop y COMPLETED aproximadamente 19 s. Cada delay se cuenta desde la confirmación del paso anterior.

sequence-test comprueba tiempos reales, finalización, conflictos 409 y cancelación/STOP ALL después del paso hidráulico, observando otros 13 segundos para descartar reactivaciones. No operar el panel durante estas pruebas. En revisión visual, iniciar SHOW FNE, observar progreso, luces, banco, motor, pistón y servo; repetir y detener después de varios pasos. Confirmar CANCELLED, historial MANUAL/SEQUENCE y controles recuperados.

## Digital Twin — Iteración 6

La Vista Carroza reutiliza el estado de useDevices; no requiere endpoints nuevos. Los ocho LED son los booleanos channels confirmados (incluido STOP_EFFECT, que congela patrón). Ruedas: 60/speed segundos por vuelta, adelante/reversa; motor parado o velocidad cero pausa la animación. Hidráulico desplaza la plataforma según position y servo gira según grados confirmados. Sin interpolación temporal ni extrapolación. API offline conserva la última representación y advierte que puede estar desactualizada.

Pruebas frontend sin nuevas dependencias:

```powershell
npm --prefix frontend run test
```

Compila los módulos de prueba con TypeScript en .test-build (ignorado por Git), ejecuta node:test y renderiza SVG con react-dom/server. Comprueba mapping, canales, sentido/velocidad, posiciones, STOP ALL, estados de secuencia y ausencia de lógica de control en la vista.

Revisión visual: iniciar ambos servidores en 5173/5080. No ejecutar scripts HTTP mientras se opera manualmente. Observar Vista Carroza y HUD al encender/apagar luces, barridos derecha/izquierda, BLINK, motor adelante a 20/50/100%, STOP, hidráulico y servo. Ejecutar SHOW_FNE completo; repetir y pulsar Detener todo con movimientos activos. El HUD debe coincidir con el panel. Verificar móvil sin overflow y reducción de movimiento (ruedas estáticas, HUD y posiciones conservados). La temporización de LED continúa siendo exclusivamente del backend.


La suite frontend contiene 11 pruebas, incluida la regla de movimiento reducido y su indicador estático alternativo. Resultados actuales y alcance exacto de revisión visual: ITERACION-6.md.

## Matriz de cierre del Harness

Las reglas permanentes están en [AGENTS](../AGENTS.md); el procedimiento de evidencia y reportes está en [HARNESS](HARNESS.md). No es necesario repetirlas en cada prompt.

| Tipo de cambio | Verificación requerida |
| --- | --- |
| Incremento funcional o refactor de código | Ambos builds, suite .NET/simulador, npm test, scripts smoke/contract/iteration-3/iteration-4/fault/sequence/frontend-proxy y búsquedas arquitectónicas de este documento |
| API, DTOs o documentación HTTP | Además, OpenAPI/rutas/esquemas/respuestas y Swagger disponible; probar operación afectada |
| UI, animación o sincronización | Además, revisión visual con API real y checklist aplicable, incluyendo SHOW_FNE/STOP ALL, responsive y movimiento reducido |
| Continuación después de interrupción | Repetir el cierre aplicable al código actual; resultados anteriores no aprueban la nueva ejecución |
| Solo documentación, sin cambiar contratos ni archivos ejecutables | Revisar diff/whitespace, enlaces/rutas/comandos mencionados y coherencia con código/alcance; registrar builds/HTTP/visual como NO APLICA con motivo |

Los scripts HTTP mutan estado: ejecutarlos secuencialmente, sin otra prueba o persona operando la misma API. La suite .NET independiente puede verificarse por separado. Confirmar primero servidores en 5173/5080 y dejar el simulador detenido al finalizar. El puerto 5081 de fault-test es una instancia temporal aislada del script, no sustituye los puertos del TP.

Diagnóstico de HTML en `/api`: registrar request exacta y respuesta (status, Content-Type y contenido), comprobar API directa y proxy con frontend-proxy-test, identificar servidor y configuración Vite. No esconder el fallo de parseo ni cambiar puertos. Si no se reproduce, documentarlo sin atribuir una causa no demostrada.

En PowerShell, comprobar el resultado individual y código de salida de cada comando; el éxito del último no demuestra que los anteriores pasaron. `rg` sin coincidencias retorna 1 en las búsquedas arquitectónicas, lo que aquí es el resultado esperado. Para filtrar arrays de Invoke-RestMethod, usar una variable o una expresión parentizada que enumere componentes, sin envolver todo el array como un solo componente.

## Gateway ESP32 — Iteración 7

Modo predeterminado conservado: Simulator. Para un controlador compatible con el protocolo v1, configurar sin editar código (reemplazar el host de ejemplo):

```powershell
dotnet run --project backend/Carroza.Api -- --ComponentGateway:Mode=ESP32 --ComponentGateway:ESP32:Endpoint=http://controlador.local/v1/exchange --ComponentGateway:ESP32:TimeoutMs=2000
```

Configuración equivalente en appsettings o variables de entorno `ComponentGateway__Mode`, `ComponentGateway__ESP32__Endpoint`, `ComponentGateway__ESP32__TimeoutMs`. Endpoint obligatorio solo en ESP32, timeout 100..30000 ms. No cambiar puertos 5080/5173. Sin placa/servidor compatible las consultas devuelven error JSON; no hay fallback silencioso a Simulator.

La suite habitual `dotnet run --project backend/Carroza.Simulator.Tests` incluye Esp32Tests: transporte HTTP con handler controlado, serialización, respuestas/errores, DI, SHOW_FNE, cancelación, STOP ALL y API HTTP real con gateway alternativo. No requiere placa ni servicio externo. El servidor HTTP de prueba usa puerto efímero y se elimina al finalizar. Las comprobaciones temporales del simulador siguen en sus suites anteriores; el show del doble ESP32 usa delay controlado.

Swagger documenta también errores de lectura/parada: 500/503/504 con el formato existente. No se agregan rutas públicas ni se renombra ningún DTO. Ante TIMEOUT de hardware, ejecución desconocida: se conserva último estado confirmado, sin reintento automático. No interpretar cancelación de secuencia como confirmación de parada física si la operación devolvió error.

El frontend se conserva sin cambios. Los textos históricos SIMULATOR/SIMULADO son etiquetas estáticas de la demo, no detección del gateway ni telemetría; no deben usarse para identificar una placa al probar el modo ESP32. La integración física y su identificación visual quedan fuera del alcance actual.

## Dashboard y supervisión — Iteración 9

El dashboard ahora obtiene el gateway real de `GET /api/telemetry`; reemplaza las
etiquetas estáticas descritas en el contexto histórico anterior. Mantiene un solo
polling frontend, con resultados independientes de dispositivos, secuencia y
telemetría. STOP ALL tiene una fila persistente fuera del contenido desplazable.

Telemetría: `gateway`, `source` (SIMULATED/UNAVAILABLE), `online`, `sampledAt` UTC,
`ageMs`, `quality` (VALID/STALE/INVALID/OFFLINE/UNAVAILABLE), `voltage` V, `current` A,
`temperature` °C, `motorSpeed` %, `protectionLatched`, `stopConfirmed`,
`canInjectFaults`, `alerts` con code/active/acknowledged/activatedAt/resolvedAt.
Null no equivale a cero. Una consulta puede devolver 200 con calidad OFFLINE y
últimos valores: la API responde, pero el controlador no confirma su estado actual.

En Simulator y firmware/HAL simulada, para velocidad confirmada s=0..100:
V=24−0.01s, A=1+0.08s, °C=25+0.3s. Es un modelo determinista de demostración, sin
sensores físicos ni calibración. Cada consulta actualiza la muestra salvo lectura
obsoleta inyectada; STALE fuerza ageMs≥5000 para probar la advertencia.

POST `/api/telemetry/simulation` permite SET_FAULT (code y active obligatorios),
ACKNOWLEDGE (code opcional) y RESET (sin parámetros). Códigos:
OVER_TEMPERATURE=85 °C, OVER_CURRENT=15 A, VOLTAGE_RANGE=16 V,
COMMUNICATION_LOSS=OFFLINE, INVALID_SENSOR=null/INVALID, STALE_SENSOR=STALE.
Todas son condiciones críticas discretas: paran y enclavan. Retirar una causa
con active=false no rearma. Reconocer tampoco; RESET exige todas resueltas y
reconocidas, con parada confirmada. No se reanuda ninguna secuencia o movimiento.
Fuera de HAL simulada, la inyección se rechaza en el controlador.

El supervisor backend se ejecuta cada 250 ms sin depender del navegador. Ante
pérdida real de transporte intenta STOP ALL y cancela/bloquea operaciones, pero
no confirma parada física si falla la comunicación. La recuperación requiere
reconocimiento y restablecimiento. Los comandos bloqueados devuelven conflicto
409 en la coordinación backend; los parámetros de recuperación inválidos, 400.

Agregar al cierre de ambos modos, después de la regresión y sin operar el panel:

```powershell
./backend/telemetry-test.ps1
```

Repetir en Simulator y en ESP32 → host.py/HAL simulada según README de firmware.
El script verifica ambos endpoints nuevos en OpenAPI, las seis fallas, parada,
cancelación, bloqueo, reconocimiento/reset y ausencia de reactivación. Para revisión
visual: escritorio y móvil sin overflow, STOP accesible, métricas al variar motor,
laboratorio de fallas, alertas activas/reconocidas/resueltas, recuperación y pérdida
de comunicación conservando la última lectura. Resultados: [ITERACION-9](ITERACION-9.md).

## Vista 3D y tabs — Iteración 10

Vista general contiene Twin/telemetría; Componentes conserva todos los controles;
Secuencias e historial contiene SHOW_FNE y eventos. STOP ALL queda fuera de las
tabs. Un único useDevices alimenta todo. Sin endpoints nuevos.

La escena 3D propia carga Three/Fiber de forma diferida. Arrastrar rota; botones
o flechas cambian cámara; +/− hacen zoom y Home/Restablecer cámara vuelve al inicio.
Ver 2D conserva el SVG existente; falta de WebGL2/error gráfico selecciona esa
alternativa. Pausar animación solo pausa ruedas visuales, nunca componentes.
La preferencia prefers-reduced-motion también elimina rotación continua.

Al revisar, comprobar 3D/2D/3D, cámara conservada entre tabs, SHOW_FNE completo y
STOP durante movimiento, calidad/alertas, caída de API, y las cuatro resoluciones
de PLAN. Probar WebGL deshabilitado y movimiento reducido cuando el navegador
permita esos escenarios; informar PENDIENTE si no se pueden activar. Pruebas con
los mismos comandos npm/backend anteriores; tests 3D adicionales cubren mapping,
cámara, detección WebGL y limpieza de contexto. Resultados en ITERACION-10.
