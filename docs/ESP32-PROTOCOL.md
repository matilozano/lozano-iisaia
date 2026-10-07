# Protocolo ESP32 — versión 1

Contrato de la frontera implementada en Iteración 7. Se prueba sin placa mediante un controlador falso; no describe firmware instalado ni garantiza seguridad física. Las rutas `/api/devices` del navegador no cambian.

## Transporte y configuración

Backend → POST al `ComponentGateway:ESP32:Endpoint` completo, HTTP(S), JSON UTF-8, Content-Type `application/json`. Ruta recomendada del futuro controlador: `/v1/exchange`; dirección completa configurable. Sin descubrimiento, redirecciones, reintentos automáticos, MQTT ni heartbeat independiente. Las consultas existentes comprueban comunicación; no se agrega polling al frontend.

`TimeoutMs`: 2000 por defecto, configurable entre 100 y 30000. Incluye envío y lectura. Respuestas limitadas a 64 KiB en el cliente registrado. Endpoint absoluto, sin credenciales embebidas ni fragmento; Simulator no requiere dirección.

## Solicitudes

Todos los mensajes contienen `version` (1), `requestId` (UUID nuevo), `operation`, `componentId` y `command`. Los dos últimos son null cuando no aplican. Propiedades y valores distinguen mayúsculas.

```json
{
  "version": 1,
  "requestId": "a6ddda9f-3ccd-4894-b1d4-3464bff6e93b",
  "operation": "EXECUTE",
  "componentId": "main-motor",
  "command": { "action": "start", "direction": "forward", "speed": 40, "position": null }
}
```

| operation | componentId | command | Respuesta correcta |
| --- | --- | --- | --- |
| GET_STATE | ID existente | null | Un estado del ID solicitado |
| EXECUTE | ID existente | Objeto de comando | Un estado posterior a aplicar el comando |
| STOP_ALL | null | null | Los seis estados en una instantánea coherente tras la parada |

IDs: `front-lights`, `side-lights`, `main-motor`, `main-light-bank`, `hydraulic-1`, `servo-1`. No se envían GPIO ni nombres eléctricos.

`command` conserva los parámetros lógicos existentes: `action`, `direction`, `speed`, `position`. Parámetros no aplicables se envían null. No convertir start en un nuevo comando FORWARD: dirección es un parámetro. Las validaciones de intención continúan en Domain, antes del gateway; el transporte no las duplica.

| Componente | action y parámetros |
| --- | --- |
| Luces | on, off |
| Motor | start (direction forward/reverse; speed 0..100); stop |
| Banco | ALL_ON, ALL_OFF, SWEEP_RIGHT, SWEEP_LEFT, PING_PONG, BLINK, STOP_EFFECT; SET_SPEED con speed 1..100 |
| Hidráulico | EXTEND, RETRACT, STOP |
| Servo | SET_POSITION con position finito 0..180 |

El controlador aplica los defaults existentes de start: forward y 50 si son null. Efectos y desplazamiento se ejecutan en el controlador, nunca dentro del transporte ni del navegador. STOP_EFFECT conserva canales; stop del motor conserva dirección y deja speed 0; el hidráulico se detiene en la posición alcanzada. Ver [DESARROLLO](DESARROLLO.md).

## Respuestas

HTTP 200 con sobre JSON, tanto para éxito como para rechazo lógico:

```json
{
  "version": 1,
  "requestId": "a6ddda9f-3ccd-4894-b1d4-3464bff6e93b",
  "success": true,
  "executedAt": "2026-10-07T12:00:00Z",
  "states": [{ "id": "main-motor", "state": "running", "online": true, "direction": "forward", "speed": 40 }],
  "error": null
}
```

Los seis campos del sobre son obligatorios incluso cuando son null. `executedAt` es la hora del controlador de captura/ejecución, ISO 8601 con zona; se normaliza a UTC al devolverla. No sustituirla por la hora de envío. Para éxito no puede faltar estado ni fecha; error debe ser null.

