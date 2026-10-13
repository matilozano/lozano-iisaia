# Iteración 11 — Diseñador visual de secuencias personalizadas

Fecha: 2026-10-11, rama tpfinal. Implementación realizada y verificaciones
automatizadas aprobadas; cierre visual PENDIENTE. No se declara Definition of Done
completa mientras no se revise el editor con navegador real.

## Estado inicial

Rama tpfinal. Solo docs/PLAN.md estaba modificado: definición truncada en
Hidráulico. Se conserva todo el trabajo de Iteración 10. SequenceService ejecuta
SHOW_FNE una sola vez; no existe persistencia ni editor de definiciones.
El modo continuo mencionado por el pedido no existe en el código inspeccionado.
El banco no soporta comandos por canal individual; se usan sus acciones existentes.

## Decisiones previas a implementación

- Añadir ISequenceStore en Application y JSON versionado en Infrastructure. Archivo
  configurable, reemplazo atómico en el mismo directorio, acceso serializado y
  bloqueo de archivo entre procesos. Una revisión por definición evita perder
  ediciones concurrentes. Archivo corrupto: error explícito, sin sobrescribir ni
  fingir catálogo vacío. Validar también lo leído de disco.
- SHOW_FNE permanece incorporada e inmutable; puede duplicarse. Las definiciones
  personalizadas tienen identificadores de secuencia/paso estables y orden explícito.
  Espera relativa delayMs antes de cada comando; no introduce parada implícita tras
  una duración. Para sostener un movimiento se añade espera antes del paso STOP.
- Límites: 1..200 pasos, espera 0..600000 ms por paso, nombre 1..100 caracteres,
  descripción hasta 2000, descripción de paso hasta 500. Orden consecutivo 1..N,
  IDs únicos. Reutilizar CommandValidator y catálogo; no crear comandos nuevos.
- Extender SequenceService, no crear otro ejecutor. Snapshot de definición al inicio;
  rechazar mutaciones del catálogo durante RUNNING mediante el coordinador compartido
  (también sobre otras definiciones, para no intercalar escritura y ejecución).
  Modo único por defecto para compatibilidad;
  modo continuo explícito. Comprobar telemetría entre ciclos y token antes del siguiente
  comando. Espera cancelable mínima entre ciclos evita bucle sin pausa para pasos de 0 ms.
- CRUD y reordenamiento persistido mediante Controllers y servicio de definiciones.
  API existente permanece compatible; extender estado con modo y ciclos completados.
- Editor separado de App, @dnd-kit ya instalada, handles independientes y teclado.
  Borrado confirmado. Estado del formulario es borrador, nunca estado operativo.
  Catálogo se consulta al editar/guardar; estado operativo usa solo useDevices.
- Twin sin lógica nueva: recibe los mismos estados confirmados durante cualquier
  secuencia. STOP ALL y supervisión conservan sus mecanismos de prioridad y enclavamiento.

## Plan de ejecución

- [x] Persistencia/validación/CRUD con pruebas de disco, revisión concurrente y corrupción.
- [x] Extensión del motor con modo continuo, ciclos y pruebas de cancelación/seguridad.
- [x] API/DTOs/OpenAPI compatibles y pruebas HTTP.
- [x] Editor e integración con polling/estado/Twin existentes; pruebas automatizadas.
- [x] Matriz automatizada de cierre; registrar evidencia real.
- [ ] Revisión visual con navegador real, bloqueada por la herramienta.

## Implementación y arquitectura final

Frontend → Controllers → SequenceDefinitions/SequenceService → abstracciones de
almacenamiento/ComponentCommandExecutor → IComponentGateway. Program conserva solo
composición y DI. No se modifica el firmware, los gateways ni sus comandos.

Las definiciones se guardan en JSON v1 con revisión por definición, bloqueo de
archivo compartido entre instancias del almacenamiento y escritura temporal con
Flush(true)/File.Replace (File.Move para creación). El archivo anterior permanece
intacto ante corrupción o escritura fallida; se informa STORAGE_CORRUPT/STORAGE_ERROR
sin fabricar éxito. No se reconstruye silenciosamente el catálogo ni se borra el
archivo corrupto. Restauración manual de copia válida. Los reordenamientos válidos
se serializan, prevaleciendo el último confirmado sin sobrescribir campos editados.

