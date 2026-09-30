# Iteración 4 — Hidráulico, servo y fallas

Fecha de cierre de esta continuación: 2026-09-30.

## Estado previo a la interrupción

Se revisaron todos los cambios sin commit, los tres AGENTS.md y la documentación de docs/. Ya estaban implementados:

- hydraulic-1 (HYDRAULIC_ACTUATOR): desplazamiento progresivo 20%/s, EXTEND/RETRACT/STOP, límites y posición confirmada.
- servo-1 (SERVO): SET_POSITION 0..180, posición inicial 90°.
- SimulatorFaultPlan: cinco errores deterministas configurados al inicio, independientes del comportamiento normal.
- Contratos, validaciones, traducción HTTP y OpenAPI de nuevos comandos y errores.
- STOP ALL con instantánea opcional, conservando el 204 anterior.
- SVG del pistón/servo, errores por componente y estado offline en frontend.
- 52 pruebas deterministas de posiciones/fallas y un script HTTP de Iteración 4, todavía pendiente de ejecución completa.

Se conservó este trabajo. No se sustituyeron modelos, servicios, gateway ni controles que ya estaban correctos.

## Completado en esta continuación

- Prueba HTTP reproducible de las cinco fallas en proceso aislado (fault-test.ps1).
- Corrección de presentación: el error del banco permanece en su sección al operar otro componente.
- Ejecución completa de builds, suites, smoke, contratos, Swagger y búsquedas arquitectónicas.
- Verificación real en navegador de hidráulico, servo, STOP ALL, offline, timeout y comando desde Swagger.
- Actualización de PLAN, ARCHITECTURE, COMPONENTS y DESARROLLO con contratos y configuración reproducible.
- Restauración del servidor sin fallas al finalizar. No hubo commits.

## Componentes, contratos y decisiones

Se conserva Frontend → Controller → Application Service → IComponentGateway → SimulatorComponentGateway. Las cuatro rutas genéricas existentes atienden los seis componentes; no se agregaron endpoints particulares.

GET /api/devices agrega hydraulic-1 y servo-1. GET /api/devices/{id} devuelve posiciones y movimiento. POST /api/devices/{id}/commands admite los nuevos comandos y documenta 400/500/503/504. POST /api/devices/stop-all sigue devolviendo 204; con includeState=true devuelve 200 y todos los estados capturados bajo el mismo lock. El frontend utiliza esa instantánea.

La simulación hidráulica usa tiempo monotónico, avanza progresivamente y se detiene en límites 0/100. STOP congela la posición alcanzada. El servo acepta números finitos 0..180; STOP ALL conserva su ángulo. Esta decisión del simulador no afirma seguridad física de hardware real.

Las fallas se configuran por ID al iniciar; TIMEOUT devuelve inmediatamente el resultado de timeout sin ejecutar ni programar una ejecución posterior. DEVICE_OFFLINE devuelve online=false y rechaza comandos. Las consultas permanecen disponibles. STOP ALL evita la inyección de fallas para detener el simulador, pero no borra offline. Todos los detalles y comandos reproducibles están en DESARROLLO.md.

## Archivos creados en la Iteración 4

Ya existentes al retomar:

- backend/Carroza.Api/Domain/Commands/ComponentOperationException.cs
- backend/Carroza.Api/Infrastructure/Gateways/SimulatedHydraulic.cs
- backend/Carroza.Api/Infrastructure/Gateways/SimulatorFaultPlan.cs
- backend/Carroza.Simulator.Tests/PositionAndFaultTests.cs
- backend/iteration-4-test.ps1
- frontend/src/PositionControls.tsx

Creados ahora:

- backend/fault-test.ps1
- docs/ITERACION-4.md
- docs/iteracion-4-panel.png (evidencia de offline y operación independiente)

## Archivos modificados en la Iteración 4

