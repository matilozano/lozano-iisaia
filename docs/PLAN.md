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

## Iteración 9 — Dashboard operativo, telemetría y protecciones simuladas

### Objetivo

Evolucionar el sistema de control de la carroza FNE mediante un dashboard operativo que permita visualizar, controlar y supervisar los componentes desde una interfaz centralizada, incorporando telemetría simulada, detección de fallas y protecciones automáticas.

La implementación debe funcionar íntegramente sin hardware físico, utilizando los gateways y la arquitectura establecidos en las iteraciones anteriores.

### 1. Rediseño del dashboard

Reorganizar el frontend para mejorar la visibilidad y accesibilidad de las funciones existentes.

**Distribución propuesta:**

- Encabezado compacto con nombre del sistema, modo de gateway, conectividad y estado general.
- Indicadores superiores de tensión, corriente, temperatura y velocidad.
- Digital Twin como elemento visual principal.
- Panel de controles agrupados por componente: luces, motor, banco LED, hidráulico y servo.
- Panel de secuencias con estado, progreso, cancelación e historial.
- Panel de alertas, fallas y protecciones.
- Control STOP ALL permanentemente accesible.

**Requisitos:**

- Priorizar la visualización del Digital Twin.
- Evitar desplazamiento horizontal.
- Minimizar el desplazamiento vertical en pantallas de escritorio.
- Adaptar la distribución a escritorio, tablet y dispositivos móviles.
- Conservar todas las acciones y funcionalidades existentes.
- No duplicar controles que puedan provocar comandos contradictorios.
- Mostrar estados de carga, desconexión, error y operación en curso.
- Utilizar indicadores visuales comprensibles, sin depender exclusivamente del color.
- Respetar `prefers-reduced-motion`.
- Mantener STOP ALL accesible mediante una ubicación persistente que no tape otros controles.
- No generar datos operativos ficticios desde el frontend.

### 2. Telemetría simulada

Incorporar un modelo de telemetría que represente las condiciones operativas de la carroza.

**Variables mínimas:**

- Tensión de alimentación, expresada en voltios.
- Corriente consumida, expresada en amperios.
- Temperatura simulada del controlador o motor, expresada en °C.
- Velocidad estimada del motor, expresada en porcentaje.
- Estado de conectividad y antigüedad de la última lectura.

La telemetría debe producirse desde la capa de simulación correspondiente, mediante una evolución determinista o controlable para las pruebas.

No debe calcularse arbitrariamente en React.

Se deben definir DTOs, unidades, límites, timestamps y reglas de actualización, manteniendo compatibilidad con los contratos públicos existentes.

El gateway Simulator y el firmware con HAL simulada deben ofrecer un comportamiento coherente para las variables comunes.

### 3. Fallas simuladas

Implementar mecanismos reproducibles para simular:

- Sobretemperatura.
- Sobrecorriente.
- Tensión fuera del rango operativo.
- Pérdida de comunicación.
- Lecturas de sensores inválidas o desactualizadas.

Las fallas deben poder activarse y restablecerse de manera controlada, sin depender de hardware físico.

La inyección de fallas debe quedar limitada al entorno de simulación y no introducir capacidades peligrosas o ambiguas en una futura integración física.

### 4. Protecciones automáticas

Definir reglas explícitas para detectar condiciones críticas y llevar los componentes afectados a estados seguros.

Requisitos:

- Registrar la causa y el momento de activación.
- Aplicar una política de parada coherente con STOP ALL.
- Impedir la reactivación de componentes mientras permanezca una falla crítica activa.
- Mantener las barreras contra comandos obsoletos incorporadas en Iteración 8.
- No reanudar automáticamente secuencias ni movimientos después de una falla.
- Diferenciar fallas activas, reconocidas y resueltas.
- Definir condiciones verificables para el restablecimiento.
- No presentar como segura una operación cuya confirmación no se haya recibido.

Las protecciones deben implementarse en las capas responsables del control, nunca exclusivamente en la interfaz.

### 5. Integración con Digital Twin

