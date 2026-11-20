# Plan – Retención por pagos con tarjeta

Objetivo: cuando se cobra con tarjeta, mostrar el % que retiene el procesador, igual que el ITBIS, para que la tienda vea su ganancia real.
Rama de trabajo: la que indique el usuario (`ventura` solo con su autorización). **Una parte a la vez; entregar y esperar aprobación antes de seguir.**
Agentes: `analista` para revisar cada parte antes de empezar, `explorador` para ubicar el código y `programador` para el cambio.

## Decisiones del usuario (19/11/2026)
- **Un solo % por tienda**, el mismo para todas las tarjetas. En `Tipos_Pagos_Cata` la tarjeta es **una sola fila**.
- Cada empresa tiene su propia base, así que el % de una tienda no afecta a otra.
- El % lo configura el usuario **directo en la base** (por script). **No** se hace pantalla de mantenimiento.
- Se muestra en **Consulta de facturas** y en **Utilidades**.
- Si en el futuro una tienda tiene varios %, se ajusta en ese momento.
- En las empresas con espejo, el % se configura **en las dos bases** (si no, la espejo calcula 0).
- **La rebaja de un artículo no recalcula la retención**: la retención sigue lo cobrado con tarjeta, y el procesador normalmente no devuelve su comisión.

## Cómo está hoy (revisado en `ventura`, commit 066ebf2)
- `Facturas.ID_Tipo_Pagos`: forma de pago de la factura (1 contado, 2 crédito, 6 mixto; la tarjeta tiene su propio ID en cada base).
- `DetallesPagos_Factura` (SP `InsertDetallesPagoFactura`): **solo se llena en pago mixto** (`IdTipoPago == 6`, `FacturacionController` ~línea 110). Si se paga todo con tarjeta, no hay fila de detalle.
- Facturas se crean por dos caminos: `AccesoDatos.PostFacturaConPagos` (facturación) y el SP `Procesar_PreFactura` (caja, prefacturas; ~línea 3445 de `AccesoDatos.cs`). Los pagos de la prefactura pasan a la factura con `CopiarDetallesPagoPrefacturaAFactura`. Hay además un `PostFactura` antiguo (`FacturacionReporteController` ~46): confirmar si todavía se usa.
- Abonos de crédito: `Historio_Pagos` ya guarda `ID_Tipo_Pagos` del abono.
- La sincronización con la espejo solo mueve disponibilidad de equipos, no facturas. Aun así, **los cambios de tablas y SP van en principal y espejo** para que las bases queden iguales (DbCompare).

## Regla de cálculo
- Una forma de pago es tarjeta si `Tipos_Pagos_Cata.Porcentaje_Retencion > 0`.
- Monto con tarjeta de la factura:
  - Forma de pago tarjeta: `Facturas.Total`.
  - Pago mixto: suma de las líneas de `DetallesPagos_Factura` (Tipo_Documento 'F') cuyo tipo es tarjeta.
  - Otras formas: 0.
- `Monto_Retencion = ROUND(monto con tarjeta × % / 100, 2)`. Se calcula sobre lo cobrado, ITBIS incluido.
- **El % se copia en la factura al momento de la venta.** Si después cambia el %, las facturas anteriores no cambian.
- Facturas anteriores a este cambio quedan en 0. **No** se recalculan con el % actual.
- Facturas anuladas no se cuentan (las consultas ya filtran `Estado = 'A'`).

---

## Parte 0 – Rebaja en facturas con tarjeta o mixtas (antes de la Parte 1)
Hallazgo del 19/11/2026: `Proc_Detalle_Factura_Elimina_Equipo` (rebaja de artículo, `AccesoDatos.RebajaEquipo`) solo ajusta `SubTotal`, `Itbis` y `Total` de la factura cuando `ID_Tipo_Pagos IN (1, 2)` (contado y crédito). Con tarjeta o mixto (6), la línea pasa a 'X' pero la cabecera y `DetallesPagos_Factura` quedan igual: la Consulta de facturas muestra el monto original y Utilidades sí descuenta la línea.
1. Sacar la definición actual del SP (MV_SYSTEM, DbCompare opción 9) y revisar el caso de tarjeta y mixto.
2. El analista propone cómo ajustar la cabecera en esos casos y qué hacer con los pagos del mixto (qué pago se reduce), sin tocar la retención.
3. Decidir con el usuario antes de programar.
- **Decidido (19/11/2026):** solo cambia el SP (script `UPDATE_Proc_Detalle_Factura_Elimina_Equipo.sql`). La cabecera se ajusta con cualquier forma de pago; en el mixto se baja primero el efectivo y después el otro pago. De paso: ITBIS con centavos (antes se redondeaba a entero) y la doble rebaja ya no deja el total en NULL.
- Aceptación: (a) rebajar un artículo de una factura con tarjeta o mixta baja el total de la factura; (b) contado y crédito siguen igual; (c) la retención no cambia.

