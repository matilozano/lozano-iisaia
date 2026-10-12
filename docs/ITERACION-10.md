# Iteración 10 — Navegación y presentación del dashboard

Fecha de ejecución: 2026-10-11. Rama: `tpfinal`. Sin commit ni push.

Estado actual: tabs, Twin 3D, grilla compacta y reordenamiento de tarjetas
implementados. Se conserva la alternativa SVG, cámara y fuente de estado
compartida. Las secciones iniciales preservan evidencia histórica; las últimas
secciones registran los apartados 14 y 15 y sus verificaciones reales.
Persisten los pendientes visuales explícitos del cierre, sin aprobación ficticia.

## Estado encontrado y alcance

Al comenzar, el único cambio existente era la definición de esta iteración en
`docs/PLAN.md`; se conservó. La Iteración 9 ya tenía controles, Digital Twin,
telemetría, protecciones, secuencias e historial funcionando. Se reemplazó la
navegación mediante anclas y la página con todas las secciones simultáneas por
tres pestañas. No se reimplementaron casos de uso ni se inició Iteración 11.

## Implementación y arquitectura

- **Vista general:** Twin, métricas, calidad/origen/antigüedad, alertas y laboratorio.
- **Componentes:** controles existentes de luces, motor, hidráulico, servo y banco.
- **Secuencias e historial:** SHOW_FNE, progreso, cancelación y eventos.

`App` conserva una sola instancia de `useDevices`. Continúan intactos HTTP,
polling, ejecución de comandos, sincronización y exclusión backend. `DashboardTabs`
solo mantiene la selección visual. Los tres paneles permanecen montados y los
inactivos usan `hidden`: cambiar de pestaña no reinicia controles ni historial.
No hay polling ni lógica operativa dentro del componente de navegación.

Flujo conservado: API → useDevices → controles / secuencias / mapping del Twin.
El Digital Twin continúa representando estados confirmados; no ejecuta SHOW_FNE.
STOP ALL permanece fuera del contenido de las pestañas, en la fila persistente
existente; conserva exactamente su mecanismo de parada y confirmación.

Se agregaron `tablist`, `tab`, `tabpanel`, relaciones ARIA, selección y tabulación
con foco único. Flechas izquierda/derecha recorren circularmente las pestañas;
Home/End seleccionan primera/última; un botón permite enfocar el panel activo.
No hay recarga, cambios de URL ni navegación mediante anclas.

Los estilos compactan tarjetas, métricas, encabezado y mecanismos, mejoran el
contraste del SVG y limitan su altura en escritorio para mostrar también el HUD.
Tablet/móvil usan una columna y escalado proporcional. Los valores inciertos de
telemetría se distinguen con borde discontinuo y color, además de los mensajes
de calidad existentes. No se convierten valores ausentes en ceros.
Los avisos de protección, errores de telemetría y bloqueo por RUNNING se muestran
fuera de las pestañas. Se informa el envío de operaciones de telemetría/secuencias.
Se conservó el CSS de movimiento reducido, sin agregar animaciones.

## Archivos

Creado:

- `frontend/src/DashboardTabs.tsx`.
- `frontend/src/dashboard.css`.
- `frontend/tests/tabs.test.mjs`.
- `docs/ITERACION-10.md`.

Modificado:

- `frontend/src/App.tsx`: composición de las tres pestañas y avisos comunes.
- `frontend/src/Telemetry.tsx`: clase visual para métricas inciertas.
- `frontend/src/main.tsx`: carga de los estilos de presentación después del CSS base.
- `frontend/tsconfig.test.json`: inclusión del componente de pestañas en las pruebas.
- `frontend/tests/dashboard.test.mjs`: criterio de navegación actualizado; conserva
  comprobaciones de región STOP, scrolling y una fuente de estado.
- `docs/PLAN.md`: alcance previo conservado y registro de estado de esta ejecución.

Backend, firmware, dependencias, DTOs y endpoints: **sin cambios**.

## Pruebas y verificaciones de la ejecución inicial (antes del 3D)

Resultados de esta ejecución, no trasladados desde informes anteriores.