Extender la visualización existente para mostrar:

- Estado real confirmado de cada componente.
- Indicadores de telemetría.
- Alertas operativas.
- Protecciones activas.
- Estado de la secuencia SHOW_FNE.
- Pérdida de comunicación y datos desactualizados.

Las animaciones deben continuar derivándose de estados confirmados por backend.

No deben introducirse movimientos visuales que representen acciones no confirmadas.

### 6. Arquitectura y compatibilidad

Respetar el flujo:

Frontend → API REST → Application → IComponentGateway → Simulator / ESP32 → HAL.

Condiciones:

- Mantener Controllers delgados.
- Preservar responsabilidades de Application, Infrastructure y firmware.
- Mantener Program.cs como Composition Root.
- No duplicar reglas de protección entre frontend y backend.
- Extender contratos mediante mecanismos compatibles y documentados.
- Mantener SHOW_FNE coordinado desde backend.
- Conservar STOP ALL como operación prioritaria.
- No introducir dependencias físicas obligatorias.
- No incorporar bases de datos, MQTT, SignalR ni servicios externos sin justificación y aprobación.
- No modificar funcionalidades de iteraciones anteriores fuera de lo estrictamente necesario para la integración.

### 7. Pruebas y verificaciones

Incorporar pruebas automatizadas que cubran:

**Backend y simulación**

- Lecturas de telemetría con unidades y rangos válidos.
- Evolución reproducible de las variables.
- Activación y restablecimiento de fallas.
- Protección ante condiciones críticas.
- Rechazo de comandos durante fallas activas.
- STOP ALL y cancelación de SHOW_FNE.
- Ausencia de reactivación posterior a una parada.
- Lecturas desactualizadas y pérdida de comunicación.
- Compatibilidad con ambos gateways.

**Firmware**

- Telemetría con HAL simulada.
- Propagación de fallas.
- Barreras de seguridad y comandos obsoletos.
- Compatibilidad con el protocolo versionado.

**Frontend**

- Visualización correcta de estados y telemetría.
- Estados de carga, error y desconexión.
- STOP ALL accesible.
- Coherencia entre panel y Digital Twin.
- Ausencia de valores inventados durante errores de comunicación.
- Adaptación responsive y ausencia de desbordamiento horizontal.
- Accesibilidad básica y reducción de movimiento.

Ejecutar nuevamente las suites y pruebas de regresión existentes.

Distinguir las verificaciones automatizadas de las inspecciones visuales manuales.

### 8. Documentación obligatoria

Crear:

`docs/ITERACION-9.md`

Registrar:

- Alcance implementado.
- Archivos creados y modificados.
- Decisiones arquitectónicas.
- Contratos de telemetría.
- Modelo de fallas y protecciones.
- Resultados reales de builds y pruebas.
- Evidencia de integración con ambos gateways.
- Revisión visual y limitaciones.
- Pendientes y trabajo fuera de alcance.

Actualizar cuando corresponda:

- `docs/ARCHITECTURE.md`
- `docs/COMPONENTS.md`
- `docs/ESP32-PROTOCOL.md`
- `docs/DESARROLLO.md`
- `README.md`

No alterar los informes históricos de iteraciones anteriores.

### 9. Fuera de alcance

- Conexión con ESP32 físico.
- Sensores eléctricos reales.
- Electrónica de potencia.
- Calibración de motores o actuadores reales.
- Autenticación o administración de usuarios.
- Persistencia histórica en base de datos.
- Integraciones con servicios externos.
- Desarrollo de una aplicación móvil independiente.
- Implementación de un servidor MCP.

### 10. Criterios de aceptación

La Iteración 9 se considerará funcionalmente completada cuando:

