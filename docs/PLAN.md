# Plan de implementación

## Base funcional — Iteración 1

Primera versión funcional del simulador, sin hardware físico:

- Backend ASP.NET Core .NET 9 con Controllers.
- Frontend React + TypeScript + Vite que consume la API real.
- Dos sectores de iluminación: `front-lights` y `side-lights`, con ON/OFF.
- Motor `main-motor`: START/STOP, dirección `forward`/`reverse` y velocidad 0..100.
- Consulta de estados y parada general.

Flujo: Frontend → Controllers → IComponentService → IComponentGateway → SimulatorComponentGateway.

El catálogo y las reglas de validación están en Domain. Application coordina las operaciones; Infrastructure conserva el estado simulado en memoria. Los Controllers solo reciben solicitudes y traducen resultados a HTTP. `Program.cs` contiene configuración, DI y arranque.

## Decisiones de la Iteración 1

- El gateway se selecciona mediante `ComponentGateway:Mode`. Solo `Simulator` está implementado; otro valor detiene el arranque con un error explícito.
- El estado inicial tiene ambas luces apagadas y el motor detenido, con velocidad cero. Reiniciar el backend reinicia el estado.
- `start` permite actualizar dirección y velocidad de un motor en marcha. Si se omiten, usa adelante y 50%. Velocidad 0 significa motor habilitado sin giro; `stop` lo deja detenido a 0%.
- Las respuestas de comandos incluyen el estado completo confirmado y fecha UTC. Los errores no alteran el estado.
- STOP ALL apaga ambas luces y detiene el motor bajo el mismo bloqueo del simulador. Una petición de parada aceptada continúa aunque su cliente se desconecte. No es un enclavamiento: una orden nueva posterior puede activar un componente.
- Las rutas públicas conservan la terminología `/api/devices` del README; el contrato interno usa `IComponentGateway`, como exige AGENTS.md.
- El frontend consulta cada segundo, muestra el envío y conserva el último estado ante errores. Las animaciones solo representan estados confirmados.
- Vite redirige `/api` al backend en localhost:5080.

## Iteraciones posteriores — pendientes

El README describe el objetivo completo del proyecto. Swagger y banco de iluminación se incorporaron en Iteración 3; hidráulico, servo y fallas en Iteración 4; secuencias coordinadas en Iteración 5. ESP32 real, firmware, GPIO, autenticación y base de datos continúan pendientes.

Las decisiones de Iteración 1 anteriores son históricas; las secciones siguientes registran las ampliaciones implementadas.

No se realizan commits de Git en esta tarea.

## Iteración 5 — Secuencias coordinadas

Modelo genérico en Domain, ejecución temporal en Application mediante el mismo ComponentCommandExecutor usado por comandos manuales. SHOW_FNE tiene 14 pasos y dura aproximadamente 19 segundos. OperationCoordinator serializa las mutaciones, bloquea comandos manuales durante RUNNING y da prioridad a STOP ALL invalidando solicitudes anteriores. Cancelación y fallas aplican parada segura; no hay pasos posteriores. UI con polling, progreso e historial MANUAL/SEQUENCE. Ver ITERACION-5.md y DESARROLLO.md. El Digital Twin se incorpora posteriormente en Iteración 6.

## Iteración 2 — Arquitectura

Refactor acotado a la estructura del Harness: Domain/Components, Domain/Commands, Contracts/DTOs e Infrastructure/Gateways, con mapeo de transporte en Controllers. Se conserva el flujo existente mediante IComponentService e IComponentGateway, todos los contratos HTTP y el frontend de la Iteración 1. Sin nuevos componentes ni funcionalidades. Trabajo y verificaciones registrados en ITERACION-2.md.

## Iteración 3 — Simulador visual y Swagger

Swagger/OpenAPI en Development; banco lógico de ocho canales con efectos temporizados en SimulatorComponentGateway; panel visual con estados confirmados, control directo, sección de sistema e historial de sesión. Se conserva la arquitectura y el contrato de los componentes anteriores. Ver ITERACION-3.md para archivos, decisiones y verificación, y DESARROLLO.md para ejecución y contrato actualizado.
## Iteración 4

Implementados hydraulic-1 (HYDRAULIC_ACTUATOR), servo-1 (SERVO) y fallas reproducibles por configuración del simulador. STOP ALL ofrece una instantánea opcional sin romper su respuesta 204 histórica. Se mantienen servicios y gateway genéricos; no hay rutas específicas por dispositivo. Ver ITERACION-4.md y DESARROLLO.md para alcance, contrato, configuración y pruebas. ESP32, firmware, GPIO, autenticación, persistencia y secuencias complejas continúan fuera de alcance.

## Iteración 6 — Vista Carroza / Digital Twin

SVG propio del chasis, ruedas, iluminación, plataforma hidráulica y pieza servo. Comparte devices/execution/connected de useDevices con el panel; no incorpora consultas, comandos ni temporización de secuencias. El mapping de presentación y los componentes SVG están separados en frontend/src/carroza. Los contratos HTTP y puertos se conservan. Detalle y verificaciones en ITERACION-6.md. La Iteración 7 no se implementa.


Verificación de cierre de Iteración 6 repetida el 2026-10-06: builds, suites y regresión HTTP aprobados; revisión visual de controles, show y parada realizada. Pendiente la activación manual de movimiento reducido; cobertura CSS/automatizada aprobada. Ver ITERACION-6.md.
