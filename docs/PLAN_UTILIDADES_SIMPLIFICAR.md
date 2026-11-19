# Plan – Simplificar la pantalla de Utilidades

Objetivo: menos tarjetas y totales repetidos, y que los números cuadren entre sí. Lo mínimo pero funcional, sin pantallas ni botones nuevos.
Rama: la que indique el usuario. Depende de `retencion-tarjeta` (usa la retención de facturas y abonos). **Una parte a la vez; entregar y esperar aprobación.**
Agentes: `analista` antes de cada parte, `explorador` para ubicar el código y `programador` para el cambio.

Archivos (front): `src/app/MVSystem/pages/utilidades/utilidades-consulta/utilidades.component.html`, `.ts` y `.css`.

## Cómo está hoy (revisado el 19/11/2026)
- 7 tarjetas, 3 gráficos con sus totales, y un modal de análisis. Ventas aparece 3 veces; ganancias y costos, 2.
- Hay 3 ganancias distintas: contado sin retención (`getTotalGanancias`), "Ganancia real" solo de contado (`getGananciaReal`) y la ganancia de abonos saldados. Ninguna junta contado y crédito.
- El margen se calcula sobre la venta **con** ITBIS y se redondea a entero.
- "vs período anterior" muestra la flecha verde aunque las ventas hayan bajado.
- La retención de los abonos solo aparece en el modal de detalle.
- La ganancia de abonos saldados se muestra en 0 mientras carga el modal (comparte la lista `detalleAbonos`).
- El botón "Actualizar" no tiene `(click)`.

## Decisiones del usuario (19/11/2026)
- La **Ganancia real** del período junta las dos partes:
  - **Contado:** ganancia de equipos, accesorios, reparaciones y desbloqueos, menos la retención de esas facturas.
  - **Crédito saldado:** ganancia de las facturas a crédito que se saldaron en el período, menos la retención de sus abonos.
- Se muestran las dos partes por separado en la tabla y la suma como total; las filas cuadran con el total.
- **Una fila de categoría sin venta ni ganancia en el período se oculta** (por ejemplo desbloqueos o reparaciones). Aparece sola cuando haya datos.

## Fórmulas
- Ventas = suma de `venta_*` (con ITBIS). Debajo, "sin ITBIS".
- ITBIS = suma de `itbis_*`.
- Costo = suma de `costo_*`.
- Retención tarjeta = `retencion_Tarjeta` (facturas de contado) + retención de los abonos del período.
- Ganancia contado = suma de `ganancia_*` − `retencion_Tarjeta`.
- Ganancia crédito saldado = ganancia de abonos saldados − su retención.
- **Ganancia real = ganancia contado + ganancia crédito saldado.**
- Margen % = ganancia / venta **sin ITBIS**, con un decimal.

---

## Parte 1 – Resumen (front)
Una sola fila con 5 números, en lugar de las 7 tarjetas:
1. Ventas (y "sin ITBIS: X" debajo).
2. Costo.
3. ITBIS.
4. Retención tarjeta.
5. **Ganancia real** (destacada) y el margen debajo.
- La lista de abonos del resumen se carga aparte de la del modal, para que no se ponga en 0 mientras el modal carga.
- Aceptación: (a) los 5 números caben sin scroll en escritorio y se acomodan en móvil; (b) Ganancia real = contado + crédito saldado; (c) compila.

## Parte 2 – Tabla por categoría (front)
Reemplaza las demás tarjetas y los totales de los gráficos.

| Fila | Venta | ITBIS | Costo | Retención | Ganancia | Margen % |
|---|---|---|---|---|---|---|
| Equipos / Accesorios / Reparaciones / Desbloqueos | … | … | … | | … | … |
| Subtotal contado | … | … | … | −retención | ganancia − retención | … |
| Crédito saldado en el período | … | … | … | −retención abonos | … | … |
| **Total** | | | | | **Ganancia real** | |

- Filas de categoría con venta y ganancia en 0: ocultas.
- La fila "Crédito saldado" abre el modal de detalle de abonos que ya existe (solo los que saldan).
- Debajo de la tabla, una línea: "Cobrado de crédito en el período: X", con el enlace al detalle completo de abonos.
- Aceptación: (a) la suma de las filas da el total; (b) el total coincide con la Ganancia real del resumen; (c) un período sin desbloqueos no muestra esa fila; (d) compila.

