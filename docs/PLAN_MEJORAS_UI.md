# Plan – Mejoras de interfaz (revisión con `/revisar-ui`)

Solo front (`C:\mvsystem\MVSystemFrontend`). Rama la que indique el usuario; `ventura` no se toca sin autorización.
**Una tarea a la vez; entregar y esperar aprobación antes de seguir.**

## ✅ Tarea UI-1 – Montos y fechas en formato de República Dominicana (todo el sistema) — APLICADA
Estado verificado el 17/11/2026: `app.module.ts` líneas 22, 54 y 55 (`registerLocaleData`, `LOCALE_ID`, `DEFAULT_CURRENCY_CODE`). Cambios visibles: `date` sin formato muestra "11 nov. 2026"; `shortDate` en Cotización pasa a día/mes/año; `short` en Caja muestra "a. m./p. m.".
Hoy no hay `LOCALE_ID`: `{{ x | currency }}` muestra `$1,500.00` (dólar) y `{{ f | date }}` muestra "Nov 11, 2026" (inglés).
- En `app.module.ts`: `registerLocaleData(localeEsDo)` (`import localeEsDo from '@angular/common/locales/es-DO'`) y en `providers`: `{ provide: LOCALE_ID, useValue: 'es-DO' }` y `{ provide: DEFAULT_CURRENCY_CODE, useValue: 'DOP' }`.
- No cambiar los pipes que ya indican formato (`currency:'RD$':'symbol':'1.2-2'`, `date:'dd/MM/yyyy'`, etc.).
- Revisar que nada lea de vuelta un texto ya formateado con pipe para calcular (buscar `parseFloat`/`Number(` sobre valores mostrados).
- Aceptación: (a) Consulta de facturas en escritorio muestra `RD$` y la fecha en español; (b) separador de miles `,` y decimal `.` como hoy; (c) facturas impresas/PDF, cotización, caja y utilidades se ven bien; (d) compila.

## ✅ Tarea UI-2 – Consulta de facturas: botones de la fila — APLICADA
Estado verificado el 17/11/2026 en `facturacion-lista.component.html`: `stopPropagation`, `title` y `aria-label` en los botones (escritorio y móvil) y el campo IMEI con `aria-label`, `autocomplete="off"` y `spellcheck="false"`.
Archivo: `src/app/MVSystem/pages/facturacion/facturacion-consulta/facturacion-lista/facturacion-lista.component.html`
1. **Anular (escritorio, línea ~108):** agregar `$event.stopPropagation()` al `(click)`, como ya tienen Nota de crédito e Imprimir. Hoy pulsar Anular también abre el detalle de la fila.
2. **Botones de solo icono (líneas ~108–116, escritorio):** reemplazar `data-bs-original-title` por `title` y agregar `aria-label` con el mismo texto ("Anular factura", "Emitir nota de crédito", "Imprimir nota de crédito"). En móvil (líneas ~196–207) ya tienen `title`: agregar `aria-label`.
3. **Campo IMEI al editar (línea ~163):** agregar `aria-label="Nuevo IMEI o serial"`, `autocomplete="off"` y `spellcheck="false"`. **No** poner `inputmode="numeric"`: hay seriales con letras.
- Aceptación: (a) pasar el mouse sobre cada icono muestra su nombre; (b) Anular ya no abre el detalle; (c) el campo IMEI acepta letras y números; (d) compila.

## Revisión del 17/11/2026 (sistema publicado, empresa MVSYSTEM)
Agentes: `explorador` para ubicar el código, `programador` para el cambio. Rutas relativas a `src/app/MVSystem/pages/`.

