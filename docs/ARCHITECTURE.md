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

### Implementación de la iteración 3

`SequencesController` depende de `ISequenceService`. `SequenceService` consulta el catálogo de Domain y ejecuta los pasos mediante `IComponentGateway`, pasando por `OperationCoordinator`. Los comandos manuales de `ComponentService` pasan por el mismo coordinador.

El coordinador mantiene una exclusión de escrituras y una versión de admisión. STOP ALL cambia esa versión antes de esperar al gateway: los pasos y comandos que estaban en cola quedan invalidados. La cancelación interrumpe las esperas entre pasos. La confirmación de parada solo se envía después de que el gateway confirmó STOP ALL. Las consultas de estado siguen disponibles durante una secuencia.

El servicio de secuencias y el coordinador son singleton por el estado compartido entre solicitudes. `SequenceLifetime`, en Infrastructure, solicita cancelación y parada al apagar el host. El frontend consulta el estado; nunca administra los tiempos de ejecución.

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
