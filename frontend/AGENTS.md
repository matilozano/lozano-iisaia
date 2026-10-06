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

No agregar estados de dominio EXTENDED/RETRACTED: los extremos se expresan con position y limitExtended/limitRetracted.

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

## Fuente única de estado y Digital Twin

- Reutilizar useDevices para panel, secuencias y Twin. No crear polling por elemento ni una segunda fuente de estado físico.
- Separar adquisición, mapping puro de presentación, componentes SVG/HUD y CSS. Mantener App como composición; la vista no envía comandos ni implementa SHOW_FNE.
- Los canales del banco provienen de channels confirmado, no de un contador o patrón local. Hidráulico/servo usan posiciones confirmadas; no extrapolar movimiento físico. CSS solo representa movimiento según estado, velocidad y dirección recibidos.
- Mantener distintos los valores solicitados de controles y los valores confirmados. Respuestas anteriores a STOP ALL no pueden volver a pintar estados activos; conservar la protección de sincronización existente.
- Ante fallo, conservar estado y error por componente; permitir operar otros componentes cuando la política backend lo permita. OFFLINE/TIMEOUT no significan éxito. Una vista desactualizada debe advertirlo, sin inventar datos de hardware.
- STOP ALL siempre accesible; reflejar snapshot y CANCELLED cuando se confirmen. No cambiar anticipadamente los componentes al presionar el botón.
- SVG propio con viewBox y dimensiones adaptables; sin imágenes remotas ni dependencias pesadas sin necesidad. Respetar prefers-reduced-motion y ofrecer indicadores estáticos de estado/dirección/velocidad.
- Historial diferencia MANUAL y SEQUENCE, con hora, componente, comando y resultado; no agregar persistencia por inferencia.

## Evidencia de verificación

Ejecutar también `npm --prefix frontend run test`. Probar mapping backend→presentación y comportamiento significativo; evitar tests de detalles cosméticos que solo repiten CSS. El test de accesibilidad debe comprobar la alternativa al movimiento, no reemplaza por sí solo una prueba manual.

Intentar revisión visual con API real: luces ON/OFF, ambos barridos y BLINK, motor/dirección/varias velocidades/STOP, hidráulico y servo, SHOW_FNE completo y STOP ALL a mitad de secuencia. Comparar panel/Twin con estados confirmados y revisar tamaños menores, errores y movimiento reducido. Documentar qué se observó y qué quedó pendiente. HTTP, snapshots de markup y build no demuestran por sí solos animación visible.

Si `/api` devuelve HTML, identificar URL exacta, status, Content-Type y cuerpo; verificar proxy/backend con frontend-proxy-test. Corregir la causa, no ocultar el error JSON con try/catch ni cambiar puertos para esquivarlo. Si no se reproduce, informar la evidencia y no inventar una causa histórica.


Para cambios exclusivamente documentales, aplicar la verificación documental del AGENTS raíz y DESARROLLO; no presentar compilación o revisión visual como realizadas si no se ejecutaron.
