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
