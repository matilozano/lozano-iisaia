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

### Alcance actual — Iteración 2

Las secuencias descriptas arriba son una evolución futura, no una funcionalidad implementada. La Iteración 2 conserva exclusivamente las luces frontales, laterales, motor y parada global de la Iteración 1.

La implementación actual sigue estas responsabilidades:

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
