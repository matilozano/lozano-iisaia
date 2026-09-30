# AGENTS.md — Backend

## Stack

Backend:

- ASP.NET Core
- .NET 9
- Controllers
- Dependency Injection

## Arquitectura obligatoria

La dependencia debe seguir:

Controller
    ↓
Application Service
    ↓
IComponentGateway
    ├── SimulatorComponentGateway
    └── Esp32ComponentGateway

No saltar capas.

## Program.cs

`Program.cs` es exclusivamente Composition Root.

Puede contener:

- creación del WebApplicationBuilder;
- registro de Controllers;
- Dependency Injection;
- middleware;
- configuración;
- MapControllers();
- startup de la aplicación.

No debe contener:

- endpoints de aplicación;
- lógica de componentes;
- estado del simulador;
- reglas de luces;
- reglas de motores;
- reglas hidráulicas;
- secuencias;
- comunicación con ESP32.

No utilizar para endpoints de aplicación:

- MapGet
- MapPost
- MapPut
- MapDelete
- MapPatch

Los endpoints deben implementarse con ASP.NET Core Controllers.

## Controllers

Los Controllers representan exclusivamente la frontera HTTP.

Ejemplo:

ComponentsController
        ↓
IComponentService
        ↓
IComponentGateway

Un Controller puede:

- recibir requests;
- validar datos de entrada;
- invocar servicios;
- traducir resultados a HTTP.

Un Controller no puede:

- administrar estado del simulador;
- controlar directamente un motor;
- ejecutar secuencias;
- conocer GPIO;
- llamar directamente al ESP32;
- instanciar gateways concretos.

Mantener Controllers pequeños.

## Application Services

Los casos de uso pertenecen a Application.

Ejemplos:

- GetComponents
- GetComponentState
- ExecuteCommand
- StopAll
- StartSequence
- StopSequence

La lógica de aplicación no pertenece a Controllers.

## Contratos

Las capas superiores dependen de abstracciones.

Contrato principal:

`IComponentGateway`

Responsabilidades:

- obtener estado;
- ejecutar comandos;
- detener componentes;
- reportar resultados.

No exponer detalles del ESP32 en este contrato.

## Infrastructure

Infrastructure contiene implementaciones externas.

Ejemplo:

Infrastructure/
  Gateways/
    SimulatorComponentGateway.cs
    Esp32ComponentGateway.cs

`SimulatorComponentGateway` mantiene el estado virtual.

`Esp32ComponentGateway` traduce los comandos al protocolo definido en
`docs/ESP32-PROTOCOL.md`.

Ambos implementan el mismo contrato.

## Modelo de componentes

No crear endpoints específicos como:

- `/api/front-light/on`
- `/api/motor1/start`

Preferir recursos genéricos.

Ejemplo:

GET /api/components

GET /api/components/{id}

POST /api/components/{id}/commands

POST /api/components/stop-all

Los comandos dependen de las capabilities del componente.

## Respuestas

Un comando exitoso debe devolver estado confirmado.

Enviar un comando no significa asumir que se ejecutó correctamente.

El resultado debe permitir distinguir:

- éxito;
- comando inválido;
- componente inexistente;
- componente offline;
- timeout;
- error del controlador.

## Concurrencia

El estado simulado compartido debe protegerse frente a operaciones concurrentes.

STOP ALL debe ejecutarse de forma consistente respecto de los demás comandos.

No introducir concurrencia innecesaria.

## Validaciones

Ejemplos:

Motor:

- velocidad: 0..100;
- dirección válida;
- comando soportado.

Servo:

- posición: 0..180.

Actuador:

- posición: 0..100.

Los valores inválidos deben producir errores HTTP 4xx apropiados.

## Estructura objetivo

Mantener una estructura equivalente a:

backend/Carroza.Api/
  Controllers/
  Application/
    Services/
  Domain/
    Components/
    Commands/
  Contracts/
    DTOs/
  Infrastructure/
    Gateways/
  Program.cs

La estructura puede evolucionar, pero debe conservar la separación de
responsabilidades.

## Verificación obligatoria

Antes de finalizar un cambio:

1. ejecutar:

   `dotnet build backend/Carroza.Api`

2. ejecutar los tests existentes;

3. ejecutar el smoke test cuando corresponda;

4. buscar en `Program.cs`:

   - MapGet
   - MapPost
   - MapPut
   - MapDelete
   - MapPatch

   No deben existir endpoints de aplicación implementados de esta manera.

5. comprobar que Controllers no dependan de:

   - SimulatorComponentGateway
   - Esp32ComponentGateway

Deben depender de servicios/abstracciones.