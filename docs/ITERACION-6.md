# Iteración 6 — Vista Carroza / Digital Twin

Cierre de la continuación: 2026-10-06. Rama `tpfinal`; sin commits, push, reset ni cambios de rama. No se inició Iteración 7.

## Estado encontrado al retomar

Se inspeccionaron status, diff, archivos nuevos, AGENTS de raíz/frontend/backend, documentación de arquitectura/componentes/desarrollo/plan y reportes 1 a 5. No existía este informe. La implementación de Iteración 6 ya estaba completa en código: SVG, mapping, HUD, estilos, integración en App y diez pruebas. No había archivos cortados, marcadores de conflicto ni necesidad de reemplazar funcionalidad. Los builds y pruebas anteriores no se tomaron como evidencia del cierre: se repitieron.

Archivos de Iteración 6 ya creados sin seguimiento:

- frontend/src/carroza/model.ts
- frontend/src/carroza/CarrozaView.tsx
- frontend/src/carroza/CarrozaSvg.tsx
- frontend/src/carroza/Visuals.tsx
- frontend/src/carroza/carroza.css
- frontend/tests/carroza.test.mjs
- frontend/tsconfig.test.json

Archivos ya modificados por esa ejecución: .gitignore, frontend/package.json, frontend/src/App.tsx, docs/PLAN.md y docs/DESARROLLO.md. Existían además cambios pendientes de Iteración 5 en backend/Carroza.Api/Program.cs, Controllers/DevicesController.cs, backend/sequence-test.ps1 y docs/ARCHITECTURE.md, más backend/frontend-proxy-test.ps1, docs/ITERACION-5.md, docs/iteracion-5-stop-all.png y la solución backend/Carroza.Api/Carroza.Api.sln sin seguimiento. Se conservaron todos.

## Trabajo pendiente encontrado y completado ahora

Faltaban el informe final y completar/repetir la revisión visual del motor, posiciones, show y parada. Se revisó la implementación sin rehacerla, se repitieron todas las suites, se ejecutó el checklist visual y se agregó una prueba de accesibilidad para movimiento reducido. Se documentan por separado los límites de esa revisión. No fue necesario cambiar contratos, backend, controles existentes ni polling.

En esta continuación se modificó frontend/tests/carroza.test.mjs (undécima prueba), se creó este informe y evidencia visual, y se completaron las notas de verificación en PLAN/DESARROLLO. Los demás archivos de Iteración 6 se conservaron.

## Arquitectura final y flujo de datos

Backend → Application → IComponentGateway → estado confirmado → API → useDevices → App → CarrozaView → mapping puro → SVG/HUD.

- useDevices conserva una única fuente de sincronización para panel, secuencias y Twin; usa el polling existente de 150 ms después de finalizar cada consulta.
- App agrega únicamente import y composición de CarrozaView con devices, connected y execution.
- model.ts adapta DTOs a propiedades de presentación sin relojes, comandos, peticiones, hooks de estado ni pasos de secuencia.
- CarrozaView compone avisos de conexión, SVG y CarrozaHud (función separada en el mismo archivo). El HUD compacto informa API, motor/velocidad/dirección, hidráulico, servo, banco y secuencia/estado.
- CarrozaSvg define la escena lateral: chasis, ruedas, decoración FNE, plataforma elevable y pieza articulada.
- Visuals separa LightVisual, LedBankVisual, WheelsVisual, HydraulicVisual y ServoVisual.
- carroza.css contiene presentación, animación de ruedas, adaptación móvil y movimiento reducido.

No hay endpoints nuevos ni contratos modificados por esta iteración. No hay recursos remotos, imágenes externas, dependencias de animación ni librerías nuevas.

## Relación estado → representación

- Luces: state=on activa disco iluminado y halo; off conserva el cuerpo oscuro.
- Banco: exactamente ocho booleanos channels recibidos, ordenados CH01 a CH08 de izquierda a derecha. El Twin no calcula barridos, fases ni BLINK. También representa ALL_ON, ALL_OFF, PING_PONG y STOP_EFFECT por los mismos canales.
- Motor: ruedas giran únicamente con state=running y speed>0. Duración 60/speed segundos por vuelta: 20%=3 s, 50%=1.2 s, 100%=0.6 s. reverse invierte rotación. El chasis no se desplaza fuera del layout; indicador estático muestra sentido y porcentaje.
- Hidráulico: posición 0..100 mueve verticalmente plataforma y decoración entre 0 y 110 unidades SVG; varillas acompañan el desplazamiento. No se extrapola entre consultas ni se inventan estados.
- Servo: rotación igual a position, con pieza inicial hacia la izquierda a 0°, vertical a 90° y extremo derecho a 180°. Sin valor confirmado el HUD indica ausencia de datos.
- Desconexión: conserva última representación y muestra advertencia explícita de que no confirma movimiento actual. Componentes offline aparecen identificados. No convierte una desconexión en una parada supuesta.

## SHOW_FNE y STOP ALL

SHOW_FNE no aparece codificado en el Twin: solo se recibe el identificador de ejecución. En revisión de navegador se inició desde el panel y se confirmó COMPLETED con iluminación apagada, motor detenido, hidráulico retraído y servo a 120°. Se observaron barridos, motor al 40%, extensión y retracción; BLINK se verificó además directamente con todas las luces alternando ON/OFF. Las pruebas HTTP comprobaron el orden y tiempos de los 14 pasos completos.

