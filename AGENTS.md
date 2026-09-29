# AGENTS.md — Simulador y Control de Componentes Electromecánicos

## Objetivo

Este repositorio implementa una plataforma web para simular y controlar
componentes electromecánicos.

El caso de aplicación inicial es una carroza técnica de la
Fiesta Nacional de los Estudiantes (FNE).

El sistema debe permitir trabajar inicialmente sin hardware mediante un
simulador y posteriormente utilizar un ESP32 sin modificar la lógica
principal de la aplicación.

## Documentación obligatoria

Antes de realizar cambios relevantes, consultar:

- `PLAN.md`: etapas y decisiones de implementación.
- `DESARROLLO.md`: ejecución, compilación y comprobaciones.
- `docs/ARCHITECTURE.md`: arquitectura y dependencias permitidas.
- `docs/COMPONENTS.md`: componentes y comportamiento esperado.
- `docs/ESP32-PROTOCOL.md`: contrato entre backend y ESP32.

Si se trabaja en frontend, consultar además:

- `frontend/AGENTS.md`

Si se trabaja en backend, consultar además:

- `backend/AGENTS.md`

## Arquitectura general

El flujo obligatorio es:

Usuario
  ↓
Frontend React
  ↓ HTTP
ASP.NET Core API
  ↓
Application Services
  ↓
IComponentGateway
  ├── SimulatorComponentGateway
  └── Esp32ComponentGateway
          ↓
        ESP32
          ↓
       Hardware

## Principios

### Frontend

El frontend representa el estado físico y envía intenciones del operador.

No conoce:

- GPIO;
- relés;
- drivers;
- protocolo interno del ESP32;
- detalles eléctricos.

Nunca debe comunicarse directamente con el ESP32.

### Backend

El backend contiene los casos de uso y coordina los componentes.

Los endpoints HTTP deben implementarse mediante ASP.NET Core Controllers.

No implementar endpoints de aplicación mediante Minimal APIs en Program.cs.

### Gateway

Toda interacción con componentes pasa por:

`IComponentGateway`

Deben existir implementaciones intercambiables:

- `SimulatorComponentGateway`
- `Esp32ComponentGateway`

La selección debe realizarse mediante configuración e inyección de dependencias.

### Simulador

El simulador debe reproducir comportamiento observable de los componentes
sin requerir hardware físico.

No debe existir lógica especial en Controllers para determinar si se utiliza
simulador o hardware real.

## Componentes iniciales

El diseño debe contemplar:

- LIGHT
- MOTOR
- HYDRAULIC_ACTUATOR
- SERVO

No asumir que solamente existirán luces y motores.

## Seguridad operacional

STOP ALL tiene prioridad conceptual sobre las operaciones normales.

Debe:

- detener motores;
- detener actuadores;
- cancelar secuencias;
- detener efectos;
- apagar iluminación cuando corresponda;
- actualizar el estado del sistema.

El frontend nunca debe asumir que un comando fue ejecutado hasta recibir
confirmación del backend.

## Desarrollo incremental

No implementar todo el sistema en una única modificación.

Trabajar por incrementos verificables.

Orden recomendado:

1. contratos;
2. simulador;
3. API;
4. frontend;
5. pruebas;
6. secuencias;
7. simulación de fallas;
8. protocolo ESP32;
9. firmware;
10. gateway real.

## Antes de implementar

Para tareas no triviales:

1. inspeccionar el código existente;
2. identificar los archivos afectados;
3. verificar las reglas del AGENTS.md correspondiente;
4. realizar el cambio mínimo necesario;
5. compilar;
6. ejecutar las pruebas relacionadas;
7. revisar que no se hayan violado las fronteras arquitectónicas.

No reestructurar partes no relacionadas con la tarea sin necesidad.

## Definition of Done

Una tarea no está terminada solamente porque compile.

Debe cumplirse, cuando corresponda:

- backend compila;
- frontend compila;
- tests pasan;
- smoke test pasa;
- comportamiento visual coincide con el estado;
- errores se muestran correctamente;
- no se introducen dependencias arquitectónicas prohibidas;
- documentación se actualiza si cambió un contrato.

Consultar `DESARROLLO.md` para los comandos concretos.