# Pase a producción – Módulo de Laptops

> ## ⛔ PROHIBIDO SIN AUTORIZACIÓN EXPRESA DEL USUARIO
>
> - **NO** ejecutar ningún script, `CREATE`, `ALTER`, `UPDATE`, `INSERT` ni `DELETE` en las **bases de datos de producción** (principal ni espejo de ninguna empresa).
> - **NO** hacer commit, merge, cherry-pick, rebase ni push en la rama **`ventura`**. Tampoco merge de `claude` → `ventura`.
> - Todo el trabajo del módulo se hace **solo en la rama `claude`** y se prueba **solo en la base de pruebas**.
> - En producción solo se permite **leer** (por ejemplo `OBJECT_DEFINITION` / `sp_helptext`) y **solo si el usuario lo pide**.
> - Este documento es una lista para **cuando el usuario decida** hacer el pase. Tenerla no autoriza a ejecutarla.
>
> Un cambio en producción o en `ventura` sin autorización puede dañar la facturación, el inventario y la sincronización con la espejo de todos los clientes.

---

## Lista de verificación (en este orden, cuando el usuario lo autorice)

### 1. Antes del pase (solo lectura en producción)
- [ ] Sacar de producción (solo lectura) la definición actual de **cada SP general** que reemplaza `SCRIPT_MODULO_LAPTOPS_COMPLETO.sql` (los que no son propios de laptops), entre ellos:
  `Procesar_PreFactura`, `PreFacturas_Anular`, `PreFacturas_Consultar`, `Equipo_Actualiza_Estado_y_precio`, `Proc_Equipo_Disponible_Consulta`, `Equipo_Busca_Disponible`, `Proc_Equipo_Consulta_Vendidos`, `Modelos_Consulta_Combo`, `Accesorio_Insert`, `Accesorios_Modifica`, `Detalle_Factura_Insert`, `Facturas_Anular`, `Proc_Anular_Factura`, `Ajustar_Inventario`, `Subir_Inventario`, `Transfiere_Accesorio`, `Accesorio_trasfiere_actualiza`, y cualquier otro que el script tenga con `CREATE OR ALTER` y no empiece por `Equipo_Cambio_Pieza`, `Equipo_Configuracion`, `Componente`, `Accesorio_Movimiento`.
- [ ] Comparar cada uno contra la versión del script. Solo se aceptan las diferencias acordadas (contexto de movimientos, columna `Configuracion`, `ID_Grupo`, estados `X`/`C` de prefacturas, validación de duplicados en accesorios, correcciones revisadas). Cualquier otra diferencia → corregir el script **antes** de seguir.
- [ ] Confirmar que la rama `claude` tiene todo lo de `ventura` (merge `ventura` → `claude`) y que compila: `dotnet build` (API) y `ng build` (front).
- [ ] Probar en la base de pruebas el flujo completo: registrar laptop, cambio de pieza, custodia (recibir / entregar al cliente), anular cambio, vender por caja y por factura directa, anular factura, movimientos de accesorios.
- [ ] Respaldo (backup) completo de cada base de producción (principal y espejo) justo antes del pase.

### 2. Base de datos (por cada empresa, principal y espejo)
- [ ] Ejecutar `SCRIPT_MODULO_LAPTOPS_COMPLETO.sql` de arriba hacia abajo.
- [ ] Ejecutar `SCRIPT_MODULO_LAPTOPS_DATOS.sql` (una sola vez por base).
- [ ] **Módulos con el mismo ID:** migrar en la principal y verificar que `Equipo_Configuracion_Modulo` tenga los mismos `ID_Modulo` en la espejo (consulta de verificación del script). Si no coinciden, alinear antes de usar el módulo.
- [ ] Ejecutar los permisos en la BD de seguridad (si aplica) y asignarlos a los usuarios.

### 3. Código
- [ ] Merge `claude` → `ventura` **solo con autorización del usuario**.
- [ ] Publicar API y front.

