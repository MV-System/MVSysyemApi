# Pendientes – Módulo de Laptops (rama claude)

> Lista de correcciones y mejoras acordadas. Marcar [x] al terminar. Entre paréntesis, la tarea de `PLAN_MEJORAS_LAPTOPS.md` que lo resuelve.

## Hechas
- [x] Mejora 2: la espejo guarda el cambio de pieza con el mismo ID_Cambio (`UPDATE_Equipo_Cambio_Pieza_Registrar_ID_Explicito.sql`).

## Pendientes
- [x] **Especificación de componentes:** capacidad y tipo de cada pieza deducidos del accesorio. (Tarea 1)
- [x] **Varios slots / varios discos:** configuración por módulo (1×16 vs 2×8, SSD + HDD), con corrección manual de módulos. (Tarea 2)
- [x] **Pieza retirada propuesta por el sistema:** se elige entre los módulos reales de la laptop. (Tarea 3)
- [x] **Mejora 5 – Validar compatibilidad:** no mezclar DDR3/DDR4; la retirada es un módulo de la laptop. (Tarea 3)
- [x] **Mejora 6 – Capacidad calculada:** sin "Capacidad nueva" ni "Tipo" manuales. (Tarea 3)
- [x] **Crear la pieza retirada automáticamente:** cambios nuevos en Tarea 3; cambios viejos al recibir en Tarea 4. (Tareas 3 y 4)
- [x] **Mejora 3 – Costo de la pieza usada.** Obligatoria antes de producción. (Tarea 5)
- [x] **Mejora 4 – Anular un cambio de pieza.** (Tarea 6)
- [x] **Kardex por accesorio y reporte general de consumo** (filtros fecha, almacén, técnico, tipo, motivo). (Tarea 7)
- [x] **Comprobante de custodia.** (Tarea 8)
- [x] **Mejora 7 – Fechas:** unificar `GETDATE()` vs `DATEADD(HOUR,-4,GETDATE())`. (Tarea 9)
- [x] **Botón "Actualizar lista" en "Pieza que se instala"** (sin perder el formulario). (Tarea 10)
- [x] **Quitar la variante "(Usado)":** la pieza retirada vuelve al mismo accesorio, al costo de catálogo (reemplaza la regla de la Tarea 5). (Tarea 11)
- [x] **Registro de todos los movimientos de stock de accesorios** (entradas, ajustes, ventas, anulaciones, cambios): quién, cuándo, cuánto y por qué. (Tarea 12)
- [x] **Vista "Movimientos" en la consulta de accesorios** y eliminar las pantallas de Kardex y Consumo. (Tarea 13)
- [x] **Separación técnico / encargado:** técnico automático, sin destino "cliente" al registrar, recibir/entregar/anular solo por otro usuario con permiso (validado en el API). (Tarea 14)
- [x] **Una sola entrada "Laptops" en el menú** (selector Cambio de pieza | Custodia según permiso). (Tarea 15)
- [x] **Quitar el comprobante de custodia** (front, API y script). (Tarea 16)
- [x] **Quitar el campo "Tipo de pieza"**: se elige la pieza que entra y el sistema filtra y preselecciona la que sale (si hay un solo candidato o son iguales). (Tarea 17)

- [ ] **Retirar una pieza sin instalar otra** — ⏸️ para después, no ejecutar hasta que el usuario lo indique. (Tarea 18)

## Para evaluar más adelante (no implementar todavía)
- **Pieza retirada dañada:** hoy toda pieza retirada vuelve al stock como buena al recibirla; no hay forma de marcarla como dañada.
- **Agrupar permisos** (técnico / encargado): esperar a ver en la práctica quién hace cada tarea.

## Mejoras generales (fuera del módulo de laptops)
- [x] **Consulta de equipos vendidos – búsqueda por teléfono:** hoy se envía una consulta por cada dígito. Decidido: buscar solo cuando el campo tenga los 10 dígitos o cuando el usuario presione Enter (sin búsqueda por nombre). → **Tarea 19** ✅ (commit 7abcd7c, 04/11/2026).
- [ ] **Ayuda interactiva dentro del sistema:** un menú "Ayuda" donde el usuario que no sepa usar un módulo vea cómo funciona (para después).
- [x] **Sincronización de accesorios con la espejo:** revisar que todo lo que rebaje o sume cantidad de accesorios en la principal se replique igual en la espejo (mismo comportamiento que equipos). → **Tarea 22** del plan (accesorios: ventas, anulaciones y prefacturas; se publica el 04/11/2026). Las reparaciones quedan aparte.
  - Revisar también la posibilidad de **sincronizar las reparaciones** que se registran en la principal, para poder **facturarlas en la espejo**.
- [ ] **Nota de crédito electrónica que anula la factura:**
  - Contexto: se factura con comprobante electrónico en las bases ESPEJO. En la base Seguridad (tabla Empresa) el campo Certificada y el campo del proveedor dicen si la empresa está certificada y con qué proveedor.
  - Proveedor 2 (BESTPHONE, TECNO_STYLE): ya genera nota de crédito desde el sistema. Proveedor 1 (GAPMOVILES): no; hoy se hace en la plataforma del proveedor.
  1. Programar la nota de crédito para el **proveedor 1** desde el sistema.
  2. Al generar la nota de crédito (cualquier proveedor), **anular la factura automáticamente**.
  - Decidido: la nota de crédito siempre es **total** (no hay parcial por ahora). El botón Anular NO se oculta por código: en las certificadas el usuario le quita el permiso; en las no certificadas Anular sigue igual (solo a nivel de sistema, sin enviar a DGII).
- [x] **Sesión vencida: el botón "Reintentar" no sirve.** → **Tarea 20** ✅ (front 66cccdb + API JwtService: vence a medianoche local; probada 04/11/2026). Causa (revisado en el código): los guards (`AuthenticationGuard`, `LoginGuard`) solo verifican que haya usuario en el token guardado, no que esté vencido. Si la primera petición del día falla con status 0 (red / API dormida), se va a la pantalla "Conexión lenta" sin borrar el token; "Reintentar" va a `/` → login → LoginGuard ve usuario → `home` → falla otra vez → bucle.
  - Propuesta (solo front): 1) un método `sesionVigente()` en LoginService que revise `expires` (o el `exp` del JWT); 2) los dos guards lo usan: si está vencida, `localStorage.clear()` y al login; 3) en el error status 0, si la sesión está vencida, tratarlo como 401 (ir al login, no a "Conexión lenta"); 4) "Reintentar" recarga la página anterior si la sesión es válida, o va al login si está vencida.
- [ ] **GAPMOVILES con el mismo esquema que las demás:** hoy su principal es GAPMOVILES_E y la espejo GAPMOVILES (al revés que BESTPHONE y TECNO_STYLE). Corregir a futuro para que todas estén bajo la misma condición.
- [x] **Registrar un accesorio inactivo:** elegirlo en la ventana de coincidencias y reactivarlo al sumar cantidad. → **Tarea 21** del plan.
- [x] **Proc_Equipo_Consulta_Vendidos:** recuperado filtro por teléfono, IMEI ignora fechas, unión factura por número + literal (sin duplicados) y "Cliente no encontrado" cuando la venta no tiene cliente. Probado en PRUEBAS 04/11/2026.