1. El dashboard permita visualizar y operar todos los componentes existentes.
2. El Digital Twin tenga una ubicación destacada y refleje los estados confirmados.
3. La telemetría sea suministrada por backend/simulación y no inventada por el frontend.
4. Las fallas puedan reproducirse y verificarse mediante pruebas.
5. Las protecciones impidan movimientos o reactivaciones indebidas.
6. STOP ALL permanezca accesible y conserve su comportamiento prioritario.
7. SHOW_FNE funcione correctamente en condiciones normales y se cancele ante condiciones críticas.
8. Los modos Simulator y ESP32 con HAL simulada mantengan compatibilidad.
9. Las pruebas automatizadas y de regresión resulten satisfactorias.
10. Se documente el resultado de la revisión visual en escritorio y móvil.
11. No se introduzcan regresiones en las funcionalidades de iteraciones anteriores.
12. Se genere `docs/ITERACION-9.md` con resultados verificables.

Toda verificación no ejecutada debe registrarse como PENDIENTE, sin declararse aprobada.

### 11. Restricciones del Harness

- Inspeccionar el estado del repositorio antes de modificar archivos.
- Respetar AGENTS.md y las instrucciones específicas de backend/frontend.
- Trabajar exclusivamente sobre la rama `tpfinal`.
- No descartar cambios existentes.
- Mantener compatibilidad con el código y los contratos anteriores.
- No avanzar a Iteración 10.
- No realizar commit ni push.
- Aplicar la Definition of Done y documentar resultados reales.
- Ante interrupciones de cuota, preservar el trabajo y seguir el protocolo de continuidad.

Estado verificado de Iteración 9 (2026-10-11): dashboard, telemetría y protecciones
implementados; builds, suites y regresión HTTP aprobados en Simulator y ESP32 con
HAL simulada. Revisión visual posterior aprobada en escritorio/móvil, SHOW_FNE,
STOP ALL y fallas controladas. Pendientes visuales: activación de movimiento
reducido y caída real del transporte; no se declara cierre visual completo.
Resultados y limitaciones en [ITERACION-9](ITERACION-9.md).

## Iteración 10 — Mejoras visuales y experiencia de usuario

### Objetivo

Optimizar la interfaz del sistema de control de la carroza FNE para ofrecer una experiencia visual moderna, clara, compacta y responsive, conservando íntegramente las funcionalidades y contratos implementados hasta la Iteración 9.

La iteración se concentra exclusivamente en presentación, navegación, accesibilidad y usabilidad del frontend.

### 1. Navegación mediante tabs

Reemplazar la navegación actual por tres pestañas funcionales:

- **Vista general:** Digital Twin, telemetría, conectividad y alertas.
- **Componentes:** luces, motor, banco LED, hidráulico y servo.
- **Secuencias e historial:** SHOW_FNE, progreso, cancelación e historial de operaciones.

Requisitos:

- Cambiar entre pestañas sin recargar la página ni navegar mediante anclas.
- Mantener claramente identificada la pestaña activa.
- Utilizar roles ARIA, navegación por teclado y foco visible.
- Conservar un único mecanismo de polling y evitar solicitudes duplicadas.
- Mantener los estados operativos sincronizados al cambiar de pestaña.
- No perder funcionalidades existentes.

### 2. Reorganización del dashboard

Implementar una distribución visual jerárquica:

1. Encabezado con nombre, modo del gateway y estado de conexión.
2. Navegación mediante tabs.
3. Contenido correspondiente a la pestaña activa.
4. Área persistente de seguridad con STOP ALL.

En Vista general, priorizar el Digital Twin y las métricas principales.

En Componentes, organizar controles mediante tarjetas compactas, agrupadas por función.

En Secuencias e historial, separar claramente el control de SHOW_FNE, su progreso y los eventos registrados.

Evitar tarjetas excesivamente altas, espacios vacíos innecesarios y duplicación de información.

### 3. Digital Twin

Mejorar su presentación visual sin modificar su lógica operativa:

- Darle mayor protagonismo.
- Ajustar dimensiones y proporciones.
- Mejorar etiquetas, indicadores y contraste.
- Mantener visibles los estados de luces, motor, banco LED, hidráulico y servo.
- Conservar animaciones vinculadas exclusivamente a estados confirmados por backend.
- Respetar reducción de movimiento.
- Evitar que se recorte o desborde en pantallas pequeñas.

