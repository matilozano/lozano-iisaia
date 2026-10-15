# AGENTS.md — Harness del TP Final FNE

## Propósito y alcance

Plataforma de simulación y control de componentes electromecánicos de una carroza FNE. La aplicación debe poder usar otro gateway en el futuro sin trasladar lógica de control al frontend.

Este archivo contiene reglas permanentes. El alcance de cada entrega se define en [docs/PLAN.md](docs/PLAN.md), no se deduce del número de iteración ni de ejemplos futuros del README. Si una iteración no está definida, informar la ausencia y solicitar su alcance; no inventarla ni avanzar automáticamente a la siguiente. Las tareas de documentación o refinamiento del Harness no reciben número de iteración funcional.

## Lectura y fuentes

Antes de cambios relevantes, leer:

- [docs/PLAN.md](docs/PLAN.md): alcance aprobado y estado actual.
- [docs/DESARROLLO.md](docs/DESARROLLO.md): entorno, comandos de verificación y contrato HTTP implementado.
- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md): responsabilidades y dependencias.
- [docs/COMPONENTS.md](docs/COMPONENTS.md): comportamiento de componentes; distinguir modelo conceptual de DTOs reales.
- [docs/HARNESS.md](docs/HARNESS.md): flujo de trabajo, evidencia y formato de cierre.
- AGENTS de las áreas afectadas: [backend](backend/AGENTS.md), [frontend](frontend/AGENTS.md), y cualquier instrucción más específica existente.

Consultar [docs/ESP32-PROTOCOL.md](docs/ESP32-PROTOCOL.md) cuando se afecte el gateway o su contrato; define la frontera de Iteración 7 y no autoriza por sí solo implementar firmware/hardware. Leer los informes `docs/ITERACION-N.md` relevantes para el cambio o la continuación. No es necesario releer todos para una modificación local cuya historia ya está cubierta por el Harness.

Las instrucciones explícitas del usuario gobiernan la tarea. Ante contradicciones entre documentación y código, identificar la diferencia y preservar contratos hasta resolverla; no modificar código solo para satisfacer un ejemplo histórico. Los informes registran evidencia de una ejecución, no sustituyen nuevas verificaciones.

## Estado del repositorio y continuidad

1. Confirmar rama, `git status` y `git diff`; inspeccionar también archivos no trackeados y código relevante antes de editar.
2. Trabajar en `tpfinal`. Si la rama es otra, informarlo; no cambiarla automáticamente.
3. Conservar cambios existentes. No resetear, descartar, hacer checkout de archivos modificados ni sobrescribir trabajo sin inspección. No hacer commit ni push salvo instrucción explícita posterior.
4. Al retomar una interrupción, identificar qué está completo, parcial y pendiente según el código real. Continuar desde allí; no reiniciar la implementación ni confiar en un informe parcial como prueba de éxito.
5. Hacer el cambio mínimo necesario. No reestructurar áreas ajenas ni crear placeholders de funciones futuras. Resolver decisiones locales dentro del alcance sin pedir aprobación rutinaria.

## Fronteras obligatorias

```text
Frontend React → HTTP → Controllers → Application Services → IComponentGateway
                                                               ↓
                                                   SimulatorComponentGateway
```

- `Program.cs` es Composition Root: configuración, DI, middleware y `MapControllers`. Sin endpoints de aplicación Minimal API ni reglas de negocio.
- Controllers traducen HTTP/DTOs y delegan; no dependen de gateways concretos ni manejan estado del simulador.
- Domain contiene modelos/reglas; Application coordina casos de uso mediante abstracciones; Infrastructure implementa el gateway y la simulación.
- Frontend no conoce GPIO, relés, drivers, protocolo interno ni direcciones del ESP32; nunca se conecta directamente a hardware.
- `SimulatorComponentGateway` y `Esp32ComponentGateway` son implementaciones seleccionables por configuración/DI. La frontera ESP32 se prueba sin hardware; no implica firmware ni integración física terminados.
- Mantener componentes y comandos genéricos, compatibles con luces, banco, motor, hidráulico y servo. Conservar `/api/devices` y sus contratos; cualquier extensión indispensable debe justificarse, documentarse y probar compatibilidad.

## Estado confirmado, secuencias y seguridad

- Enviar una intención no confirma ejecución. UI y Digital Twin representan exclusivamente respuestas/consultas del backend; ante error conservan el último estado confirmado e identifican el fallo/componente.
- Panel, secuencias y Twin comparten la fuente de estado existente. No duplicar polling, ejecución de comandos ni temporización de secuencias en los componentes visuales.
- Secuencias se ejecutan en Application con el mismo mecanismo de comandos manuales. Backend decide exclusión y concurrencia; botones deshabilitados no son una garantía suficiente.
- STOP ALL tiene prioridad: cancelar secuencia y trabajo pendiente, detener motor/hidráulico/efectos, apagar iluminación y sincronizar estados confirmados. Conservar posición del servo según contrato; no inventar un ángulo seguro.
- Ningún step anterior pendiente puede reactivar componentes después de la confirmación de parada. Se admiten órdenes nuevas posteriores: la parada actual no es un enclavamiento permanente.
- Fallas simuladas deben ser controladas y reproducibles, separadas del comportamiento normal; sin lógica especial en Controllers. No asumir éxito ni ejecución tardía tras TIMEOUT.
- No confundir garantías del simulador con seguridad física de hardware desconectado.

## Entorno y alcance futuro

Puertos del TP: frontend `http://127.0.0.1:5173`, backend `http://127.0.0.1:5080`. Conservarlos y verificar `/api` mediante proxy; no atribuir logs de otros proyectos a este sistema. Diagnóstico y pruebas en DESARROLLO.

No incorporar ESP32 real, firmware, GPIO, autenticación, persistencia, MQTT, SignalR/WebSockets o editor de secuencias por inferencia. Requieren alcance explícito futuro; no son prohibiciones irrevocables cuando se apruebe ese trabajo.

## Definition of Done

Para un incremento funcional o refactor de código:

- builds backend y frontend correctos;
- suites backend/simulador y frontend aprobadas, conservando pruebas previas;
- smoke, contrato, regresión de componentes, fallas, secuencias y proxy según DESARROLLO ejecutados;
- fronteras arquitectónicas verificadas;
- Swagger/OpenAPI verificado si se afecta API/DTOs/documentación HTTP;
- revisión visual intentada cuando se afecta UI, animación o sincronización; observar coherencia con API, errores, STOP ALL, responsive y movimiento reducido;
- documentación actualizada y reporte con archivos, decisiones, comandos, resultados y pendientes reales.

Al retomar trabajo interrumpido, repetir las verificaciones de cierre del alcance actual. No aprobar por resultados de otra ejecución. Si una herramienta falla, registrar causa, comprobaciones alternativas y revisión pendiente; build/HTTP no equivalen a aprobación visual. Una tarea con requisitos no verificados debe reportar esa limitación, no un cumplimiento total ficticio.

Para cambios exclusivamente documentales, aplicar la verificación documental de DESARROLLO; builds y pruebas funcionales pueden declararse **no aplicables**, con razón explícita. Esto no permite omitirlas si cambian código, contratos, configuración ejecutable o scripts.