## Tarea UI-3 – Paginación en español — PENDIENTE
Seis pantallas muestran "Previous" y "Next" porque su `<pagination-controls>` no tiene etiquetas:
- `Clientes/Cuentas-por-cobrar/cuentas-por-cobrar.component.html` (~356)
- `Clientes/Clientes-consulta/clientes-consulta.component.html` (~174 y ~208)
- `Equipo/Equipo-consulta/Equipo-consulta-disponible/equipo-disponible.component.html` (~373)
- `Equipo/Equipo-consulta/equipos-consulta-transferencias/equipos-consulta-transferencias.component.html` (~138)
- `Equipo/Equipo-consulta/Equipo-consulta-vendido/equipo-consulta-vendido.component.html` (~510)
- `Equipo/equipo-consulta-recibido/equipo-consulta-recibido.component.html` (~174)

Agregar `previousLabel="Anterior" nextLabel="Siguiente"` y, si no lo tiene, `[responsive]="true"`, igual que en Consulta de facturas. Antes de cerrar, buscar en todo el front otro `<pagination-controls>` sin `previousLabel`.
- Aceptación: (a) ninguna pantalla muestra "Previous" ni "Next"; (b) la paginación sigue funcionando; (c) compila.

## Tarea UI-4 – Equipos disponibles: filtros — PENDIENTE
Archivo: `Equipo/Equipo-consulta/Equipo-consulta-disponible/equipo-disponible.component.html` y su `.ts`.
1. **Combo de sucursal vacío:** el formulario empieza con `almacen: ['']` y ninguna opción tiene valor `''`, por eso el combo se ve en blanco, aunque la consulta usa la sucursal del usuario (`loginService.getUserIdSucursal`, línea ~681 del `.ts`). Al cargar `almacenListado` (~745), poner en el formulario la sucursal del usuario **sin disparar** la búsqueda otra vez (`setValue(..., { emitEvent: false })`). Así el combo muestra la sucursal real en escritorio y en móvil. No cambiar el valor que se envía al API.
2. **Etiqueta en móvil (~165):** el combo de sucursal en móvil solo tiene icono. Agregar la misma etiqueta "Sucursal:" que ya usa Suplidor (`mobile-filter-label`).
3. **Estilo de "Modelo o marca" en móvil:** el `ng-select` (~195) se ve con otra altura y otras esquinas que los `select` de al lado. Igualar el alto, el borde y el radio con `.mobile-select`, solo en el CSS de este componente.
- Aceptación: (a) al entrar se ve el nombre de la sucursal en el combo; (b) la cantidad de equipos es la misma que antes del cambio; (c) en móvil los cuatro filtros se ven iguales y la sucursal tiene etiqueta; (d) compila.

## Tarea UI-5 – Consulta de clientes: aviso al entrar — PENDIENTE
Archivo: `Clientes/Clientes-consulta/clientes-consulta.component.ts` (~117).
- Quitar el `toastr.success("Se cargaron N cliente(s)")` al cargar la lista: la pantalla ya muestra "N encontrados". Dejar el `toastr.error` del fallo.
- Botones ver y editar de cada cliente (solo icono): agregar `title` y `aria-label` ("Ver cliente", "Editar cliente").
- Aceptación: (a) al entrar no sale el aviso verde; (b) si falla la carga sí sale el error; (c) al pasar el mouse por los iconos se ve su nombre; (d) compila.

## Pendientes de la revisión (no hacer todavía)
- Títulos de pantalla con tres estilos distintos; sin título: Caja, Utilidades, Inventario, Asignaciones y Consulta de reparaciones.
- Facturación: etiquetas al lado en "Datos de facturación" y arriba en "Filtros"; el campo Cliente sin texto de ejemplo.
- Botones de solo icono sin nombre: Consulta de accesorios (10), Caja (2), Consulta de reparaciones (2).
- Campos con el texto visible pero sin etiqueta asociada (`<label for>`), en los registros: Clientes, Suplidores, Vendedores, Accesorios, Recepción, Cotización, Avisos, Cambiar estatus y Equipo.
- Las pantallas abren escribiendo la URL aunque el usuario no tenga el permiso (el API responde 403). Evaluar un guard de ruta.

## Entrega de cada tarea
1. Archivos modificados. 2. Cómo probar. 3. Supuestos o dudas. 4. Confirmar la rama. 5. **No continuar** sin aprobación.