## Parte 3 – Limpieza (front)
- Quitar las tarjetas viejas, la franja de ventas y los totales repetidos de los gráficos.
- **Decidido (19/11/2026), lo más simple:** quitar "vs período anterior", los 3 gráficos, el modal de análisis (insights) y el botón "Actualizar" sin acción. La tabla ya muestra cuánto aporta cada categoría.
- Quitar también el código del .ts que solo usaban esas partes (gráficos, insights, período anterior) y la librería de gráficos si ya nadie la usa.
- Aceptación: (a) ningún número se repite en la pantalla; (b) compila.

## Parte 3b – Gráficos animados (front)
Decidido (19/11/2026): la pantalla quedó muy plana; se agregan gráficos con **ApexCharts** (`ng-apexcharts` 1.6.0, última compatible con Angular 12; `apexcharts` ya estaba en package.json). Solo en Utilidades; el dashboard sigue con ngx-charts.
Los gráficos usan **los mismos números de la tabla** (`resumen`, `filasCategoria`, `filaCreditoSaldado`), sin totales propios.
1. Resumen: los 5 números cuentan de 0 a su valor al cargar (respetando `prefers-reduced-motion`).
2. Dona "¿A dónde va cada peso vendido?": Costo, ITBIS, Retención y Ganancia real; en el centro, el margen.
3. Barras de ganancia por categoría (solo las que tienen datos, incluido crédito saldado).
4. La tabla queda debajo como detalle.
- Aceptación: (a) los números de los gráficos coinciden con la tabla; (b) se ven animados y caben en móvil; (c) compila después de `npm install`.

## Parte 4 – Opcional (API y SP): que cuadren los centavos y reparaciones
Solo si el usuario lo aprueba.
- **Decidido (19/11/2026):** las reparaciones se cobran solo en facturas de tipo reparación (`ID_Tipo_Factura = 4`); al elegir ese tipo, el sistema busca las reparaciones pendientes del cliente. Se mantiene el filtro `= 4`.
- **Pendiente (19/11/2026):** desbloqueos (si `Desbloqueos.SubTotal` es el costo y el join con facturas directas). Los clientes no usan ese módulo; el bloque de desbloqueos del SP no se toca en esta parte.
- **Decidido (19/11/2026):** quitar el filtro `Equipos.Disponible = 'N'` (y el join con Equipos) del bloque de equipos; la venta la define la línea y la factura activas. En MV_SYSTEM solo agrega 2 líneas (IMEI que ya no existe en Equipos).
- **Decidido (19/11/2026):** en reparaciones, los nombres de columnas de costo y precio no reflejan su uso actual (se ajustó sin renombrar). Tratarlas como se comportan hoy; renombrar después en otra tarea. El costo del crédito saldado (`Ver_Utilidades_Abonos_Credito_Detalle`) no se cambia en esta parte. Afecta también al dashboard (usa el mismo `GetUtilidades` y el mismo SP).
1. `Ver_Utilidades_Precio_Detalle`, reparaciones: la ganancia usa `r.precio` y la venta `df.total`, así que venta − costo − ITBIS no da la ganancia. Revisar también si el join por `numero_factura` repite filas.
2. `Utilidades_Negocio.cs`: cambiar `Convert.ToInt32` por `Convert.ToDecimal` (y los tipos en `Model/Utilidades.cs` y el front) para no perder centavos.
3. La retención filtra por `Facturas.Fecha_registro` y las ventas por `Detalle_Facturas.fecha_registro`: unificar.
- Scripts en principal y espejo de cada empresa.

## Riesgos
- La nueva Ganancia real dará distinto de la que ven hoy (suma el crédito y resta la retención de los abonos). Avisar a los clientes.
- Facturas y abonos anteriores a la retención tienen retención 0 (no se recalculan).
- Ningún permiso oculta los costos en esta pantalla; si se quiere, es otra tarea.
- No hay impresión ni exportación en esta pantalla.

## Entrega de cada parte
1. Archivos modificados. 2. Cómo probar. 3. Supuestos o dudas. 4. Confirmar la rama. 5. **No continuar** sin aprobación.