| Comando / comprobación | Resultado | Evidencia / alcance |
| --- | --- | --- |
| `dotnet build backend/Carroza.Api` | APROBADA | 0 warnings y 0 errores |
| `dotnet run --no-restore --project backend/Carroza.Simulator.Tests` | APROBADA | 422 comprobaciones: 34 + 75 + 52 + 82 + 63 + 116; incluye 60 operaciones concurrentes |
| `npm --prefix frontend run build` | APROBADA | TypeScript y Vite, 46 módulos |
| `npm --prefix frontend test` | APROBADA | 19 tests, 0 fallas; dos nuevos tests de pestañas/teclado y regresiones existentes |
| `python -m unittest discover -s firmware/esp32-carroza/tests -v` | APROBADA | 32 tests con Python del runtime local |
| `python firmware/esp32-carroza/build.py` | APROBADA | 10 módulos MicroPython compilados |
| `backend/smoke-test.ps1` | APROBADA | Simulator: seis componentes, luces, motor, consulta y parada |
| `backend/contract-test.ps1` | APROBADA | Simulator: JSON, límites, defaults, 400/404, STOP y catálogo |
| `backend/iteration-3-test.ps1` | APROBADA | Banco, evolución temporal, Swagger UI/assets, operaciones y DTOs |
| `backend/iteration-4-test.ps1` | APROBADA | Hidráulico progresivo, servo, validaciones y snapshot seguro |
| `backend/sequence-test.ps1` | APROBADA | SHOW completo, timing/orden, exclusión, cancelación, STOP ALL e historial |
| `backend/telemetry-test.ps1` | APROBADA | Simulator: seis fallas, protecciones, reconocimiento/reset y no reactivación |
| `backend/fault-test.ps1` | APROBADA | Cinco fallas HTTP en instancia aislada |
| `backend/frontend-proxy-test.ps1` | APROBADA | Simulator: 12 consultas JSON entre 5080 y 5173 |
| `firmware/esp32-carroza/integration-test.ps1` | APROBADA | ESP32 → HTTP → firmware/HAL simulada: smoke, contrato, banco, hidráulico/servo y secuencias |
| `backend/telemetry-test.ps1` en ESP32/HAL simulada | APROBADA | Seis fallas, reconocimiento/reset, protección, cancelación, no reactivación y Swagger |
| `backend/frontend-proxy-test.ps1` en ESP32/HAL simulada | APROBADA | Las 12 consultas devolvieron JSON; no HTML |
| Búsquedas arquitectónicas de DESARROLLO | APROBADA | Tres búsquedas sin coincidencias; salida 1 esperada |

Las pruebas HTTP se ejecutaron secuencialmente, sin comandos desde el navegador
en paralelo. Se mantuvieron frontend 5173 y backend 5080; host HAL local 5090.
Swagger se verificó por la regresión HTTP, aunque no cambió el contrato.

## Revisión visual de la ejecución inicial (antes del 3D)

Navegador integrado, API real y observaciones DOM/capturas de pantalla. Se probó
la navegación de las tres pestañas en las cuatro resoluciones:

| Viewport | Ancho body/document | Ancho scroll / client | Región STOP (top → bottom) |
| --- | --- | --- | --- |
| 1440 × 900 | 1440 / 1440 | 1425 / 1425 | 825 → 900 |
| 1366 × 768 | 1366 / 1366 | 1351 / 1351 | 693 → 768 |
| 768 × 1024 | 768 / 768 | 753 / 753 | 949 → 1024 |
| 390 × 844 | 390 / 390 | 375 / 375 | 773 → 844 |

Los valores se repitieron en las tres pestañas: sin overflow horizontal, con STOP
accesible y contenido en su propia región de scroll. En 1440 el Twin completo/HUD
quedó entre y=214 e y=697.8; en notebook entre y=214 e y=658.2. En móvil se observó
el escalado de SVG, HUD y etiquetas de tabs, con contenido secundario debajo.

APROBADA: flechas, Home/End, foco visible, selección y un único panel visible.
La URL permaneció en `/`, sin hash. Una velocidad solicitada de 1% mediante
teclado sobrevivió al cambio Componentes → Vista general → Componentes sin
ejecutar comandos. El comando posterior se confirmó al 1% y apareció en el Twin.
Se observó luz frontal ON y, luego de STOP ALL, OFF con tracción detenida.

SHOW_FNE iniciado desde Secuencias se observó RUNNING desde otras pestañas,
con luces activas, SWEEP_LEFT y motor confirmado al 40%; los controles manuales
quedaron bloqueados. Finalizó COMPLETED, paso 14/14, 19.1 s, último comando
hidráulico STOP. Una segunda ejecución recibió STOP ALL desde Componentes y quedó
CANCELLED, paso 5/14, 5.2 s. Se observó después Twin seguro: luces/canales OFF,
motor 0% detenido, hidráulico detenido y servo conservado en 120°. La ausencia de
steps posteriores se comprobó además en la regresión de secuencias.
STOP ALL se operó desde las tres pestañas. El historial conservó eventos MANUAL
y SEQUENCE al cambiar de vista.

