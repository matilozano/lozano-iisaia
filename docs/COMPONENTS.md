# Componentes

## Modelo

Un componente representa un dispositivo lógico controlable.

Propiedades mínimas:

- id;
- name;
- type;
- status;
- capabilities;
- online.

## LIGHT

Ejemplo:

ID:
`front-lights`

Capabilities:

- ON
- OFF

Estados:

- ON
- OFF

## LIGHT BANK

Ejemplo:

ID:
`main-light-bank`

Representa múltiples canales de iluminación.

Cantidad inicial:

8 canales.

Capabilities:

- ALL_ON
- ALL_OFF
- SWEEP_RIGHT
- SWEEP_LEFT
- PING_PONG
- BLINK
- STOP_EFFECT
- SET_SPEED

El barrido debe poder variar su velocidad.

## MOTOR

Ejemplo:

ID:
`main-motor`

Capabilities:

- FORWARD
- REVERSE
- STOP
- SET_SPEED

Propiedades:

- running;
- direction;
- speed.

Speed:

0..100

Direction:

- FORWARD
- REVERSE

STOP debe detener el movimiento.

## HYDRAULIC_ACTUATOR

Ejemplo:

ID:
`hydraulic-1`

Capabilities:

- EXTEND
- RETRACT
- STOP

Propiedades:

- position;
- movement;
- limitExtended;
- limitRetracted.

Position:

0..100

Movement:

- STOPPED
- EXTENDING
- RETRACTING

El simulador debe cambiar gradualmente la posición mientras el componente se
encuentra en movimiento.

## SERVO

Ejemplo:

ID:
`servo-1`

Capabilities:

- SET_POSITION

Propiedades:

- position.

Rango:

0..180 grados.

## Estado online

Todo componente puede reportar:

- online;
- offline.

Un componente offline no debe confirmar comandos como ejecutados.

## Errores simulables

El SimulatorComponentGateway debe evolucionar para poder representar:

- DEVICE_OFFLINE;
- INVALID_COMMAND;
- TIMEOUT;
- INVALID_PARAMETER;
- INTERNAL_ERROR.

Estos errores permiten verificar el comportamiento de la aplicación sin
provocar fallas físicas.

## STOP ALL

STOP ALL afecta a todos los componentes relevantes.

Debe:

- apagar iluminación;
- cancelar efectos;
- detener motores;
- detener actuadores;
- cancelar secuencias.

La respuesta debe contener suficiente información para que el frontend pueda
actualizar los estados confirmados.
## Contrato implementado del banco (Iteración 3)

El identificador es main-light-bank y su type HTTP es light_bank, siguiendo la convención en minúsculas de los componentes existentes. Las capabilities enumeradas arriba se envían como action en mayúsculas. SET_SPEED recibe speed de 1 a 100. STOP_EFFECT conserva el patrón; ALL_OFF y STOP ALL apagan todos los canales. Canales, efecto y velocidad se consultan por API; ver DESARROLLO.md para campos y semántica temporal.

## Contrato implementado de posiciones (Iteración 4)

hydraulic-1 usa type HYDRAULIC_ACTUATOR y devuelve position 0..100, movement STOPPED/EXTENDING/RETRACTING, limitExtended y limitRetracted. EXTEND/RETRACT/STOP no reciben parámetros. servo-1 usa type SERVO y SET_POSITION con position 0..180; inicial 90°. La parada conserva el ángulo del servo y congela la posición del hidráulico. Ver DESARROLLO.md para fallas reproducibles y respuesta opcional de STOP ALL.
