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

## Iteración 8 — Firmware ESP32 y controlador físico

### Objetivo

Implementar el firmware base del ESP32 que permita al controlador físico
comunicarse con el backend mediante el protocolo definido en
`docs/ESP32-PROTOCOL.md`.

La Iteración 8 debe convertir al ESP32 en una implementación real del
controlador esperado por `Esp32ComponentGateway`, manteniendo sin cambios
la arquitectura de aplicación desarrollada hasta la Iteración 7.

El objetivo principal es validar la frontera:

Frontend
    ↓
ASP.NET Core API
    ↓
Application Services
    ↓
IComponentGateway
    ↓
Esp32ComponentGateway
    ↓ HTTP
ESP32
    ↓
Hardware Abstraction

La disponibilidad de una placa física no debe ser obligatoria para poder
compilar y verificar la lógica principal del firmware.

---

### 1. Proyecto de firmware

Crear un proyecto independiente para el firmware ESP32.

Ubicación sugerida:

firmware/
    esp32-carroza/

El firmware debe mantenerse separado del backend y frontend.

Documentar:

- plataforma utilizada;
- estructura del proyecto;
- procedimiento de compilación;
- configuración necesaria;
- procedimiento para cargar el firmware cuando exista hardware disponible.

No introducir dependencias innecesarias con .NET o React.

---

### 2. Protocolo ESP32 v1

Implementar el lado ESP32 del contrato definido en:

`docs/ESP32-PROTOCOL.md`

Soportar las operaciones existentes:

- GET_STATE;
- EXECUTE;
- STOP_ALL.

Respetar:

- versión del protocolo;
- UUID/correlationId;
- componentId;
- command;
- parámetros;
- respuesta;
- códigos de error;
- snapshot confirmado.

No crear un protocolo alternativo si el contrato existente resulta
suficiente.

Si durante la implementación se detecta una inconsistencia real en el
protocolo, documentarla y realizar el cambio mínimo compatible necesario.

---

### 3. Servidor HTTP del ESP32

Implementar el endpoint HTTP requerido por `HttpEsp32Transport`.

El firmware debe:

1. recibir el mensaje;
2. validar el envelope;
3. validar versión;
4. validar correlationId;
5. identificar operación;
6. validar componente y comando;
7. ejecutar mediante la abstracción de hardware;
8. obtener el estado resultante;
9. devolver una respuesta conforme al protocolo.

No devolver éxito antes de que el comando haya sido aceptado por el
controlador.

---

### 4. Command Dispatcher

Separar el protocolo HTTP de la ejecución de componentes.

Arquitectura conceptual:

HTTP Server
    ↓
Protocol Parser
    ↓
Command Dispatcher
    ↓
Component Controller
    ↓
Hardware Abstraction

El servidor HTTP no debe contener directamente lógica específica de:

- luces;
- motor;
- hidráulico;
- servo;
- banco de LEDs.

El dispatcher debe resolver el componente y delegar la operación.

---

### 5. Estado del controlador

Mantener un estado explícito de los componentes controlados por ESP32.

Como mínimo representar los componentes existentes del sistema:

- luces frontales;
- luces laterales;
- banco de LEDs;
- motor;
- actuador hidráulico;
- servo.

El estado informado al backend debe representar el último estado
confirmado por el controlador.

No inventar estados que no existan en el modelo actual.

---

### 6. Hardware Abstraction Layer

Crear una capa de abstracción entre la lógica del firmware y los GPIO.

Conceptualmente:

Component Controller
        ↓
IHardware / Hardware Abstraction
        ├── SimulatedHardware
        └── Esp32Hardware

Los nombres concretos pueden adaptarse a las convenciones de la plataforma.

La lógica de protocolo y despacho no debe depender directamente de números
de GPIO.

Esto debe permitir probar la mayor parte del firmware sin conectar
hardware físico.

---

### 7. Implementación simulada del hardware

Incluir una implementación simulada/fake de la abstracción de hardware
para pruebas.

Debe permitir comprobar:

- encendido/apagado de luces;
- estados del banco;
- dirección y velocidad del motor;
- posición hidráulica;
- posición del servo;
- STOP_ALL.

Esta simulación pertenece exclusivamente al firmware y a sus pruebas.

No reemplaza `SimulatorComponentGateway` del backend.

---

### 8. STOP_ALL

STOP_ALL es una operación prioritaria del controlador.

Debe llevar los componentes a un estado seguro compatible con el contrato
existente.

Como mínimo:

- detener motor;
- detener movimiento hidráulico;
- detener efectos del banco;
- apagar las luces que corresponda según la política existente;
- impedir que una operación pendiente vuelva a activar componentes después
  de la parada.

El estado resultante debe devolverse únicamente después de aplicar la
parada.

El servo debe conservar el comportamiento seguro definido por el contrato
actual y no asumir una posición nueva si no está especificada.

---

### 9. Watchdog

Implementar un mecanismo de watchdog de comunicación/control.

Si el controlador deja de recibir comunicación válida durante un intervalo
configurable, debe poder aplicar una política de seguridad.

La política debe priorizar:

- motor detenido;
- hidráulico detenido;
- efectos activos detenidos;
- ausencia de reactivación automática.

El timeout debe estar centralizado/configurable.

No dispersar valores mágicos por el firmware.

Documentar claramente qué componentes modifica el watchdog.

---

### 10. Mensajes demorados y comandos obsoletos

Evitar que un comando recibido con demora pueda revertir un STOP_ALL
posterior.

Definir una estrategia simple y determinista compatible con el protocolo
actual.

Puede utilizar información de correlación/orden disponible en el contrato,
sin introducir infraestructura distribuida innecesaria.

La propiedad requerida es:

