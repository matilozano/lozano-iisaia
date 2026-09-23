# Trabajo Práctico Final — Sistema de Control de Carrozas

## 1. Objetivo

Desarrollar una plataforma web para controlar dispositivos electrónicos utilizados en una carroza técnica, permitiendo operar iluminación y movimientos mecánicos desde una interfaz gráfica.

El sistema estará compuesto por tres componentes principales:

- Un frontend web para la operación de la carroza.
- Una API que gestione dispositivos, comandos, estados y secuencias.
- Un ESP32 encargado de interactuar con los componentes electrónicos físicos.

El operador podrá realizar acciones como:

- Encender y apagar sectores de iluminación.
- Encender y detener motores.
- Controlar el sentido de movimiento de determinados mecanismos.
- Regular la velocidad de motores cuando el dispositivo lo permita.
- Ejecutar secuencias predefinidas de movimientos e iluminación.
- Detener los dispositivos desde un comando general de parada.

El proyecto contará además con un simulador de hardware para permitir desarrollar, probar y verificar el sistema sin necesidad de disponer permanentemente de un ESP32 y de los dispositivos físicos.

---

# 2. Arquitectura

La arquitectura estará dividida en tres niveles principales:

```text
┌─────────────────────────────┐
│          FRONTEND           │
│     React + TypeScript      │
│                             │
│ Panel de control            │
│ Estado de dispositivos      │
│ Ejecución de secuencias     │
└──────────────┬──────────────┘
               │
               │ HTTP / REST
               ▼
┌─────────────────────────────┐
│          API .NET           │
│                             │
│ Devices                     │
│ Commands                    │
│ Sequences                   │
│ Command History             │
│                             │
│      IDeviceGateway         │
└──────────────┬──────────────┘
               │
        ┌──────┴───────┐
        │              │
        ▼              ▼
┌───────────────┐ ┌───────────────┐
│   Simulator   │ │ Esp32Gateway  │
│    Gateway    │ │               │
└───────────────┘ └───────┬───────┘
                          │
                       HTTP/Wi-Fi
                          │
                          ▼
                  ┌───────────────┐
                  │     ESP32     │
                  │               │
                  │ GPIO          │
                  │ PWM           │
                  │ Relés         │
                  │ Drivers       │
                  └───────┬───────┘
                          │
                 ┌────────┴────────┐
                 ▼                 ▼
              Luces             Motores
```

Una decisión central de la arquitectura será mantener desacoplada la aplicación del hardware.

La API no accederá directamente a GPIO, motores o relés. La comunicación se realizará mediante una abstracción denominada `IDeviceGateway`.

Esto permitirá utilizar dos implementaciones:

```text
IDeviceGateway
      │
      ├── SimulatorDeviceGateway
      │
      └── Esp32DeviceGateway
```

De esta manera, el frontend y la lógica de negocio serán exactamente los mismos independientemente de si el sistema está utilizando hardware real o simulado.

---

# 3. Frontend

El frontend será desarrollado utilizando:

- React
- TypeScript
- Vite

Presentará un panel de operación de la carroza.

Ejemplo:

```text
┌────────────────────────────────────────────┐
│           CONTROL DE CARROZA               │
├────────────────────────────────────────────┤
│                                            │
│ ILUMINACIÓN                                │
│                                            │
│ Luces frontales       ● ENCENDIDAS         │
│ [ ENCENDER ] [ APAGAR ]                    │
│                                            │
│ Luces laterales       ○ APAGADAS           │
│ [ ENCENDER ] [ APAGAR ]                    │
│                                            │
├────────────────────────────────────────────┤
│ MOVIMIENTOS                                │
│                                            │
│ Alas                                       │
│ [ ◀ CERRAR ] [ STOP ] [ ABRIR ▶ ]          │
│                                            │
│ Velocidad                                  │
│ ───────────────●────── 70%                 │
│                                            │
├────────────────────────────────────────────┤
│ SECUENCIAS                                 │
│                                            │
│ [ ▶ PRESENTACIÓN ]                         │
│ [ ▶ EFECTO DE LUCES ]                      │
│ [ ▶ FINAL ]                                │
│                                            │
├────────────────────────────────────────────┤
│                                            │
│           [ DETENER TODO ]                 │
│                                            │
└────────────────────────────────────────────┘
```