## Parte 1 – SQL (primero en la base de pruebas)
Script `CREATE_RETENCION_TARJETA.sql`, **idempotente**, para correr en **principal y espejo**:
1. `Tipos_Pagos_Cata`: columna `Porcentaje_Retencion DECIMAL(5,2) NOT NULL DEFAULT 0`.
2. `Facturas`: columnas `Porcentaje_Retencion_Tarjeta DECIMAL(5,2) NOT NULL DEFAULT 0` y `Monto_Retencion_Tarjeta DECIMAL(18,2) NOT NULL DEFAULT 0`.
3. `Historio_Pagos`: las mismas dos columnas (para la Parte 5).
4. SP `dbo.Factura_Calcular_Retencion_Tarjeta @Numero_Factura INT`: aplica la regla de cálculo y hace `UPDATE` de las dos columnas de esa factura. Si la factura no tiene tarjeta, deja 0. Se puede ejecutar varias veces con el mismo resultado.
5. Al final, una consulta de verificación (columnas y SP existen).

Script aparte para el usuario, `CONFIGURAR_RETENCION_TARJETA.sql`: muestra `Tipos_Pagos_Cata` y deja comentado el `UPDATE ... SET Porcentaje_Retencion = 4.50 WHERE ID_Tipo_Pagos = <id de tarjeta>`. El usuario lo ejecuta en cada base.

- Aceptación: (a) el script corre dos veces sin error; (b) en pruebas, con el % configurado, el SP calcula bien una factura con tarjeta, una mixta (tarjeta + efectivo) y una en efectivo (0).

## Parte 2 – API: calcular al crear la factura
1. `PostFacturaConPagos`: después de insertar los detalles de pago y **dentro de la misma transacción**, ejecutar `Factura_Calcular_Retencion_Tarjeta`.
2. Prefacturas (caja): ejecutar el mismo SP con la factura generada por `Procesar_PreFactura`, después de copiar los pagos. Si es más seguro, llamarlo dentro de `Procesar_PreFactura` o de `CopiarDetallesPagoPrefacturaAFactura`; el analista decide y lo explica.
3. `PostFactura` antiguo: si se sigue usando, aplicar lo mismo; si no, dejarlo como está e indicarlo.
4. Si cambian los pagos de una factura ya creada (por ejemplo `PostDetallesPagoFactura` con Tipo 'F'), volver a ejecutar el SP.
- No cambia el monto de la factura ni lo que se envía a la DGII.
- Aceptación: (a) factura con tarjeta desde Facturación y desde Caja guardan el % y el monto; (b) factura mixta calcula solo la parte de tarjeta; (c) efectivo queda en 0; (d) compila.

## Parte 3 – Consulta de facturas
1. `Proc_Facturas_Consulta`: devolver `F.Monto_Retencion_Tarjeta` por factura y el total `@TotalRetencionTarjeta` (mismo filtro que `@TotalItbis`). Script `UPDATE_Proc_Facturas_Consulta_Retencion.sql` (principal y espejo).
2. API: agregar el campo al modelo que sale de `GetFacturas`.
3. Front (`facturacion-consulta`): en el resumen, una tarjeta **"Retención tarjeta"** junto a ITBIS, con el mismo estilo. En la lista y en el detalle de la factura, mostrar la retención solo cuando sea mayor que 0. Sin botones ni pantallas nuevas.
4. Si la tienda no tiene % configurado (todo 0), la tarjeta del resumen muestra RD$0 como las demás.
- Aceptación: (a) el total del resumen cuadra con la suma de las facturas; (b) con filtro por tipo de pago, vendedor y fecha sigue cuadrando; (c) compila.

## Parte 4 – Utilidades
1. `Ver_Utilidades_Precio_Detalle`: **no está en el repo**; sacar su definición actual de la base de pruebas antes de cambiarlo. Agregar la retención de la factura y descontarla de la ganancia: **ganancia real = ganancia actual − retención**. Si el SP trabaja por línea de artículo, repartir la retención de la factura de forma proporcional a cada línea (o mostrarla como total aparte); el analista propone cuál, sin duplicarla.
2. API y front de Utilidades: mostrar el total "Retención tarjeta" y la ganancia real, junto a lo que ya se muestra.
- Aceptación: (a) ganancia real = ganancia − retención en el total del período; (b) una factura con varias líneas no descuenta la retención más de una vez; (c) compila.

## Parte 5 – Abonos con tarjeta (crédito)
1. Al registrar un abono cuyo `ID_Tipo_Pagos` sea tarjeta, guardar en `Historio_Pagos` el % y el monto retenido (mismo cálculo sobre `Total_Abono`).
2. `Ver_Utilidades_Abonos_Credito_Detalle` y la vista de abonos en Utilidades: mostrar la retención del abono.
- Aceptación: (a) abono con tarjeta guarda la retención; (b) abono en efectivo queda en 0; (c) compila.

## Entrega de cada parte
1. Archivos creados o modificados. 2. Scripts SQL y en qué bases se ejecutan (principal y espejo; primero pruebas). 3. Cómo probar. 4. Supuestos o dudas. 5. Confirmar la rama. 6. **No continuar** sin aprobación.
