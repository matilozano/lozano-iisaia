# Iteración 7 — Gateway ESP32 y frontera de hardware

Fecha de trabajo: 2026-10-07. Rama `tpfinal`. Sin commit, push, reset ni cambio de rama.

## Estado inicial y alcance conservado

Las iteraciones 1–6 estaban implementadas. El único cambio previo era la sección de Iteración 7 agregada por el usuario a docs/PLAN.md; se conservó íntegramente su alcance. No existía gateway ESP32, transporte ni implementación parcial de esta entrega. ESP32-PROTOCOL.md era una lista de pendientes, por lo que se definió el contrato v1 necesario junto con la implementación. Se corrigieron dos frases históricas del plan que todavía indicaban ausencia de alcance aprobado.

Se inspeccionaron estado/diff, AGENTS raíz/backend, Harness, plan, desarrollo, arquitectura, componentes, protocolo y código de gateway/servicios/Controllers/suites. Se consultaron los reportes relevantes de secuencias y Twin. No se reimplementaron funcionalidades anteriores.

## Arquitectura y flujo

`Controllers → Application → IComponentGateway → Esp32ComponentGateway → IEsp32Transport → HttpEsp32Transport`.

Program conserva Composition Root y registra AddComponentGateway. Configuración selecciona Simulator o ESP32; modo desconocido o configuración ESP32 inválida falla al arrancar. Simulator permanece predeterminado, sin dirección obligatoria ni nuevos requisitos. HttpClientFactory configura el transporte; endpoint obligatorio y timeout configurable. No hay dirección de placa hardcodeada en producción.

Esp32ComponentGateway mapea el comando del dominio a un mensaje versionado, solicita intercambio y valida UUID/versión/IDs/estado antes de devolver ComponentResult. El transporte no conoce GPIO ni decide comportamiento de componentes. No hay cache, simulación ni extrapolación de estado dentro del gateway ESP32. Contratos wire separados de DTOs públicos y modelos Domain.

Un comando manual o de SHOW_FNE recorre el mismo ComponentCommandExecutor existente. **Controllers, Application Services, Domain, IComponentGateway y frontend no se modificaron.** Se comprobó además con git diff. El selector se prueba por DI para ambos modos.

ComponentExceptionFilter global traduce las fallas que ahora pueden ocurrir en lectura, cancelación y parada al formato JSON existente. GatewayErrorsOperationFilter documenta esas respuestas. No hay lógica de hardware en Controllers ni endpoints de aplicación en Program.

## Configuración y protocolo

Ver [ESP32-PROTOCOL](ESP32-PROTOCOL.md) y [DESARROLLO](DESARROLLO.md). POST HTTP(S) a un endpoint completo configurable, con operaciones GET_STATE, EXECUTE y STOP_ALL. UUID de correlación, versión 1, parámetros lógicos sin renombrar y estados completos confirmados por controlador. Timeout predeterminado 2000 ms; rango 100..30000. Sin redirecciones ni reintentos automáticos.

No se agregaron endpoints públicos ni se cambiaron campos de DTOs existentes. Se documentaron respuestas 500/503/504 para consultas de dispositivos, STOP ALL y cancelación. Los comandos conservan códigos y formato de errores anteriores. STOP ALL conserva 204 o snapshot 200 únicamente tras confirmación válida.

## Pruebas nuevas

Esp32Tests está integrado a la suite ejecutable existente. Utiliza HttpMessageHandler controlado: atraviesa serialización HTTP/JSON real y un doble del controlador, que reutiliza el simulador exclusivamente dentro de tests. No hay emulador oculto en producción. Incluye:

- catálogo, consultas y actualización de estados; todos los comandos de SHOW_FNE y comandos adicionales del banco/motor/servo;
- correlación, versión, parámetros, JSON con campos nulos y timestamps;
- cinco códigos de rechazo, conexión offline, timeout y cancelación sin reintento;
- JSON vacío/HTML/incompleto, Content-Type incorrecto, estados fuera de rango, campos ajenos al tipo, efecto/canales inválidos;
- STOP ALL remoto único, respuesta completa, rechazo de parada parcial/offline/con movimiento, servo conservado;
- SHOW_FNE COMPLETED con el gateway ESP32 y delay controlado; FAILED; exclusión manual; STOP ALL → CANCELLED sin más steps;
- selección por configuración/DI y rechazo de configuración inválida;
- servidor API HTTP real en puerto efímero con Controllers existentes y gateway alternativo: consultas/comandos/parada y errores JSON 500/503/504.

## SHOW_FNE, STOP ALL y fallas

Modo Simulator: show completo de 14 pasos aprobado por sequence-test con tiempos reales, conflictos, cancelación y STOP ALL luego de varios pasos; observación prolongada sin reactivación. Fallas simuladas anteriores continúan aprobadas.

Modo ESP32: mismo servicio de secuencias aprobado con doble controlado (no representa medición temporal sobre Wi-Fi ni placa). Comandos pasan por el gateway/transporte, secuencia termina, falla o se cancela según caso. Se prueba parada global y ausencia de pasos posteriores en backend.

TIMEOUT/desconexión de hardware significa resultado desconocido: no se confirma éxito ni se reintenta. Fallar la parada no produce 204 ni instantánea segura ficticia. La barrera contra mensajes demorados en la red y la parada física deben implementarse/verificarse en el futuro controlador. No se certifican seguridad física ni firmware con estas pruebas.

## Resultados de verificación

