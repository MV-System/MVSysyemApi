# Plan – Mejoras de interfaz (revisión con `/revisar-ui`)

Solo front (`C:\mvsystem\MVSystemFrontend`). Rama la que indique el usuario; `ventura` no se toca sin autorización.
**Una tarea a la vez; entregar y esperar aprobación antes de seguir.**

## Tarea UI-1 – Montos y fechas en formato de República Dominicana (todo el sistema)
Hoy no hay `LOCALE_ID`: `{{ x | currency }}` muestra `$1,500.00` (dólar) y `{{ f | date }}` muestra "Nov 11, 2026" (inglés).
- En `app.module.ts`: `registerLocaleData(localeEsDo)` (`import localeEsDo from '@angular/common/locales/es-DO'`) y en `providers`: `{ provide: LOCALE_ID, useValue: 'es-DO' }` y `{ provide: DEFAULT_CURRENCY_CODE, useValue: 'DOP' }`.
- No cambiar los pipes que ya indican formato (`currency:'RD$':'symbol':'1.2-2'`, `date:'dd/MM/yyyy'`, etc.).
- Revisar que nada lea de vuelta un texto ya formateado con pipe para calcular (buscar `parseFloat`/`Number(` sobre valores mostrados).
- Aceptación: (a) Consulta de facturas en escritorio muestra `RD$` y la fecha en español; (b) separador de miles `,` y decimal `.` como hoy; (c) facturas impresas/PDF, cotización, caja y utilidades se ven bien; (d) compila.

## Tarea UI-2 – Consulta de facturas: botones de la fila
Archivo: `src/app/MVSystem/pages/facturacion/facturacion-consulta/facturacion-lista/facturacion-lista.component.html`
1. **Anular (escritorio, línea ~108):** agregar `$event.stopPropagation()` al `(click)`, como ya tienen Nota de crédito e Imprimir. Hoy pulsar Anular también abre el detalle de la fila.
2. **Botones de solo icono (líneas ~108–116, escritorio):** reemplazar `data-bs-original-title` por `title` y agregar `aria-label` con el mismo texto ("Anular factura", "Emitir nota de crédito", "Imprimir nota de crédito"). En móvil (líneas ~196–207) ya tienen `title`: agregar `aria-label`.
3. **Campo IMEI al editar (línea ~163):** agregar `aria-label="Nuevo IMEI o serial"`, `autocomplete="off"` y `spellcheck="false"`. **No** poner `inputmode="numeric"`: hay seriales con letras.
- Aceptación: (a) pasar el mouse sobre cada icono muestra su nombre; (b) Anular ya no abre el detalle; (c) el campo IMEI acepta letras y números; (d) compila.

## Entrega de cada tarea
1. Archivos modificados. 2. Cómo probar. 3. Supuestos o dudas. 4. Confirmar la rama. 5. **No continuar** sin aprobación.
