# Plan de implementación

1. Base inicial: contratos, API .NET, gateway simulado y panel React.
2. Completar contratos: catálogo independiente del simulador, errores uniformes, historial y pruebas automatizadas.
3. Secuencias: ejecución en backend, cancelación y prioridad de parada general.
4. Simulación de fallas y verificación de interfaz con Playwright.
5. Contrato HTTP del ESP32, firmware y gateway real seleccionable por configuración.

## Decisiones de la base inicial

- .NET 9 según SDK disponible; React, TypeScript y Vite.
- Estado en memoria: se reinicia al reiniciar la API.
- Luces admiten on/off; motor admite start con dirección y velocidad, y stop.
- Ejecutar start sobre un motor activo actualiza dirección y velocidad.
- La parada general actualiza todos los dispositivos bajo el mismo bloqueo del simulador.
- El resultado HTTP conserva `deviceId` y un estado completo; internamente se utilizan `ComponentResult` y `ComponentState`.
- Vite redirige /api a localhost:5080 durante desarrollo. Un despliegue deberá configurar un proxy equivalente.
- La base inicial no incluía historial, secuencias, hardware ni prioridad de parada ante comandos concurrentes. Los avances posteriores se detallan abajo.

## Refactor de backend

- `Program.cs` contiene exclusivamente configuración, DI y arranque de Controllers.
- Flujo: Controllers → IComponentService → IComponentGateway → SimulatorComponentGateway.
- Domain contiene catálogo, estados, comandos y validaciones; Application coordina los casos de uso; Infrastructure mantiene el estado simulado con un bloqueo compartido para comandos y STOP ALL.
- Los DTOs HTTP conservan `/api/devices`, nombres, valores predeterminados, errores de comandos y STOP ALL con HTTP 204. Las capabilities son internas en este incremento para no cambiar el contrato público.
- `ComponentGateway:Mode` selecciona el gateway en DI. Solo `Simulator` está implementado; otro valor falla explícitamente al iniciar. ESP32 sigue pendiente.
- El catálogo ya no pertenece al simulador y el tipo no se deduce del identificador. No se agregan componentes ni funciones nuevas.

## Iteración 2 — Simulador visual y Swagger (implementada)

- Banco `main-light-bank` con ocho canales y efectos calculados en backend; el navegador consulta estados cada 100 ms, sin temporizar el efecto.
- Luces con brillo, rotor con dirección/velocidad confirmadas, estado de conexión e historial de sesión (hasta 100 entradas, sin persistencia).
- Swagger UI en `/swagger` y documento en `/swagger/v1/swagger.json`, solo en Development.
- El listado incorpora capabilities y el banco agrega channels/effect. Los comandos y tipos del contrato HTTP continúan en minúsculas.

## Iteración 3 — Secuencias, cancelación y prioridad de parada (implementada)

Alcance correspondiente al punto 3 de este plan:

- Tres secuencias predefinidas: `presentation` (7 s), `light-show` (15,5 s) y `final` (6 s).
- Los pasos se temporizan en Application con reloj monotónico del servidor. No dependen del navegador ni del token HTTP de la solicitud inicial.
- Se permite una ejecución por vez. Los comandos manuales y un segundo inicio reciben 409 mientras hay una secuencia activa.
- Al iniciar, finalizar o cancelar se detienen todos los componentes. Una falla de un paso también solicita parada y queda visible en el estado de ejecución.
- `OperationCoordinator` serializa escrituras y da prioridad a STOP ALL: invalida trabajo en espera antes de adquirir el acceso al gateway y bloquea nuevas admisiones durante la parada. Una operación ya iniciada en el gateway termina antes de la parada; una orden nueva, posterior a la confirmación, puede volver a activar componentes.
- STOP ALL cancela la secuencia, detiene el motor, apaga luces y banco y conserva HTTP 204. La parada aceptada continúa ante desconexión del cliente.
- El panel lista secuencias, inicia/cancela y muestra estado y progreso confirmados. La recarga recupera la ejecución actual. Las respuestas previas a una parada no sobrescriben su confirmación.
- Secuencias y último resultado viven en memoria; reiniciar el backend reinicia el estado. No se agregan secuencias editables, hardware, autenticación ni persistencia.
- Endpoints: GET `/api/sequences`, GET `/api/sequences/status`, POST `/api/sequences/{id}/execute` (202/404/409), POST `/api/sequences/stop` (200).
