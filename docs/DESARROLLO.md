# Desarrollo local — Iteración 3

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

Mantener libres los puertos 5080 y 5173. El proxy de Vite conecta el navegador con la API. En Development, Swagger UI está en http://localhost:5080/swagger y OpenAPI en /swagger/v1/swagger.json. No hay endpoints de secuencias.

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
./backend/iteration-3-test.ps1
```

Suite del servicio y simulador, independiente del servidor HTTP:

```powershell
dotnet run --project backend/Carroza.Simulator.Tests
```

La suite .NET es un ejecutable de comprobaciones sin frameworks externos; se ejecuta con `dotnet run`, no con `dotnet test`. Los scripts HTTP comprueban catálogo, ambos sectores de luces, motor, dirección, límites 0/100, validaciones 400, componentes inexistentes 404, contrato JSON y parada general 204. Terminan con todos los componentes apagados/detenidos. La prueba iteration-3-test comprueba Swagger UI, sus assets, cuatro operaciones OpenAPI y cinco DTOs, además de comandos y evolución temporal del banco. Las secuencias siguen devolviendo 404.

La suite .NET incluye pruebas del banco con reloj controlado: barridos, ida y vuelta, parpadeo, velocidad, congelamiento, validaciones y STOP ALL.

## Verificación manual

1. Iniciar la API y abrir el frontend. Confirmar ambas luces apagadas y motor detenido.
2. Encender/apagar luces frontales y laterales; observar el indicador y consultar `GET /api/devices`.
3. Iniciar motor adelante, seleccionar 70% y pulsar Aplicar velocidad.
4. Detener el motor y reiniciarlo en reversa. Comprobar estado, sentido y velocidad.
5. Encender ambas luces e iniciar el motor. Pulsar Detener todo y confirmar luces apagadas y motor detenido a 0%.
6. Enviar velocidad 101 a la API y confirmar HTTP 400 sin cambio de estado (cubierto por contract-test).
7. Detener temporalmente la API: debe aparecer OFFLINE y un error conservando el último estado conocido. Reiniciar la API: el panel debe recuperar la conexión y mostrar el estado reiniciado.
8. Comprobar que los controles y Detener todo sean accesibles en móvil y notebook.
9. En el banco probar barrido derecha, izquierda, ida y vuelta y parpadeo; observar ocho canales CH01..CH08 y contrastarlos con GET /api/devices/main-light-bank.
10. Probar velocidad 1 y 100; detener efecto (conserva patrón), Todos ON y Todos OFF.
11. Activar luces, motor y efecto; STOP ALL debe apagar iluminación y detener animaciones tras confirmación.
12. Verificar historial y último comando (OK/ERROR); recargar borra el historial, pero no el estado del backend.
13. Abrir /swagger, expandir cada operación y los esquemas; usar Try it out para consultar estados, ejecutar comandos de luz/motor/banco y STOP ALL. Verificar el resultado desde el panel.

## Fronteras arquitectónicas

Estos comandos no deben encontrar coincidencias (código de salida 1 de rg significa que no hubo coincidencias):

```powershell
rg 'Map(Get|Post|Put|Delete|Patch)' backend/Carroza.Api/Program.cs
rg 'SimulatorComponentGateway|Esp32ComponentGateway|IComponentGateway|Infrastructure' backend/Carroza.Api/Controllers
rg 'Infrastructure|Controllers|Contracts|Microsoft.AspNetCore' backend/Carroza.Api/Application backend/Carroza.Api/Domain
```

## Contrato HTTP

- GET `/api/devices`: lista de los cuatro componentes con estado actual.
- GET `/api/devices/{id}`: estado de un componente; 404 si no existe.
- POST `/api/devices/{id}/commands`: 200 con confirmación; 400 ante comando inválido; 404 si no existe.
- POST `/api/devices/stop-all`: 204 tras apagar luces y canales, detener efectos y motor.

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

## Contrato del banco — Iteración 3

Componente `main-light-bank`, type `light_bank`. Usa las mismas rutas genéricas. Los comandos existentes de luces/motor siguen en minúsculas; el banco admite `ALL_ON`, `ALL_OFF`, `SWEEP_RIGHT`, `SWEEP_LEFT`, `PING_PONG`, `BLINK`, `STOP_EFFECT`, `SET_SPEED`.

`SET_SPEED` requiere `{ "action": "SET_SPEED", "speed": 50 }`, rango 1..100. Los otros comandos del banco no admiten speed ni direction. Un comando inválido devuelve 400 sin cambiar la configuración.

Sus estados agregan `channels` (ocho booleanos CH01..CH08), `effect` (NONE o efecto activo) y `effectSpeed` (1..100). Estos campos se omiten para los tres componentes anteriores, cuyo JSON se conserva. `speed` sigue siendo el campo del motor; para el banco queda en 0. Estado del banco: running con efecto, on si hay canales encendidos sin efecto, off si todos están apagados.

El simulador utiliza tiempo monotónico: intervalo de paso = 1200 - 9 × effectSpeed milisegundos. Los barridos vuelven al inicio; ida y vuelta recorre 0..7..0 sin repetir extremos. Cambiar velocidad reinicia la fase del efecto. STOP_EFFECT congela los canales actuales. ALL_ON/ALL_OFF cancelan el efecto; STOP ALL apaga todos los canales y conserva effectSpeed. Al reiniciar el servidor se restablece velocidad 50 y canales apagados.

El panel consulta cada 150 ms después de finalizar la consulta anterior, sin generar canales localmente. Con latencia alta puede omitir cuadros intermedios, pero cada patrón mostrado fue confirmado por API. Historial limitado a 100 entradas por sesión. Gateway SIMULATOR y controlador SIMULADO describen el único modo soportado; no representan telemetría de hardware.