SHOW_FNE sigue incorporada/inmutable y puede duplicarse. IDs estables, revisión,
esperas y descripciones se validan tanto al guardar como al leer. Los dos gateways
existentes comparten el mismo catálogo y contrato de comandos, verificados en sus
regresiones; no se inventan capacidades por canal. Las fallas operativas se evalúan
en admisión/supervisión y al ejecutar, no se confunden con validez del borrador.

El motor mantiene una sola lease por ejecución. CONTINUOUS incrementa completedCycles,
espera 100 ms cancelables y consulta telemetría antes del siguiente ciclo. Esa
consulta ocurre fuera del bloqueo de comandos, evitando un interbloqueo con el
supervisor que necesita el coordinador para parar. No hay bucle de espera activa.
ONCE conserva semántica previa: no agrega una parada al final de una definición
que no la incluya; el editor explica la necesidad de comandos de cierre.

SequenceDesigner separa catálogo/borrador y acciones de persistencia de StepEditor,
SortableList y mapping puro. @dnd-kit existente, sin dependencias nuevas: agarre
independiente, mouse/puntero/táctil y teclado Espacio/flechas/Escape, más Subir/Bajar
pasos. Edición/duplicación de secuencias y pasos, eliminación confirmada, orden
persistido de secuencias y orden de pasos al guardar. Cambiar de definición advierte
que se descarta el borrador. Se ejecuta la versión guardada, no el borrador.

Una instancia de useDevices conserva polling operativo de componentes/telemetría/
ejecución/eventos. Solo se amplía la orden de inicio con id/mode. No hay timers de
secuencia, comandos de dispositivos ni polling operativo dentro del editor o Twin.
El catálogo se carga bajo demanda; nunca determina el estado físico. El Twin 3D
recibe los mismos estados confirmados. Tabs, grilla de componentes, alternativa 2D,
cámara, STOP fijo e historial MANUAL/SEQUENCE se conservan. Campos adaptables,
controles de 44 px y reduced-motion sin transiciones del arrastre.

## API y compatibilidad

Nuevos POST /api/sequences (201), PUT /api/sequences/{id} (200),
DELETE /api/sequences/{id}?revision=... (204/404),
POST /api/sequences/{id}/duplicate (200/404) y POST /api/sequences/order (204).
Errores de validación 400, revisión/conflicto/coordinación 409, almacenamiento 500.
SHOW_FNE no se modifica/elimina; puede copiarse. Las mutaciones se bloquean durante
RUNNING/protección/parada, también si afectan otra definición.

Inicio existente admite query mode=ONCE/CONTINUOUS; sin query sigue ONCE. DTOs
agregan id/description de paso, revision y mode/completedCycles de ejecución.
Todos los campos y endpoints previos de dispositivos permanecen. Swagger publica
rutas, DTOs, parámetro de modo y errores; se probaron por HTTP/OpenAPI/UI/assets,
no se declara Try it out manual porque el navegador quedó bloqueado.

## SHOW_FNE, STOP ALL y fallas

SHOW_FNE completo mantiene orden, tiempos, progreso y COMPLETED (14 pasos), con
parada de iluminación/motor/hidráulico y servo conservado según sus comandos.
Se probó además cancelación y STOP ALL a mitad de SHOW_FNE con observación prolongada
de los pasos pendientes. Ningún paso posterior reactivó componentes.

Definición personalizada de prueba combina los seis componentes, finaliza una vez,
ejecuta al menos dos ciclos continuos, rechaza doble inicio/manual/edición y permite
STOP ALL/cancelación. Confirmación de parada segura, CANCELLED y ausencia de nuevos
eventos tras esperar más que un ciclo. Pérdida de comunicación simulada enclava,
cancela y bloquea nuevos inicios; resolver/reconocer/reset no reinicia la ejecución.
TIMEOUT continuo termina FAILED con un evento y sin pasos posteriores.