Se inyectó pérdida de comunicación desde el laboratorio: API ONLINE, controlador
OFFLINE, métricas `—`, calidad SIN COMUNICACIÓN y advertencia de último estado
confirmado. En Componentes permanecieron visibles la protección y el bloqueo;
STOP ALL seguía habilitado. Retirar falla → reconocer → restablecer permitió
recuperar la operación, sin reanudación automática.

También se detuvo exclusivamente la sesión de API propia, después de terminar
los scripts: el proxy devolvió error 500 y el panel mostró API OFFLINE, error
HTTP, controlador con «dato anterior», telemetría «ÚLTIMA LECTURA» y aviso del
Twin. Cambiar pestañas mantuvo el error y los estados confirmados. Los comandos
manuales y el inicio del show estaban deshabilitados, mientras STOP seguía
accesible (no se interpreta su accesibilidad como garantía de parada sin API).
Al reiniciar en Simulator se recuperó ONLINE sin recargar la página.

Se operaron además luz lateral ON/OFF (tres luces del Twin confirmadas ON), motor
FORWARD al 100%, cambio al 50%, REVERSE al 50% y STOP, ambos barridos, BLINK y
detención de efecto. Se observó BLINK con los ocho LED iluminados y los modos
confirmados en el HUD. Se enviaron EXTEND/STOP/RETRACT y posición de servo 180°;
Twin y controles mostraron las posiciones confirmadas. La regresión HTTP cubre
también límites, todos ON/OFF y los comandos restantes; no se sustituye evidencia
de movimiento reducido por estos resultados.

## Incidencias y límites

- El primer build encontró restricciones de acceso a configuración NuGet; el
  reintento autorizado pasó. La suite encontró un apphost ocupado por la API
  propia previa: se detuvo esa sesión y la repetición pasó.
- Una primera regresión HTTP se invalidó por un inicio externo de SHOW_FNE que
  cambió runId. Se pidió liberar la API; tras respuesta del usuario se repitió
  completa y pasó. No se relajó la exclusión ni se modificó el backend.
- El primer wrapper de integración HAL heredó un LASTEXITCODE anterior aunque
  todos sus scripts informaron OK. Se repitió con LASTEXITCODE inicializado a 0.
- **PENDIENTE:** activación visual de `prefers-reduced-motion`. El navegador
  disponible no ofrece emulación de esa preferencia; lectura CSS y tests no se
  presentan como aprobación visual de la preferencia activada.

No se declara cumplimiento visual total mientras permanezca ese pendiente.

## Continuación: Digital Twin 3D (2026-10-11)

### Inspección y trabajo conservado

Se verificaron rama `tpfinal`, status, diff y archivos no trackeados antes de editar.
Existían las pestañas, dashboard.css, mejoras de telemetría, pruebas de tabs y este
informe sin commitear. Se conservaron. PLAN incorporaba además el punto 13 de
Twin tridimensional, todavía sin implementación. Los resultados de las secciones
anteriores son históricos de la primera ejecución; el cierre se repitió abajo.

### Arquitectura y decisiones

`CarrozaView` sigue recibiendo exclusivamente dispositivos, conexión y ejecución
de la única instancia de `useDevices`. Reutiliza `toCarrozaModel`, el HUD y SVG.
Agrega `TwinViewport` mediante importación diferida y Suspense, con SVG durante
la carga. No se modificaron polling, HTTP, comandos, reglas de seguridad ni
backend/firmware/DTOs. Tampoco se agregaron endpoints.

- `carroza/sceneModel.ts`: mapping puro de posiciones y velocidad angular visual,
  más límites de cámara. Hidráulico = 1.25 + position × 0.015; servo = grados a
  radianes. Ruedas = π × speed / 30 rad/s, con signo inverso en reversa. Motor
  detenido, API offline o movimiento reducido producen velocidad angular cero.
- `twin3d/CarrozaScene.tsx`: geometría propia con primitivas, chasis, cuatro ruedas,
  motor, luces frontales/laterales, ocho canales por lado, plataforma/pistones y
  brazo servo. Los canales son los booleanos recibidos, sin generar fases de
  barrido o BLINK. El único uso de tiempo de render es la rotación decorativa de
  ruedas; no extrapola posiciones ni decide cambios de modo.
- `twin3d/TwinViewport.tsx`: cámara, selección 2D/3D, pausa visual, preferencia del
  sistema, visibilidad y ciclo de vida WebGL. Rotación/altura/zoom mediante botones,
  arrastre y teclado; Home restablece la cámara. Estas acciones no envían HTTP.
- `twin3d/webgl.ts`: detección de WebGL2, liberación de la prueba y suscripción con
  limpieza a pérdida de contexto. Ausencia de WebGL, pérdida o error de render
  seleccionan el SVG con aviso; se conservan los mismos datos y HUD.

