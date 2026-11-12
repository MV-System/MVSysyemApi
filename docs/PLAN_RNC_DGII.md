# Plan – Catálogo de RNC de la DGII para facturar con comprobante

Objetivo: al facturar con comprobante, el usuario escribe el RNC y el sistema llena el nombre de la empresa desde el listado oficial de la DGII.
Rama de trabajo: la que indique el usuario (`ventura` solo con su autorización). **Una parte a la vez; entregar y esperar aprobación antes de seguir.**

## Decisiones del usuario (12/11/2026)
- **Una sola tabla en SEGURIDAD** (no en cada base de empresa): son ~800 mil filas.
- Al facturar se usa la **razón social** (nombre legal), no el nombre comercial.
- Con comprobante solo se acepta un RNC **ACTIVO**. Si el RNC está en el listado con otro estado (suspendido, dado de baja, etc.), no se deja facturar con comprobante y se avisa. Si **no está** en el listado, se permite escribir el nombre a mano (puede ser un registro reciente).
- Los registros que en el archivo vienen **partidos en varias líneas se omiten**; el usuario los registra a mano después.
- Las tablas `RNC_cata` que ya existen en cada base de empresa y los SP `RNC_Consulta` / `RNC_Guarda` **no se tocan** (hoy el API ni el front los usan).

## El archivo (revisado por Claude el 12/11/2026)
`C:\mvsystem\comparacion\DGII_RNC.TXT`
- **Codificación ISO-8859-1 (Latin-1)**, no UTF-8 (Ñ y tildes). Fin de línea CRLF. 797,988 líneas.
- Separador `|`, **11 columnas**: `0` RNC/cédula · `1` razón social · `2` nombre comercial · `3` actividad (cortada a 30) · `4–7` vacías · `8` fecha (dd/MM/yyyy, a veces vacía) · `9` estado · `10` régimen.
- RNC de 9 dígitos (505,422) y cédulas de 11 (292,521); ninguno repetido. Razón social máx. 65 caracteres; nombre comercial máx. 60.
- Estados: ACTIVO, SUSPENDIDO, DADO DE BAJA, CESE TEMPORAL, ANULADO, RECHAZADO.
- **44 líneas no tienen 11 columnas** (22 registros partidos por un salto de línea dentro del nombre): se omiten y se listan en un archivo aparte.
- No trae dirección.

---

## Parte 1 – SQL en SEGURIDAD
Script `CREATE_RNC_DGII_SEGURIDAD.sql` (idempotente), **solo en SEGURIDAD**:
1. Tabla `dbo.RNC_DGII`:
   - `RNC varchar(11) NOT NULL` **PRIMARY KEY**
   - `Razon_Social varchar(100) NOT NULL`
   - `Nombre_Comercial varchar(100) NULL`
   - `Estado varchar(15) NOT NULL` (texto de la DGII: ACTIVO, SUSPENDIDO…)
   - `Fecha_Actualizacion datetime NOT NULL` (cuándo se cargó o cambió por última vez)
2. Tabla de carga `dbo.RNC_DGII_Carga` (mismas 4 primeras columnas, sin llave; se vacía en cada carga).
3. SP `dbo.RNC_DGII_Aplicar_Carga`: en una transacción, `MERGE` de `RNC_DGII_Carga` → `RNC_DGII`: inserta los nuevos; actualiza razón social, nombre comercial y estado si cambiaron; **no borra** los que ya no vengan. Devuelve `Insertados, Actualizados, SinCambio`. Al final vacía `RNC_DGII_Carga`.
4. SP `dbo.RNC_DGII_Consulta @RNC varchar(11)`: devuelve `RNC, Razon_Social, Nombre_Comercial, Estado` (o nada).
- Aceptación: el script corre dos veces sin error; las tablas y SP existen.