Reinicio real durante RUNNING, en procesos aislados: se conserva definición,
ejecución vuelve a IDLE y Simulator inicializa componentes seguros, sin reanudación.
También se mantiene viva la HAL simulada mientras se termina abruptamente el backend:
al volver a iniciar, SequenceLifetime espera TelemetryService.InitializeAsync,
que aplica la parada coordinada existente antes de atender operaciones. Así se
detienen estados remotos heredados sin reanudar pasos. Si no hay confirmación,
se reutiliza la alerta/enclavamiento de comunicación; la API permanece disponible
para diagnóstico, sin permitir nuevas ejecuciones ni inventar StopConfirmed.
No demuestra seguridad física de una placa; sigue requiriendo confirmación remota.

## Problemas encontrados y correcciones

- PLAN truncado: completado con las reglas aprobadas, sin reducir alcance ni crear
  Iteración 12. Se informó la ausencia previa de modo continuo y de comandos por canal.
- Windows/NuGet: build estándar bloqueado primero por acceso a NuGet.Config y luego
  por DLL/apphost de Carroza.Api 17072/20376. Con autorización se detuvo 17072;
  Windows rechazó detener 20376 incluso elevado. Se compiló en salida aislada.
- La prueba deliberadamente fallida de concurrencia dejó procesos .NET reteniendo
  otra DLL; Windows también negó su cierre. Se utilizó una nueva salida
  `.artifacts-11/final`, sin borrar ni reemplazar archivos bloqueados.
- Pruebas Node: un import sin extensión no se resolvía en ESM; corregido con .js,
  compatible con TypeScript/Vite. Suite completa pasó después.
- Revisión independiente encontró captura de definición antes de esperar telemetría:
  podía iniciar una versión ya editada/eliminada. Regresión con telemetría retenida
  falló realmente con “Inicio ejecutó edición obsoleta”. Se reconsulta dentro de
  ManualAsync antes de Begin. Las dos regresiones (editar/eliminar) pasaron y la
  revisión independiente confirmó la corrección, sin nuevos hallazgos importantes.
- Reinicio abrupto con HAL sobreviviente: la prueba nueva falló con el binario
  anterior porque el motor seguía RUNNING. La parada coordinada al inicio corrigió
  el caso; pasó además el arranque sin HAL con protección y parada sin confirmar.
  Revisión independiente aprobó el orden de locks y la ausencia de reinicio automático.

## Verificaciones ejecutadas en esta tarea

PowerShell del runtime se ejecutó con -NoProfile -File; Python del runtime local.
Scripts mutantes secuenciales por instancia, sin operación manual del panel.
Puertos del TP conservados 5173/5080; HAL 5090; pruebas aisladas API 5081/HAL 5091.
Tras la corrección de parada al inicio se repitieron las regresiones HTTP finales
en Simulator y ESP32/HAL, incluidas SHOW_FNE, telemetría y proxy. Al terminar se
detuvieron únicamente las instancias de prueba propias, se restauró Simulator en
5080 con almacenamiento backend/Carroza.Api/App_Data/sequences.json y se repitió
smoke (aprobado, finaliza con STOP ALL). Vite en 5173 se conservó.
Rama final tpfinal y git diff --check aprobado; solo avisos de conversión LF/CRLF.