### 4. Telemetría y alertas

Mejorar la representación de:

- Tensión.
- Corriente.
- Temperatura.
- Velocidad.
- Conectividad.
- Calidad y antigüedad de lectura.
- Protecciones y alertas activas.

Utilizar tarjetas compactas y estados visuales diferenciados.

No representar valores `null`, inválidos, desactualizados o sin conexión como mediciones normales.

Conservar las unidades y el origen real de los datos.

### 5. Controles operativos

Reorganizar visualmente:

- Luces.
- Motor.
- Banco LED.
- Hidráulico.
- Servo.

Requisitos:

- Identificar claramente acciones y estados.
- Mostrar cuándo un control está deshabilitado o bloqueado por protección.
- Incorporar feedback visual durante operaciones pendientes.
- Evitar botones demasiado pequeños.
- Mantener consistencia de colores, iconografía y tamaños.
- No modificar las reglas de ejecución de comandos.

### 6. STOP ALL

Mantener STOP ALL permanentemente accesible en las tres pestañas.

Requisitos:

- Posición persistente sin tapar contenido.
- Contraste visual adecuado.
- Funcionamiento en escritorio y móvil.
- Accesibilidad mediante teclado.
- No depender de la pestaña activa.
- Conservar exactamente el comportamiento existente de parada y confirmación.

### 7. Diseño responsive

Verificar como mínimo:

- Escritorio: 1440 × 900.
- Notebook: 1366 × 768.
- Tablet: 768 × 1024.
- Móvil: 390 × 844.

Evitar desplazamiento horizontal.

En escritorio, priorizar una distribución que permita observar la carroza y sus indicadores principales con mínimo desplazamiento vertical.

En móvil, adaptar tabs, tarjetas y controles sin perder accesibilidad.

### 8. Accesibilidad y experiencia de usuario

- Navegación por teclado.
- Roles y etiquetas accesibles.
- Indicadores de foco.
- Contraste adecuado.
- Estados de carga, error y desconexión.
- Respeto por `prefers-reduced-motion`.
- Mensajes comprensibles y consistentes.
- No depender exclusivamente del color para indicar estados.

### 9. Arquitectura y restricciones

- Trabajar exclusivamente sobre frontend y documentación asociada.
- No modificar backend, firmware, DTOs ni endpoints.
- No introducir telemetría ficticia ni simulación local en React.
- Mantener el backend como fuente de verdad.
- Preservar el mecanismo de polling existente.
- Reutilizar componentes y estilos cuando corresponda.
- Evitar dependencias adicionales salvo necesidad justificada.
- No incorporar nuevas funcionalidades operativas.
- Mantener compatibilidad con Simulator y ESP32 con HAL simulada.

### 10. Pruebas y criterios de aceptación

Verificar:

1. Funcionamiento de las tres pestañas.
2. Conservación de todas las acciones existentes.
3. STOP ALL visible y operativo desde cualquier pestaña.
4. Sin duplicación de polling.
5. Digital Twin y telemetría sincronizados.
6. Alertas y protecciones correctamente representadas.
7. Sin overflow horizontal en las resoluciones definidas.
8. Navegación accesible por teclado.
9. Estados de error y desconexión correctamente visibles.
10. Builds y tests frontend aprobados.
11. Regresiones relevantes del sistema aprobadas.
12. Revisión visual real en escritorio y móvil, documentando cualquier limitación.

### 11. Documentación

Crear `docs/ITERACION-10.md` con:

- Cambios realizados.
- Componentes reorganizados.
- Decisiones visuales.
- Archivos modificados.
- Resultados de compilación y pruebas.
- Evidencia de revisión visual.
- Pendientes y limitaciones.

Actualizar la documentación técnica únicamente cuando corresponda.

### 12. Harness y Definition of Done

- Seguir AGENTS.md y docs/HARNESS.md.
- Inspeccionar el repositorio antes de editar.
- Preservar cambios existentes.
- Trabajar en la rama `tpfinal`.
- No modificar iteraciones anteriores.
- No avanzar a Iteración 11.
- No hacer commit ni push.
- Registrar las verificaciones como APROBADA, FALLÓ o PENDIENTE según evidencia real.

