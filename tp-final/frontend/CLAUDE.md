# CLAUDE.md — frontend

Frontend del sistema de control de carroza técnica.

React + TypeScript + Vite. Este archivo describe cómo funciona el frontend y cuáles son sus límites respecto de la API y del hardware.

## Responsabilidad

El frontend es exclusivamente una interfaz de operación.

Permite al usuario:

- visualizar los dispositivos disponibles;
- consultar su estado;
- encender y apagar sectores de iluminación;
- controlar movimientos;
- modificar parámetros permitidos, como velocidad;
- iniciar y detener secuencias;
- ejecutar la parada general;
- visualizar errores de comunicación.

El frontend **no controla directamente el ESP32**.

Todo comando debe pasar por la API.

```text
Usuario
   ↓
Frontend
   ↓
API
   ↓
IDeviceGateway
   ↓
ESP32 / Simulator
```

Nunca implementar comunicación directa:

```text
Frontend → ESP32
```

## Contrato con la API

Toda comunicación HTTP se centraliza en la capa `api/`.

Los componentes React no deben realizar `fetch` o llamadas HTTP directamente.

```text
Component
   ↓
Hook / Service
   ↓
api/deviceApi.ts
   ↓
.NET API
```

Ejemplo:

```text
LightControl
     ↓
useDeviceCommand()
     ↓
deviceApi.sendCommand()
     ↓
POST /api/devices/{id}/commands
```

## Dispositivos

El frontend trabaja con dispositivos lógicos.

Ejemplos:

```text
front-lights
side-lights
main-motor
```

No debe conocer:

- números de GPIO;
- configuración de relés;
- drivers electrónicos;
- direcciones físicas de motores;
- implementación del firmware.

El frontend representa únicamente el estado informado por la API.

## Estado

Enviar un comando no significa que haya sido ejecutado.

No actualizar un dispositivo a `ON`, `RUNNING` o equivalente solamente porque el usuario presionó un botón.

Flujo esperado:

```text
Usuario pulsa ON
      ↓
UI = enviando
      ↓
POST command
      ↓
API confirma ejecución
      ↓
UI = ON
```

Si la operación falla:

```text
Usuario pulsa ON
      ↓
UI = enviando
      ↓
API devuelve error
      ↓
UI conserva estado anterior
      ↓
mostrar error
```

## Comandos

Las acciones disponibles dependen del tipo de dispositivo.

### LIGHT

```text
ON
OFF
```

### MOTOR

```text
START
STOP
FORWARD
REVERSE
SET_SPEED
```

No mostrar una operación que el dispositivo no soporte.

## Parada general

`STOP ALL` es una operación especial.

Debe permanecer visible en el panel principal y requerir una acción explícita del operador.

La UI solamente solicita la parada:

```text
POST /api/devices/stop-all
```

La lógica para determinar qué dispositivos detener pertenece al backend.

## Secuencias

El frontend puede:

- listar secuencias;
- iniciar una secuencia;
- visualizar cuál está ejecutándose;
- detener una secuencia.

El frontend **no ejecuta los pasos temporizados**.

Nunca implementar una secuencia mediante:

```javascript
setTimeout(...)
```

desde React.

La ejecución temporal pertenece al backend.

```text
Frontend
   │
   │ "Ejecutar Presentación"
   ▼
API
   │
   ├── paso 1
   ├── espera
   ├── paso 2
   ├── espera
   └── paso 3
```

Esto permite que una recarga del navegador no controle el funcionamiento de la carroza.

## Errores

La capa de API transforma los errores HTTP en errores utilizables por la interfaz.

El componente no interpreta códigos HTTP.

Ejemplo:

```text
503
 ↓
deviceApi
 ↓
"Controlador sin conexión"
 ↓
Component
```

Estados importantes:

```text
loading
sending
online
offline
error
```

No ocultar un error de comunicación cambiando visualmente el dispositivo como si el comando hubiera sido ejecutado.

## Componentes

Mantener los componentes pequeños y con una única responsabilidad.

Estructura prevista:

```text
src/
├── api/
│   └── deviceApi.ts
├── components/
│   ├── DeviceCard.tsx
│   ├── LightControl.tsx
│   ├── MotorControl.tsx
│   ├── SequencePanel.tsx
│   └── EmergencyStop.tsx
├── hooks/
│   ├── useDevices.ts
│   └── useDeviceCommand.ts
├── pages/
│   └── ControlPage.tsx
└── types/
    ├── device.ts
    └── command.ts
```

Si un componente empieza a contener lógica de comunicación, estado de hardware y presentación al mismo tiempo, dividirlo antes de continuar.

## Regla principal

El frontend representa y solicita.

No decide cómo controlar físicamente la carroza.

```text
Frontend = intención del operador

Backend = lógica de control

ESP32 = ejecución física
```
