# Plan – Código escrito a mano en el front (formato, impresión, estilos y textos)

Solo front (`C:\mvsystem\MVSystemFrontend`). Rama: la que indique el usuario (hoy `mejoras-ui`); `ventura` no se toca sin autorización.
Continúa la Tarea UI-1 (locale `es-DO` y `RD$` en todo el sistema). Hallazgos del 11/11/2026, solo lectura.
**Una tarea a la vez; entregar y esperar aprobación antes de seguir.**

## Hallazgos (resumen)
| # | Hallazgo | Cantidad | Prioridad |
|---|---|---|---|
| F-1 | Funciones de formato repetidas en cada componente: `formatNumber` (7 archivos, 44 usos), `formatCurrency` (6 archivos), `formatearMoneda` y 3 `Intl.NumberFormat` propios (cierre de caja, detalle de abonos, utilidades) | ~17 copias | Alta |
| F-2 | Fechas para mostrar armadas con `getDate()/getMonth()/padStart` (88 usos de `getDate/getMonth/getFullYear`, 36 `padStart`) en ~14 archivos; 5 `toLocaleDateString()` **sin locale** (salen distintas según la PC del usuario) | 88 / 5 | Alta / Media |
| F-3 | Impresión de etiquetas y cierre de caja armada con `document.write` (83 líneas en 3 archivos). Los datos (IMEI, descripción, cliente) entran al HTML **sin escapar** | 83 | Media |
| F-4 | Textos con `...` en vez de `…` (81 en plantillas, 5 en avisos) | 86 | Baja (rápida) |
| F-5 | Estilos en línea: 180 `style="…"`, 78 con color fijo. Concentrados en `caja-lista` (51), `facturacion-lista` (34), `cuentas-por-cobrar` (20), `equipo-registro` (13) | 180 | Baja |
| F-6 | `console.log` de depuración (144), algunos con montos y datos de pago (`DEBUG_PAGO_MIXTO`) | 144 | Baja |
| F-7 | `toFixed` en 2 plantillas para mostrar porcentajes (`anomalias-widget.html:113`, `top-modelos-widget.html:76`); los otros ~100 `toFixed` son cálculos (dentro de `Number()`/`parseFloat()`) y **no se tocan** | 2 | Baja |

Lo que ya está bien: casi todos los `| date` llevan formato explícito (`dd/MM/yyyy`); el formato de moneda de plantillas quedó unificado en `RD$` (UI-1 y Utilidades); los widgets del dashboard ya usan `es-DO`.

---

## Tarea F-1 – Un solo formateador de montos y números
- Crear `src/app/core/utils/formato.ts` con: `formatearMonto(valor, decimales = 2)` (`Intl.NumberFormat('es-DO', { style: 'currency', currency: 'DOP' })`), `formatearNumero(valor, decimales = 0)` y `formatearPorcentaje(valor, decimales = 1)`. Aceptan `null/undefined/NaN` y devuelven `—`.
- Reemplazar las copias locales por esas funciones, **sin cambiar el texto que ve el usuario** (mismos decimales que hoy en cada pantalla). Archivos: widgets del dashboard (`anomalias`, `top-modelos`, `vendedores`, `top-equipos-lentos`), `dashboard-analytics.service.ts`, `cierre-caja-reporte`, `detalle-abonos`, `utilidades`, `equipo-consulta-vendido`.
- No tocar `money.directive.ts` (formatea mientras se escribe) ni los `toFixed` de cálculos.
- Aceptación: (a) una sola definición de cada función; (b) los montos se ven igual que antes; (c) una celda vacía muestra `—` y no `NaN`; (d) compila y pasan las pruebas.

## Tarea F-2 – Fechas
1. **Rápida:** los 5 `toLocaleDateString()` sin locale → `'es-DO'` (`accesorio-movimientos:144`, `accesorio-vendido:163`, `equipo-consulta-vendido:276`, `facturacion-consulta:174`, `utilidades:41`).
2. Revisar los ~88 usos de `getDate/getMonth/getFullYear`. Separar: (a) **fechas para el API** (formato que espera el servidor, p. ej. `MM/dd/yyyy`): dejarlas en UN helper compartido (`helpers/helper.ts` ya tiene `transformDate`/`currentDate`; usarlo en lugar de copias en `accesorio-movimientos:183`, `accesorio-vendido:155`, `cotizacion-consulta:181`, `cotizacion:339`, `top-modelos:244`, `equipo-consulta-vendido:268`); (b) **fechas para mostrar**: pasarlas a `DatePipe`/`formatDate` con `es-DO`.
- No cambiar el formato que recibe el API.
- Aceptación: (a) ninguna fecha depende de la configuración regional de la PC; (b) los filtros por fecha devuelven lo mismo que hoy; (c) compila y pasan las pruebas.

## Tarea F-3 – Impresión armada a mano
Archivos: `equipo-disponible.component.ts` (etiqueta del equipo), `accesorio-list.component.ts` (etiqueta del accesorio), `cierre-caja-reporte.component.ts` (cierre de caja).
- Función `escaparHtml(texto)` en `core/utils/` y usarla en todo valor de datos que entre al HTML de impresión (IMEI, descripción, modelo, cliente, nota, usuario).
- Los montos de la impresión usan el formateador de F-1.
- No rediseñar las etiquetas (50×30 mm) ni el cierre de caja.
- Aceptación: (a) un nombre con `<` o `&` se imprime como texto; (b) las etiquetas y el cierre salen idénticos para datos normales; (c) compila.

## Tarea F-4 – Textos `...` → `…`
- Reemplazar `...` por `…` solo en textos visibles de estados y placeholders (plantillas y avisos), no en código, comentarios ni rutas. Revisar el resultado archivo por archivo (script con lista de cambios).
- Aceptación: ningún "Cargando...", "Procesando..." ni placeholder con tres puntos; compila.

## Tarea F-5 – Estilos en línea
- Mover a clases CSS del componente, en este orden: `caja-lista` (51), `facturacion-lista` (34; el desglose de pagos mixtos), `cuentas-por-cobrar` (20). Colores fijos a variables CSS. **Sin cambiar el aspecto.**
- Aceptación: la pantalla se ve igual (comparar capturas antes y después); menos de 10 `style=` por archivo.

## Tarea F-6 – Depuración
- Quitar los `console.log` que imprimen datos de pagos, montos o clientes (`DEBUG_PAGO_MIXTO` y similares); el resto, protegerlos con una bandera de desarrollo o quitarlos.
- Aceptación: en producción la consola queda sin datos de negocio; compila.

## Tarea F-7 – Porcentajes en plantilla
- `anomalias-widget.component.html:113` y `top-modelos-widget.component.html:76`: `| number:'1.1-1'` en lugar de `toFixed`.

---

## Orden recomendado
1. F-2 punto 1 y F-4 (rápidas, bajo riesgo). 2. F-1 (base de lo demás). 3. F-3 (por el escape de HTML). 4. F-2 punto 2. 5. F-5, F-6, F-7.

## Entrega de cada tarea
1. Archivos modificados. 2. Cómo probar. 3. Supuestos o dudas. 4. **No continuar** sin aprobación.