El frontend no tendrá conocimiento de GPIO, direcciones IP del ESP32 ni protocolos físicos.
El ESP32 es un microcontrolador en formato de sistema en un chip (SoC) de bajo costo y consumo energético que incluye Wi-Fi y Bluetooth integrados. Fue creado por la empresa Espressif Systems.Características principalesProcesador: Cuenta con un microprocesador de doble núcleo (o un solo núcleo según la variante) que funciona a una velocidad de hasta 240 MHz.Conectividad: Dispone de Wi-Fi de 2.4 GHz y Bluetooth de modo dual (incluyendo Bluetooth estándar y BLE de bajo consumo).Pines de E/S (GPIO): Incluye múltiples pines programables para conectar sensores, pantallas y motores, además de interfaces como I2C, UART, SPI, salidas PWM y convertidores analógicos a digital (ADC).Bajo consumo: Está diseñado para optimizar la energía, lo que permite usarlo en dispositivos que funcionan con baterías.

Su único contrato será la API REST.

---

# 4. API

La API será desarrollada con .NET.

Sus responsabilidades serán:

- Administrar los dispositivos.
- Recibir comandos del frontend.
- Validar comandos.
- Enviar comandos al gateway correspondiente.
- Consultar el estado de los dispositivos.
- Ejecutar secuencias.
- Registrar el resultado de las operaciones.
- Informar errores de comunicación con el hardware.

La API no dependerá directamente del ESP32.

Utilizará el contrato:

```csharp
public interface IDeviceGateway
{
    Task<DeviceResult> ExecuteAsync(
        DeviceCommand command,
        CancellationToken cancellationToken);

    Task<DeviceState> GetStateAsync(
        string deviceId,
        CancellationToken cancellationToken);
}
```

---

# 5. Dispositivos

Para la primera versión se utilizarán tres dispositivos.

| Dispositivo | Tipo | Operaciones |
|---|---|---|
| Luces frontales | LIGHT | ON / OFF |
| Luces laterales | LIGHT | ON / OFF |
| Motor principal | MOTOR | START / STOP / dirección / velocidad |

El modelo podrá extenderse posteriormente a:

- Servomotores.
- Motores adicionales.
- Sensores.
- Nuevos sectores de iluminación.

---

# 6. Endpoints iniciales

## Obtener dispositivos

```http
GET /api/devices
```

Respuesta:

```json
[
  {
    "id": "front-lights",
    "name": "Luces frontales",
    "type": "light",
    "state": "on"
  },
  {
    "id": "main-motor",
    "name": "Motor principal",
    "type": "motor",
    "state": "stopped"
  }
]
```

---

## Obtener estado

```http
GET /api/devices/{id}
```

Ejemplo:

```http
GET /api/devices/front-lights
```

Respuesta:

```json
{
  "id": "front-lights",
  "state": "on",
  "online": true
}
```

---

## Ejecutar comando

```http
POST /api/devices/{id}/commands
```

Ejemplo para iluminación:

```json
{
  "action": "on"
}
```

Ejemplo para motor:

```json
{
  "action": "start",
  "direction": "forward",
  "speed": 70
}
```

Respuesta:

```json
{
  "deviceId": "main-motor",
  "success": true,
  "state": "running",
  "executedAt": "2026-09-22T23:00:00Z"
}
```

---

## Detener todos los dispositivos

```http
POST /api/devices/stop-all
```

Este comando tendrá prioridad sobre las operaciones normales y solicitará la detención de los dispositivos controlados por el sistema.

---

# 7. Secuencias

Además del control manual, la API permitirá ejecutar secuencias.

Una secuencia representa una serie de comandos separados por intervalos de tiempo.

Ejemplo:

```text
Secuencia: PRESENTACIÓN

0 ms       Luces frontales ON
1000 ms    Motor principal START
2500 ms    Luces laterales ON
5000 ms    Motor principal STOP
6000 ms    Luces laterales OFF
7000 ms    Luces frontales OFF
```

Endpoint:

```http
POST /api/sequences/{id}/execute
```

También se podrá detener una secuencia:

```http
POST /api/sequences/stop
```

La ejecución estará administrada por el backend y no por temporizadores del navegador.

