# Guía del Harness Engineering

## Función de cada documento

| Documento | Responsabilidad |
| --- | --- |
| [AGENTS raíz](../AGENTS.md) | Reglas permanentes, continuidad, alcance y Definition of Done |
| [AGENTS backend](../backend/AGENTS.md) / [frontend](../frontend/AGENTS.md) | Fronteras y verificaciones específicas de cada área |
| [PLAN](PLAN.md) | Alcance aprobado de entregas y estado actual; no inventar una entrega ausente |
| [ARCHITECTURE](ARCHITECTURE.md) | Dependencias y responsabilidades de implementación |
| [COMPONENTS](COMPONENTS.md) | Modelo conceptual y comportamiento; usar contrato HTTP real al construir requests |
| [DESARROLLO](DESARROLLO.md) | Entorno, comandos reproducibles, contrato implementado y checklist visual |
| [ESP32-PROTOCOL](ESP32-PROTOCOL.md) | Contrato futuro de hardware, todavía pendiente |
| ITERACION-N.md | Registro histórico de alcance, cambios y evidencia de cada ejecución |
| [README](../README.md) | Visión general; sus etapas conceptuales no asignan números a iteraciones aprobadas |

Evitar copiar reglas en cada nuevo prompt o repetir contratos en varios archivos. Mantener la regla en su documento responsable y enlazarla. Un aprendizaje generalizable se incorpora al AGENTS correspondiente; un tiempo de SHOW_FNE, una captura o cantidad de tests pertenece al contrato/informe. Una excepción de alcance no se convierte en prohibición permanente.

## Flujo de trabajo

1. **Inspección:** rama, status, diff y no trackeados; instrucciones aplicables, alcance y código real. En una continuación, separar implementado/parcial/pendiente antes de editar.
2. **Cambio acotado:** identificar archivos y fronteras afectadas; reutilizar servicios, contratos y fuente de estado. Resolver las decisiones rutinarias dentro del alcance autorizado.
3. **Verificación incremental:** pruebas relacionadas durante el desarrollo. En el cierre funcional ejecutar la matriz de DESARROLLO, sin reemplazar regresiones anteriores por las nuevas.
4. **Revisión real:** ejecutar contra API/proxy correctos, observar UI si aplica y verificar STOP ALL con operaciones en curso. No operar manualmente mientras los scripts usan la misma instancia.
5. **Cierre:** revisar diff, actualizar documentación aplicable, registrar resultados y limitaciones. No hacer commit/push ni iniciar otra iteración automáticamente.

Un puerto ocupado, un runId que cambia por otro operador o un rechazo 409 durante pruebas puede indicar interferencia. Identificarla y repetir la prueba en condiciones controladas. No deshabilitar la exclusión backend para hacer pasar tests. No detener procesos ajenos sin identificar su pertenencia.

Si hay error del entorno (permisos, SDK, navegador), diferenciarlo de un defecto del producto. Resolverlo mediante mecanismos autorizados cuando sea posible y registrar el resultado final; no cambiar infraestructura global o eliminar archivos para eludirlo.

## Evidencia y estado de verificaciones

Cada comprobación se informa como **APROBADA**, **FALLÓ**, **PENDIENTE** o **NO APLICA** (con motivo). Registrar comando/procedimiento, resultado y alcance observado. No usar APROBADA cuando solo se leyó código, hubo un intento fallido o existe evidencia de una ejecución anterior.

- Build confirma compilación; no confirma comportamiento visual.
- HTTP confirma contrato/estado; no demuestra brillo, giro, responsive o accesibilidad.
- Lectura del CSS no demuestra que se haya activado una preferencia del sistema.
- SHOW_FNE completo y cancelación son escenarios distintos: comprobar ambos.
- Una captura ayuda a revisar un estado; para temporización usar observaciones sucesivas y pruebas de secuencia.
- Conservar pendientes concretos sin reabrir retrospectivamente reportes ya cerrados. Resolverlos en la tarea que los cubra y enlazar la nueva evidencia.

## Reporte de cierre

Para una iteración funcional, usar `docs/ITERACION-N.md` solo cuando N y alcance estén definidos. Para mantenimiento del Harness, usar un registro descriptivo sin numeración funcional.

Contenido mínimo:

- estado inicial y trabajo previo conservado, especialmente tras interrupciones;
- objetivo, archivos creados/modificados y trabajo completado ahora;
- arquitectura, flujo de datos y decisiones relevantes;
- contratos/endpoints afectados o confirmación de que no cambiaron;
- tabla de verificaciones ejecutadas con resultados y problemas resueltos;
- SHOW_FNE, STOP ALL y fallas cuando corresponda;
- revisión visual efectivamente realizada y pendientes reales;
- rama y ausencia de commits/push.

## Prompt breve para próximas tareas

Una vez definido el alcance en PLAN:

> Implementá la iteración N definida en docs/PLAN.md. Seguí AGENTS.md y la Definition of Done. Documentá resultados reales. No hagas commit ni push.

Para retomar:

> Continuá la tarea desde el estado actual siguiendo el protocolo de continuidad de AGENTS.md. No descartes trabajo existente.

Estos prompts no crean alcance por sí mismos: N debe existir en PLAN. La definición de una próxima entrega sigue siendo una decisión del usuario.

## Consolidación de aprendizajes de las iteraciones 1–6

- **1–2:** integración API real, contratos conservados, Controllers y separación de capas → AGENTS raíz/backend.
- **3:** animación observable, canales confirmados, Swagger y diferencia entre pruebas HTTP y visuales → AGENTS frontend/backend y DESARROLLO.
- **4:** posiciones progresivas, fallas reproducibles, error por componente, snapshot seguro y recuperación tras interrupción → AGENTS raíz y de áreas.
- **5:** motor de secuencias único en backend, exclusión, cancelación sin reactivación, interferencias entre pruebas y diagnóstico del proxy → AGENTS de áreas y esta guía.
- **6:** fuente de estado compartida, mapping/SVG/HUD separados, responsive y evidencia honesta de movimiento reducido → AGENTS frontend.

Este refinamiento es documental. No define Iteración 7, no cambia producto, contratos ni configuración ejecutable, y conserva los informes históricos.

## Registro de este refinamiento

Tarea de mantenimiento documental posterior a Iteración 6, sin numeración funcional. Se actualizaron AGENTS raíz/backend/frontend, README, PLAN, DESARROLLO, ARCHITECTURE y COMPONENTS; se creó esta guía. No se modificaron informes históricos, código, contratos ni archivos ejecutables.

Verificación documental: enlaces locales resueltos, rutas de scripts/suites existentes, comandos npm contrastados con package.json, revisión de coherencia y diff exclusivamente Markdown. Se corrigieron blancos al final de archivo detectados por git diff --check; comprobación final sin errores. Builds, tests de producto, HTTP y revisión visual: NO APLICA porque no se alteró comportamiento ejecutable. Los resultados funcionales históricos siguen en sus informes y no se presentan como pruebas nuevas de esta tarea.

Sin commit ni push; rama tpfinal. No se definió ni inició Iteración 7.
