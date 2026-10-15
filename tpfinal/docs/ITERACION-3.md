# Iteración 3 — Simulador visual y Swagger/OpenAPI

Fecha: 2026-09-29.

## Alcance entregado

Se leyeron los AGENTS.md de raíz, backend y frontend y todos los documentos existentes en docs/ antes de modificar código. Se conservó la arquitectura de Iteración 2 y el comportamiento de luces/motor. Las luces ya tenían brillo y el motor ya giraba según dirección y velocidad confirmadas; esas representaciones se conservaron y se integraron con el nuevo panel.

Se incorporó Swagger en Development, banco de ocho canales controlado por backend, estado del sistema e historial de sesión. No se implementaron ESP32, firmware, GPIO, hidráulicos, servo, autenticación, persistencia ni secuencias complejas. No se hicieron commits.

## Archivos creados

- backend/Carroza.Api/Infrastructure/Gateways/SimulatedLightBank.cs: patrones y temporización del banco.
- backend/Carroza.Simulator.Tests/LightBankTests.cs: pruebas deterministas con reloj controlado.
- backend/iteration-3-test.ps1: verificación HTTP de Swagger/OpenAPI y banco.
- frontend/src/LightBank.tsx: ocho indicadores, controles de efectos y velocidad.
- docs/ITERACION-3.md: este informe.

## Archivos modificados

| Archivo | Cambio |
| --- | --- |
| backend/Carroza.Api/Carroza.Api.csproj | Swashbuckle.AspNetCore 10.2.3 y documentación XML. |
| backend/Carroza.Api/Program.cs | Registro de OpenAPI y middleware Swagger/UI solo en Development. |
| backend/Carroza.Api/Controllers/DevicesController.cs | Descripciones, ejemplos y tipos/códigos de respuesta para OpenAPI. |
| backend/Carroza.Api/Controllers/DeviceMapping.cs | Mapeo de los campos opcionales del banco. |
| backend/Carroza.Api/Contracts/DTOs/DeviceDtos.cs | Descripción de DTOs/comandos; canales, efecto y velocidad opcionales. |
| backend/Carroza.Api/Domain/Components/ComponentCatalog.cs | Cuarto componente main-light-bank, type light_bank. |
| backend/Carroza.Api/Domain/Components/ComponentState.cs | Estado de canales, efecto y velocidad del efecto. |
| backend/Carroza.Api/Domain/Commands/CommandValidator.cs | Validaciones del banco. |
| backend/Carroza.Api/Infrastructure/Gateways/SimulatorComponentGateway.cs | Integra bancos bajo el lock existente; extiende STOP ALL. |
| backend/Carroza.Simulator.Tests/Program.cs | Ejecuta las nuevas pruebas además de las existentes. |
| backend/smoke-test.ps1 | Espera cuatro componentes y verifica parada del banco. |
| backend/contract-test.ps1 | Catálogo ampliado; conserva aserciones de JSON anterior y secuencias 404. La prueba de ausencia de Swagger se reemplaza por comprobaciones positivas en iteration-3-test. |
| frontend/src/api.ts | Tipos y comandos del banco. |
| frontend/src/useDevices.ts | Polling de canales, historial de 100 comandos, STOP ALL extendido. |
| frontend/src/App.tsx | Integra banco, estado del sistema e historial; conserva luces y motor. |
| frontend/src/styles.css | Diseño responsive de canales, controles, estado e historial. |
| docs/PLAN.md | Alcance actualizado de Iteración 3. |
| docs/ARCHITECTURE.md | Responsabilidades del banco y Swagger. |
| docs/COMPONENTS.md | Contrato implementado del banco. |
| docs/DESARROLLO.md | Ejecución, pruebas, checklist visual y contrato actualizado. |

## Endpoints y compatibilidad

No se agregaron rutas de aplicación: los cuatro endpoints genéricos existentes cubren también el banco.

- GET /api/devices: agrega main-light-bank al catálogo.
- GET /api/devices/{id}: admite consultar el banco y sus ocho canales.
- POST /api/devices/{id}/commands: admite comandos del banco; conserva comandos anteriores y HTTP 200/400/404.
- POST /api/devices/stop-all: también cancela efectos y apaga canales; conserva 204 sin cuerpo.

Swagger agrega /swagger (UI), /swagger/index.html y /swagger/v1/swagger.json en Development. La UI utiliza las rutas reales de los Controllers y muestra cinco DTOs, descripciones, ejemplos y códigos de respuesta. Program.cs sigue siendo Composition Root, sin endpoints de aplicación.

Los campos channels, effect y effectSpeed solo se serializan para el banco. El JSON de los tres componentes anteriores se conserva. Los comandos de luces/motor siguen en minúsculas. Los del banco son ALL_ON, ALL_OFF, SWEEP_RIGHT, SWEEP_LEFT, PING_PONG, BLINK, STOP_EFFECT y SET_SPEED.

