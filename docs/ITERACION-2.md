# Iteración 2 — Refactor arquitectónico

Fecha: 2026-09-29.

## Alcance y diagnóstico

Se inspeccionaron AGENTS.md, backend/AGENTS.md, frontend/AGENTS.md, README.md y todos los documentos de docs/ antes de modificar código. La implementación de la Iteración 1 ya utilizaba Controllers, servicios de aplicación e IComponentGateway. Program.cs ya era Composition Root. No fue necesario migrar endpoints ni reescribir casos de uso.

El ajuste organiza los archivos según la estructura objetivo del Harness y separa los DTOs del mapeo de dominio. Conserva ambas luces, el motor, las consultas de estado y STOP ALL. No se agregaron funcionalidades, componentes, paquetes, endpoints ni commits. El frontend no se modificó.

## Cambios por archivo

Rutas relativas a la raíz del repositorio:

| Archivo | Cambio |
| --- | --- |
| backend/Carroza.Api/Domain/Components/Component.cs | Modelo separado del anterior Domain/Component.cs. |
| backend/Carroza.Api/Domain/Components/ComponentState.cs | Estado separado del mismo archivo. |
| backend/Carroza.Api/Domain/Commands/ComponentCommand.cs | Comando separado del mismo archivo. |
| backend/Carroza.Api/Domain/Commands/ComponentResult.cs | Resultado separado del mismo archivo; se elimina el contenedor anterior. |
| backend/Carroza.Api/Domain/Components/ComponentCatalog.cs | Movido desde Domain/ComponentCatalog.cs; catálogo sin cambios. |
| backend/Carroza.Api/Domain/Commands/CommandValidator.cs | Movido desde Domain/CommandValidator.cs; reglas y mensajes conservados. |
| backend/Carroza.Api/Infrastructure/Gateways/SimulatorComponentGateway.cs | Movido desde Infrastructure/SimulatorComponentGateway.cs; mismo estado y sincronización. |
| backend/Carroza.Api/Contracts/DTOs/DeviceDtos.cs | Movido desde Contracts/DeviceDtos.cs; conserva todos los campos y elimina el método de conversión dependiente de Domain. |
| backend/Carroza.Api/Controllers/DeviceMapping.cs | Nuevo adaptador entre requests/resultados HTTP y modelos internos. |
| backend/Carroza.Api/Controllers/DevicesController.cs | Utiliza el adaptador; conserva respuestas y manejo de errores. |
| backend/Carroza.Api/Application/IComponentGateway.cs | Imports ajustados a los namespaces de dominio. |
| backend/Carroza.Api/Application/Services/IComponentService.cs | Imports ajustados. |
| backend/Carroza.Api/Application/Services/ComponentService.cs | Imports ajustados; casos de uso sin cambios. |
| backend/Carroza.Api/Program.cs | Imports ajustados para DI; sigue siendo Composition Root. |
| backend/Carroza.Simulator.Tests/Program.cs | Imports ajustados; mismas comprobaciones existentes. |
| docs/ARCHITECTURE.md | Describe las responsabilidades actuales y aclara que las secuencias son futuras. |
| docs/PLAN.md | Registra el alcance arquitectónico de esta iteración. |
| docs/ITERACION-2.md | Nuevo informe de cambios y resultados. |

## Compatibilidad y decisiones

Se mantiene Frontend → DevicesController → IComponentService → IComponentGateway → SimulatorComponentGateway. Application y Domain no conocen HTTP ni Infrastructure. Contracts/DTOs contiene únicamente datos de transporte; el mapeo pertenece a la frontera HTTP y no ejecuta reglas de negocio.

Los cuatro endpoints conservan rutas, payloads y códigos: GET /api/devices (200), GET /api/devices/{id} (200/404), POST /api/devices/{id}/commands (200/400/404) y POST /api/devices/stop-all (204). Se conserva el objeto state completo en la confirmación y los valores por defecto del motor: forward y 50%. No se renombran las rutas a /api/components para evitar romper clientes.

La configuración sigue seleccionando el gateway en Program.cs. Solo Simulator está implementado; ESP32 continúa pendiente. No se crean implementaciones vacías ni funcionalidades futuras para satisfacer ejemplos de la documentación. El lock compartido del simulador y la ejecución de STOP ALL pese a la desconexión del cliente se conservan. No se divide la solución en ensamblados nuevos: las fronteras existentes son suficientes para este incremento.

## Verificaciones ejecutadas

| Verificación | Resultado |
| --- | --- |
| dotnet build backend/Carroza.Api | OK: 0 errores y 0 advertencias en la ejecución final. El primer intento encontró el ejecutable bloqueado por la API anterior; se detuvo ese proceso y se repitió. |
| npm.cmd --prefix frontend run build | OK: TypeScript y Vite; 32 módulos compilados. |
| dotnet run --project backend/Carroza.Simulator.Tests | OK: 28 comprobaciones, 60 operaciones concurrentes y parada final. |
| ./backend/smoke-test.ps1 | OK: tres componentes, ambas luces ON/OFF, motor adelante/reversa a 70%, parada y consulta. |
| ./backend/contract-test.ps1 | OK: JSON, límites 0/100, defaults, errores 400/404 sin mutación, STOP ALL 204 y ausencia de Swagger/secuencias. |
| ./backend/smoke-test.ps1 -BaseUrl http://127.0.0.1:5173 | OK: operaciones reales a través del proxy de Vite hacia la API. |
| GET http://127.0.0.1:5173 | OK: HTTP 200. |
| API detenida y reiniciada | OK a nivel HTTP: proxy devuelve 500 durante desconexión y recupera los tres estados iniciales tras reiniciar. |
| Búsqueda de MapGet/Post/Put/Delete/Patch en Program.cs | OK: sin coincidencias. |
| Búsqueda de gateways concretos, IComponentGateway e Infrastructure en Controllers | OK: sin coincidencias. |
| Búsqueda de Infrastructure, Controllers, Contracts y Microsoft.AspNetCore en Application y Domain | OK: sin coincidencias. |
| Búsqueda de dependencias Domain/Application/Infrastructure en Contracts | OK: sin coincidencias. |
| git diff --check | OK: sin errores de whitespace. Git advierte normalización LF/CRLF en algunos archivos. |

Se ejecutaron las tres búsquedas arquitectónicas de DESARROLLO.md y una adicional sobre DTOs. La suite del proyecto es ejecutable con dotnet run, no dotnet test. Los scripts HTTP y las aserciones existentes se conservaron.

## Limitación de verificación manual

Se intentó iniciar la herramienta de navegador para recorrer el checklist visual de DESARROLLO.md, pero falló antes de abrir una sesión: `failed to write kernel assets: El sistema no puede encontrar la ruta especificada. (os error 3)`.

Por ello quedan pendientes la observación de luces/rotación/sentido/velocidad, el feedback visual OFFLINE y de errores, y la accesibilidad de controles en móvil/notebook. La revisión del código frontend y las comprobaciones HTTP respaldan la compatibilidad, pero no sustituyen esas verificaciones manuales. No se declara aprobación visual. Banco, efectos y secuencias no aplican al alcance de esta iteración.
