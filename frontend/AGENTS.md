# AGENTS.md — Frontend

## Objetivo

El frontend representa un panel de simulación y control de componentes
electromecánicos.

No debe parecer un CRUD administrativo.

El usuario debe poder comprender el estado físico de un componente observando
la interfaz, sin depender únicamente de texto.

## Stack

- React
- TypeScript
- Vite

## Comunicación

Toda comunicación debe realizarse contra la API.

Frontend
    ↓
API
    ↓
Application
    ↓
Gateway

Está prohibido:

Frontend
    ↓
ESP32

El frontend no conoce:

- GPIO;
- IP interna del ESP32;
- relés;
- drivers;
- protocolo del firmware.

## Estado confirmado

No asumir éxito al presionar un botón.

Flujo:

acción del usuario
    ↓
estado "enviando"
    ↓
API
    ↓
respuesta confirmada
    ↓
actualización visual

Ante error:

- conservar el último estado confirmado;
- mostrar el error;
- permitir reintentar cuando corresponda.

## Diseño visual

La interfaz debe representar un panel de control técnico/electromecánico.

Priorizar:

- estados visuales;
- indicadores;
- animaciones funcionales;
- telemetría;
- controles directos;
- feedback inmediato.

Evitar una interfaz compuesta únicamente por formularios, selects y texto.

## LIGHT

Una luz debe mostrar físicamente su estado.

OFF:

- oscura;
- sin glow.

ON:

- iluminada;
- glow visible;
- indicador de estado.

No alcanza con cambiar el texto de "Apagada" a "Encendida".

## MOTOR

Debe existir representación visual del motor.

STOPPED:

- animación detenida.

FORWARD:

- rotación visible en un sentido.

REVERSE:

- rotación visible en sentido contrario.

La velocidad visual debe responder al porcentaje configurado.

Controles recomendados:

[ REVERSA ] [ STOP ] [ ADELANTE ]

y control de velocidad 0..100%.

Evitar utilizar un select para la dirección si botones directos representan
mejor la operación.

## LIGHT SWEEP

Representar al menos ocho canales:

CH01 CH02 CH03 CH04 CH05 CH06 CH07 CH08

Cada canal debe tener representación visual.

Efectos mínimos:

- barrido izquierda → derecha;
- barrido derecha → izquierda;
- ida y vuelta;
- parpadeo;
- todos ON;
- todos OFF.

Debe poder modificarse visualmente la velocidad del efecto.

## HYDRAULIC ACTUATOR

Representar visualmente un cilindro/pistón.

Estados:

- STOPPED
- EXTENDING
- RETRACTING
- EXTENDED
- RETRACTED

Mostrar posición:

0..100%

La animación debe representar el desplazamiento.

Controles:

[ RETRAER ]
[ STOP ]
[ EXTENDER ]

## SERVO

Representar un brazo o indicador angular.

Rango:

0..180°

La representación debe cambiar visualmente con la posición.

## Secuencias

La UI permite:

- listar secuencias;
- iniciar;
- detener;
- visualizar cuál está activa.

El frontend no implementa la temporización de la secuencia.

No ejecutar secuencias mediante múltiples setTimeout del frontend cuando
la funcionalidad esté integrada con backend.

La ejecución pertenece al backend.

## STOP ALL

STOP ALL debe ser siempre claramente visible.

Cuando el backend confirme la parada:

- detener animación del motor;
- detener actuadores;
- detener barridos;
- apagar luces;
- actualizar estados.

## Estado del sistema

Mostrar información relevante:

- API ONLINE/OFFLINE;
- Gateway SIMULATOR/ESP32;
- estado del controlador;
- último comando;
- errores recientes.

## Historial

Cuando exista soporte backend, mostrar:

- hora;
- componente;
- comando;
- resultado.

Ejemplo:

17:42:01  LIGHT_FRONT  ON           OK
17:42:05  MOTOR_MAIN   FORWARD 70%  OK
17:42:12  MOTOR_MAIN   STOP         OK

## Responsive

El panel debe poder utilizarse desde:

- notebook;
- tablet;
- dispositivo móvil.

Los controles críticos deben seguir siendo accesibles.

## Criterio de aceptación visual

Una funcionalidad de simulación no se considera terminada si solamente cambia
texto.

El comportamiento físico relevante debe tener representación visual.

Ejemplos:

- luz → brillo;
- motor → rotación;
- actuador → desplazamiento;
- servo → ángulo;
- barrido → propagación entre canales.

## Verificación

Antes de finalizar:

`npm --prefix frontend run build`

Cuando existan pruebas E2E, ejecutar las pruebas relacionadas.

Verificar además manualmente:

1. encender/apagar luces;
2. iniciar/detener motor;
3. cambiar velocidad;
4. cambiar dirección;
5. ejecutar barrido;
6. detener efectos;
7. ejecutar STOP ALL.