En una nueva ejecución desde UI se pulsó Detener todo con motor al 40% e hidráulico EXTENDING. Después de confirmar backend, HUD mostró CANCELLED; ruedas paused, luces sin halos, banco NONE, hidráulico STOPPED aproximadamente en 6.7% y servo conservado en 120°. Se verificó por API que durante otros 13 segundos no cambiaron snapshot, runId ni cantidad de eventos. La suite sequence-test repitió cancelación y STOP ALL con movimiento y observación prolongada.

La respuesta STOP ALL actualiza devices con el snapshot existente; el siguiente polling confirma CANCELLED. No se adelanta ese resultado en la vista. No se duplica la política de seguridad del backend.

## Responsive y accesibilidad

SVG con viewBox 0 0 960 480, width 100% y altura proporcional. HUD usa tres columnas y dos en móvil. En viewport de 390 px se observó documento de 375 px (scrollbar) y SVG de 349 px: sin overflow horizontal, controles y parada accesibles. Se revisó también escritorio.

prefers-reduced-motion desactiva la animación continua de ruedas. El HUD e indicador de dirección/velocidad permanecen estáticos y los canales/posiciones continúan reflejando estados confirmados. Se verificaron regla CSS cargada y prueba automatizada; no se cambió la preferencia del sistema operativo ni se emuló esa media feature, que la herramienta de navegador no expone.

## Verificaciones ejecutadas de nuevo

| Verificación | Resultado |
| --- | --- |
| dotnet build backend/Carroza.Api | OK, 0 errores y 0 advertencias |
| npm.cmd --prefix frontend run build | OK, TypeScript/Vite, 40 módulos |
| dotnet run --project backend/Carroza.Simulator.Tests | OK: 34 generales, 75 banco, 52 posiciones/fallas, 82 secuencias; 60 operaciones concurrentes adicionales |
| npm.cmd --prefix frontend run test | OK, 11 pruebas; las 10 previas más movimiento reducido |
| smoke-test.ps1 | OK |
| contract-test.ps1 | OK |
| iteration-3-test.ps1 | OK, banco y Swagger UI/assets/DTOs |
| iteration-4-test.ps1 | OK, posiciones, validaciones y snapshot |
| fault-test.ps1 | OK, cinco fallas reproducibles y FAILED sin pasos posteriores |
| sequence-test.ps1 | OK, 14 pasos/tiempos, conflictos, cancelación y STOP ALL prolongados |
| frontend-proxy-test.ps1 | OK, diez consultas JSON en 5080 y 5173 |
| Búsquedas arquitectónicas de DESARROLLO | Sin coincidencias: sin endpoints en Program ni dependencias prohibidas |
| Revisión visual | Realizada; checklist abajo |

El primer build/test .NET falló por acceso del sandbox a NuGet.Config del usuario. Se repitió con el permiso correspondiente y pasó; no se alteró NuGet ni se ocultó el fallo. La suite backend usa dotnet run por diseño, no dotnet test.

Las pruebas frontend validan luces, canales de barridos/BLINK, motor parado/adelante/reversa/velocidad, geometría hidráulica/servo, snapshot de STOP ALL, RUNNING/CANCELLED, datos conservados ante desconexión y ausencia de peticiones/timers/comandos en el Twin. Renderizan SVG real con react-dom/server; compilación de pruebas en .test-build ignorado por Git.

## Checklist visual realizado en esta continuación

1. Estado inicial: API ONLINE, secuencia IDLE, luces apagadas, hidráulico 0%, servo 90°, motor detenido.
2. Luces ON/OFF: controles reales, halos presentes/ausentes en Twin y panel coherentes.
3. SWEEP_RIGHT: se observó avance hacia CH02; mismo patrón recibido.
4. SWEEP_LEFT: se observó avance desde CH08 hacia CH07.
5. BLINK: ocho canales simultáneamente ON y luego OFF.
6. Motor FORWARD: confirmado en ambos componentes visuales.
7. Velocidades 20/50/100: duraciones 3/1.2/0.6 s; también REVERSE.
8. STOP motor: velocidad cero y ruedas pausadas.
9. Hidráulico: extensión progresiva, extremo 100%, retracción y extremo 0%.
10. Servo: posiciones 0°/180° manuales, 90° inicial y 120° de SHOW_FNE.
11. SHOW_FNE: inicio real, RUNNING, progresión y COMPLETED.
12. SHOW_FNE + STOP ALL: CANCELLED y estados seguros sin reactivación posterior.

Algunas esperas de la herramienta vencieron antes del estado esperado (hidráulico 100% y fase breve del show); se comprobó el estado en observaciones posteriores y se complementó con ejecución HTTP completa. No se confundió ese timeout de automatización con una falla de la API.

## Pendientes reales y límites

Pendiente únicamente la comprobación manual con prefers-reduced-motion activado en el sistema/navegador: la regla y el indicador alternativo sí están probados. No se declara emulación realizada. La simulación es esquemática, sin calibración física, hardware, firmware, GPIO, MQTT, SignalR, WebSockets, autenticación, persistencia ni editor de secuencias.


Evidencia final: iteracion-6-carroza.png, captura de escritorio con secuencia CANCELLED y estados detenidos. git diff --check sin errores de whitespace; avisos LF/CRLF habituales de Git.