| Verificación | Resultado y evidencia |
| --- | --- |
| Build estándar backend | FALLÓ por permisos NuGet/archivo bloqueado; no se presenta como éxito |
| `dotnet build backend/Carroza.Api --artifacts-path .artifacts-11` y build de suite con `--artifacts-path .artifacts-11/final` | APROBADA: API y suite compiladas, 0 errores/advertencias en la salida final |
| `dotnet .artifacts-11/final/bin/Carroza.Simulator.Tests/debug/Carroza.Simulator.Tests.dll` | APROBADA: 468 comprobaciones (34+75+52+82+46+63+116), incluidas 60 operaciones concurrentes |
| `npm.cmd --prefix frontend run build` | APROBADA: TypeScript/Vite, 83 módulos; persiste aviso del chunk 3D de 928.54 kB |
| `npm.cmd --prefix frontend run test` | APROBADA: 36 tests, 0 fallas; cuatro nuevos de editor/mapping/orden/controles/frontera |
| smoke / contract / iteration-3 / iteration-4 | APROBADAS en Simulator: contratos, componentes, errores, progresión y Swagger |
| sequence-test | APROBADA en Simulator: SHOW completo, timing/orden, conflictos, cancelación, STOP ALL sin reactivación |
| telemetry-test | APROBADA en Simulator y ESP32/HAL: seis fallas, enclavamiento, recuperación, cancelación y no reinicio |
| fault-test con -ApiDirectory de salida aislada | APROBADA: cinco fallas HTTP, no mutación, componente sano y parada |
| frontend-proxy-test | APROBADA en ambos modos: doce consultas JSON en 5080/5173; no HTML |
| `python backend/custom-sequence-test.py` | APROBADA en Simulator y ESP32/HAL: CRUD, revisión, orden, duplicación, único/continuo, conflictos, STOP/cancelación, pérdida simulada y OpenAPI |
| `python backend/sequence-restart-test.py --api-dll .artifacts-11/final/bin/Carroza.Api/debug/Carroza.Api.dll` | APROBADA: persistencia/IDLE seguro, TIMEOUT continuo FAILED, reinicio con HAL sobreviviente y arranque sin HAL bloqueado |
| firmware integration-test | APROBADA: backend ESP32 → HTTP → firmware/HAL simulada, cinco regresiones incluida SHOW_FNE |
| `python -m unittest discover -s firmware/esp32-carroza/tests -v` | APROBADA: 32 tests conservados; no se modificó firmware |
| Tres búsquedas arquitectónicas de DESARROLLO | APROBADAS: sin coincidencias, salida 1 esperada |
| Swagger/OpenAPI | APROBADA por HTTP: UI/assets, operaciones CRUD, esquemas y campos mode/completedCycles; ejecución real de operaciones |
| Revisión visual real del editor/Twin | PENDIENTE: getState mostró páginas de conexión rechazada; getTab rechazado por política de protocolo no permitido. Sin bypass ni aprobación ficticia |

## Archivos

Creados:

- backend/Carroza.Api/Application/Services/ISequenceStore.cs
- backend/Carroza.Api/Application/Services/SequenceDefinitions.cs
- backend/Carroza.Api/Domain/Sequences/SequenceValidation.cs
- backend/Carroza.Api/Infrastructure/Sequences/JsonSequenceStore.cs
- backend/Carroza.Simulator.Tests/CustomSequenceTests.cs
- backend/custom-sequence-test.py y backend/sequence-restart-test.py
- frontend/src/sequences/model.ts, SortableList.tsx, StepEditor.tsx,
  SequenceDesigner.tsx y designer.css
- frontend/tests/sequence-designer.test.mjs
- docs/ITERACION-11.md

Modificados:

- .gitignore (datos locales y salida de compilación aislada)
- backend/Carroza.Api/Application/Services/SequenceService.cs
- backend/Carroza.Api/Application/Services/TelemetryService.cs y Infrastructure/Lifecycle/SequenceLifetime.cs (parada coordinada de inicio)
- backend/Carroza.Api/Domain/Sequences/SequenceDefinition.cs
- backend/Carroza.Api/Contracts/DTOs/SequenceDtos.cs
- backend/Carroza.Api/Controllers/SequencesController.cs y Program.cs (DI)
- backend/Carroza.Simulator.Tests/Program.cs y backend/fault-test.ps1
- frontend/src/App.tsx, Sequences.tsx, api.ts y useDevices.ts
- frontend/tsconfig.test.json
- docs/PLAN.md, docs/DESARROLLO.md y docs/ARCHITECTURE.md

## Pendientes reales

Revisión visual del editor y Drag & Drop con mouse/táctil/teclado, responsive,
reduced-motion, custom única/continua y coherencia Twin/STOP con navegador real.
Swagger Try it out manual también pendiente. Ninguno se aprueba por SSR/HTTP.
La compilación estándar debe repetirse cuando Windows libere los archivos retenidos;
la compilación y ejecución verificadas corresponden a la salida aislada final.
No se realizaron commits/push, no se cambió rama ni se avanzó a Iteración 12.