Esto permite que una recarga o cierre accidental del frontend no sea quien determine la continuidad de la secuencia.

---

# 8. Contrato API → ESP32

Inicialmente la comunicación se realizará mediante HTTP sobre Wi-Fi.

La API utilizará:

```text
Esp32DeviceGateway
```

para transformar comandos del dominio en solicitudes al ESP32.

Ejemplo:

```http
POST /api/devices/front-lights/command
```

```json
{
  "action": "on"
}
```

El ESP32 será responsable de conocer la relación entre el dispositivo lógico y el hardware físico.

Por ejemplo:

```text
front-lights
      │
      ▼
GPIO 18
      │
      ▼
Relay
      │
      ▼
Luces
```

La API .NET no necesitará conocer que `front-lights` corresponde al GPIO 18.

---

# 9. Firmware ESP32

El firmware será responsable únicamente del control físico.

Su estructura conceptual será:

```text
ESP32

├── WiFi
├── HTTP Server
├── Command Handler
├── Light Controller
├── Motor Controller
└── Device Status
```

Ejemplo:

```text
POST /api/devices/front-lights/command

             ↓

       CommandHandler

             ↓

       LightController

             ↓

       digitalWrite()

             ↓

           Relay

             ↓

           Luces
```

Para motores se utilizará un driver electrónico adecuado y las salidas correspondientes del ESP32.

El motor no será conectado directamente a un GPIO.

---

# 10. Simulador

Una pieza importante del proyecto será `SimulatorDeviceGateway`.

El simulador implementará el mismo contrato que el ESP32:

```text
                 IDeviceGateway
                       │
              ┌────────┴─────────┐
              ▼                  ▼
     SimulatorGateway       Esp32Gateway
```

En desarrollo:

```text
DeviceGateway = Simulator
```

En hardware real:

```text
DeviceGateway = Esp32
```

El simulador mantendrá estados como:

```text
front-lights = ON
side-lights  = OFF
main-motor   = RUNNING
speed        = 70
```

Esto permitirá ejecutar pruebas automáticas sin disponer del hardware.

---

# 11. Estado y confirmación

El frontend no asumirá que un comando fue ejecutado correctamente simplemente porque la solicitud fue enviada.

El flujo será:

```text
Usuario
   │
   ▼
Frontend
   │
   │ comando
   ▼
API
   │
   ▼
IDeviceGateway
   │
   ▼
ESP32
   │
   ▼
Hardware
   │
   ▼
Resultado
   │
   ▼
API
   │
   ▼
Frontend
```

Por lo tanto podrán existir estados como:

```text
ENCENDIDO

APAGADO

MOVIENDO

DETENIDO

EJECUTANDO

SIN RESPUESTA

ERROR
```

Esto permitirá diferenciar una orden enviada de una orden efectivamente confirmada.

---

# 12. Historial de comandos

La API registrará las operaciones realizadas.

Ejemplo:

```text
22:31:04  Luces frontales   ON        OK
22:31:10  Motor principal   START     OK
22:31:13  Motor principal   SPEED 70  OK
22:31:28  Motor principal   STOP      OK
22:31:31  Luces frontales   OFF       OK
```

Se registrará como mínimo:

- Dispositivo.
- Acción.
- Parámetros.
- Fecha y hora.
- Resultado.
- Mensaje de error cuando corresponda.

Esto permitirá observar qué ocurrió durante una prueba y facilitará el diagnóstico de problemas.

---

# 13. Manejo de errores

Se contemplarán, entre otros:

```text
400 Bad Request
Comando inválido.

404 Not Found
Dispositivo inexistente.

409 Conflict
El dispositivo no permite ejecutar el comando en su estado actual.

503 Service Unavailable
ESP32 sin conexión.

504 Gateway Timeout
El ESP32 no respondió dentro del tiempo esperado.
```

Ejemplo:

```json
{
  "status": 503,
  "error": "DEVICE_UNAVAILABLE",
  "message": "No se pudo establecer comunicación con el controlador."
}
```

---

# 14. Cómo se gestionará el contexto

Antes de implementar se trabajará en modo plan.

El proyecto se dividirá en contratos independientes:

```text
Frontend
     ↓
REST API Contract
     ↓
Application
     ↓
IDeviceGateway
     ↓
ESP32 Protocol
     ↓
Hardware
```