| Verificación ejecutada | Resultado real |
| --- | --- |
| dotnet build backend/Carroza.Api | APROBADA: compilación final, 0 errores/advertencias |
| npm.cmd --prefix frontend run build | APROBADA: TypeScript y Vite, 40 módulos |
| npm.cmd --prefix frontend run test | APROBADA: 11 pruebas |
| dotnet run --project backend/Carroza.Simulator.Tests -c Iteration7 | APROBADA: 34 generales + 75 banco + 52 posiciones/fallas + 82 secuencias + 112 ESP32; 60 operaciones concurrentes adicionales |
| smoke-test.ps1 | APROBADA: catálogo, luces, motor y parada |
| contract-test.ps1 | APROBADA: JSON, parámetros/límites, errores, catálogo y parada |
| iteration-3-test.ps1 | APROBADA: banco temporal, Swagger UI/assets/OpenAPI |
| iteration-4-test.ps1 | APROBADA: hidráulico, servo, snapshot y Swagger |
| fault-test.ps1 | APROBADA: cinco fallas HTTP, no mutación, secuencia FAILED y parada |
| sequence-test.ps1 | APROBADA: SHOW_FNE temporal, conflictos, cancelación, STOP ALL y no reactivación |
| frontend-proxy-test.ps1 | APROBADA: diez consultas JSON por 5080/5173 |
| Búsquedas arquitectónicas de DESARROLLO | APROBADA: tres rg sin coincidencias (exit 1 esperado) |
| Comparación Controllers/Application/frontend | APROBADA: git diff sin cambios (exit 0) |
| OpenAPI: respuestas 500/503/504 de consultas, STOP ALL y cancelación | APROBADA: esquema obtenido por HTTP y comprobado explícitamente |
| Documentación y diff | APROBADA: 31 enlaces locales resueltos, git diff --check sin errores |
| Revisión Swagger en navegador | PENDIENTE: herramienta no pudo inicializarse, failed to write kernel assets (os error 3) |
| Revisión visual de UI/Twin | NO APLICA al cambio visual: no se alteraron UI, animación ni polling; no se declara regresión visual aprobada |
| Integración física ESP32 | NO APLICA a esta entrega; sin firmware ni placa |

Los siete scripts HTTP se ejecutaron dos veces; la segunda ronda utilizó el build final. Se ejecutaron secuencialmente, sin operación manual concurrente, en Simulator y puertos 5080/5173. La suite .NET es un ejecutable, no se presenta dotnet test como prueba adicional. Los resultados de iteraciones anteriores no se reutilizaron como evidencia.

## Problemas encontrados y resueltos / límites del entorno

1. Protocolo previo sin definir: se concretó contrato v1 documentado y probado sin modificar API pública.
2. Primera prueba detectó que la serialización omitía null mientras el lector exigía campos del sobre: corregido enviando nulos explícitos y probado.
3. Fallo de secuencia con fallo adicional en STOP ALL perdía el código en el mensaje final: el gateway incluye código/contexto al propagar errores, sin modificar Application.
4. Sandbox impidió leer NuGet.Config: se repitió mediante ejecución autorizada fuera del sandbox.
5. Las primeras excepciones no controladas dejaron procesos de test reteniendo DLLs Debug/Release (PIDs 6220 y 20952). Windows rechazó detener el primero incluso con ejecución escalada. No se eliminaron archivos ni procesos ajenos. Se agregó salida de fallo controlada para Esp32Tests y se repitió la suite en configuración `Iteration7`, con directorio bin independiente y exit 0. El comando habitual sin -c se intentó pero quedó bloqueado por esos archivos; no se lo declara aprobado. Puede requerirse cerrar los procesos retenidos desde la sesión propietaria para volver a compilar ese directorio.
6. Navegador no disponible por error del runtime: HTTP/OpenAPI comprobado; revisión interactiva pendiente, no aprobada.

## Archivos

Creados:

- backend/Carroza.Api/Infrastructure/Esp32/Esp32Protocol.cs
- backend/Carroza.Api/Infrastructure/Esp32/Esp32Options.cs
- backend/Carroza.Api/Infrastructure/Esp32/HttpEsp32Transport.cs
- backend/Carroza.Api/Infrastructure/Esp32/GatewayRegistration.cs
- backend/Carroza.Api/Infrastructure/Gateways/Esp32ComponentGateway.cs
- backend/Carroza.Api/Infrastructure/Http/ComponentExceptionFilter.cs
- backend/Carroza.Api/Infrastructure/Http/GatewayErrorsOperationFilter.cs
- backend/Carroza.Simulator.Tests/Esp32Tests.cs
- docs/ITERACION-7.md

Modificados:

- backend/Carroza.Api/Program.cs: registro DI, filtro de errores y OpenAPI.
- backend/Carroza.Simulator.Tests/Program.cs: integrar suite nueva y salida de error controlada.
- docs/ESP32-PROTOCOL.md: contrato antes pendiente.
- docs/ARCHITECTURE.md, docs/COMPONENTS.md, docs/DESARROLLO.md: frontera, configuración, pruebas y límites.
- docs/PLAN.md: conservar alcance agregado por el usuario y aclarar frases históricas.
- AGENTS.md, backend/AGENTS.md y docs/HARNESS.md: actualizar referencias que describían la frontera como futura, manteniendo reglas.

## Pendientes reales

Revisión interactiva de Swagger por indisponibilidad del navegador. Limpieza de procesos de prueba retenidos por Windows y repetición opcional en directorio Debug habitual; la suite completa está aprobada en salida independiente. La revisión manual de movimiento reducido de Iteración 6 no se reabrió ni se declara resuelta.

Firmware, integración física, watchdog y ordenamiento de mensajes demorados quedan para alcance futuro. El frontend se conserva: sus etiquetas SIMULATOR/SIMULADO son texto estático histórico y no identifican el gateway activo. No deben confundirse con telemetría al probar ESP32; no se agregó endpoint ni lógica visual para detectarlo.