## Parte 2 – Cargador en DbCompare (opción nueva)
**Modificar DbCompare: autorizado por el usuario para esta tarea.** Nueva opción del menú: **"15 – Cargar RNC de la DGII"** (archivo nuevo `CargarRncDgii.cs`):
1. Pide la ruta del TXT (por defecto `C:\mvsystem\comparacion\DGII_RNC.TXT`) y usa la conexión de **SEGURIDAD** ya configurada.
2. Lee línea por línea con **`Encoding.Latin1`** (o `Encoding.GetEncoding("ISO-8859-1")`).
3. Por cada línea: `Split('|')`. Si **no tiene exactamente 11 columnas**, no se carga y se escribe en `RNC_omitidos_<fecha>.txt` (junto al TXT) con su número de línea. Si tiene 11: `Trim()` a cada campo; RNC = col 0 (solo dígitos, 9 u 11, si no → omitido); razón social = col 1 (recortar a 100); nombre comercial = col 2 (vacío → NULL); estado = col 9.
4. `TRUNCATE` de `RNC_DGII_Carga` y **`SqlBulkCopy`** en lotes de 10,000 (con `BulkCopyTimeout` amplio).
5. Ejecuta `RNC_DGII_Aplicar_Carga` y muestra: líneas leídas, cargadas, omitidas (y el archivo), insertados, actualizados, sin cambio, tiempo total.
6. Repetible: la próxima vez que la DGII publique el archivo se corre de nuevo y solo actualiza diferencias.
- Aceptación: (a) primera carga ≈ 797,900 registros y **44 líneas omitidas** en el archivo de omitidos; (b) Ñ y tildes correctas (p. ej. buscar un nombre con "Ñ"); (c) segunda corrida: 0 insertados, 0 actualizados; (d) compila.

## Parte 3 – API: consulta de RNC
1. `AccesoDatos` (o un servicio pequeño) con `ConsultarRncDgii(string rnc)` que llama a `dbo.RNC_DGII_Consulta` con la conexión **MVSystemSeguridad**.
2. Endpoint `GET Catalogos/RncDgii/{rnc}`:
   - Valida: solo dígitos y largo 9 u 11 → si no, 400 "El RNC debe tener 9 dígitos o la cédula 11".
   - Encontrado → 200 `{ rnc, razonSocial, nombreComercial, estado, activo: estado == "ACTIVO" }`.
   - No encontrado → 404 `{ error: "RNC no encontrado en el listado de la DGII" }`.
   - Permiso: `[RequierePermiso(PermisosApi.MantFacturacion, PermisosApi.CotizacionMenu)]` (las pantallas que piden RNC).
- Aceptación: RNC activo, suspendido, inexistente y con formato inválido devuelven lo indicado; compila.

## Parte 4 – Front: facturación con comprobante
Archivo: `src/app/MVSystem/pages/facturacion/facturacion-registro/` (campos `RNC` y `empresa` que ya existen).
1. Al salir del campo RNC o pulsar Enter, si tiene 9 u 11 dígitos (quitar guiones y espacios antes), consultar el endpoint. Sin botones nuevos.
2. **Encontrado y ACTIVO:** llenar `empresa` con la razón social. El campo sigue editable.
3. **Encontrado y NO activo:** mostrar `toastr.warning("RNC <estado> según la DGII: no se puede facturar con comprobante a este RNC.")` y, al guardar una factura **con comprobante**, no dejar continuar con ese mismo mensaje (sumar a `validarRncComprobante`).
4. **No encontrado (404):** aviso suave `toastr.info("RNC no encontrado en el listado de la DGII; escriba el nombre de la empresa.")` y se permite escribir a mano.
5. Mientras consulta, no bloquear la pantalla; si el usuario ya escribió un nombre distinto, reemplazarlo por la razón social solo si el campo estaba vacío o lo había llenado la consulta anterior.
- Aceptación: (a) RNC activo llena el nombre; (b) RNC suspendido avisa y no deja facturar con comprobante; (c) RNC no listado permite escribir el nombre; (d) sin comprobante (consumidor final) nada cambia; (e) compila.

## Entrega de cada parte
1. Archivos creados/modificados. 2. Scripts SQL y dónde se ejecutan (Parte 1: solo SEGURIDAD). 3. Cómo probar. 4. Supuestos o dudas. 5. Confirmar la rama. 6. **No continuar** sin aprobación.