Se incorporaron Three.js 0.186.1, React Three Fiber 9.8.1 y tipos Three 0.186.0,
con lockfile. Son las dependencias previstas por PLAN; no se agregaron Drei,
postprocesado, modelos, texturas ni recursos remotos. La carga diferida separa
el motor gráfico del bundle principal. Build final: principal 255.85 kB / 79.99
kB gzip, viewport 3D 928.54 kB / 250.33 kB gzip, escena 3.52 kB. Vite advierte
del chunk 3D mayor a 500 kB: advertencia conservada, no silenciada.

Render continuo solo cuando la vista es visible, motor en marcha, API actual y
movimiento habilitado; en otros casos se renderiza por demanda. IntersectionObserver
pausa el bucle continuo al ocultar la pestaña, sin desmontar cámara ni panel.
Pixel ratio limitado a 1..1.5; sin sombras costosas. No se midió FPS ni se declara
un benchmark. La interacción fue usable en el navegador de revisión.

`matchMedia('(prefers-reduced-motion: reduce)')` y su evento change desactivan
rotación continua. El botón adicional **Pausar animación** permite la misma
alternativa estática en 3D/2D sin detener componentes; canales, posiciones y HUD
siguen actualizándose desde API. Los controles conservan foco y etiquetas.

### Archivos agregados/modificados en esta continuación

Nuevos: `frontend/src/carroza/sceneModel.ts`, `frontend/src/twin3d/CarrozaScene.tsx`,
`TwinViewport.tsx`, `webgl.ts`, `twin3d.css` y `frontend/tests/three.test.mjs`.

Modificados: `frontend/src/carroza/CarrozaView.tsx`, `frontend/package.json`,
`frontend/package-lock.json`, `frontend/tsconfig.test.json`, este informe,
`docs/PLAN.md`, `docs/ARCHITECTURE.md` y `docs/DESARROLLO.md`.
El resto del trabajo previo de frontend se mantuvo. Sin cambios backend/firmware.

### Verificaciones repetidas y resultados reales

| Comando/procedimiento de esta continuación | Resultado |
| --- | --- |
| `dotnet build backend/Carroza.Api` | APROBADA: 0 warnings/errores |
| `dotnet run --no-restore --project backend/Carroza.Simulator.Tests` | APROBADA: 422 comprobaciones, incluidas secuencias, fallas, ESP32 y concurrencia |
| `npm --prefix frontend run build` | APROBADA: 71 módulos; advertencia de tamaño del chunk 3D |
| `npm --prefix frontend test` | APROBADA: 25 tests, 0 fallas; conserva los 19 previos y agrega 6 de 3D/WebGL |
| `python -m unittest discover -s firmware/esp32-carroza/tests -q` | APROBADA: 32 tests, Python del runtime local |
| `python firmware/esp32-carroza/build.py` | APROBADA: bytecode MicroPython |
| Smoke + contract + iteration-3 + iteration-4 + sequence + telemetry, cada script con `-BaseUrl http://127.0.0.1:5080` | APROBADA en Simulator: comandos, temporalidad, STOP, errores, protección y Swagger |
| `backend/fault-test.ps1` | APROBADA: cinco fallas, instancia aislada 5081 |
| `backend/frontend-proxy-test.ps1` | APROBADA en Simulator: 12 respuestas JSON, sin HTML |
| `firmware/esp32-carroza/integration-test.ps1` | APROBADA: smoke, contrato, banco, hidráulico/servo y secuencias en ESP32 → HTTP → HAL simulada |
| `backend/telemetry-test.ps1` y `backend/frontend-proxy-test.ps1` en ESP32/HAL simulada | APROBADA: seis fallas/protecciones/recuperación y 12 consultas JSON |
| Tres búsquedas arquitectónicas de DESARROLLO | APROBADA: sin coincidencias, salida 1 esperada |
| `git diff --check` | APROBADA: sin errores de whitespace |
| `npm --prefix frontend audit --json` (adicional) | FALLÓ: informa cadena Vite → PostCSS → source-map-js 1.2.1, severidad alta, sin fixAvailable. Esa versión ya estaba en HEAD; no proviene de Three/Fiber. No se ejecutó audit fix ni se cambió la cadena ajena al alcance |

Las baterías HTTP fueron secuenciales; no se enviaron comandos desde UI durante
los scripts. La cámara y las tabs solo cambiaron presentación. Se detuvieron
exclusivamente sesiones propias para alternar gateways y se dejó API Simulator
en 5080; Vite existente en 5173 se reutilizó, sin detener procesos ajenos.