### 4. Después del pase
- [ ] Probar en producción: una factura normal, una prefactura, una anulación, un registro de accesorio y la consulta de vendidos.
- [ ] Revisar el log de sincronización (`[SYNC-FALLO]`) y el monitor de principal/espejo.
- [ ] Revisar que `Accesorio_Movimiento` registre los movimientos con su motivo y usuario.

---

## Bases de datos actualizadas (solo BD, sin subir API ni front)

Antes de cada base: extraer los 19 SP generales (`comparacion/EXTRAER_SP_GENERALES_PRODUCCION.sql`) y compararlos con el script; consulta de accesorios duplicados; respaldo.

| Base | Espejo | SP comparados | Duplicados | Script completo | Script datos | Estado |
|---|---|---|---|---|---|---|
| INVERTER | — | ✅ 04/11/2026 | ✅ sin duplicados | ✅ 04/11/2026 | ✅ (no usa prefacturas: 0 cambios) | ✅ **Completado** |
| VISIONTECH | No | ✅ 04/11/2026 (igual a INVERTER) | ✅ sin duplicados | ✅ 04/11/2026 | ✅ 43 prefacturas A→X, 155 detalles A→X, 525 detalles P→C | ✅ **Completado** |
| DATAMOVIL | No | ✅ 04/11/2026 (igual a VISIONTECH salvo Facturas_Anular) | ✅ sin duplicados | ✅ 04/11/2026 | ✅ 04/11/2026 | ✅ **Completado** |
| DELSIO_PC_GAMES | Pendiente (espejos después) | ✅ 04/11/2026 (igual a SMART9) | ✅ 3 pares corregidos (1229, 1122, 1389 → "(VIEJO)", inactivos; 1388 = 2 uds) | ✅ 04/11/2026 | ✅ 04/11/2026 | ✅ **Completado** |
| HANYOS_SMARTPHONE | Pendiente (espejos después) | ✅ 04/11/2026 (igual a INVERTER) | ✅ | ✅ 04/11/2026 (script ya corregido) | ✅ 04/11/2026 | ✅ **Completado** |
| JOTECH | Pendiente (espejos después) | ✅ 04/11/2026 (igual a SMART9; 0 reparaciones afectadas) | ✅ 19 grupos corregidos con ACCESORIOS_DUPLICADOS_CORREGIR (21 inactivados, 35 líneas de factura reapuntadas) | ✅ 04/11/2026 | ✅ 04/11/2026 | ✅ **Completado** |
| SOLUSHOP_TECHNOLOGY | Pendiente (espejos después) | ✅ iguales a VISIONTECH (verificado por el usuario) | ✅ sin duplicados | ✅ 04/11/2026 | ✅ 04/11/2026 | ✅ **Completado** |
| MV_SYSTEM (pruebas) | — | — | — | ✅ 04/11/2026 | ✅ | ✅ **Completado** |
| MYM_SMARTPHONE (sin uso, activo) | — | — | — | ✅ 04/11/2026 | ✅ | ✅ **Completado** |
| SMART9 | No | ✅ 04/11/2026 (igual a DELSIO/JOTECH) | ✅ 104 grupos corregidos (3 líneas de factura reapuntadas) | ✅ 04/11/2026 | ✅ 04/11/2026 | ✅ **Completado** |
| BESTPHONE_STORE | Sí | ⏳ | ⏳ | ⏳ | ⏳ | ⏳ Pendiente (principal + espejo) |
| TECNO_STYLE | Sí | ⏳ | ⏳ | ⏳ | ⏳ | ⏳ Pendiente (principal + espejo) |
| GAPMOVILES | Sí | ✅ 04/11/2026 | ⏳ | ⏳ | ⏳ | ⏳ Último (principal + espejo, horario de poco movimiento) |

