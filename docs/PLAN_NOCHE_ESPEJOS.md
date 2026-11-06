# Plan: bases con espejo (BESTPHONE_STORE, TECNO_STYLE, GAPMOVILES)

Regla: **la principal manda** en accesorios. La espejo queda igual a la principal (IDs, datos y cantidades).

| Base | Principal | Espejo |
|---|---|---|
| BESTPHONE_STORE | BESTPHONE_STORE | BESTPHONE_STORE_E |
| TECNO_STYLE | TECNO_STYLE | TECNO_STYLE_E |
| GAPMOVILES | **GAPMOVILES_E** | **GAPMOVILES** |

## A. Hoy, antes de la noche (solo lectura)
Por cada base, en la **principal** y en la **espejo**:
1. `comparacion/EXTRAER_SP_GENERALES_PRODUCCION.sql` → guardar `<BASE>.rpt` (6 archivos). Los reviso contra el script.
2. En la principal: `ACCESORIOS_DUPLICADOS_VER.sql` y `VER_ACCESORIOS_SIN_HOMOLOGO.sql` → CSV si traen filas.
3. En la principal y en la espejo: consultas de reparaciones / desbloqueos afectados (si su `Facturas_Anular` tiene el error).
4. DbCompare 11 ya hecho (04/11 16:28): IDs alineados; solo difieren cantidades.

## Avance
- ✅ 04/11/2026 17:45 **BESTPHONE_STORE (principal): duplicados corregidos** (118 grupos, 193 inactivados, 330 líneas de factura reapuntadas; costo más alto). Esta noche, en BESTPHONE, el paso 1 ya está hecho: seguir desde el paso 2.
- ✅ 04/11/2026 17:55 **BESTPHONE_STORE:** principal → almacenes X a cero, script completo y datos; espejo → script completo y datos. **Falta esta noche:** homólogos en la principal → opción 13 → su .sql en la espejo → opción 11.
- ✅ 04/11/2026 17:58 **TECNO_STYLE:** sin duplicados; principal → almacenes X a cero, script completo y datos; espejo → script completo y datos. **Falta esta noche:** homólogos en la principal → opción 13 → su .sql en la espejo → opción 11.
- ✅ 04/11/2026 18:06 **GAPMOVILES:** principal GAPMOVILES_E → duplicados corregidos (costo más alto; XIAOMI EARBUDS 750), almacenes X a cero, script completo y datos (259 prefacturas A→X, 399 detalles A→X, 16609 detalles P→C); espejo GAPMOVILES → script completo y datos (51 / 33 / 7167). **Falta esta noche:** homólogos en GAPMOVILES_E → opción 13 → su .sql en GAPMOVILES → opción 11.
- ✅ 04/11/2026 19:55 **BESTPHONE_STORE alineada:** homólogos (63) → opción 13 (63 creados, 390 actualizados, 193 pares reapuntados, contador 1194) → opción 11: 1148/1148 sin diferencias.
- ✅ 04/11/2026 20:42 **GAPMOVILES alineada:** opción 11: 18310/18310 sin diferencias. (TECNO_STYLE alineada según el usuario.)
- ⚠️ **Regla de IDs:** el API repite cada `Accesorio_Insert` en la espejo y la espejo genera su propio ID. Para que no se descuadren:
  - `SCRIPT_MODULO_LAPTOPS_COMPLETO.sql` se corre en principal **y** espejo seguidos (el `Accesorio_Insert` nuevo no crea en almacenes X; si solo una base lo tiene, los IDs se separan en el próximo registro).
  - `ACCESORIOS_HOMOLOGOS_CREAR.sql` (crea registros solo en la principal) se corre **de noche**, sin operaciones, e inmediatamente después opción 13 + su .sql en la espejo (que también iguala el contador de ID con `DBCC CHECKIDENT`).