Una vez confirmado STOP_ALL, un comando anterior que llegue posteriormente
no debe reactivar componentes.

Documentar la estrategia utilizada y sus limitaciones.

---

### 11. Validaciones

El firmware debe rechazar de manera controlada:

- versión de protocolo incompatible;
- operación desconocida;
- componentId desconocido;
- comando desconocido;
- parámetros inválidos;
- valores fuera de rango;
- mensajes incompletos;
- JSON inválido cuando corresponda.

Utilizar los códigos de error definidos por el protocolo existente.

No responder éxito ante comandos inválidos.

---

### 12. Concurrencia

Evitar ejecución concurrente incoherente sobre el estado físico.

La recepción HTTP y la ejecución de comandos deben preservar consistencia
del estado.

STOP_ALL debe tener prioridad semántica sobre operaciones normales.

No implementar una arquitectura distribuida compleja; utilizar los
mecanismos de sincronización apropiados para ESP32.

---

### 13. SHOW_FNE

No implementar el motor de secuencias SHOW_FNE dentro del ESP32.

SHOW_FNE continúa perteneciendo al backend.

El ESP32 únicamente debe ejecutar los comandos individuales recibidos.

Debe ser posible ejecutar desde backend:

SHOW_FNE
    ↓
Sequence Service
    ↓
Esp32ComponentGateway
    ↓
ESP32

sin que el firmware conozca la existencia de SHOW_FNE.

---

### 14. Compatibilidad con el backend

La Iteración 8 no debe requerir cambios funcionales en:

- frontend;
- Controllers;
- Application Services;
- motor de secuencias;
- Digital Twin.

El backend en modo:

ComponentGateway:Simulator

debe continuar funcionando como hasta ahora.

El modo:

ComponentGateway:ESP32

debe ser compatible con el firmware desarrollado en esta iteración.

---

### 15. Configuración de red

La configuración específica de red del ESP32 debe quedar aislada de la
lógica de componentes.

No almacenar credenciales reales de Wi-Fi en el repositorio.

Proveer configuración de ejemplo o mecanismo documentado para establecer:

- SSID;
- credenciales;
- dirección/puerto;
- parámetros necesarios del controlador.

Los secretos reales deben permanecer fuera del control de versiones.

---

### 16. Pruebas

Agregar pruebas automatizadas de la lógica que pueda ejecutarse sin placa
física.

Como mínimo verificar:

- parsing de mensajes;
- validación de protocolo;
- GET_STATE;
- EXECUTE;
- STOP_ALL;
- luces;
- banco de LEDs;
- motor;
- hidráulico;
- servo;
- límites de parámetros;
- errores;
- watchdog;
- rechazo de mensajes obsoletos;
- consistencia de estado.

Verificar además la compatibilidad de mensajes entre el contrato producido
por backend y el esperado por firmware.

---

### 17. Integración sin hardware

Cuando no exista una placa ESP32 disponible, debe ser posible verificar:

Backend
    ↓
Esp32ComponentGateway
    ↓
contrato HTTP
    ↓
controlador/firmware verificable en entorno de desarrollo

sin declarar que se realizó una integración física.

Las pruebas sin hardware deben distinguirse explícitamente de las pruebas
realizadas sobre una placa real.

---

### 18. Integración física opcional

Si durante esta iteración existe una placa ESP32 disponible, se podrá
realizar una prueba adicional de comunicación real.

La prueba física no es requisito para considerar completada la Iteración 8.

Si se realiza, documentar separadamente:

- placa utilizada;
- transporte;
- conectividad;
- comandos probados;
- resultados;
- limitaciones.

No conectar motores, actuadores o cargas de potencia directamente a GPIO.

---

### 19. Documentación

Crear:

`docs/ITERACION-8.md`

Documentar como mínimo:

- arquitectura del firmware;
- estructura del proyecto;
- flujo HTTP → protocolo → dispatcher → hardware;
- estrategia de estado;
- STOP_ALL;
- watchdog;
- protección contra mensajes obsoletos;
- configuración;
- pruebas realizadas;
- diferencia entre pruebas simuladas y físicas;
- limitaciones y pendientes reales.

Actualizar cuando corresponda:

- `docs/ARCHITECTURE.md`;
- `docs/COMPONENTS.md`;
- `docs/DESARROLLO.md`;
- `docs/ESP32-PROTOCOL.md`.

No modificar retrospectivamente los informes históricos.

---

### 20. Definition of Done específica

La Iteración 8 se considera terminada cuando:

- el firmware compila en el entorno disponible;
- el protocolo v1 está implementado;
- GET_STATE funciona;
- EXECUTE funciona;
- STOP_ALL funciona;
- existe separación entre protocolo y hardware;
- existe una abstracción de hardware verificable sin placa;
- watchdog está implementado y probado;
- comandos obsoletos no pueden reactivar componentes después de STOP_ALL;
- las pruebas automatizadas aplicables pasan;
- SimulatorComponentGateway continúa sin regresiones;
- la documentación refleja únicamente verificaciones realmente ejecutadas.

La integración física solo se considera APROBADA si fue ejecutada sobre
hardware real.

---

### Fuera de alcance

Esta iteración NO incluye:

- electrónica de potencia definitiva;
- conexión directa de motores de potencia;
- relés/contactores definitivos;
- drivers finales;
- diseño de PCB;
- cableado definitivo de la carroza;
- asignación física definitiva de todos los GPIO;
- MQTT;
- SignalR/WebSockets;
- base de datos;
- autenticación;
- cambios visuales del frontend;
- editor de secuencias;
- migrar SHOW_FNE al ESP32.

La integración completa con actuadores y electrónica real queda para una
iteración posterior.