## Decisiones técnicas

- Flujo conservado: Frontend → Controller → ComponentService → IComponentGateway → SimulatorComponentGateway. Los Controllers no conocen gateways y el servicio no depende de Infrastructure.
- El simulador calcula canales según tiempo monotónico de TimeProvider al leer el estado. Esto evita tareas en segundo plano y mantiene efectos independientes del navegador. Todas las operaciones comparten el lock del gateway.
- Velocidad del banco 1..100, inicial 50; cada paso dura 1200 - 9 × velocidad ms. A máxima velocidad son 300 ms. Cambiar velocidad reinicia la fase. El motor mantiene 0..100 y sus defaults previos.
- STOP_EFFECT congela el patrón, ALL_ON/ALL_OFF cancelan el efecto, STOP ALL apaga todos los canales y detiene el motor. La velocidad configurada del banco se conserva durante la parada.
- El frontend consulta cada 150 ms después de terminar la consulta previa. Los canales provienen de la API, sin temporizadores locales de efectos. En conexiones lentas pueden omitirse cuadros intermedios; no se inventan estados.
- Durante el envío se deshabilitan los controles normales y se mantiene accesible la parada general. La representación se actualiza al recibir confirmación. El 204 de STOP ALL confirma el resultado documentado de apagado/parada, que se refleja inmediatamente y se refresca por polling. Ante error se mantiene el último estado confirmado.
- Gateway SIMULATOR y controlador SIMULADO describen el único modo soportado, no sensores ni telemetría real. Historial local de hasta 100 entradas con hora, componente, comando y OK/ERROR; se pierde al recargar.
- Se conservan el brillo de luces y la animación del motor existente, incluidos sentido, porcentaje confirmado y preferencia de movimiento reducido.

## Resultados de las verificaciones

| Verificación ejecutada | Resultado |
| --- | --- |
| dotnet build backend/Carroza.Api | OK: cero errores y advertencias. El primer intento dentro del sandbox no pudo leer NuGet.Config; se repitió con permisos y compiló. |
| npm.cmd --prefix frontend run build | OK: TypeScript y Vite, 33 módulos. |
| dotnet run --project backend/Carroza.Simulator.Tests | OK: 30 verificaciones originales adaptadas al cuarto componente, 60 operaciones concurrentes, más 75 verificaciones deterministas del banco. |
| ./backend/smoke-test.ps1 | OK: cuatro componentes, luces ON/OFF, motor adelante/reversa/stop, consulta y parada global incluyendo banco. |
| ./backend/contract-test.ps1 | OK: JSON anterior, límites/defaults motor, errores 400/404 sin mutación, 204 y secuencias ausentes. |
| ./backend/iteration-3-test.ps1 | OK: UI y bundle Swagger HTTP 200, cuatro operaciones y cinco DTOs en OpenAPI, documentación de comandos/canales/respuesta 400. Todos los comandos del banco, avance temporal, congelamiento, velocidad inválida y STOP ALL por HTTP. |
| GET /swagger | OK: HTTP 200 tras redirección convencional. |
| ./backend/smoke-test.ps1 -BaseUrl http://127.0.0.1:5173 | OK: integración real por proxy Vite. |
| Desconexión y recuperación de API por proxy | OK HTTP: 500 con backend detenido; al reiniciar devuelve cuatro componentes y banco apagado. |
| rg Map(Get/Post/Put/Delete/Patch) en Program.cs | OK: sin coincidencias. |
| rg gateways concretos/IComponentGateway/Infrastructure en Controllers | OK: sin coincidencias. |
| rg Infrastructure/Controllers/Contracts/Microsoft.AspNetCore en Application y Domain | OK: sin coincidencias. |
| git diff --check | OK: sin errores de whitespace; avisos de normalización LF/CRLF de Git. |

La suite determinista verifica recorridos completos y rebotes, parpadeo, cambio de velocidad, congelamiento, rechazo de comandos y ausencia de reactivación tras STOP ALL para cada efecto. No se reemplazaron las verificaciones existentes por pruebas exclusivamente del banco.

## Verificaciones pendientes

Se intentó iniciar la herramienta de navegador, pero falló antes de abrir una sesión: `failed to write kernel assets: El sistema no puede encontrar la ruta especificada. (os error 3)`.

Queda pendiente observar en navegador el brillo, rotación/sentido/velocidad, barridos y parpadeo; verificar feedback de envío/error/OFFLINE, historial y responsive en móvil/tablet/notebook; y operar Try it out desde Swagger. La disponibilidad HTTP de Swagger, sus esquemas y los comandos se verificó automáticamente, pero no se declara aprobada su interacción visual. El checklist reproducible está en DESARROLLO.md.