## B. Esta noche, después de las operaciones (por cada base, de a una)
**Principal** (respaldo antes):
1. `ACCESORIOS_DUPLICADOS_CORREGIR.sql`
2. `ACCESORIOS_ALMACENES_X_A_CERO.sql`
3. `ACCESORIOS_HOMOLOGOS_CREAR.sql`
4. `SCRIPT_MODULO_LAPTOPS_COMPLETO.sql`
5. `SCRIPT_MODULO_LAPTOPS_DATOS.sql`

**Espejo** (respaldo antes):
6. `SCRIPT_MODULO_LAPTOPS_COMPLETO.sql`
7. `SCRIPT_MODULO_LAPTOPS_DATOS.sql`
8. DbCompare **13** → genera `resultados/alinear_accesorios_<BASE>_ESPEJO_<fecha>.sql` (no ejecuta nada).
9. Revisar el encabezado del .sql (cuántos crea / actualiza / reapunta) y **ejecutarlo en la espejo**. Tiene control de nombre de base: si se corre en otra base, se detiene sin cambiar nada.

**Verificar:**
10. DbCompare **11** → debe salir "sin diferencias".
11. En la principal: `ACCESORIOS_DUPLICADOS_VER.sql` y `VER_ACCESORIOS_SIN_HOMOLOGO.sql` vacíos.

Orden sugerido: BESTPHONE_STORE → TECNO_STYLE → GAPMOVILES.

**Costo en duplicados con unidades a costos distintos:** se deja el costo MÁS ALTO (decidido con BESTPHONE).
✅ GAPMOVILES: XIAOMI EARBUDS queda en 750 (ya no se usa; misma regla del costo más alto).
Pendiente aparte (no esta noche): sincronización de accesorios y reparaciones en el API (tarea del agente).

---
## C. Pase a producción esta noche (decisión del usuario 04/11/2026: **publicar la rama `claude`** de front y API; `ventura` queda como está y el merge se hace en otro momento)
Verificado 04/11 18:30: en front y API, `ventura` ya está contenida en `claude` (merge sin conflictos esperados).

1. **Bases con espejo:** terminar B (homólogos → opción 13 → .sql en la espejo → opción 11) en BESTPHONE, TECNO_STYLE y GAPMOVILES.
2. **Script completo otra vez en TODAS las bases** (lleva la Tarea 21: `Accesorio_Sumar_Existencia` reactiva inactivos). Sin el script de datos (ya aplicado). Bases: INVERTER, VISIONTECH, DATAMOVIL, DELSIO_PC_GAMES, HANYOS_SMARTPHONE, JOTECH, SOLUSHOP_TECHNOLOGY, MYM_SMARTPHONE, SMART9, MV_SYSTEM, BESTPHONE_STORE + _E, TECNO_STYLE + _E, GAPMOVILES_E + GAPMOVILES. En las de espejo: principal y espejo seguidas.
3. **Código:** publicar desde la rama `claude` (front y API; `ventura` no se toca). Compilar (`dotnet build`, `ng build`), publicar **primero el API y después el front**. Revisar que `appsettings` de producción tenga la sección `Laptops` (HorasMaxCustodia 24, BloquearTecnicoConPendientesVencidos true).
4. **Permisos:** ya existen en SEGURIDAD (LAPTOP_*, ACCESORIOS_MOVIMIENTOS). Asignarlos a los usuarios que correspondan (técnico / encargado).
5. **Pruebas en producción:** login; factura normal con equipo y accesorio; prefactura; anulación; registrar accesorio (y uno repetido → ventana de coincidencias); consulta de vendidos por teléfono (10 dígitos / Enter) y por IMEI; movimientos de accesorios.
6. **Después:** revisar el log `[SYNC-FALLO]` y DbCompare opción 11 en las tres con espejo.

Nota: los tokens emitidos antes del pase siguen venciendo a las 8:00 p. m.; los nuevos vencen a la medianoche local.
Pendiente aparte (no bloquea el pase): sincronizar con la espejo las ventas/anulaciones de accesorios y las reparaciones (tarea del agente).
