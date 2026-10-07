# Arquitectura

## Objetivo

Separar la interfaz, lógica de aplicación y hardware para permitir que el
sistema funcione tanto con componentes simulados como con componentes físicos.

## Vista general

Usuario
   │
   ▼
┌───────────────────┐
│ Frontend React    │
└─────────┬─────────┘
          │ HTTP
          ▼
┌───────────────────┐
│ ASP.NET Core API  │
│ Controllers       │
└─────────┬─────────┘
          ▼
┌───────────────────┐
│ Application       │
│ Services          │
└─────────┬─────────┘
          ▼
┌───────────────────┐
│IComponentGateway  │
└─────┬────────┬────┘
      │        │
      ▼        ▼
 Simulator    ESP32
 Gateway      Gateway
      │        │
      ▼        ▼
 Estado      Wi-Fi
 virtual       │
               ▼
             ESP32
               │
        ┌──────┼──────┐
        ▼      ▼      ▼
      Luces  Motor  Actuador

## Fronteras

### Frontend

Conoce:

- DTOs HTTP;
- componentes;
- capabilities;
- estados.

No conoce:

- GPIO;
- drivers;
- firmware.

### Application

Conoce:

- componentes lógicos;
- comandos;
- reglas de aplicación;
- IComponentGateway.

No conoce detalles de comunicación física.

### Infrastructure

Implementa integración con:

- simulador;
- ESP32.

## Simulador

El SimulatorComponentGateway permite desarrollar el sistema sin hardware.

Debe poder representar:

- estado;
- transición;
- errores;
- componentes offline;
- timeouts.

Esto permite verificar frontend y backend antes de integrar el ESP32.

## Hardware real

El Esp32ComponentGateway implementará el mismo contrato.

Por lo tanto:

Application
     │
     ▼
IComponentGateway

no cambia al pasar de:

Simulator

a:

ESP32.

## Regla fundamental

Cambiar entre simulación y hardware no debe requerir modificar Controllers ni
frontend.

La selección del gateway se realiza mediante configuración.

## Secuencias

Las secuencias se ejecutan en backend.

Ejemplo:

Presentación
    │
    ├── luces frontales ON
    ├── esperar
    ├── barrido
    ├── esperar
    ├── motor FORWARD
    ├── esperar
    └── STOP

El navegador puede cerrarse sin que la temporización dependa de JavaScript del
cliente.

### Alcance histórico — Iteración 2

En Iteración 2 las secuencias todavía eran futuras. Esa entrega conservó exclusivamente las luces frontales, laterales, motor y parada global de la Iteración 1. La extensión actual está documentada al final de este archivo.

La implementación de Iteración 2 seguía estas responsabilidades:

- `Controllers/DevicesController`: frontera HTTP; depende de `IComponentService`. `DeviceMapping` convierte requests y resultados sin ejecutar reglas de componentes.
- `Contracts/DTOs`: datos del contrato HTTP, sin dependencias del dominio ni del simulador.
- `Application/Services/ComponentService`: consulta catálogo, valida comandos mediante Domain y coordina `IComponentGateway`.
- `Domain/Components`: catálogo, componente y estado. `Domain/Commands`: comando, resultado y validación, sin dependencias HTTP ni de infraestructura.
- `Infrastructure/Gateways/SimulatorComponentGateway`: estado virtual compartido, transiciones y exclusión mutua para comandos y STOP ALL.
- `Program.cs`: configuración, registro de dependencias y Controllers. El gateway es singleton y el servicio scoped. `ComponentGateway:Mode` selecciona Simulator; el gateway ESP32 todavía no está implementado.

Se conserva `/api/devices` por compatibilidad. STOP ALL conserva su ejecución aunque se desconecte el cliente; comandos posteriores pueden volver a activar componentes. No se incorporan coordinadores de secuencias ni bloqueos nuevos.

## Evolución

Fase 1:
SimulatorGateway

Fase 2:
SimulatorGateway + fallas

Fase 3:
protocolo ESP32

Fase 4:
firmware

Fase 5:
Esp32Gateway

Fase 6:
selección por configuración

## Extensión de Iteración 3

El banco light_bank sigue Controller → ComponentService → IComponentGateway → SimulatorComponentGateway. SimulatedLightBank encapsula sus patrones y reloj monotónico dentro de Infrastructure; toda lectura/escritura se serializa con el lock existente del gateway. Las lecturas calculan la fase según tiempo transcurrido, sin tareas de fondo ni temporización del navegador. STOP ALL cancela los efectos bajo el mismo lock.