Cada estado requiere `id`, `state`, `online`, `direction` (nullable) y `speed`. Campos adicionales son opcionales/null salvo los requeridos por el tipo:

| Tipo | Estado recibido |
| --- | --- |
| light | state on/off; speed 0 y direction null |
| motor | state running/stopped, speed 0..100; running requiere forward/reverse; stopped requiere speed 0 |
| light_bank | channels de ocho booleanos CH01..CH08; effect NONE/SWEEP_RIGHT/SWEEP_LEFT/PING_PONG/BLINK; effectSpeed 1..100; state running con efecto, on si algún canal está encendido sin efecto, off en caso contrario |
| HYDRAULIC_ACTUATOR | position 0..100; movement STOPPED/EXTENDING/RETRACTING; limitExtended y limitRetracted coherentes con 100 y 0; state stopped/running según movimiento |
| SERVO | position 0..180; state stopped |

Campos de otro tipo no deben incluir valores. `online=false` puede aparecer en consulta válida; un comando no se confirma con un componente offline. La respuesta debe corresponder al UUID, versión e IDs esperados, sin duplicados. Formato incompleto, rango inválido o instantánea incoherente produce INTERNAL_ERROR; no se publica parcialmente ni se genera estado local sustituto.

Ejemplo de rechazo:

```json
{
  "version": 1,
  "requestId": "a6ddda9f-3ccd-4894-b1d4-3464bff6e93b",
  "success": false,
  "executedAt": null,
  "states": null,
  "error": { "code": "DEVICE_OFFLINE", "message": "Controlador de tracción no disponible." }
}
```

Códigos: DEVICE_OFFLINE, TIMEOUT, INVALID_COMMAND, INVALID_PARAMETER, INTERNAL_ERROR. El gateway identifica componente/STOP_ALL en el mensaje. En API pública conservan 503, 504, 400, 400 y 500 respectivamente. HTTP remoto 503 se traduce a DEVICE_OFFLINE; 408/504 a TIMEOUT; otros no exitosos a INTERNAL_ERROR. Fallo de conexión equivale a DEVICE_OFFLINE, JSON/Content-Type inválido a INTERNAL_ERROR. Cancelación solicitada por Application se propaga como cancelación, no timeout.

## STOP ALL y pérdida de comunicación

STOP_ALL es una operación global única; no se envían seis órdenes independientes. El controlador debe serializar mutaciones, cancelar trabajo previo/efectos, apagar luces/banco, detener motor/hidráulico y conservar servo. Solo responder éxito después de aplicar la parada y capturar todos los estados online, detenidos/apagados. Una respuesta parcial, offline o con movimiento activo no confirma parada.

El controlador deberá garantizar que órdenes anteriores en tránsito no reactiven mecanismos tras confirmar STOP_ALL, incluso entre conexiones. Esta obligación de firmware (ordenamiento/barrera y tratamiento de solicitudes demoradas) deberá validarse al integrar hardware; el cliente HTTP por sí solo no puede garantizarla. Application conserva coordinación y cancelación para órdenes/steps pendientes en backend.

No hay reintentos automáticos: TIMEOUT o desconexión significa **resultado desconocido**, no demuestra que la placa no ejecutó la orden. El gateway no inventa éxito, no actualiza estado local ni ejecuta luego una orden fallida. Una consulta posterior puede confirmar estado. Si falla STOP_ALL se informa error, no un 204 ni una instantánea segura ficticia. Una secuencia deja de enviar steps y puede quedar FAILED/CANCELLED aunque la parada física no esté confirmada; consultar el error de la operación. No hay recuperación/reanudación automática.

Pendientes para hardware: firmware, sincronización de reloj, ordenamiento ante pérdida/reordenamiento de red, watchdog y parada física independiente, alimentación, GPIO y pruebas de placa. Esta entrega verifica la frontera software con fallas controladas; no certifica esos mecanismos.
