# Plan de implementación

## Estado actual — Iteración 1

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

El README describe el objetivo completo del proyecto, no funcionalidades incluidas en esta primera entrega. Swagger, banco de iluminación, secuencias, simulación de fallas, ESP32, firmware, GPIO, hidráulicos, servos, autenticación y base de datos no forman parte de esta implementación.

La documentación anterior marcaba como implementadas funciones de iteraciones posteriores cuyos archivos ya no existían al comenzar esta tarea. Este plan refleja el alcance de Iteración 1 solicitado para el repositorio actual.

No se realizan commits de Git en esta tarea.

## Iteración 2 — Arquitectura

Refactor acotado a la estructura del Harness: Domain/Components, Domain/Commands, Contracts/DTOs e Infrastructure/Gateways, con mapeo de transporte en Controllers. Se conserva el flujo existente mediante IComponentService e IComponentGateway, todos los contratos HTTP y el frontend de la Iteración 1. Sin nuevos componentes ni funcionalidades. Trabajo y verificaciones registrados en ITERACION-2.md.