Estado de Iteración 10 (2026-10-11): navegación por tres pestañas, presentación
compacta y responsive implementadas, sin cambios de API/backend/firmware.
Builds, suites y regresión HTTP en Simulator y ESP32/HAL simulada aprobados.
Revisión de las tres pestañas en las cuatro resoluciones, SHOW_FNE, STOP ALL,
protecciones y caída real de API realizada. Pendiente: activación visual de
movimiento reducido; no se declara cierre visual completo. Evidencia y límites
en [ITERACION-10](ITERACION-10.md). No se inició Iteración 11.

### 13. Digital Twin tridimensional

Evolucionar el Digital Twin existente hacia una representación 3D interactiva de la carroza, utilizando preferentemente React Three Fiber y Three.js, siempre que su incorporación sea compatible con la arquitectura y el rendimiento del proyecto.

**Requisitos:**

- Modelar en 3D la estructura, ruedas, luces, banco LED, motor, hidráulico y servo.
- Permitir rotación, zoom y restablecimiento de cámara.
- Sincronizar movimientos y efectos exclusivamente con estados confirmados por backend.
- No generar estados operativos ni ejecutar comandos desde el motor gráfico.
- Mantener el Digital Twin como componente de visualización, separado de la lógica de negocio.
- Reutilizar el mecanismo de polling existente.
- Mostrar visualmente alertas y protecciones activas.
- Mantener compatibilidad con Simulator y ESP32 con HAL simulada.
- Proporcionar alternativa 2D ante ausencia de WebGL o fallas de renderizado.
- Respetar `prefers-reduced-motion`.
- Evitar pérdida de contexto o reinicialización innecesaria al cambiar de tabs.
- Adaptar la visualización a escritorio y móvil.
- Evitar modelos externos pesados o dependencias innecesarias.

**Verificaciones:**

- Rotación, zoom y restablecimiento de cámara.
- Coherencia entre estados backend y animaciones 3D.
- Funcionamiento durante SHOW_FNE.
- Comportamiento durante STOP ALL.
- Representación de fallas y desconexión.
- Rendimiento y ausencia de errores de consola.
- Alternativa 2D y reducción de movimiento.
- Pruebas automatizadas pertinentes y revisión visual real.

Registrar las decisiones técnicas y resultados en `docs/ITERACION-10.md`.
### 14. Vista compacta de componentes

Reorganizar la pestaña Componentes mediante una grilla responsive de tarjetas compactas.

**Requisitos:**

- Mostrar cinco componentes por fila en escritorio, siempre que el ancho disponible lo permita.
- Utilizar tres columnas en pantallas intermedias, dos en tablet vertical y una en móvil.
- Reducir márgenes, espacios y alturas innecesarias.
- Mantener títulos, iconos, estados y controles claramente identificables.
- Conservar todas las funcionalidades existentes.
- Utilizar controles compactos, accesibles y con áreas de interacción adecuadas.
- Permitir expandir los controles avanzados cuando el espacio sea insuficiente.
- Evitar desbordamiento horizontal, textos cortados y superposición de elementos.
- Mostrar estados de bloqueo, error y protección.
- No modificar la lógica operativa, los contratos ni el polling.
- Mantener STOP ALL permanentemente visible fuera de las tarjetas.

**Criterios de aceptación:**

1. Cinco tarjetas por fila en escritorio de 1440 px, si el contenedor dispone del ancho necesario.
2. Distribución responsive en notebook, tablet y móvil.
3. Todos los componentes y acciones existentes siguen disponibles.
4. Los controles son legibles y utilizables.
5. No existe desplazamiento horizontal.
6. La revisión visual confirma la distribución y la usabilidad.
7. Los tests y el build frontend resultan satisfactorios.

Documentar las verificaciones en `docs/ITERACION-10.md`.
### 15. Reordenamiento de componentes mediante Drag & Drop

