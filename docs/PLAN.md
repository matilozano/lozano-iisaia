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

SVG propio del chasis, ruedas, iluminación, plataforma hidráulica y pieza servo. Comparte devices/execution/connected de useDevices con el panel; no incorpora consultas, comandos ni temporización de secuencias. El mapping de presentación y los componentes SVG están separados en frontend/src/carroza. Los contratos HTTP y puertos se conservan. Detalle y verificaciones en ITERACION-6.md. La Iteración 7 no se implementó dentro del alcance de Iteración 6.


Verificación de cierre de Iteración 6 repetida el 2026-10-06: builds, suites y regresión HTTP aprobados; revisión visual de controles, show y parada realizada. Pendiente la activación manual de movimiento reducido; cobertura CSS/automatizada aprobada. Ver ITERACION-6.md.

## Estado actual del Harness

Las iteraciones 1–6 están cerradas; sus reportes preservan resultados y pendientes específicos. El alcance de Iteración 7 fue aprobado posteriormente y se define a continuación. Las etapas conceptuales del README y las fases futuras de arquitectura no definen una próxima iteración.

Las reglas comunes de ejecución se consolidan en [AGENTS](../AGENTS.md), los AGENTS por área y [HARNESS](HARNESS.md). Este refinamiento documental no agrega una iteración funcional. El pendiente de revisión manual de movimiento reducido de Iteración 6 conserva su condición; esta tarea documental no lo declara resuelto.

## Iteración 7 — Gateway ESP32 y frontera de hardware

### Objetivo

Preparar la integración del sistema con un controlador ESP32 real,
demostrando que la arquitectura permite reemplazar el simulador mediante
otra implementación de `IComponentGateway` sin modificar Controllers,
Application Services ni frontend.

La Iteración 7 define e implementa la frontera de comunicación con ESP32,
pero no requiere todavía disponer del hardware físico.

### Alcance

- Implementar `Esp32ComponentGateway` como nueva implementación de
  `IComponentGateway`.

- Mantener `SimulatorComponentGateway` disponible.

- Permitir seleccionar el gateway mediante configuración de la aplicación,
  sin modificar código:

  - `Simulator`
  - `ESP32`

- Utilizar `docs/ESP32-PROTOCOL.md` como contrato de comunicación.

- Implementar un cliente/transporte desacoplado para la comunicación con
  ESP32.

- Separar claramente:

  Application
      ↓
  IComponentGateway
      ↓
  Esp32ComponentGateway
      ↓
  transporte ESP32

- El transporte debe poder sustituirse por una implementación simulada
  durante las pruebas.

- Traducir los comandos existentes del dominio al protocolo ESP32.

- Traducir las respuestas del ESP32 al estado utilizado actualmente por
  la aplicación.

- Mantener los identificadores y comportamientos existentes de los
  componentes.

### Configuración

La selección del gateway debe realizarse mediante configuración.

Ejemplo conceptual:

ComponentGateway:
  Mode: Simulator

o:

ComponentGateway:
  Mode: ESP32

La dirección/endpoint del ESP32 también debe provenir de configuración.

No hardcodear direcciones del dispositivo en Controllers, Services o
Gateway.

### Compatibilidad

Al utilizar:

Mode = Simulator

el sistema debe conservar el comportamiento existente de las Iteraciones
1–6.

El frontend no debe conocer qué gateway está activo.

Los Controllers tampoco deben seleccionar ni conocer la implementación
concreta.

### Comunicación

La comunicación con ESP32 debe respetar el contrato definido en
`docs/ESP32-PROTOCOL.md`.

Errores de transporte deben convertirse en errores coherentes con el
modelo existente, incluyendo cuando corresponda:

- DEVICE_OFFLINE
- TIMEOUT
- INVALID_COMMAND
- INVALID_PARAMETER
- INTERNAL_ERROR

No duplicar reglas de negocio dentro del transporte.

### Pruebas

Agregar pruebas que permitan verificar el gateway ESP32 sin hardware real.

Utilizar un transporte controlado/falso para comprobar como mínimo:

- envío correcto de comandos;
- serialización conforme al protocolo;
- interpretación de respuestas;
- actualización de estados;
- timeout;
- dispositivo offline;
- respuesta inválida;
- error interno;
- STOP ALL;
- ejecución de comandos utilizados por SHOW_FNE.

Verificar además que el modo Simulator continúa funcionando sin regresiones.

### Verificación arquitectónica

Demostrar que cambiar entre:

SimulatorComponentGateway

y:

Esp32ComponentGateway

no requiere modificar:

- Controllers;
- Application Services;
- frontend.

La selección debe resolverse en el Composition Root/configuración.

### Documentación

Crear:

`docs/ITERACION-7.md`

Documentar:

- arquitectura implementada;
- configuración del gateway;
- flujo de un comando;
- relación con `ESP32-PROTOCOL.md`;
- estrategia de pruebas sin hardware;
- verificaciones realizadas;
- limitaciones reales.

Actualizar `ARCHITECTURE.md`, `COMPONENTS.md`, `DESARROLLO.md` y
`ESP32-PROTOCOL.md` solamente cuando la implementación requiera reflejar
cambios reales.

### Fuera de alcance

Esta iteración NO incluye:

- firmware definitivo del ESP32;
- conexión obligatoria con hardware físico;
- asignación definitiva de GPIO;
- cableado eléctrico;
- drivers de potencia;
- MQTT;
- SignalR/WebSockets;
- base de datos;
- autenticación;
- cambios visuales del Digital Twin;
- nuevas funcionalidades de la carroza.

La integración física se realizará en una iteración posterior.

Estado de implementación de Iteración 7 (2026-10-07): frontera software implementada y pruebas automatizadas aprobadas, incluyendo regresión Simulator y transporte controlado ESP32. Revisión interactiva de Swagger pendiente por falla del navegador; hardware/firmware fuera de alcance. Resultados y limitaciones del entorno en [ITERACION-7](ITERACION-7.md).