Se investigó una falla inicial de iteration-3-test: usando su default localhost,
una consulta tardó 2262 ms frente a 6 ms con 127.0.0.1, alterando la ventana de
muestreo de 380 ms. La repetición completa con la dirección establecida del TP
pasó; no se modificaron tests ni backend para hacerlos pasar.

La revisión descubrió y corrigió un defecto del ciclo WebGL: el listener de
pérdida de contexto sobrevivía al cambio intencional a 2D y activaba fallback al
volver. Ahora ContextGuard retira el listener al desmontar el Canvas. Se agregó
prueba de notificación/cleanup (falló antes de implementar y pasó después), y se
repitió 3D → 2D → 3D en navegador con resultado correcto.

### Revisión visual nueva con 3D

APROBADA para el alcance observado: navegador integrado con API real, capturas
de viewport y observaciones sucesivas. Las tres tabs se probaron nuevamente en
1440×900, 1366×768, 768×1024 y 390×844. En las 12 combinaciones el ancho del
documento fue igual al viewport y scrollWidth igual a clientWidth. STOP siguió
en [825,900], [693,768], [949,1024] y [773,844] respectivamente, fuera del scroll.
En 1440/Secuencias el contenido no necesitó scrollbar (client/scroll 1440); las
otras medidas coinciden con la tabla inicial. El SVG y el canvas escalaron sin
overflow; en móvil el HUD/telemetría requieren desplazamiento vertical.

Se observaron rotación y acercamiento mediante botones, restablecimiento, y
flechas/+ /Home con foco en la escena. Se comprobó selección 2D y retorno 3D.
La cámara/pausa permanecieron al navegar entre tabs; controles y secuencias
siguieron recibiendo la fuente de estado existente.

SHOW_FNE completo: observado en 3D con luces, SWEEP_LEFT, motor 40%, plataforma
elevándose (76.8% EXTENDING), luego retracción (16.7% RETRACTING) y final
COMPLETED 14/14 en 19.1 s. En otra ejecución se observó 40%, hidráulico activo y
BLINK antes de STOP ALL. Tras la respuesta quedó CANCELLED, motor 0%, canales
oscuros, hidráulico STOPPED conservado en 100% y servo 120°. Después de navegar
por las cuatro resoluciones siguió CANCELLED sin reactivación. La regresión HTTP
comprobó además orden/timing y espera explícita para descartar steps posteriores.

En móvil se observaron motor FORWARD 100%, reversa 50%, STOP, geometría hidráulica
elevada y servo 90°/120°. Pausar animación dejó el HUD en running 100% y ruedas
estáticas, con aviso explícito: la pausa no envía una parada operativa.

Se detuvo la sesión propia de API: OFFLINE, error 500 del proxy, aviso de dato
anterior/Twin y telemetría ÚLTIMA LECTURA; el 3D conservó geometría. El reinicio
recuperó ONLINE sin recargar. Se inyectó Sensor inválido: métricas `—`, calidad
INVÁLIDA, protección y comandos bloqueados en Componentes. Retirar → reconocer →
restablecer recuperó sin movimiento automático. Los otros casos se cubrieron
en las regresiones de ambos gateways.

Consola: sin errores en la pestaña de revisión final. Hay advertencia de
deprecación THREE.Clock emitida por Fiber; no se ocultó. Una captura fullPage
falló; se conservaron observaciones/capturas de viewport y medidas DOM como
evidencia alternativa, sin inventar un archivo de captura persistido.

### Pendientes de cierre reales

- **PENDIENTE:** activar `prefers-reduced-motion` desde el sistema/emulación del
  navegador. Se verificó la pausa visual equivalente y el mapping automático
  mediante tests, pero no se confunden con preferencia del sistema activada.
- **PENDIENTE:** revisión visual con WebGL deshabilitado y pérdida/error gráfico
  forzado. La detección negativa y limpieza/pérdida tienen pruebas automatizadas;
  2D seleccionado y retorno 3D se observaron, pero no sustituyen ese escenario.
- La advertencia de tamaño/deprecación y el resultado adicional de audit quedan
  registrados arriba; no se declara una consola sin advertencias ni audit limpio.

Implementación 3D incorporada y regresiones aprobadas con las limitaciones
anteriores; no se declara cierre visual total. Sin commit/push ni Iteración 11.

## Apartado 14 — Vista compacta de componentes (2026-10-11)

### Estado encontrado y alcance de esta continuación

Se conservaron los cambios existentes de tabs, telemetría y Digital Twin 3D.
La pestaña Componentes todavía usaba tres columnas en escritorio y el banco
ocupaba una sección independiente. Esta continuación modifica exclusivamente
la presentación y su cobertura de pruebas; no modifica backend, firmware,
contratos HTTP, useDevices, polling ni ejecución de comandos.

