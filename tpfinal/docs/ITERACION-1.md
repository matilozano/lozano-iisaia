# Entrega — Iteración 1

Implementación de ASP.NET Core .NET 9 y React + TypeScript para dos luces y un motor simulado. El frontend consume la API real. No se incluyeron Swagger, ESP32, firmware, GPIO, hidráulicos, servos, banco de iluminación, secuencias, autenticación ni base de datos.

## Archivos creados

Backend:

- `backend/Carroza.Api/Carroza.Api.csproj`
- `backend/Carroza.Api/Program.cs`
- `backend/Carroza.Api/appsettings.json`
- `backend/Carroza.Api/Properties/launchSettings.json`
- `backend/Carroza.Api/Domain/Component.cs`
- `backend/Carroza.Api/Domain/ComponentCatalog.cs`
- `backend/Carroza.Api/Domain/CommandValidator.cs`
- `backend/Carroza.Api/Application/IComponentGateway.cs`
- `backend/Carroza.Api/Application/Services/IComponentService.cs`
- `backend/Carroza.Api/Application/Services/ComponentService.cs`
- `backend/Carroza.Api/Infrastructure/SimulatorComponentGateway.cs`
- `backend/Carroza.Api/Contracts/DeviceDtos.cs`
- `backend/Carroza.Api/Controllers/DevicesController.cs`
- `backend/smoke-test.ps1`
- `backend/contract-test.ps1`
- `backend/Carroza.Simulator.Tests/Carroza.Simulator.Tests.csproj`
- `backend/Carroza.Simulator.Tests/Program.cs`

Frontend:

- `frontend/package.json`
- `frontend/package-lock.json`
- `frontend/index.html`
- `frontend/tsconfig.json`
- `frontend/vite.config.ts`
- `frontend/src/main.tsx`
- `frontend/src/App.tsx`
- `frontend/src/api.ts`
- `frontend/src/useDevices.ts`
- `frontend/src/MotorControls.tsx`
- `frontend/src/styles.css`

Documentación creada: este informe.

## Archivos modificados

- `docs/PLAN.md`: alcance real de Iteración 1 y decisiones de implementación.
- `docs/DESARROLLO.md`: ejecución, compilación, contrato y verificaciones aplicables.

La documentación existente describía funcionalidades posteriores cuyos archivos ya no estaban en el repositorio. Se actualizaron esas referencias según el alcance solicitado. Se conservaron README.md y AGENTS.md. No se realizaron commits ni cambios de configuración de Git.

## Resultados de verificación

| Verificación | Resultado |
|---|---|
| `dotnet build backend/Carroza.Api` | OK; cero errores y advertencias |
| `npm --prefix frontend run build` | OK; TypeScript y bundle de Vite |
| `dotnet run --project backend/Carroza.Simulator.Tests` | OK; 28 comprobaciones y 60 operaciones concurrentes con parada final |
| `backend/smoke-test.ps1` | OK; catálogo, luces ON/OFF, motor adelante/reversa, 70%, consulta y parada general |
| `backend/contract-test.ps1` | OK; formato JSON, 0/100%, defaults, errores 400/404 sin mutación y STOP ALL 204 |
| Ausencia de Swagger y secuencias | OK; las rutas devuelven 404 |
| Fronteras arquitectónicas con rg | OK; sin Minimal APIs ni dependencias prohibidas en Controllers/Application/Domain |
| Integración HTTP mediante Vite | OK; comando por el proxy confirmado con consulta directa a la API |
| Backend desconectado y recuperado | OK a nivel HTTP; proxy devuelve 500 durante desconexión y vuelve a entregar tres estados iniciales tras reiniciar |
| Interacción visual, responsive y mensajes en navegador | Pendiente: la herramienta de navegador no inicia por archivos faltantes de su entorno; se reintentó tras reiniciarla |

Los pasos manuales para completar la revisión visual están en DESARROLLO.md. Las pruebas de secuencias, banco y Swagger de las iteraciones posteriores no corresponden a esta entrega.

## Ejecución

API: `dotnet run --project backend/Carroza.Api` → http://localhost:5080/api/devices

Frontend: `npm --prefix frontend run dev` → http://127.0.0.1:5173

El estado vive en memoria y se reinicia al reiniciar el backend. `ComponentGateway:Mode` selecciona `Simulator` por DI. El contrato `IComponentGateway` permite agregar otra implementación en una iteración futura.
