# Iteración 5 — Secuencias coordinadas

Cierre verificado el 5 de octubre de 2026, rama `tpfinal`. Sin commits, push ni cambio de rama.

## Estado encontrado y trabajo de esta continuación

Ya estaban implementados y versionados el catálogo SHOW_FNE, motor de ejecución, coordinación de concurrencia, endpoints/DTOs, integración React e historial, pruebas deterministas y scripts HTTP. El diff inicial estaba vacío; solo existía sin seguimiento `backend/Carroza.Api/Carroza.Api.sln`, que se conservó intacto. Se leyeron AGENTS de raíz/backend/frontend y documentación, incluidos los informes 1 a 4. No existía ITERACION-5.md. PLAN, DESARROLLO y ARCHITECTURE todavía describían secuencias como futuras.

Se preservó la implementación. Esta continuación agregó prueba del proxy, reforzó cancelación y STOP ALL con movimientos activos y observación hasta después del final previsto del show, verificó integración real, actualizó descripciones Swagger y completó documentación. No se implementó Iteración 6.

## Modelo y arquitectura implementados

- Domain/Sequences: SequenceDefinition contiene id, name, description y steps; cada SequenceStep contiene order, delayMs relativo al paso previo, componentId y ComponentCommand con parámetros. El modelo es genérico; solo el catálogo de demostración referencia IDs concretos.
- Application: SequenceService ejecuta pasos ordenados, mantiene ejecución/eventos y usa ISequenceDelay/TimeProvider. ComponentCommandExecutor es compartido con comandos manuales: valida mediante Domain y ejecuta IComponentGateway. No hay un segundo mecanismo de comandos.
- OperationCoordinator singleton serializa mutaciones y controla admisión, generación y token de cancelación. ComponentService y SequenceService son singleton. SequenceLifetime aplica parada al cerrar el host.
- Controllers solo traducen HTTP y DTOs. Program.cs conserva exclusivamente composición/configuración. SimulatorComponentGateway sigue siendo el responsable del estado físico simulado.
- React consulta estados, ejecución y eventos cada 150 ms después de completar la consulta anterior. No ejecuta pasos ni inventa posiciones; historial local limitado a 100 entradas con MANUAL/SEQUENCE. Backend retiene 500 eventos automáticos sin persistencia.

Estados: IDLE, RUNNING, COMPLETED, CANCELLED y FAILED. La respuesta informa runId, sequenceId, currentStep, totalSteps, startedAt, finishedAt, elapsedSeconds, lastResult y error. Los delays comienzan después de la confirmación previa: los tiempos son aproximados, no tiempo real estricto.

## API y Swagger

| Método | Ruta | Resultado |
| --- | --- | --- |
| GET | /api/sequences | Catálogo y pasos |
| GET | /api/sequences/{id} | Definición o 404 |
| POST | /api/sequences/{id}/start | RUNNING; 404 desconocida; 409 conflicto |
| GET | /api/sequences/execution | Ejecución actual/última |
| POST | /api/sequences/cancel | Cancelación y parada global, respuesta de ejecución |
| GET | /api/sequences/events?after=0 | Eventos automáticos posteriores al cursor |

Son endpoints ya encontrados, no añadidos nuevamente. Comandos manuales existentes incorporan 409 OPERATION_CONFLICT durante RUNNING/parada. STOP ALL mantiene 204 por defecto y 200 con snapshot mediante includeState=true. Swagger Development publica todos los endpoints y DTOs en /swagger; se corrigió su descripción de STOP ALL para incluir secuencias, hidráulico y preservación del servo.

## SHOW_FNE

| Tiempo aproximado | Acción |
| --- | --- |
| 0 s | Luces frontales y laterales ON |
| 1 s | SWEEP_RIGHT |
| 3 s | SWEEP_LEFT |
| 5 s | Motor adelante 40% |
| 7 s | Hidráulico EXTEND |
| 9 s | Servo SET_POSITION 120° |
| 11 s | BLINK |
| 14 s | Hidráulico RETRACT |
| 16 s | Motor STOP |
| 17 s | Banco ALL_OFF y ambas luces OFF |
| 19 s | Hidráulico STOP y COMPLETED |

Son 14 pasos. El paso final permite completar la retracción antes de declarar COMPLETED. Verificación HTTP confirmó 14 eventos OK en orden, delays respetados, hidráulico en 0%, motor detenido, iluminación apagada y servo en 120°.

## Cancelación, STOP ALL y fallas

Durante RUNNING se rechazan comandos manuales y otro inicio desde backend con 409; consultas, cancelación y STOP ALL permanecen disponibles. STOP ALL cancela el token e invalida solicitudes anteriores en espera, espera la mutación en curso y aplica parada segura. Impide steps posteriores, apaga luces/canales, cancela efectos, detiene motor y hidráulico en su posición alcanzada, conserva ángulo del servo y devuelve estados confirmados. No es enclavamiento permanente: se admiten órdenes nuevas después de confirmar la parada.

La cancelación de secuencia también aplica parada global. Un fallo registra componente/código y evento fallido, aplica parada segura y finaliza FAILED sin pasos posteriores. El comando fallido no se considera ejecutado. Se conservaron las cinco fallas deterministas de Iteración 4, separadas del comportamiento normal. STOP ALL del simulador omite inyección de fallas; no implica capacidad de detener hardware desconectado.