### Cambios

- `frontend/src/DevicePanel.tsx`: incorpora el banco como sexta tarjeta dentro
  de la misma grilla; conserva identificación, errores y bloqueo de comandos.
  Imports con extensión `.js` compatibles con la compilación de pruebas Node.
- `frontend/src/LightBank.tsx`: ocho canales en dos filas de cuatro, con estado
  confirmado; Todos OFF y Detener efecto siempre visibles. Los restantes
  efectos y velocidad se agrupan en expansión nativa `details/summary`.
- `frontend/src/MotorControls.tsx`: mantiene REVERSA/Detener/ADELANTE visibles;
  ajuste y aplicación de velocidad en expansión, cuyo resumen muestra el valor
  solicitado. La velocidad confirmada permanece visible junto al rotor.
- `frontend/src/dashboard.css`: estilos acotados a Control manual, menor
  padding y altura visual, acciones de al menos 44 px, texto adaptable y
  columnas 1 / 2 desde 600 px / 3 desde 1000 px / 5 desde 1400 px.
- Nuevo `frontend/tests/components.test.mjs`: cobertura de banco dentro de la
  grilla, controles conservados, expansión, ocho canales, errores y bloqueo
  global incluso dentro del contenido expandible.
- `frontend/tsconfig.test.json`: incluye DevicePanel en la compilación de tests.

Los controles plegados permanecen montados. No se introduce estado operativo
local: los únicos valores solicitados son los controles ya existentes, mientras
las representaciones siguen usando snapshots confirmados. STOP ALL permanece
fuera del área desplazable, sin cambios. No se agregan endpoints ni librerías.

### Verificaciones ejecutadas nuevamente

| Verificación | Resultado real |
| --- | --- |
| `dotnet build backend/Carroza.Api` | APROBADA; 0 errores y advertencias. Primer intento bloqueado por acceso a NuGet.Config del sandbox; reejecución autorizada correcta. |
| `dotnet run --no-restore --project backend/Carroza.Simulator.Tests` | APROBADA: 422 comprobaciones y 60 operaciones concurrentes. |
| `npm.cmd --prefix frontend test` | APROBADA: 27 tests, incluyendo dos nuevos de tarjetas. La incorporación inicial necesitó corregir resolución de imports Node. |
| `npm.cmd --prefix frontend run build` | APROBADA. Continúa advertencia previa del chunk 3D de 928.54 kB; no es un fallo de compilación. |
| `smoke-test.ps1` | APROBADA. |
| `contract-test.ps1` | APROBADA. |
| `iteration-3-test.ps1` | APROBADA: Swagger/OpenAPI, banco y comandos. |
| `iteration-4-test.ps1` | APROBADA: hidráulico, servo, validaciones y STOP ALL. |
| `sequence-test.ps1` | APROBADA: SHOW_FNE completo, tiempos/orden, exclusión, cancelación, STOP ALL e historial. |
| `telemetry-test.ps1` (Simulator) | APROBADA: seis fallas, protección, reconocimiento/reset, cancelación y ausencia de reactivación. |
| `fault-test.ps1` | APROBADA: cinco fallas reproducibles y estados confirmados. |
| `frontend-proxy-test.ps1` | APROBADA: seis rutas JSON en 5080 y 5173. |
| Búsquedas arquitectónicas de DESARROLLO | APROBADAS: sin endpoints Minimal API, gateways en Controllers ni dependencias invertidas en Domain/Application. |
| `python -m unittest discover -s firmware/esp32-carroza/tests -q` | APROBADA: 32 tests. |
| `python firmware/esp32-carroza/build.py` | APROBADA: bytecode MicroPython compilado. |

Los scripts HTTP se ejecutaron secuencialmente sobre `127.0.0.1:5080`.
Durante ellos el navegador solo observó estados o modificó presentación;
los comandos manuales de revisión se ejecutaron después de terminarlos.

### Revisión visual real del apartado 14

Navegador integrado, API real Simulator y Vite en los puertos establecidos.
Se inspeccionaron capturas de viewport y geometría del DOM:

| Viewport | Columnas observadas | Ancho documento | STOP ALL (top → bottom) |
| --- | --- | --- | --- |
| 1440×900 | 5 | 1440 px | 825 → 900 px |
| 1366×768 | 3 | 1366 px | 693 → 768 px |
| 768×1024 | 2 | 768 px | 949 → 1024 px |
| 390×844 | 1 | 390 px | 773 → 844 px |

