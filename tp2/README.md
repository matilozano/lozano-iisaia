TP N°2 - Generacion de un archivo openapi.yaml
# Sistema de Gestión y Firma Digital de Recibos de Sueldo

## Descripción del Dominio Seleccionado

El proyecto consiste en el diseño de una **API REST para la gestión integral de recibos de sueldo**, contemplando una arquitectura **multi-tenant**, de manera que una misma solución pueda ser utilizada por múltiples organismos, empresas o instituciones manteniendo sus datos completamente aislados.

La API deberá permitir administrar empleados, períodos de liquidación, conceptos, liquidaciones y recibos de sueldo, incorporando además la gestión de documentación digital de cada empleado y un mecanismo de **firma digital de recibos mediante certificado digital almacenado en token criptográfico**.

La especificación de los servicios se encuentra definida mediante **OpenAPI 3.0**, utilizando un archivo YAML que documenta los endpoints disponibles, DTOs, esquemas de datos, parámetros, mecanismos de autenticación y respuestas HTTP.
