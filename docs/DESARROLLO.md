# Desarrollo local

Requisitos: SDK .NET 9 y Node.js 22 con npm.

Desde la raíz, iniciar la API:

```powershell
dotnet run --project backend/Carroza.Api
```

En otra terminal:

```powershell
cd frontend
npm install
npm run dev
```

Abrir la URL que indique Vite (habitualmente http://localhost:5173).
La API escucha en http://localhost:5080 y su diagnóstico está en /api/health.
En Development, Swagger UI está en http://localhost:5080/swagger y el documento OpenAPI en `/swagger/v1/swagger.json`.

## Compilación

```powershell
dotnet build backend/Carroza.Api
npm --prefix frontend run build
```

## Comprobación manual

Con la API iniciada, también se puede ejecutar la prueba automática desde PowerShell 7:

```powershell
./backend/smoke-test.ps1
./backend/contract-test.ps1
./backend/sequence-test.ps1
dotnet run --project backend/Carroza.Simulator.Tests
```

1. Confirmar dos luces apagadas y motor detenido.
2. Encender y apagar cada sector.
3. Iniciar motor a 70%, cambiar dirección y aplicar.
4. Detener todo y confirmar luces apagadas, motor detenido y velocidad cero.
5. Enviar velocidad 101 a la API y confirmar HTTP 400.

El panel muestra estados confirmados por la API. Los errores conservan el último estado conocido con una advertencia.

## Verificación del refactor

El smoke test y `contract-test.ps1` modifican el simulador y terminan con STOP ALL. El segundo verifica el contrato JSON, luces, motor, límites, valores predeterminados, errores 400/404 y respuesta 204 sin cuerpo.

`Carroza.Simulator.Tests` es una suite ejecutable .NET sin dependencias de frameworks de tests: se ejecuta con `dotnet run`, no con `dotnet test`. Verifica efectos mediante reloj controlado, ejecución de secuencias, cancelación, fallas y prioridad de STOP frente a comandos en espera, incluyendo 80 carreras de inicio/parada.

`sequence-test.ps1` requiere la API en Development. Comprueba catálogo, 202/404/409, continuidad después de responder al inicio, cancelación, STOP ALL, finalización natural y documentación OpenAPI. Modifica el estado del simulador y termina en parada general.

Verificar las fronteras (estos comandos no deben devolver coincidencias):

```powershell
rg 'Map(Get|Post|Put|Delete|Patch)' backend/Carroza.Api/Program.cs
rg 'SimulatorComponentGateway|Esp32ComponentGateway|IComponentGateway|Infrastructure' backend/Carroza.Api/Controllers
rg 'Infrastructure|Controllers|Contracts|Microsoft.AspNetCore' backend/Carroza.Api/Application backend/Carroza.Api/Domain
```

El modo se configura en `backend/Carroza.Api/appsettings.json` mediante `ComponentGateway:Mode`, actualmente `Simulator`. Un valor no implementado impide el arranque para evitar usar silenciosamente otro gateway.

## Verificación visual de secuencias

1. Verificar el control manual de luces, motor, dirección, velocidad y banco antes de iniciar.
2. Iniciar Presentación; confirmar estado En ejecución y progreso. Los controles manuales deben quedar bloqueados.
3. Recargar el navegador mientras corre: la ejecución y sus estados deben recuperarse desde la API.
4. Dejar finalizar: estado Completada, motor detenido y luces/canales apagados.
5. Iniciar Efecto de luces y pulsar Detener secuencia: estado Cancelada y todos los componentes detenidos.
6. Iniciar Final y pulsar DETENER TODO: ninguna acción pendiente debe volver a encender componentes después de confirmarse la parada.
7. Comprobar que se recupera el control manual tras cancelar y que STOP ALL sigue visible en móvil/tablet.
8. En Swagger, probar GET `/api/sequences` y POST `/api/sequences/{id}/execute`. Un segundo inicio durante ejecución devuelve 409. Cancelar con POST `/api/sequences/stop`.
9. Detener temporalmente la API: el panel debe informar OFFLINE, conservar el último estado conocido y recuperar la conexión al reiniciarla.

Los pasos automáticos se reflejan en el progreso y en los componentes; el historial del navegador registra las órdenes del operador de esa sesión, no es un historial persistente del backend.