Incorporar la posibilidad de reorganizar libremente las tarjetas de la pestaña Componentes mediante arrastrar y soltar.

**Requisitos funcionales:**

- Permitir arrastrar cualquier tarjeta de componente hacia otra posición de la grilla.
- Reordenar automáticamente las tarjetas al soltarlas.
- Mostrar feedback visual durante el arrastre.
- Utilizar un área de agarre específica para evitar conflictos con los controles operativos.
- Mantener cinco tarjetas por fila en escritorio y la distribución responsive definida.
- Conservar el orden personalizado al cambiar de pestaña y recargar la aplicación.
- Persistir el orden en `localStorage`, con una clave versionada.
- Permitir restablecer el orden predeterminado.
- Incorporar soporte de reordenamiento mediante teclado.
- Conservar todos los controles, estados y funcionalidades de cada tarjeta.
- Evitar que el reordenamiento genere comandos hacia el backend.
- No duplicar el polling ni reinicializar innecesariamente los componentes.
- Mantener STOP ALL visible y fuera de la zona arrastrable.

**Consideraciones técnicas:**

- Evaluar `@dnd-kit` como biblioteca de drag & drop.
- Utilizar identificadores estables para cada componente.
- Validar los identificadores recuperados de `localStorage`.
- Incorporar componentes nuevos aunque no figuren en un orden previamente guardado.
- Ignorar identificadores que ya no existan.
- No almacenar estados operativos ni telemetría en `localStorage`.
- No modificar contratos del backend ni firmware.

**Criterios de aceptación:**

1. Las tarjetas pueden cambiarse de posición mediante drag & drop.
2. El orden se mantiene después de recargar.
3. Restablecer orden funciona correctamente.
4. Los controles operativos no se activan accidentalmente al arrastrar.
5. Funciona con mouse, teclado y dispositivos táctiles compatibles.
6. No hay desbordamiento horizontal.
7. STOP ALL continúa disponible y operativo.
8. Los tests frontend y la revisión visual verifican el comportamiento.

Documentar implementación, pruebas y limitaciones en `docs/ITERACION-10.md`.

# Iteración 11 — Diseñador visual de secuencias personalizadas

## 1. Objetivo

Implementar un módulo que permita crear, editar, guardar, organizar y ejecutar secuencias personalizadas de componentes de la carroza FNE mediante una interfaz visual.

El sistema deberá permitir combinar luces, banco LED, motor, hidráulico y servo, definiendo acciones, parámetros y tiempos de ejecución.

La ejecución será responsabilidad exclusiva del backend, reutilizando la arquitectura, los gateways y las protecciones desarrolladas en las iteraciones anteriores.

La secuencia `SHOW_FNE` existente deberá continuar funcionando sin modificaciones incompatibles.

## 2. Alcance funcional

### 2.1. Administración de secuencias

Permitir al operador:

- Crear una secuencia.
- Asignar nombre y descripción.
- Editar una secuencia existente.
- Duplicar una secuencia.
- Eliminar una secuencia, previa confirmación.
- Consultar el listado de secuencias guardadas.
- Seleccionar una secuencia para editarla o ejecutarla.

Las secuencias deberán persistirse en el backend para que estén disponibles después de reiniciar la aplicación.

### 2.2. Editor visual de pasos

Implementar un editor que permita agregar, modificar, duplicar, eliminar y reordenar pasos mediante Drag & Drop.

Cada paso deberá incluir:

- Identificador único.
- Orden de ejecución.
- Componente de destino.
- Acción.
- Parámetros de la acción.
- Duración o tiempo de espera, según corresponda.
- Descripción opcional.

Los controles del editor deberán adaptarse al componente y a la acción seleccionada.

El editor deberá ofrecer una alternativa accesible mediante teclado para reordenar pasos.

### 2.3. Componentes disponibles

**Luces**

- Encender.
- Apagar.
- Seleccionar luces disponibles.

**Banco LED**

- Encender o apagar canales.
- Configurar patrones compatibles con el gateway existente.
- Ejecutar barridos cuando estén soportados.

**Motor**

