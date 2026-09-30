# Protocolo ESP32

## Estado

Pendiente de definición.

Este documento definirá el contrato entre `Esp32ComponentGateway` y el firmware
del ESP32.

## Objetivo

El protocolo debe permitir que `Esp32ComponentGateway` traduzca los comandos
lógicos de `IComponentGateway` a operaciones físicas sin filtrar detalles de
hardware hacia Application o Frontend.

## Pendiente

Definir:

- transporte;
- descubrimiento/configuración de dirección;
- formato de comandos;
- formato de respuestas;
- identificación de componentes;
- confirmación de ejecución;
- consulta de estado;
- timeouts;
- errores;
- heartbeat;
- STOP ALL;
- comportamiento ante pérdida de comunicación.