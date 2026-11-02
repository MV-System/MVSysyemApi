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