Cada componente tendrá una responsabilidad acotada.

El agente no necesitará cargar todo el proyecto para modificar una funcionalidad determinada.

Por ejemplo:

- Para modificar el panel visual no será necesario cargar el firmware.
- Para modificar el control de un motor no será necesario cargar los componentes React.
- Para trabajar con el simulador no será necesario disponer del ESP32.
- Para modificar el firmware se utilizará como referencia el contrato de comunicación definido previamente.

Se buscará mantener archivos y funciones pequeños, evitando componentes que acumulen responsabilidades.

Las decisiones importantes se documentarán para que el contexto relevante permanezca en el repositorio y no solamente en la conversación con el agente.

---

# 15. Estrategia de implementación

La implementación se realizará incrementalmente.

## Etapa 1 — Contratos

Definir:

- Modelo `Device`.
- Modelo `DeviceCommand`.
- Modelo `DeviceState`.
- `IDeviceGateway`.
- Contrato HTTP del ESP32.
- Endpoints públicos de la API.

Todavía no habrá hardware.

---

## Etapa 2 — Simulator Gateway

Implementar:

```text
SimulatorDeviceGateway
```

Debe permitir:

- ON/OFF de luces.
- START/STOP del motor.
- Cambio de velocidad.
- Consulta de estados.

Al finalizar esta etapa, la API podrá probarse completamente sin ESP32.

---

## Etapa 3 — API

Implementar:

- Devices.
- Commands.
- Estados.
- Validaciones.
- Historial.
- Manejo de errores.

Se agregarán pruebas automáticas de los endpoints.

---

## Etapa 4 — Frontend

Construir el panel React.

Primero:

```text
Luces frontales
ON / OFF
```

Luego:

```text
Luces laterales
ON / OFF
```

Finalmente:

```text
Motor
START
STOP
Dirección
Velocidad
```

Cada etapa deberá quedar funcional antes de continuar.

---

## Etapa 5 — Secuencias

Agregar:

- Modelo de secuencia.
- Pasos.
- Tiempo entre comandos.
- Ejecución.
- Cancelación.

Primero se verificará completamente contra el simulador.

---

## Etapa 6 — Firmware

Implementar el servidor HTTP del ESP32.

Inicialmente se utilizará un LED para representar una salida.

Una vez validada la comunicación:

```text
API
 ↓
ESP32
 ↓
LED
```

se incorporarán relés y el control de motor mediante driver.

---

## Etapa 7 — Esp32DeviceGateway

Implementar la comunicación real:

```text
.NET
 ↓
Esp32DeviceGateway
 ↓
HTTP
 ↓
ESP32
```

El resto del sistema no deberá modificarse.

El objetivo será reemplazar:

```text
SimulatorDeviceGateway
```

por:

```text
Esp32DeviceGateway
```

mediante configuración.

---

# 16. Harness Engineering

El harness del proyecto estará formado por herramientas y mecanismos que permitan al agente verificar automáticamente lo que construye.

```text
                 HARNESS

          ┌───────────────────┐
          │                   │
          │ Tests backend     │
          │                   │
          │ Simulator ESP32   │
          │                   │
          │ Playwright        │
          │                   │
          │ Health checks     │
          │                   │
          │ Command logs      │
          │                   │
          │ Contract tests    │
          │                   │
          └─────────┬─────────┘
                    │
                    ▼

 React → API → IDeviceGateway → Simulator
                         │
                         └────→ ESP32 → Hardware
```

El objetivo es que el agente no se limite a escribir código.

También deberá poder:

1. Levantar el backend.
2. Ejecutar las pruebas.
3. Levantar el frontend.
4. Consultar el estado inicial.
5. Accionar controles desde el navegador.
6. Verificar las solicitudes realizadas.
7. Consultar nuevamente los estados.
8. Inspeccionar el historial.
9. Simular errores del dispositivo.
10. Verificar el comportamiento visual frente a esos errores.

---

# 17. Escenario de verificación

Un escenario automático podrá realizar:

```text
1. Iniciar API.

2. Utilizar SimulatorDeviceGateway.

3. GET /api/devices.

4. Confirmar:
   front-lights = OFF.

5. Abrir frontend.

6. Pulsar:
   ENCENDER LUCES FRONTALES.

7. Confirmar:
   POST /api/devices/front-lights/commands.

8. Consultar estado.

9. Confirmar:
   front-lights = ON.

10. Pulsar:
    APAGAR.

11. Confirmar:
    front-lights = OFF.

12. Iniciar motor.

13. Configurar velocidad = 70.

14. Confirmar:
    motor = RUNNING
    speed = 70.

15. Detener todo.

16. Confirmar:
    todos los dispositivos en estado seguro.
```

Después el mismo escenario podrá ejecutarse parcialmente contra hardware real.

---

# 18. Pruebas de error

El simulador permitirá introducir fallas intencionalmente.

Por ejemplo:

```text
SIMULATE ESP32 OFFLINE
```

La API deberá responder:

```text
503 Service Unavailable
```

El frontend deberá mostrar:

```text
CONTROLADOR SIN CONEXIÓN
```

También podrán simularse:

```text
Timeout.

Comando rechazado.

Dispositivo ocupado.

Motor detenido.

Respuesta inválida.
```

Esto permite verificar escenarios difíciles de reproducir de manera controlada utilizando hardware físico.

---

# 19. Estrategia de commits

Se realizará un commit por cada incremento funcional.

Ejemplo:

```text
01 - Define device contracts

02 - Add simulator device gateway

03 - Add device API

04 - Add light controls

05 - Add motor controls

06 - Add command history

07 - Add sequence execution

08 - Add ESP32 firmware

09 - Add ESP32 gateway

10 - Add end-to-end verification
```

Cada commit deberá dejar una parte funcional y verificable del proyecto.

Los mensajes deberán describir el cambio realizado y, cuando sea relevante, la razón de la decisión.

---

# 20. Decisiones iniciales

## React no controla el ESP32 directamente

El navegador solamente se comunica con la API.

Esto evita acoplar la interfaz gráfica al hardware.

---

## La API tampoco conoce GPIO

La API trabaja con identificadores lógicos:

```text
front-lights
side-lights
main-motor
```

El ESP32 conoce la asignación física.

---

## El hardware puede reemplazarse por un simulador

Esta es una decisión central del proyecto.

Permite desarrollar y verificar:

```text
Frontend + API + lógica
```

sin depender del hardware.

---

## Las secuencias se ejecutan en el backend

El frontend inicia o detiene una secuencia, pero no controla sus tiempos.

Esto evita que una recarga del navegador sea responsable de la lógica temporal de la carroza.

---

## Un comando enviado no significa un comando ejecutado

La aplicación diferenciará entre:

```text
COMMAND SENT
```

y:

```text
COMMAND CONFIRMED
```

El estado mostrado al operador deberá provenir de la respuesta del sistema de control.

---

# 21. Alcance inicial

Para evitar sobredimensionar el trabajo práctico, la primera versión tendrá:

```text
2 sectores de iluminación.

1 motor.

ON/OFF.

START/STOP.

Dirección del motor.

Control de velocidad.

3 secuencias.

Parada general.

Historial de comandos.

SimulatorDeviceGateway.

Esp32DeviceGateway.

Dashboard web.
```

Funciones como autenticación, múltiples carrozas, administración avanzada de usuarios, telemetría histórica o edición visual compleja de secuencias quedarán fuera del alcance inicial.

---

# 22. Resultado esperado

Al finalizar el trabajo deberá ser posible demostrar dos escenarios.

### Modo simulado

```text
React
   ↓
.NET API
   ↓
SimulatorDeviceGateway
```

Permitirá ejecutar automáticamente las pruebas y demostrar el sistema sin hardware.

### Modo real

```text
React
   ↓
.NET API
   ↓
Esp32DeviceGateway
   ↓
Wi-Fi
   ↓
ESP32
   ↓
GPIO / PWM
   ↓
Relés / Driver
   ↓
Luces / Motor
```

El cambio entre ambos modos deberá realizarse mediante configuración y no mediante modificaciones en la lógica de negocio.

El objetivo del trabajo no será solamente controlar una luz o un motor, sino demostrar una arquitectura desacoplada en la que software, simulación y hardware puedan desarrollarse y verificarse independientemente mediante contratos explícitos y un harness de pruebas reproducible.