- Application/IComponentGateway.cs y Application/Services/{IComponentService,ComponentService}.cs: parada con instantánea.
- Contracts/DTOs/DeviceDtos.cs y Controllers/DeviceMapping.cs: position, movement y límites opcionales.
- Controllers/DevicesController.cs: traducción de errores, Swagger y includeState.
- Domain/Commands/{CommandValidator,ComponentCommand}.cs y Domain/Components/{ComponentCatalog,ComponentState}.cs: tipos, comandos y validaciones.
- Infrastructure/Gateways/SimulatorComponentGateway.cs y Program.cs: integración y DI del plan de fallas.
- backend/Carroza.Simulator.Tests/Program.cs: ejecución de nuevas pruebas.
- backend/{smoke-test,contract-test}.ps1: catálogo de seis conservando verificaciones previas.
- frontend/src/{App,LightBank}.tsx, api.ts, useDevices.ts y styles.css: componentes visuales, errores e instantánea confirmada.
- docs/{PLAN,ARCHITECTURE,COMPONENTS,DESARROLLO}.md: documentación actualizada en esta continuación.

Las rutas abreviadas de Application, Contracts, Controllers, Domain e Infrastructure corresponden a backend/Carroza.Api/.

## Verificaciones realizadas ahora

| Verificación | Resultado |
| --- | --- |
| dotnet build backend/Carroza.Api | OK, 0 errores y 0 advertencias. |
| npm.cmd --prefix frontend run build | OK, TypeScript y Vite, 34 módulos. |
| dotnet run --project backend/Carroza.Simulator.Tests | OK: 34 comprobaciones generales, 75 del banco y 52 de posiciones/fallas; 60 operaciones concurrentes adicionales. |
| backend/smoke-test.ps1 | OK: seis componentes, luces, motor y parada global. |
| backend/contract-test.ps1 | OK: contrato JSON anterior, validaciones y códigos HTTP, 204 y ausencia de secuencias. |
| backend/iteration-3-test.ps1 | OK: Swagger, DTOs, efectos, evolución temporal y STOP ALL del banco. |
| backend/iteration-4-test.ps1 | OK: movimiento progresivo, STOP hidráulico, servo 0/90/180, parámetros inválidos, instantánea y OpenAPI. |
| backend/fault-test.ps1 | OK: DEVICE_OFFLINE/503, TIMEOUT/504, INVALID_COMMAND/400, INVALID_PARAMETER/400, INTERNAL_ERROR/500, sin mutación, componente sano operativo y STOP ALL. Proceso temporal cerrado. |
| Smoke a través de Vite (BaseUrl http://127.0.0.1:5173) | OK. |
| Tres búsquedas arquitectónicas de DESARROLLO.md | OK: sin Minimal APIs ni dependencias prohibidas. |
| git diff --check | OK; únicamente avisos de normalización LF/CRLF. |

Pruebas de dominio incluyen EXTEND/RETRACT/STOP, progresión, límites, servo 0/90/180, posición faltante/negativa/mayor a 180/NaN, errores sin mutación y parada con componentes en movimiento. No se eliminaron las pruebas anteriores.

## Revisión visual

A diferencia de ejecuciones anteriores, el navegador estuvo disponible. Primero fue necesario iniciar Vite, que estaba detenido.

Verificado en notebook mediante interacción y capturas:

- Pistón en extensión progresiva, retracción y STOP con posición congelada.
- Servo a 0° y 90° con cambio del brazo; también se observó el estado previo de 180°.
- STOP ALL confirma apagado del banco, hidráulico detenido y servo conservado.
- Luz frontal DEVICE OFFLINE con controles deshabilitados.
- Servo con TIMEOUT: petición de 180° rechazada y ángulo confirmado permanece en 90°.
- Tras ese error, luz lateral enciende con brillo y confirmación; error del servo permanece identificado.
- Swagger muestra endpoints, DTOs y errores; Try it out ejecutó SET_POSITION 90 con HTTP 200.

Se guardó iteracion-4-panel.png durante la prueba de fallas. El servidor fue reiniciado sin inyección después de esa captura.

## Pendientes y límites

La revisión visual es parcial: no se certifica el checklist completo de regresión de luces/motor/barridos ni todos los tamaños. Se intentó ajustar viewport móvil, pero la captura mantuvo las dimensiones de escritorio; quedan pendientes móvil/tablet y la comprobación completa de animaciones anteriores. Las pruebas HTTP y deterministas de esas funcionalidades sí pasaron. No se marca como aprobada ninguna de estas verificaciones pendientes.

No se incorporó hardware, firmware, GPIO, autenticación, base de datos ni secuencias complejas.