Swagger se configura en Composition Root y se publica mediante middleware solo en Development; las operaciones siguen en Controllers. Los DTOs agregan campos opcionales del banco, omitidos en componentes existentes. El frontend muestra las instantáneas recibidas y mantiene únicamente el historial de sesión y valores de controles todavía no enviados.

## Iteración 4: posiciones y errores

SimulatedHydraulic y SimulatorFaultPlan viven en Infrastructure/Gateways. El primero integra el desplazamiento bajo el lock del gateway; el segundo rechaza comandos de forma determinista sin alterar la lógica normal. ComponentOperationException expresa códigos de dominio sin HTTP; DevicesController traduce esos códigos a respuestas HTTP sin conocer el simulador. La configuración del plan se inyecta en Composition Root.

StopAllAndGetStatesAsync recorre la misma abstracción de servicio/gateway y captura estados con exclusión mutua. No se incorporan rutas particulares para hidráulico o servo. El frontend representa posiciones recibidas y conserva errores por componente. Contratos completos en DESARROLLO.md.

## Iteración 5: secuencias

SequenceCatalog/SequenceDefinition/SequenceStep pertenecen a Domain y no conocen componentes concretos fuera de la definición de demostración. SequencesController mapea DTOs y delega en ISequenceService. SequenceService conserva ejecución y eventos en memoria; usa ComponentCommandExecutor, compartido con ComponentService, que aplica validación de Domain y llama IComponentGateway. No se duplica el despacho de comandos.

OperationCoordinator es singleton, al igual que ambos servicios de aplicación: serializa mutaciones con un semáforo, cancela el token de ejecución e invalida solicitudes en espera al recibir una parada. STOP ALL espera el comando que ya estaba en curso y después aplica la parada del gateway; ningún step pendiente puede ejecutarse después de su confirmación. SequenceLifetime aplica parada al cerrar el host. Program.cs solo registra estas dependencias.

Frontend consulta estados, ejecución y eventos; no temporiza pasos. El catálogo de esta entrega contiene SHOW_FNE. Contratos y políticas completos en ITERACION-5.md y DESARROLLO.md.

## Presentación confirmada — Iteración 6

useDevices conserva la sincronización única de dispositivos, ejecución y eventos. App entrega el mismo estado al panel y CarrozaView; model.ts adapta datos sin efectos laterales. CarrozaSvg/Visuals y el HUD representan canales, velocidad, dirección y posiciones recibidas; CSS solo anima la presentación. No hay consultas ni temporización de SHOW_FNE dentro del Twin. Contrato y decisiones visuales en DESARROLLO; evidencia en ITERACION-6.

Las secciones de iteraciones anteriores describen su contexto histórico. Para las reglas permanentes de cambios y cierre, consultar [AGENTS](../AGENTS.md) y [HARNESS](HARNESS.md).

## Frontera ESP32 — Iteración 7

Selección en Composition Root mediante AddComponentGateway: Simulator mantiene el gateway anterior; ESP32 registra Esp32ComponentGateway y un HttpEsp32Transport obtenido por HttpClientFactory. Endpoint/timeout provienen de configuración validada al arrancar. No se cambian Controllers, Application Services, IComponentGateway ni frontend para seleccionar implementación.

Esp32ComponentGateway traduce modelos Domain a contratos wire independientes, verifica correlación/versión, estado completo y rangos de la respuesta, y devuelve únicamente estados confirmados. No almacena ni extrapola estado físico. IEsp32Transport permite sustituir comunicación por un doble controlado; HttpEsp32Transport solo serializa/intercambia JSON y traduce fallas de comunicación, sin reglas de componentes ni reintentos.

ComponentExceptionFilter, registrado globalmente en MVC, cubre errores de consultas, STOP ALL y cancelación que antes no podían fallar por red. Conserva el formato público de error; GatewayErrorsOperationFilter documenta 500/503/504 en OpenAPI. Controllers permanecen sin dependencias concretas.

El protocolo se define en [ESP32-PROTOCOL](ESP32-PROTOCOL.md). Las garantías de exclusión backend se conservan; la seguridad física, las solicitudes demoradas por red y la confirmación de parada de una placa desconectada no se pueden garantizar desde este cliente. Sin firmware ni hardware implementado en esta entrega.