Sin overflow horizontal en las cuatro resoluciones. Se revisaron tarjetas,
texto, canales, controles y banco expandido en móvil y en tarjeta estrecha de
escritorio. Las alturas de las tarjetas plegadas a 1440 fueron aproximadamente
238/238/291/264/313/433 px (luces/motor/hidráulico/servo/banco), sin alturas fijas
ni recortes; la expansión agrega desplazamiento vertical normal.
Los botones visibles medidos tienen al menos 44 px de altura.

Expansión por Enter y foco visible comprobados. Se cambió velocidad solicitada
50→51 con teclado, se plegó y se cambió de tab: el valor se conservó. Se observó
RUNNING y bloqueo de acciones, también dentro del banco expandido, durante la
regresión HTTP. Luego se ejecutó desde UI luz frontal ON/OFF y Todos ON: los ocho
canales confirmaron ON. STOP ALL confirmó luces/canales OFF, motor detenido,
hidráulico STOPPED y ángulo servo conservado. La parada sigue visible al hacer
scroll hasta los controles avanzados del banco.

## Apartado 15 — Reordenamiento de tarjetas (2026-10-11)

Se preservaron tabs, 3D y grilla compacta del apartado 14. Se agregaron
`ComponentGrid.tsx` (interacción de presentación) y `componentOrder.ts`
(validación, reordenamiento y almacenamiento); DevicePanel compone las mismas
tarjetas y controles dentro de esta grilla. No se modifica backend, firmware,
HTTP, useDevices, polling ni ejecución de comandos. STOP ALL sigue fuera del
DndContext y del área desplazable.

### Implementación

- Dependencias: `@dnd-kit/core` 6.3.1, `@dnd-kit/sortable` 10.0.0 y
  `@dnd-kit/utilities` 3.2.2, según documentación oficial del preset Sortable.
- Agarre independiente de 44×44 px por tarjeta. MouseSensor requiere 8 px de
  movimiento; TouchSensor usa pulsación de 250 ms con tolerancia de 5 px.
  Solo el agarre tiene listeners y touch-action:none; los controles no inician
  arrastres y el resto de la página mantiene desplazamiento táctil normal.
- KeyboardSensor con sortableKeyboardCoordinates: Espacio/Enter inicia y
  termina, flechas seleccionan destino, Escape cancela. Instrucciones y anuncios
  de nombre/posición en español; foco restaurado al agarre.
- Feedback con tarjeta atenuada, borde y DragOverlay de nombre sin duplicar
  controles. Overlay con z-index inferior a STOP ALL y sin captura de puntero.
  Sin animación de caída; CSS desactiva transiciones bajo reduced-motion y el
  desplazamiento de teclado usa auto, sin scroll suave impuesto por JavaScript.
- IDs estables como keys: mover/restablecer no desmonta controles. Restablecer
  se deshabilita durante el arrastre para evitar cambios simultáneos del orden.
- `localStorage` usa `fne.component-order.v1` y guarda exclusivamente un array
  de IDs. Se ignoran desconocidos, duplicados y valores inválidos; se incorporan
  nuevos componentes al final. JSON corrupto/almacenamiento inaccesible no rompe
  el panel; si falla la escritura, se informa que el orden dura solo la sesión.
- Orden predeterminado conserva el existente: frontales, laterales, motor,
  hidráulico, servo y banco. Restablecer guarda nuevamente este orden.

Archivos creados: `frontend/src/ComponentGrid.tsx`,
`frontend/src/componentOrder.ts`, `frontend/tests/component-order.test.mjs`.
Archivos modificados en este apartado: DevicePanel.tsx, dashboard.css,
package.json/package-lock.json, tsconfig.test.json, components.test.mjs y este
informe. No se sobrescribieron los cambios previos de la Iteración 10.

### Verificaciones de esta ejecución

| Verificación | Resultado |
| --- | --- |
| Build backend | APROBADA: 0 errores/advertencias. |
| Suite .NET/simulador | APROBADA: 422 comprobaciones y 60 operaciones concurrentes. |
| `npm.cmd --prefix frontend test` | APROBADA: 32 tests; se conservan las coberturas previas. |
| `npm.cmd --prefix frontend run build` | APROBADA: 78 módulos; principal 309.64 kB (98.16 gzip). Persiste aviso previo del chunk 3D de 928.54 kB. |
| smoke / contract / iteration-3 / iteration-4 | APROBADAS, secuencialmente en 127.0.0.1:5080. Incluyen Swagger/OpenAPI. |
| sequence-test | APROBADA: SHOW_FNE completo, orden/timing, exclusión, cancelación, STOP ALL e historial. |
| telemetry-test (Simulator) | APROBADA: fallas, protección, reconocimiento/reset y ausencia de reactivación. |
| fault-test / frontend-proxy-test | APROBADAS: errores reproducibles y seis rutas JSON en 5080/5173. |
| Búsquedas arquitectónicas de DESARROLLO | APROBADAS: sin coincidencias prohibidas en Program, Controllers, Domain/Application. |
| Mouse, teclado, persistencia y reset | APROBADAS en navegador integrado con API real. |
| Entrada táctil real | PENDIENTE: herramienta sin API de eventos táctiles/emulación táctil. Viewport móvil no equivale a esta prueba. |