**Notas**
- **VISIONTECH:** grupo Laptops ya existía (ID 2). Se crearon grupo y marca Componentes (marca 246) y modelos 4945–4951. Sus modelos de laptop llevan la configuración en el nombre (ej. "T480 I7 8RAM/256GB"): ordenar eso es una tarea aparte para después.
- **DATAMOVIL:** tenía el mismo error de `Facturas_Anular` que SMART9 (4 = desbloqueo, 3 = reparación). Consulta de reparaciones afectadas: 0 filas. El script lo corrige.
- **Proc_Equipo_Consulta_Vendidos (04/11/2026):** el script se corrigió (filtro por teléfono, IMEI ignora fechas, factura por número + literal, "Cliente no encontrado"). Probado en PRUEBAS. Script completo vuelto a ejecutar en INVERTER, VISIONTECH y DATAMOVIL ✅ 04/11/2026.
- **DELSIO_PC_GAMES:** mismo `Facturas_Anular` con error que SMART9 (0 reparaciones afectadas). Duplicados de accesorios corregidos directo en la principal: si tiene espejo, repetir allá. Facturas activas que vendían los inactivos se reapuntaron al activo: P1555 (1229→1244) y P815 (1122→779). Regla: antes de inactivar un duplicado, revisar facturas activas y prefacturas pendientes que lo usen.
- ⏳ **Accesorios sin homólogo en todos los almacenes** (`comparacion/VER_ACCESORIOS_SIN_HOMOLOGO.sql`): revisar en cada base.
  - Decidido: crear los homólogos que falten con cantidad 0, copiando modelo, descripción, costo y precios de un registro existente, en TODOS los almacenes (incluido 17 "No Disponibles" aunque sea de equipos: Accesorio_Insert ya los crea ahí; si hay que limpiar, más adelante).
  - Bases con espejo: crear en la principal y generar los INSERT con el MISMO ID para la espejo (como los modelos).
  - Orden por base: 1) `ACCESORIOS_ALMACENES_X_A_CERO.sql` 2) `ACCESORIOS_HOMOLOGOS_CREAR.sql` 3) script completo 4) VER debe salir vacío.
  - **DATAMOVIL:** ✅ 04/11/2026 (2 almacenes activos, 8 en X; accesorios de almacenes X a cero, homólogos creados, script completo re-ejecutado, VER vacío).
  - **INVERTER, VISIONTECH, DELSIO_PC_GAMES, HANYOS_SMARTPHONE, JOTECH, SOLUSHOP_TECHNOLOGY, MYM_SMARTPHONE y MV_SYSTEM (pruebas):** ✅ 04/11/2026 (incluye re-ejecución del script completo con Accesorio_Insert solo en almacenes activos).
  - **BESTPHONE_STORE:** revisado 04/11/2026 → falta casi todo en el almacén 17 y algunos accesorios solo existen en 1–2 almacenes. Tiene espejo: **se ve después**.
- ✅ **Accesorio_Insert (04/11/2026):** ya no crea el accesorio en almacenes en estado 'X'. Script completo re-ejecutado en las 9 bases completadas. Los homólogos (VER/CREAR) también ignoran los almacenes X. Antes de borrar almacenes X: `comparacion/VER_ALMACENES_X_REFERENCIAS.sql`.
- **SMART9:** ✅ scripts ejecutados 04/11/2026 (duplicados, almacenes X a cero, homólogos, completo, datos; verificación vacía). Reparaciones y desbloqueos afectados por el error de anulación: 0 (no hay reparaciones en facturas anuladas). Nada que corregir.
- **Espejos:** se trabajan al final, cuando nadie reporte errores por estos cambios.
- `Facturas_Anular`: el sistema factura reparaciones con tipo **4** (y 3 en facturas viejas) y desbloqueos con **5**. El script quedó con 3 y 4 = reparación, 5 = desbloqueo.
- **SMART9:** su `Facturas_Anular` trataba el 4 como desbloqueo → al anular facturas con reparación, la reparación no volvía al taller (el script lo corrige; revisado 04/11/2026: sin datos afectados). Su `Proc_Equipo_Consulta_Vendidos` es la versión vieja (13/12/2025); el script la reemplaza por la simplificada.
