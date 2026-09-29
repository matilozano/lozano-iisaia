# Desarrollo local — Iteración 1

Requisitos: SDK .NET 9, Node.js 22 con npm y PowerShell 7 para los scripts de prueba.

## Ejecución

Desde la raíz:

```powershell
dotnet run --project backend/Carroza.Api
```

En otra terminal:

```powershell
npm --prefix frontend install
npm --prefix frontend run dev
```

Frontend: http://127.0.0.1:5173

API: http://localhost:5080/api/devices

Mantener libres los puertos 5080 y 5173. El proxy de Vite conecta el navegador con la API. No hay Swagger ni endpoints de secuencias en esta iteración.

## Compilación

```powershell
dotnet build backend/Carroza.Api
npm --prefix frontend run build
```

## Pruebas automatizadas

Con la API iniciada:

```powershell
./backend/smoke-test.ps1
./backend/contract-test.ps1
```

Suite del servicio y simulador, independiente del servidor HTTP:

```powershell
dotnet run --project backend/Carroza.Simulator.Tests
```

La suite .NET es un ejecutable de comprobaciones sin frameworks externos; se ejecuta con `dotnet run`, no con `dotnet test`. Los scripts HTTP comprueban catálogo, ambos sectores de luces, motor, dirección, límites 0/100, validaciones 400, componentes inexistentes 404, contrato JSON y parada general 204. Terminan con todos los componentes apagados/detenidos. También comprueban que Swagger y secuencias no estén expuestos.

Las comprobaciones de banco, secuencias y Swagger descritas en la documentación anterior no aplican al alcance solicitado de la Iteración 1.

## Verificación manual

1. Iniciar la API y abrir el frontend. Confirmar ambas luces apagadas y motor detenido.
2. Encender/apagar luces frontales y laterales; observar el indicador y consultar `GET /api/devices`.
3. Iniciar motor adelante, seleccionar 70% y pulsar Aplicar velocidad.
4. Detener el motor y reiniciarlo en reversa. Comprobar estado, sentido y velocidad.
5. Encender ambas luces e iniciar el motor. Pulsar Detener todo y confirmar luces apagadas y motor detenido a 0%.
6. Enviar velocidad 101 a la API y confirmar HTTP 400 sin cambio de estado (cubierto por contract-test).
7. Detener temporalmente la API: debe aparecer OFFLINE y un error conservando el último estado conocido. Reiniciar la API: el panel debe recuperar la conexión y mostrar el estado reiniciado.
8. Comprobar que los controles y Detener todo sean accesibles en móvil y notebook.

## Fronteras arquitectónicas

Estos comandos no deben encontrar coincidencias (código de salida 1 de rg significa que no hubo coincidencias):

```powershell
rg 'Map(Get|Post|Put|Delete|Patch)' backend/Carroza.Api/Program.cs
rg 'SimulatorComponentGateway|Esp32ComponentGateway|IComponentGateway|Infrastructure' backend/Carroza.Api/Controllers
rg 'Infrastructure|Controllers|Contracts|Microsoft.AspNetCore' backend/Carroza.Api/Application backend/Carroza.Api/Domain
```

## Contrato HTTP

- GET `/api/devices`: lista de los tres componentes con estado actual.
- GET `/api/devices/{id}`: estado de un componente; 404 si no existe.
- POST `/api/devices/{id}/commands`: 200 con confirmación; 400 ante comando inválido; 404 si no existe.
- POST `/api/devices/stop-all`: 204 tras apagar luces y detener motor.

Ejemplos de comandos:

```json
{ "action": "on" }
```

```json
{ "action": "start", "direction": "reverse", "speed": 70 }
```

```json
{ "action": "stop" }
```

El resultado de un comando conserva `deviceId`, `success`, `executedAt` y `state` como objeto completo (`id`, `state`, `online`, `direction`, `speed`). El navegador no debe inferir ejecución antes de la confirmación.