Los nuevos tests verifican reconciliación de IDs, incorporación/eliminación de
componentes, datos corruptos, almacenamiento bloqueado, persistencia de IDs,
movimientos y no-ops/cancelación. Se comprueban agarres accesibles independientes,
controles y errores conservados, bloqueo operativo dentro de expansiones y
ausencia de comandos/API/polling en el componente de reordenamiento. Los tests
del apartado 14 se adaptaron al wrapper estable de tarjetas, preservando sus
aserciones funcionales.

La instalación inicial requirió reejecución autorizada por permisos de la caché
npm. `npm audit --json` informa la cadena preexistente source-map-js → postcss →
vite (alta, fixAvailable:false); no atribuirla a dnd-kit ni ejecutar actualización
global fuera del alcance. No se realizó integración HTTP ESP32/HAL adicional;
se conserva su pendiente de reejecución registrado más abajo.

### Evidencia visual y operativa

Se observaron cinco columnas a 1440×900, tres a 1366×768, dos a 768×1024 y una a
390×844. Anchos de documento 1440/1366/768/390 px: sin overflow horizontal.
STOP ALL mantuvo sus coordenadas inferiores 825→900 / 693→768 / 949→1024 /
773→844 px, también durante arrastre móvil.

Se movió Luces frontales a la segunda posición con teclado, se cambió de tab y
se recargó: el orden se conservó. Se arrastró Luces laterales con mouse de la
primera a la segunda posición: cambió el orden, con luces aún apagadas. En móvil
se desplazó Luces frontales hasta la tercera posición mediante teclado nativo,
con scroll y anuncios de destino; Escape se comprobó aparte sin modificar orden.
Durante la automatización, reenfocar el locator antes de cada flecha devolvía
scroll al agarre; se usaron teclas sobre el foco actual, sin cambiar el producto.

Se solicitó servo 119° sin aplicar (confirmado 120°), se reordenó su tarjeta y
se restableció: ambos valores siguieron diferenciados. El historial permaneció
vacío después de reordenamientos; posteriormente solo registró los tres comandos
explícitos de revisión: luz ON, motor FORWARD 50% y STOP ALL.

Con motor y luz activos, se inició un arrastre de la tarjeta del motor y se
pulsó STOP ALL: API confirmó luz/canales OFF, motor 0% detenido, hidráulico
STOPPED y servo conservado en 120°. El arrastre no impidió acceder a la parada;
luego se canceló con Escape. Los scripts HTTP ya habían terminado antes de estos
comandos manuales; no se interfirió con pruebas compartidas.

No se declara cierre total de Iteración 10: además del táctil real, permanecen
los límites de revisión de reduced-motion del sistema, WebGL y matriz HAL
descritos en sus secciones correspondientes. No se realizaron commits/push ni
se avanzó a Iteración 11.

Comprobación final tras el mensaje de continuidad: build frontend y 32 tests
repetidos, APROBADOS; `git diff --check` APROBADO. Los servidores habían dejado
de responder y se intentó recuperarlos conservando puertos. Vite inició en
5173; un segundo intento de API encontró 5080 ocupado y se dejó salir, sin
detener el proceso que ya lo atendía ni cambiar de puerto. La reapertura de la
pestaña de pruebas quedó bloqueada por la política del navegador al resolver
una página interna de error con protocolo no permitido. No se eludió el bloqueo.
Esta última comprobación de recarga queda PENDIENTE; no sustituye ni invalida
la persistencia y revisión visual realmente observadas antes en esta ejecución.

### Pendientes y límites de esta evidencia

No hay pendiente visual de la grilla en las cuatro resoluciones solicitadas.
No se repitió en esta continuación la integración HTTP ESP32→host.py/HAL ni
telemetría en modo ESP32: permanece PENDIENTE de una nueva ejecución si se
requiere revalidar esa matriz completa, sin atribuirle resultados anteriores.
No se reiniciaron procesos compartidos para cambiar de gateway.
Persisten los pendientes visuales previamente registrados para emulación del
sistema `prefers-reduced-motion` y pérdida/ausencia real de WebGL; este cambio
no agrega animaciones ni modifica esas rutas. Los tests de esas políticas
pasaron, pero no sustituyen revisión visual del sistema operativo.

No se hicieron commits ni push; no se avanzó a Iteración 11.