Prueba reforzada: tanto cancelación como STOP ALL se ejecutaron después del paso 6, con motor e hidráulico activos. Se verificó CANCELLED inmediatamente, estados seguros, ausencia de nuevos eventos, mismo runId y snapshot sin cambios durante otros 13 segundos (más allá de los 19 segundos originales).

## Integración y problemas encontrados

Frontend real en http://127.0.0.1:5173 y backend en http://127.0.0.1:5080. Vite mantiene proxy /api hacia localhost:5080; ambos nombres alcanzan el mismo backend local. No se cambiaron puertos ni se usaron rutas de otro proyecto.

No se reprodujo `Unexpected token '<'`. Se comprobaron /api/devices, /api/sequences, /api/sequences/SHOW_FNE, /api/sequences/execution y /api/sequences/events directamente y a través de Vite: todas HTTP 200, Content-Type application/json y JSON parseable. No hubo una request actual que devolviera HTML; no se puede establecer la causa histórica sin evidencia de esa ejecución. Se agregó frontend-proxy-test.ps1 para detectar el problema sin ocultarlo con try/catch. Se documentó iniciar Vite con su configuración, en lugar de servir index.html como API.

Primeras pruebas sufrieron interferencia de inicios de SHOW_FNE desde el panel: hubo 409 y cambios de runId durante comprobaciones de parada. Se pidió dejar libre la API, el usuario lo confirmó y se repitieron satisfactoriamente. Durante la ampliación del script se corrigió una agrupación PowerShell de la respuesta JSON que trataba el array completo como un componente; no era una falla del gateway.

## Verificaciones ejecutadas

| Verificación | Resultado |
| --- | --- |
| dotnet build backend/Carroza.Api | OK, 0 errores y 0 advertencias; repetido tras actualizar Swagger |
| npm.cmd --prefix frontend run build | OK, TypeScript y Vite, 35 módulos |
| dotnet run --project backend/Carroza.Simulator.Tests | OK: 34 generales, 75 banco, 52 posiciones/fallas, 82 secuencias; además 60 operaciones concurrentes |
| smoke-test.ps1 | OK, seis componentes y parada |
| contract-test.ps1 | OK, JSON, validaciones y catálogo |
| iteration-3-test.ps1 | OK, banco y Swagger UI/assets/DTOs |
| iteration-4-test.ps1 | OK, hidráulico progresivo, servo, errores y snapshot |
| fault-test.ps1 | OK, cinco errores HTTP, no mutación, offline y secuencia FAILED sin pasos posteriores |
| sequence-test.ps1 -BaseUrl http://127.0.0.1:5080 | OK, show completo/tiempos, conflictos, cancelación y STOP ALL prolongados |
| frontend-proxy-test.ps1 | OK, diez consultas JSON entre API directa y proxy |
| Tres búsquedas arquitectónicas de DESARROLLO | Sin coincidencias; rg sale 1, resultado esperado |
| Swagger navegador | UI, endpoints y DTOs visibles; Try it out de ejecución devolvió 200 application/json |
| Revisión visual de secuencias | Realizada parcialmente; detalle y límites abajo |

La suite es un ejecutable .NET, no un proyecto de test framework: se ejecutó dotnet run según DESARROLLO, no se presenta dotnet test como una prueba adicional.

## Revisión visual y pendientes

Navegador disponible. Panel ONLINE, RUNNING con progreso, comandos manuales deshabilitados, luces encendidas, banco cambiando canales/BLINK, motor 40%, hidráulico hasta 100% y servo 120° observados mediante estado accesible del navegador. Se inició SHOW desde la UI y se confirmó COMPLETED paso 14 en 19.1 segundos. Historial mostró MANUAL para inicio/STOP ALL y SEQUENCE para comandos automáticos.

En otra ejecución desde UI, STOP ALL durante RETRACT (paso 9, aproximadamente 14.5 s) confirmó CANCELLED; el motor estaba con animación running, duración 1.5 s a 40%, antes de detenerlo. Captura: iteracion-5-stop-all.png. La prueba HTTP prolongada complementa esta observación visual verificando que no haya reactivaciones.

No se certifica una revisión visual exhaustiva de cada animación ni regresión completa de todos los controles anteriores, ni todos los tamaños notebook/tablet/móvil. Tampoco se repitió visualmente cada falla; están cubiertas por pruebas deterministas y HTTP. Esas revisiones quedan pendientes, no aprobadas. No hay verificaciones automatizadas pendientes de las enumeradas arriba.

## Archivos

Creados en esta continuación: backend/frontend-proxy-test.ps1, docs/ITERACION-5.md, docs/iteracion-5-stop-all.png.

Modificados: backend/sequence-test.ps1; backend/Carroza.Api/Program.cs y Controllers/DevicesController.cs (solo descripción Swagger); docs/PLAN.md, docs/DESARROLLO.md y docs/ARCHITECTURE.md.

Implementación previa conservada: Domain/Sequences/SequenceDefinition.cs; Application/Services/SequenceService.cs, ISequenceDelay.cs, OperationCoordinator.cs, ComponentCommandExecutor.cs y ComponentService.cs; Controllers/SequencesController.cs; Contracts/DTOs/SequenceDtos.cs; Infrastructure/Lifecycle/SequenceLifetime.cs; frontend/src/Sequences.tsx, useDevices.ts, App.tsx y styles.css; Carroza.Simulator.Tests/SequenceTests.cs e integración en Program.cs de esa suite; scripts contract-test.ps1 y fault-test.ps1.

La solución .sln preexistente sin seguimiento no se modificó ni eliminó. No se hicieron commits ni push.