- Iniciar movimiento.
- Seleccionar dirección.
- Configurar velocidad dentro de límites válidos.
- Detener motor.

**Hidráulico**

- Extender (`EXTEND`), retraer (`RETRACT`) y detener (`STOP`).

**Servo**

- Configurar posición (`SET_POSITION`) entre 0 y 180 grados.

Solo se permiten acciones y parámetros de los contratos existentes. El banco
ofrece ALL_ON/ALL_OFF, barridos, PING_PONG, BLINK, STOP_EFFECT y SET_SPEED;
no se agregan comandos individuales de canales que el gateway no soporte.

### 2.4. Ejecución y detención

- Ejecución única: recorrer una vez todos los pasos, respetando orden, parámetros
  y tiempos relativos a la confirmación anterior.
- Ejecución continua: al finalizar cada ciclo, comprobar nuevamente las condiciones
  de seguridad y volver al primer paso. Mostrar ciclos completados.
- Detener secuencia cancela la ejecución activa y aplica la parada segura existente.
- STOP ALL tiene prioridad durante un paso, una espera o entre ciclos.
- Fallas críticas, protecciones y pérdida de comunicación interrumpen la ejecución
  conforme a las reglas existentes. Nunca reiniciar automáticamente; un nuevo
  inicio requiere una orden explícita del operador.
- Impedir secuencias simultáneas y comandos incompatibles mediante el coordinador,
  cancelación y tokens existentes. Sin tareas huérfanas, esperas activas ni comandos tardíos.
- Preservar SHOW_FNE, STOP ALL, telemetría, protecciones, Simulator y ESP32 con HAL
  simulada. Incorporar el modo continuo sin duplicar el mecanismo de comandos.

### 2.5. Persistencia y validación

Persistir definiciones en backend, independientemente del navegador, mediante una
abstracción. JSON local es aceptable con validación, escritura segura, tratamiento
explícito de corrupción y protección frente a modificaciones concurrentes.

No persistir ejecuciones para reanudarlas tras reiniciar. Conservar las garantías
de estado seguro existentes. Validar componentes, acciones, parámetros, tiempos,
límites y compatibilidad con el gateway activo en backend; errores HTTP claros.

### 2.6. Frontend e integración

Administración y editor visual Drag & Drop para secuencias y pasos, con alternativa
accesible por teclado. Controles adaptados a cada acción, ejecución única/continua,
progreso, paso actual y ciclos completados. Digital Twin 3D sincronizado mediante
estados confirmados y el polling único existente; React no temporiza la ejecución.
STOP ALL permanece fuera de la zona arrastrable y siempre accesible.

## 3. Arquitectura y decisiones

Reutilizar Application, OperationCoordinator, ComponentCommandExecutor,
IComponentGateway y cancelación. Controllers solo traducen HTTP. Documentar las
decisiones locales y su justificación en ITERACION-11 antes de implementarlas.
Informar incompatibilidades sin cambiar silenciosamente la semántica aprobada.

## 4. Pruebas y Definition of Done

Cubrir CRUD, duplicación y orden, persistencia/reinicio/corrupción/concurrencia,
validaciones, ejecución única/continua, cancelación durante pasos y entre ciclos,
STOP ALL, fallas, pérdida de comunicación y ausencia de reactivación posterior.
Mantener todas las regresiones previas, ambos gateways y contratos existentes.

Ejecutar la matriz de DESARROLLO y el Harness: builds, suites, smoke, contrato,
componentes, fallas, secuencias, telemetría, proxy, arquitectura y Swagger/OpenAPI.
Intentar revisión visual real de editor, teclado/Drag & Drop, responsive, Twin,
SHOW_FNE y parada. Registrar resultados y pendientes reales en ITERACION-11.
No realizar commits/push ni avanzar a Iteración 12.

Estado de implementación: ver [ITERACION-11](ITERACION-11.md). Motor, persistencia,
API/editor y verificaciones automatizadas implementados; revisión visual real
PENDIENTE por bloqueo del navegador. No se declara Definition of Done completa.
