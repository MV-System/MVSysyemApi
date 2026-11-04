# PLAN – Mejoras del Módulo de Laptops (rama `claude`)

> ## ⛔ PRODUCCIÓN Y RAMA `ventura`: NO TOCAR SIN AUTORIZACIÓN DEL USUARIO
> Todo se hace **solo en la rama `claude`** y **solo en la base de pruebas**. Prohibido ejecutar scripts en las bases de producción y hacer commit/merge/push en `ventura`. Ver `docs/PASE_A_PRODUCCION.md`.

> **Instrucción para el agente:** ejecuta **UNA SOLA TAREA** por sesión (la que se te indique: "Ejecuta la Tarea N").
> No adelantes tareas. Al terminar entrega el resumen de la sección **"Entrega de cada tarea"** y detente.
> Antes de empezar lee también `docs/PLAN_MODULO_LAPTOPS.md` (reglas R1–R13 y convenciones), que siguen vigentes.

---

## 0. Reglas de trabajo (OBLIGATORIAS)

| # | Regla |
|---|-------|
| G1 | Trabajar **solo en la rama `claude`**. Verificar con `git branch --show-current` antes de tocar nada. |
| G2 | **Nunca** hacer merge, cherry-pick ni commit en `ventura`. `ventura` es producción y no lleva laptops. |
| G3 | Los SP del módulo están en la raíz del repo API (`CREATE_Equipo_*.sql`, `CREATE_Laptops_*.sql`, `UPDATE_Equipo_*.sql`). Partir **siempre** de la versión más reciente del SP (la del último `UPDATE_*.sql` si existe; ej. `Equipo_Cambio_Pieza_Registrar` → `UPDATE_Equipo_Cambio_Pieza_Registrar_ID_Explicito.sql`). No perder el parámetro `@ID_Cambio` ni la lógica de réplica con ID explícito. |
| G4 | Los cambios a SP se entregan en un **script nuevo** `UPDATE_<SP>_<Mejora>.sql` (idempotente, `CREATE OR ALTER`, con encabezado). No editar los `CREATE_*.sql` originales. |
| G5 | Cambios de esquema: solo **agregar** (tablas, columnas NULL, funciones). Nunca quitar ni renombrar columnas o parámetros existentes. |
| G6 | Toda escritura se replica a la espejo con `_sync.Ejecutar(...)` (R12). Si un SP **inserta** filas cuyo ID usan otras pantallas, el ID se replica explícito (mismo patrón que `@ID_Cambio` en la mejora 2 y `@ID_Modelo` en `Modelos_Insert`). |
| G7 | Pantallas simples: menos campos, valores calculados y mostrados, el usuario solo confirma. Ningún campo nuevo obligatorio si el sistema lo puede deducir. |
| G8 | Al terminar cada tarea marcar `[x]` en `docs/PENDIENTES_LAPTOPS.md`. |
| G9 | Los permisos nuevos `LAPTOP_CAMBIO_ANULAR` (Tarea 6) y `LAPTOP_CORREGIR_CONFIG` (Tarea 2) ya tienen su script: `CREATE_Laptops_Permisos_Mejoras.sql` (BD SEGURIDAD, idempotente). No crear otro; si hace falta otro permiso, agregarlo a ese mismo script. |

## 1. Contexto rápido

- `Equipo_Configuracion`: **una fila por laptop** (Imei único) con `Ram_GB`, `Ram_Tipo` (DDR3/DDR4/DDR5), `Almacenamiento_GB`, `Almacenamiento_Tipo` (SSD SATA / SSD NVMe / HDD). No es un catálogo.
- Piezas = Accesorios de la marca/grupo **Componentes**. Modelos: `RAM DDR3 SODIMM`, `RAM DDR4 SODIMM`, `RAM DDR5 SODIMM`, `SSD SATA 2.5`, `SSD M.2 SATA`, `SSD M.2 NVMe`, `HDD 2.5`. La capacidad está en `Descripcion_Accesorio` (ej. `8GB 3200`, `256GB`, `1TB`).
- `Equipo_Cambio_Pieza`: historial de cambios + custodia (`PENDIENTE`, `RECIBIDA`, `ENTREGADA_CLIENTE`, `NO_APLICA`).
- Pantalla de cambio: front `src/app/MVSystem/pages/Equipo/laptop-cambio-pieza`. Lista de piezas: SP `Equipo_Cambio_Pieza_Accesorios` (hoy lista **todos** los componentes del tipo, sin mirar la laptop).
- Una laptop puede tener **varios módulos**: 2×8 de RAM, o SSD + HDD. Desde la Tarea 2 la configuración se guarda por módulo; `Equipo_Configuracion` queda como resumen (totales).

## 2. Tareas (en este orden)

### Tarea 1 – Especificación de cada componente (base para las demás)
**Problema:** el sistema no sabe la capacidad ni el tipo de una pieza; hoy el usuario los escribe a mano.
- Repo API. Script `CREATE_Componentes_Especificacion.sql`:
  - **Primero asegurar los 7 modelos** (idempotente, en cada base): si no existen la marca `Componentes`, el grupo `Componentes` o alguno de los 7 modelos, crearlos (marca `Componentes`, grupo `Componentes`). Usar la misma lógica de `Modelos_Insert`/`Marcas_Insert` con **ID explícito en la espejo** (patrón ya existente en esos SP). Al final, si falta alguno, `THROW` con la lista de los que faltan.
  - Tabla `dbo.Componente_Modelo_Tipo (ID_Modelo INT PK FK Modelos, Tipo_Pieza VARCHAR(20) 'RAM'|'ALMACENAMIENTO', Tipo VARCHAR(30))` sembrada con los 7 modelos de Componentes:
    `RAM DDR3 SODIMM→RAM/DDR3`, `RAM DDR4 SODIMM→RAM/DDR4`, `RAM DDR5 SODIMM→RAM/DDR5`, `SSD SATA 2.5→ALMACENAMIENTO/SSD SATA`, `SSD M.2 SATA→ALMACENAMIENTO/SSD SATA`, `SSD M.2 NVMe→ALMACENAMIENTO/SSD NVMe`, `HDD 2.5→ALMACENAMIENTO/HDD` (buscar `ID_Modelo` por descripción + grupo Componentes; idempotente).
  - Función `dbo.fn_Componente_Capacidad_GB(@Descripcion VARCHAR(200)) RETURNS INT`: lee el primer número seguido de `GB` o `TB` (TB × 1024). Sin coincidencia → NULL. Debe aceptar `8GB`, `8 GB`, `8gb 3200`, `1TB`, `1 TB`.
  - Vista o SP `Componente_Especificacion_Consultar @ID_Accesorio` → `Tipo_Pieza, Tipo, Capacidad_GB`.
- Front: en **Accesorios → Registro**, si la marca es Componentes, validar que la descripción empiece con la capacidad (`8GB…`, `1TB…`); mensaje claro si no.
- Aceptación: probar la función con `'8GB 3200'→8`, `'16 GB'→16`, `'1TB'→1024`, `'Memoria'→NULL`.

### Tarea 2 – Módulos por laptop (varios slots de RAM y varios discos)
**Problema:** hoy solo se guarda el total (16 GB) y un solo disco; no se sabe si son 1×16 o 2×8, ni si hay SSD + HDD.
- Repo API. Script `CREATE_Equipo_Configuracion_Modulo.sql`:
  - Tabla `dbo.Equipo_Configuracion_Modulo`:
    `ID_Modulo INT IDENTITY PK, Imei VARCHAR(30) NOT NULL, Tipo_Pieza VARCHAR(20) NOT NULL ('RAM'|'ALMACENAMIENTO'), Capacidad_GB INT NOT NULL, ID_Modelo_Componente INT NOT NULL (FK Modelos: uno de los 7 modelos de Componentes), Tipo VARCHAR(30) NOT NULL (se toma de `Componente_Modelo_Tipo` según el modelo), ID_Accesorio BIGINT NULL (si entró por cambio de pieza), Estado CHAR(1) NOT NULL DEFAULT 'A' ('A' instalado | 'R' retirado), ID_Cambio_Alta INT NULL, ID_Cambio_Baja INT NULL, Usuario VARCHAR(60) NOT NULL, Fecha_Registro DATETIME NOT NULL`. Índice `(Imei, Estado)`.
  - **Migración** (idempotente): por cada fila de `Equipo_Configuracion` sin módulos, y solo después de la Tarea 1 (si falta un modelo de componente, detenerse con mensaje claro, sin migrar a medias), crear 1 módulo RAM (`Ram_GB`, `Ram_Tipo`) y 1 de ALMACENAMIENTO (`Almacenamiento_GB`, `Almacenamiento_Tipo`) si tienen valor. Modelo de componente por tipo: `DDR3/DDR4/DDR5` → `RAM DDRx SODIMM`; `SSD NVMe` → `SSD M.2 NVMe`; `HDD` → `HDD 2.5`; `SSD SATA` → **`SSD SATA 2.5`** (por defecto: el registro viejo no distingue 2.5 de M.2; dejarlo anotado en el resumen de la tarea).
  - `Equipo_Configuracion` **se mantiene** (G5) como resumen: `Ram_GB` = suma de módulos RAM activos, `Almacenamiento_GB` = suma de discos activos, tipos = el del primer módulo. Crear SP interno `Equipo_Configuracion_Recalcular @Imei` que la actualiza desde los módulos; llamarlo desde todo SP que toque módulos.
  - `fn_Equipo_Configuracion_Texto` (nuevo script UPDATE) arma el texto desde los módulos: RAM = total + tipo (`16GB DDR4`); discos = cada uno (`256GB SSD SATA + 1TB HDD`). Si no hay módulos, comportamiento actual.
  - SP `Equipo_Configuracion_Guardar` acepta una lista de módulos (JSON `@Modulos NVARCHAR(MAX)`: `[{"tipoPieza":"RAM","capacidadGB":8,"idModeloComponente":12}, …]`); los parámetros actuales siguen funcionando (si no viene `@Modulos`, crear 1 módulo por RAM y 1 por disco). Solo en el registro de la laptop (si ya tiene cambios de pieza, rechazar la edición de módulos; para eso está la corrección de abajo).
  - SP `Equipo_Configuracion_Modulos_Consultar @Imei` → módulos activos.
  - Réplica (G6): los `ID_Modulo` deben ser iguales en la espejo (se usan en la Tarea 3). Aplicar el patrón de ID explícito.
- **Corrección de módulos** (para errores de registro o de migración, ej. un `SSD SATA 2.5` que en realidad es `M.2`):
  - SP `Equipo_Configuracion_Modulo_Corregir @ID_Modulo, @ID_Modelo_Componente, @Capacidad_GB, @Motivo, @Usuario`: solo laptop disponible; cambia modelo y/o capacidad del módulo activo **sin mover inventario ni costo**; recalcula `Equipo_Configuracion`.
  - Log en tabla nueva `dbo.Equipo_Configuracion_Correccion (ID_Correccion, Imei, ID_Modulo, Antes VARCHAR(100), Despues VARCHAR(100), Motivo, Usuario, Fecha)`; se muestra en el historial de la laptop.
  - Front: botón **Corregir** junto a cada módulo en el historial de la laptop, con motivo obligatorio. Permiso nuevo `LAPTOP_CORREGIR_CONFIG`.
- Front (registro de equipo, `Equipo-registro`): reemplazar "RAM (GB)" y "Almacenamiento (GB)" por:
  - **Memorias instaladas:** tipo (DDR3/DDR4/DDR5, uno para todas; cada uno corresponde a un modelo de componente) + capacidades como chips `[8] [8] [+ agregar]`. Total mostrado debajo (`Total: 16GB`).
  - **Discos instalados:** una fila por disco (capacidad + **modelo de componente**: SSD SATA 2.5 / SSD M.2 SATA / SSD M.2 NVMe / HDD 2.5) con `[+ agregar disco]`. Así cada módulo sabe exactamente qué pieza es.
  - Por defecto 1 memoria y 1 disco (el caso común no cambia). Mínimo 1 memoria y 1 disco.
- Aceptación: registrar laptop 2×8 DDR4 + 256 SSD + 1TB HDD → `Equipos disponibles` muestra `16GB DDR4 / 256GB SSD SATA + 1TB HDD`; laptops existentes siguen mostrándose igual después de la migración.

### Tarea 3 – Cambio de pieza por módulos (reemplaza "capacidad manual" y "pieza retirada propuesta")
**Problema:** el usuario escribe capacidad/tipo a mano y elige la retirada de una lista de accesorios que no tiene que ver con la laptop.
- SP `Equipo_Cambio_Pieza_Registrar` (`UPDATE_Equipo_Cambio_Pieza_Registrar_Modulos.sql`, partiendo de la versión con `@ID_Cambio` – G3):
  - Nuevo parámetro `@ID_Modulo_Retirado INT = NULL` (el módulo de la laptop que sale). `@ID_Accesorio_Retirado`, `@Nueva_Capacidad_GB` y `@Nuevo_Tipo` pasan a opcionales (compatibilidad).
  - Validar: el módulo pertenece a la laptop, está activo y es del `@Tipo_Pieza`; la pieza instalada es del mismo tipo de pieza y, para RAM, del mismo `Tipo` que los demás módulos RAM activos (no mezclar DDR3/DDR4).
  - Accesorio de la pieza retirada (para custodia, inventario y costo) — **siempre queda resuelto en este SP**, nunca NULL:
    1. el `ID_Accesorio` del módulo, si lo tiene;
    2. si no, el componente del almacén del cambio con el mismo `ID_Modelo_Componente` y la misma capacidad (Tarea 1);
    3. si no existe, **crearlo** dentro de la misma transacción en ese almacén: modelo = `ID_Modelo_Componente` del módulo, descripción = `<capacidad>GB` (o `<n>TB` si ≥ 1024 y múltiplo de 1024), `Cantidad = 0`.
       - Seguir la **misma lógica de `Accesorio_Insert`**: el sistema crea el accesorio **una vez por cada almacén**, con la cantidad solo en el almacén seleccionado. Aquí se crea en **todos los almacenes con `Cantidad = 0`** (también el del cambio); la existencia la suma después la recepción (Tarea 4 / Recibir) solo en el almacén del cambio. Pedir `sp_helptext 'Accesorio_Insert'` antes de escribirlo y reutilizar sus valores por defecto (mínimo/máximo, código de barras, `Disponible`, etc.). Réplica: todos los `ID_Accesorio` creados con el mismo ID en la espejo.
       - Si existe un accesorio con mismo modelo y capacidad en cualquier almacén: copiar su **costo y precios** y crearlo **activo**.
       - Si no existe: costo y precios **0** y crearlo **inactivo** (`Estado='I'`), para que nunca aparezca en ventas a precio 0. **No se le piden precios al técnico** en el cambio. Al **recibir** la pieza, si el accesorio no tiene precios, la ventana de recepción pide **costo, precio por mayor y precio al detalle** (obligatorios, mayor ≤ detalle, costo ≤ mayor), solo a usuarios con el permiso `LAPTOP_CUSTODIA_RECIBIR`; al guardar se actualizan en todos los almacenes y se activa el accesorio. El costo de la laptop **no** se recalcula en la recepción (la laptop puede estar ya vendida); ese caso lo cubre la Tarea 5.
    - Réplica (G6): el `ID_Accesorio` creado se envía explícito a la espejo.
    - Costo retirado = costo de ese accesorio. Si es 0, el resumen del front muestra: *"Esta pieza no tiene costo registrado; no se descuenta de la laptop"*. La regla definitiva del costo de la pieza usada la define la Tarea 5 y reemplaza este punto. ⚠️ Mientras la Tarea 5 no exista, una retirada con costo 0 deja el costo de la laptop sin descontar (utilidad menor): la Tarea 5 es **obligatoria antes de pasar el módulo a producción**.
  - Módulos: el retirado pasa a `Estado='R'` con `ID_Cambio_Baja`; se crea un módulo nuevo `Estado='A'` con capacidad/tipo de la pieza instalada, `ID_Accesorio` y `ID_Cambio_Alta`. Luego `Equipo_Configuracion_Recalcular`.
  - Guardar en `Equipo_Cambio_Pieza` columnas nuevas NULL `ID_Modulo_Retirado`, `ID_Modulo_Instalado`.
  - Réplica: el `ID_Modulo` nuevo se envía explícito a la espejo junto con `@ID_Cambio`.
- Front (`laptop-cambio-pieza`):
  - **Pieza que se retira:** lista de **los módulos activos de esa laptop** del tipo elegido (`RAM 8GB DDR4`, `RAM 8GB DDR4`) + opción **"Ninguna (slot libre)"**. Si hay uno solo, preseleccionado.
  - **Pieza que se instala:** solo componentes con existencia del mismo tipo de pieza. RAM: obligatorio el mismo DDR. Discos: se permite cualquier modelo de disco (la ranura de la laptop no se conoce); si el modelo es distinto al del módulo retirado, mostrar aviso: *"Verifique que la laptop tenga ranura para <modelo>"*.
  - Quitar los campos "Capacidad nueva (GB)" y "Tipo". El **Resumen** muestra el antes/después calculado: `RAM: 8+8 = 16GB → 8+16 = 24GB`.
  - Resultado: la pantalla pide serial, tipo de pieza, pieza que sale, pieza que entra, destino y técnico.
- Aceptación:
  - 1×16: retirar 16, instalar 32 → `[32]` = 32GB.
  - 2×8: retirar un 8, instalar 16 → `[8][16]` = 24GB; el otro 8 no cambia.
  - 8 + slot libre: "Ninguna", instalar 8 → `[8][8]` = 16GB.
  - Instalar DDR3 en laptop DDR4 → error claro.

### Tarea 4 – Recepción sin error para cambios viejos
**Contexto:** desde la Tarea 3 todo cambio nuevo tiene accesorio retirado. Quedan los cambios **anteriores** a la Tarea 3 cuyo accesorio retirado está en otro almacén (hoy `Equipo_Cambio_Pieza_Recibir` da error 50105).
- SP `Equipo_Cambio_Pieza_Recibir` (`UPDATE_Equipo_Cambio_Pieza_Recibir_AutoAlta.sql`): si no encuentra el accesorio en el almacén del cambio, **crearlo** copiando modelo, descripción, precios y costo del accesorio retirado (que sí existe en otro almacén), con `Cantidad = 0`, y luego sumar 1. Si `ID_Accesorio_Retirado` es NULL, mantener el error 50104 (no debería ocurrir).
- Réplica (G6): el `ID_Accesorio` creado debe ser el mismo en la espejo (patrón `IDENTITY_INSERT`).
- Aceptación: recibir una pieza cuyo accesorio solo existe en otro almacén → se crea en el almacén del cambio y queda con existencia 1.

### Tarea 5 – Costo de la pieza usada (Mejora 3)
**Problema:** la pieza retirada entra al inventario y se descuenta de la laptop con el costo de una **nueva**.
- ⚠️ **Antes de programar, preguntar al usuario** cómo se calcula el costo de la pieza usada. Propuesta por defecto: accesorio separado "usado" (`<descripción> (Usado)`) con costo = `Laptops:PorcentajeCostoUsado` (appsettings, default 50%) del costo de la nueva.
- Con la regla aprobada:
  - `Registrar`: `Costo_Retirado` = costo de la variante usada (R4/R5 siguen igual con este costo).
  - `Recibir`: la pieza entra a la variante **usada** (crearla si no existe, como en Tarea 4).
- Aceptación: retirar RAM 8GB nueva costo 1,000 → se descuenta 500 a la laptop y entra 1 "8GB 3200 (Usado)" con costo 500.

### Tarea 6 – Anular un cambio de pieza (Mejora 4)
- SP nuevo `Equipo_Cambio_Pieza_Anular @ID_Cambio, @Usuario, @Motivo` (transaccional):
  - Solo el **último** cambio de esa laptop, laptop disponible y sin prefactura pendiente.
  - Solo si la retirada está `PENDIENTE` o `NO_APLICA` (si ya se recibió o entregó, primero revertir eso → mensaje claro).
  - Revierte: +1 al accesorio instalado, `Costo_Equipo` = `Costo_Equipo_Anterior`, módulos (el instalado pasa a `'R'`, el retirado vuelve a `'A'` usando `ID_Modulo_Instalado` / `ID_Modulo_Retirado`), `Equipo_Configuracion_Recalcular`, foto en `Equipos_Historico`, estado `ANULADO` + usuario/fecha/motivo (columnas nuevas NULL).
- API `POST api/Laptops/CambioPieza/Anular` + botón **Anular** en el historial (solo en el último cambio, con motivo obligatorio). Permiso nuevo `LAPTOP_CAMBIO_ANULAR`.
- Aceptación: registrar y anular → stock, costo y configuración vuelven exactamente a como estaban; la pieza ya no aparece en custodia.

### Tarea 7 – Kardex por accesorio y reporte general de consumo
- SP `Accesorio_Kardex_Consultar @ID_Accesorio, @Fecha_Inicio, @Fecha_Final`: une en una lista ordenada por fecha:
  - Salidas por **factura** (detalle de factura con `ID_Tipo = 2` del accesorio; excluir facturas anuladas),
  - Salidas por **cambio de pieza** (pieza instalada, excluir `ANULADO`),
  - Entradas por **pieza retirada recibida** (`RECIBIDA`),
  - Entradas/ajustes existentes en `Accesorios_Historico` si aplica (revisar qué registra antes de incluirlo; preguntar si hay duda).
  - Columnas: Fecha, Tipo de movimiento, Documento (factura o serial de laptop), Entrada, Salida, Usuario. Saldo acumulado en el front.
- Front: pantalla `accesorios/kardex` (o botón "Movimientos" en la consulta de accesorios), filtros accesorio + fechas, totales por tipo.
- **Reporte general de consumo de componentes** (en la misma tarea): SP `Componentes_Movimientos_Consultar @Fecha_Inicio, @Fecha_Final, @ID_Almacen NULL, @Tecnico NULL, @Tipo_Pieza NULL, @Motivo NULL` → todos los movimientos de componentes (mismas fuentes que el kardex) con accesorio, motivo, documento, técnico/usuario, almacén y cantidad. Front: pantalla con esos filtros, totales por motivo y por accesorio, y exportar a Excel (el proyecto ya usa `xlsx`). Responde "qué memorias salieron, por qué y quién".
- Aceptación: con datos de prueba, la diferencia de existencia entre dos fechas = entradas − salidas del kardex; el reporte general suma lo mismo que los kardex de cada accesorio.

### Tarea 8 – Comprobante de custodia
- Al confirmar un cambio con estado `PENDIENTE`, ofrecer imprimir un ticket corto: fecha, laptop (serial y modelo), pieza retirada, técnico, "Debe entregarse antes de: fecha + HorasMaxCustodia", firma del técnico.
- Reutilizar el mecanismo de impresión que ya usa el proyecto (Rotativa / plantillas existentes).

### Tarea 9 – Fechas consistentes (Mejora 7)
- Hoy se mezclan `GETDATE()` y `DATEADD(HOUR,-4,GETDATE())` (ej. `Fecha_Recepcion` usa `GETDATE()`, `Equipos_Historico` usa −4h).
- Revisar cómo guarda la fecha el resto del sistema en el servidor de producción y **preguntar al usuario** cuál es la correcta. Unificar todos los SP del módulo a esa forma. Las horas de custodia (`DATEDIFF`) deben comparar fechas del mismo tipo.

### Tarea 10 – Botón "Actualizar lista" en la pantalla de cambio de pieza
**Contexto:** si la pieza a instalar no existe o no tiene existencia, no sale en la lista. El técnico avisa al encargado, que la registra en Accesorios (con su permiso, en otra pestaña). El técnico necesita ver la pieza nueva **sin salir de la pantalla ni perder lo que ya llenó**.
- Solo front (`laptop-cambio-pieza`). No cambia API, SP ni permisos.
- Agregar un botón pequeño de **actualizar** (icono `bi-arrow-clockwise`, con tooltip "Actualizar lista") junto a la lista **"Pieza que se instala"**.
- Al pulsarlo, volver a cargar esa lista con los mismos filtros (almacén de la laptop y tipo de pieza). Mientras carga, botón deshabilitado con spinner.
- **No borrar nada del formulario.** Si la pieza que estaba elegida sigue en la lista, mantenerla seleccionada; si ya no está (se quedó sin existencia), limpiar solo ese campo y avisar con un toast.
- Deshabilitado si todavía no se buscó una laptop o no se eligió el tipo de pieza.
- Aceptación: con el formulario a medio llenar, registrar en otra pestaña una pieza con existencia en ese almacén → pulsar actualizar → la pieza aparece y el resto del formulario sigue igual.

### Tarea 11 – Quitar la variante "(Usado)": la pieza retirada vuelve al mismo accesorio
**Decisión del usuario:** las piezas retiradas se reutilizan y el precio de venta lo acuerdan vendedor y encargado al facturar. No hace falta separar nuevo/usado. **Reemplaza la regla de la Tarea 5** (costo al 50% y accesorio "(Usado)").
- `Accesorio_Componente_Asegurar`: dejar de usar `@Usado` (ignorarlo si llega). Busca el accesorio por modelo de componente + capacidad **sin "(Usado)"**; si no existe en ningún almacén, se crea como hoy (todos los almacenes, cantidad 0, inactivo si no hay precios).
- `Equipo_Cambio_Pieza_Registrar`: `Costo_Retirado` = **costo del accesorio en catálogo** (sin porcentaje). Ya no crear ni buscar "(Usado)".
- `Equipo_Cambio_Pieza_Recibir`: suma +1 a **ese mismo accesorio** en el almacén del cambio.
- **Regla que no cambia:** el stock **solo sube cuando el encargado recibe la pieza** en la pantalla de Custodia (`Recibir`). Al registrar el cambio, la pieza queda `PENDIENTE` y el accesorio no se toca (si hay que crearlo, se crea con cantidad 0).
- API/appsettings: se eliminó `Laptops:PorcentajeCostoUsado` y su uso (decisión del usuario).
- **Datos ya creados en la base de prueba** (sección 12 de `SCRIPT_MODULO_LAPTOPS_COMPLETO.sql`, idempotente, principal y espejo; no hay script aparte): por cada accesorio "(Usado)", sumar su `Cantidad` (solo lo ya recibido) al accesorio normal equivalente del mismo almacén (si no existe, el "(Usado)" se renombra sin el sufijo y conserva su ID, para que los ID coincidan en la espejo), cambiar las referencias en `Equipo_Cambio_Pieza` y `Equipo_Configuracion_Modulo` al accesorio normal, poner la cantidad del "(Usado)" en 0 e inactivarlo. Mostrar al final la lista de lo movido.
- Actualizar `SCRIPT_MODULO_LAPTOPS_COMPLETO.sql` con las versiones nuevas.
- Aceptación: retirar una RAM 16GB DDR4 3200 → al recibirla sube el stock del accesorio "16GB DDR4 3200" (no se crea "(Usado)"); el costo descontado a la laptop = costo de catálogo de ese accesorio.

### Tarea 12 – Registro de TODOS los movimientos de stock de accesorios
**Problema:** el kardex no sabe de entradas de mercancía, ajustes manuales de cantidad ni devoluciones por anulación; solo cambia `Accesorios.Cantidad`. Se necesita saber **quién, cuándo, cuánto y por qué** de cada cambio de existencia.
- Tabla nueva `dbo.Accesorio_Movimiento`: `ID_Movimiento INT IDENTITY PK, ID_Accesorio BIGINT NOT NULL, ID_Almacen INT NOT NULL, Fecha DATETIME NOT NULL (hora local), Cantidad_Anterior INT, Cantidad_Nueva INT, Diferencia INT (+ entrada / − salida), Motivo VARCHAR(30) NOT NULL, Documento VARCHAR(50) NULL (factura, serial de laptop, ID de cambio…), Usuario VARCHAR(60) NULL, Nota VARCHAR(250) NULL`. Índices `(ID_Accesorio, Fecha)` y `(Fecha)`.
- **Captura con trigger** `TR_Accesorios_Movimiento` (AFTER INSERT, UPDATE) sobre `dbo.Accesorios`: inserta una fila por cada accesorio cuya `Cantidad` cambió (incluye la creación con cantidad > 0). Así **ningún camino** que mueva stock queda sin registro, aunque se nos escape un SP.
- **Contexto (motivo, documento, usuario):** los SP que mueven stock ponen `EXEC sp_set_session_context 'mov_motivo', '<MOTIVO>'; … 'mov_documento', …; … 'mov_usuario', @Usuario;` antes del UPDATE, y el trigger los lee con `SESSION_CONTEXT()`. Limpiarlos al final del SP. Si un cambio llega **sin contexto**, el trigger guarda `Motivo = 'AJUSTE'` y `Usuario = SUSER_SNAME()`.
- Motivos: `ENTRADA` (registro de accesorio o aumento de stock), `AJUSTE` (edición manual de cantidad), `VENTA` (factura / prefactura procesada), `ANULACION_FACTURA` (devolución por anular), `CAMBIO_PIEZA` (instalada), `RECEPCION_PIEZA` (retirada recibida), `ANULACION_CAMBIO`, `TRANSFERENCIA` si existe para accesorios.
- **Encontrar todos los SP que cambian la existencia**: pedir al usuario que ejecute en la base de prueba y pase el resultado:
  ```sql
  SELECT o.name FROM sys.sql_modules m JOIN sys.objects o ON o.object_id = m.object_id
   WHERE m.definition LIKE '%Accesorios%' AND m.definition LIKE '%Cantidad%' ORDER BY o.name;
  ```
  Para cada uno: pedir su definición actual (`sp_helptext`) y **solo agregar** las líneas de `sp_set_session_context` (G3/G5). No cambiar nada más de su lógica.
- El usuario de la aplicación debe llegar al SP (la mayoría ya recibe `@Usuario`); si un SP no lo recibe, no cambiar su firma: el trigger usa `SUSER_SNAME()`.
- Réplica en la espejo: el trigger también existe en la espejo y registra al replicarse el SP; no replicar la tabla de movimientos por separado.
- Aceptación: registrar un accesorio con 5, editarlo a 7, venderle 1, anular esa factura, instalarlo en un cambio de laptop y recibir una retirada → `Accesorio_Movimiento` tiene 6 filas con su motivo, documento, usuario y fecha correctos.
- **Hecho (sección 13 de `SCRIPT_MODULO_LAPTOPS_COMPLETO.sql`):** tabla `Accesorio_Movimiento`, trigger `TR_Accesorios_Movimiento` y `sp_set_session_context` agregado SOLO en los procedimientos que lo necesitan: cambio de pieza (Registrar, Recibir, Anular), `Detalle_Factura_Insert` (VENTA), `Facturas_Anular` y `Proc_Anular_Factura` (ANULACION_FACTURA), `Subir_Inventario` (ENTRADA), `Ajustar_Inventario` (AJUSTE_INVENTARIO), `Transfiere_Accesorio` y `Accesorio_trasfiere_actualiza` (TRANSFERENCIA). `Accesorio_Insert`, `Accesorios_Modifica` y `Accesorio_Actualiza_Cantidad` no se tocaron: guardan el usuario en `Accesorios.Usuario` y el trigger los registra como ENTRADA (alta) o AJUSTE (edición). Motivos: ENTRADA, AJUSTE, AJUSTE_INVENTARIO, VENTA, ANULACION_FACTURA, TRANSFERENCIA, CAMBIO_PIEZA, RECEPCION_PIEZA, ANULACION_CAMBIO.
- **Antes de producción:** los 7 procedimientos del sistema se reescribieron completos a partir de su definición en MV_SYSTEM; comparar cada uno con el de producción (DbCompare, opción 7) para no pisar diferencias. `Detalle_Factura_Insert` en MV_SYSTEM (7305 caracteres) ya difería 6 caracteres del de TECNOSTYLE (7299).

### Tarea 13 – Vista "Movimientos" dentro de la consulta de accesorios (eliminar pantallas)
**Decisión del usuario:** lo mínimo pero funcional; no crear pantallas que solo amplían el menú.
- En `Accesorios → Consulta`, el selector **Disponibles | Vendidos** pasa a **Disponibles | Vendidos | Movimientos**.
- "Movimientos": lista desde `Accesorio_Movimiento` con filtros de fecha, almacén, motivo y usuario/técnico (los filtros solo se muestran en esta vista). Si el buscador tiene un accesorio específico, mostrar además su **saldo** fila a fila. Totales de entradas y salidas por motivo. **Sin exportar a Excel** (decisión del usuario: quien necesite la información la consulta en el sistema); la exportación de la pantalla de Consumo se elimina con ella.
- Historia anterior a la Tarea 12: unir la información que ya existe (ventas por factura y cambios de pieza) solo para fechas anteriores al primer registro de `Accesorio_Movimiento`, marcada como "histórico".
- Mostrar la opción "Movimientos" solo con el permiso nuevo `ACCESORIOS_MOVIMIENTOS` (decisión del usuario; hay que crearlo en la BD de seguridad y darle el script de alta). El permiso `LAPTOP_CONSUMO_COMPONENTES` queda sin uso al eliminar la pantalla de Consumo.
- **Eliminar** las pantallas *Kardex de accesorios* y *Consumo de componentes* (componentes, rutas y opciones del menú). Los SP de kardex que ya no se usen se dejan de llamar; borrarlos del script completo solo si nada más los usa.
- Aceptación: el menú queda sin "Kardex de accesorios" ni "Consumo de componentes"; desde la consulta de accesorios se ven todos los movimientos de un accesorio normal (ej. un cargador) y de una pieza de laptop.
- **Hecho:** sección 14 de `SCRIPT_MODULO_LAPTOPS_COMPLETO.sql` (`fn_Accesorio_Movimientos_Todos`, `Accesorio_Movimientos_Consultar` y `Accesorio_Movimientos_Resumen`); la sección 7 conserva solo `fn_Accesorio_Movimientos` para la historia previa y borra los SP de Kardex y Consumo. API: `GET Accesorios/Accesorio_Movimientos` (claim `ACCESORIOS_MOVIMIENTOS`); se quitaron `Kardex`, `Kardex/Buscar` y `Consumo` de `LaptopsController` y se borró `CREATE_Accesorio_Kardex.sql`. Front: componente `accesorio-movimientos` dentro de la consulta de accesorios (tercera opción, solo con el permiso); se eliminaron las pantallas Kardex y Consumo, sus rutas, menú y servicios. El saldo se muestra en cada fila (es el saldo de ese accesorio después del movimiento; vacío en filas históricas). Permiso: `CREATE_Accesorios_Permiso_Movimientos.sql` (BD SEGURIDAD).

### Tarea 14 – Separación de funciones técnico / encargado (cerrar fugas de custodia)
**Modelo acordado:** el **técnico** solo tiene `LAPTOP_CAMBIO_PIEZA` (registra cambios). El **encargado** tiene `LAPTOP_CUSTODIA_RECIBIR` (recibe, entrega al cliente) y el permiso de anular; **no** tiene `LAPTOP_CAMBIO_PIEZA`. Así uno no puede hacer lo del otro y no hace falta más personal.
1. **Técnico automático:** en la pantalla de cambio de pieza, **quitar el campo "Técnico"**. El técnico es siempre el usuario que registra el cambio (tomarlo del token en el API, no del body). El SP `Equipo_Cambio_Pieza_Registrar` usa `@Tecnico = @Usuario` (si llega un `@Tecnico` distinto, ignorarlo). El bloqueo por piezas vencidas (Tarea 16 del plan original) aplica a ese usuario.
2. **Sin destino "cliente" al registrar:** quitar el campo **"Destino de la pieza retirada"** de la pantalla. Toda pieza retirada queda **`PENDIENTE`**. En el SP, rechazar `@Destino_Retirada = 'CLIENTE'` (mensaje: "La entrega al cliente la registra el encargado desde Custodia de piezas"). "Entregada al cliente" queda solo en la pantalla de Custodia (ya existe, con `LAPTOP_CUSTODIA_RECIBIR`).
3. **Quién recibe / entrega:** en `Equipo_Cambio_Pieza_Recibir` y `Equipo_Cambio_Pieza_EntregarCliente`, rechazar si `@Usuario` = `Tecnico` **o** = `Usuario_Registro` del cambio. En el API, exigir `LAPTOP_CUSTODIA_RECIBIR` en ambos endpoints (`[Authorize]` por permiso, además de ocultarlo en el front).
4. **Anular:** `Equipo_Cambio_Pieza_Anular` y su endpoint exigen `LAPTOP_CAMBIO_ANULAR`; el SP rechaza si quien anula es el `Tecnico` o el `Usuario_Registro` del cambio. Documentar en el resumen de la tarea que ese permiso es del encargado, no del técnico.
5. Permisos en el API: verificar que **todos** los endpoints de laptops validen su permiso en el backend (no solo ocultar menús en el front).
- Resultado en pantalla de cambio: serial, tipo de pieza, pieza que sale, pieza que entra (+ precios solo si hace falta). Dos campos menos.
- Actualizar `SCRIPT_MODULO_LAPTOPS_COMPLETO.sql`. Solo base de pruebas (ver aviso ⛔).
- Aceptación: (a) el técnico no puede elegir otro técnico ni marcar "cliente"; (b) el técnico no puede recibir, entregar ni anular su propio cambio aunque tenga los permisos por error; (c) llamando al API directo sin el permiso → 403.

### Tarea 15 – Una sola entrada "Laptops" en el menú
**Decisión del usuario:** menos opciones de menú; mismo patrón que la consulta de accesorios (selector de vistas).
- Reemplazar las dos opciones del menú "Cambio de pieza (laptop)" y "Custodia de piezas (laptop)" por **una sola: "Laptops"**.
- En esa pantalla, un selector **Cambio de pieza | Custodia**. Cada opción se muestra solo si el usuario tiene su permiso (`LAPTOP_CAMBIO_PIEZA` / `LAPTOP_CUSTODIA_CONSULTA`); si solo tiene uno, se abre directo en esa vista y el selector no se muestra.
- Reutilizar los componentes actuales (`laptop-cambio-pieza`, `laptop-custodia`) dentro de la nueva pantalla; no duplicar lógica. Mantener las rutas viejas redirigiendo a la nueva para no romper accesos guardados.
- Los permisos **no cambian** (se evaluará más adelante si se agrupan).
- Solo front. Aceptación: el menú muestra una sola opción "Laptops"; técnico ve solo Cambio de pieza; encargado ve solo Custodia; un usuario con ambos permisos ve el selector.

### Tarea 16 – Quitar el comprobante de custodia
**Decisión del usuario:** el técnico es automático y el sistema lleva el registro; el ticket es un paso que no se va a usar.
- Front: quitar el botón/ofrecimiento de imprimir comprobante (pantalla de cambio, custodia e historial) y su llamada al servicio.
- API: quitar el endpoint del comprobante, la vista `ComprobanteCustodia.cshtml` y el DTO si no se usan en otro lugar.
- SQL: quitar `Equipo_Cambio_Pieza_Comprobante` del `SCRIPT_MODULO_LAPTOPS_COMPLETO.sql` y agregar al final del script un `DROP PROCEDURE IF EXISTS dbo.Equipo_Cambio_Pieza_Comprobante;` (para las bases donde ya se creó).
- Aceptación: no queda ninguna referencia al comprobante en front, API ni script; compila.

### Tarea 17 – Quitar el campo "Tipo de pieza": se elige lo que ENTRA y el sistema propone lo que SALE
**Decisión del usuario.** El técnico sabe qué pieza va a poner; el sistema deduce el tipo y propone qué módulo sale. Evaluado con 8 escenarios (RAM 1 o 2 slots, discos 1 o 2, cambiar uno o ambos, agregar en slot libre).
- Pantalla `laptop-cambio-pieza`: quitar el campo **"Tipo de pieza"**. Orden de los campos: **serial → pieza que entra → pieza que sale** (+ precios solo si hace falta).
- **1. Pieza que entra** (solo con existencia en el almacén de la laptop): RAM del **mismo DDR** que la laptop **y** discos, **agrupados** con encabezados "Memorias" y "Discos". El botón **Actualizar lista** (Tarea 10) se mantiene aquí.
- **2. Pieza que sale:** se filtra a los **módulos activos de la laptop del mismo tipo** que la pieza que entra (RAM o disco), más la opción **"Ninguna (agregar en slot libre)"**.
  - **Preselección automática:** si hay **un solo** módulo de ese tipo, o **todos son iguales** (misma capacidad y modelo, ej. 8+8), se preselecciona uno. Si hay módulos **distintos** (ej. 8+4, o SSD+HDD), queda vacío y el usuario debe elegir (obligatorio: un módulo o "Ninguna").
  - Al cambiar la pieza que entra, se recalcula la lista y la preselección.
  - Mostrar debajo, en texto pequeño, el resultado: `RAM: 8+8 = 16GB → 8+16 = 24GB`.
- **Tipo de pieza** enviado al API = el del accesorio que entra. El SP **no cambia** (sigue validando RAM/almacenamiento, DDR y que el módulo pertenezca a la laptop). Para disco, si el modelo del que sale es distinto al que entra, mantener el aviso de ranura.
- Aceptación (8 escenarios):
  1. RAM 1 slot (8) → entra 16 → sale preseleccionada la de 8 → [16].
  2. RAM 8+8 → entra 16 → preselecciona una de 8 → [8][16].
  3. RAM 8+8 → 16+16: dos cambios seguidos, cada uno con preselección.
  4. RAM 8 + slot libre → entra 8 → el usuario cambia a "Ninguna" → [8][8].
  5. Disco único SSD 256 → entra SSD 512 → preseleccionado el 256 → [512].
  6. SSD + HDD → entra SSD → **no** preselecciona; el usuario elige el HDD → [SSD][SSD].
  7. Cambiar los dos discos: dos cambios.
  8. Agregar segundo disco → entra disco → el usuario elige "Ninguna".
  9. RAM 8+4 → entra 16 → **no** preselecciona; el usuario elige cuál sale.

### Tarea 18 – Retirar una pieza sin instalar otra  ⏸️ PARA DESPUÉS (no ejecutar hasta que el usuario lo indique)
**Caso:** laptop 8+8 que se vende con 8; quitar un disco sin poner otro.
- **Pieza que entra:** agregar la opción **"Ninguna (solo retirar)"**. Con esa opción, "Pieza que sale" muestra **todos** los módulos activos de la laptop (RAM y discos) y es obligatoria. No se permite "Ninguna" en las dos listas a la vez.
- **SP `Equipo_Cambio_Pieza_Registrar`:** aceptar `@ID_Accesorio_Instalado = NULL` cuando hay módulo retirado: no se descuenta stock, `Costo nuevo = Costo actual − Costo retirado`, el módulo retirado pasa a `'R'`, no se crea módulo nuevo, se recalcula la configuración y la pieza queda **PENDIENTE** en custodia (igual que siempre). `Tipo_Pieza` = el del módulo retirado.
- **Validación:** rechazar si la laptop quedaría **sin ninguna RAM** o **sin ningún disco**.
- **Anular** este tipo de cambio: revertir el módulo retirado a `'A'` y el costo; no hay pieza instalada que devolver.
- Réplica en la espejo con los mismos ID (igual que los demás cambios).
- Aceptación: 8+8 → quitar una → [8], costo baja el de la pieza, pieza en custodia; intentar quitar la única RAM → error.

### Tarea 19 – Consulta de equipos vendidos: buscar por teléfono solo con 10 dígitos o Enter
**Decisión del usuario.** Hoy el campo teléfono llama al API con cada tecla. Sin búsqueda por nombre (el SP solo acepta número).
- Archivos: `src/app/MVSystem/pages/Equipo/Equipo-consulta/Equipo-consulta-vendido/equipo-consulta-vendido.component.html` y `.ts`.
- HTML: en el input `formControlName="telefono"` cambiar `(keyup)="obtenerEquiposVendidos()"` por `(keyup)="onSearchByTelefono($event)" (search)="onSearchByTelefono()"`. En el botón de limpiar (`clear-search-btn`) del teléfono, después de `setValue('')`, llamar `onSearchByTelefono()` en lugar de `obtenerEquiposVendidos()`.
- TS: reemplazar el método existente `onSearchByTelefono()` (hoy no se usa) por:
```ts
  // Busca solo con los 10 digitos del telefono, al presionar Enter o al dejar el campo vacio
  onSearchByTelefono(event?: KeyboardEvent) {
    const telefono = (this.form.controls.telefono.value || '').toString().replace(/\D/g, '');
    if (event && event.key === 'Enter') {
      this.obtenerEquiposVendidos();
      return;
    }
    // Flechas, Tab, Shift, etc. no cambian el texto: no buscar
    if (event && event.key.length > 1 && event.key !== 'Backspace' && event.key !== 'Delete') {
      return;
    }
    if (telefono.length === 10 || telefono.length === 0) {
      this.obtenerEquiposVendidos();
    }
  }
```
- No tocar el filtro por IMEI (queda igual por ahora). Solo front; sin cambios en API ni SQL.
- Aceptación: (a) al escribir 1–9 dígitos no se envía ninguna petición (ver pestaña Network); (b) al llegar a 10 dígitos se envía **una**; (c) Enter busca con lo escrito; (d) borrar el campo, la X del navegador o el botón limpiar recargan la lista sin filtro; (e) flechas/Tab no disparan búsqueda; (f) compila sin errores.

### Tarea 20 – Sesión vencida: el botón "Reintentar" entra en bucle
**Problema reportado por el usuario:** si el sistema queda abierto de un día para otro, a veces aparece la pantalla "Conexión a internet lenta" con **Reintentar**, y el botón no hace nada útil.
**Causa (revisada en el código):**
- `AuthenticationGuard` y `LoginGuard` (`src/app/core/guards/`) solo verifican `loginService.getUserName()` (que haya usuario en el token guardado), **no si el token está vencido**.
- `HandleErrorsResponseService.handleError`: con `status 0` navega a `'**'` (`PageNotFoundComponent`) **sin borrar el token**. Con `401` sí limpia y va al login.
- "Reintentar" (`routerLink="/"`) → login → `LoginGuard` ve usuario → `home` → la petición vuelve a fallar → otra vez "Conexión lenta".
**Cambios (front + un cambio en el API, sin SQL). Ajustado con la revisión del agente (04/11/2026):**
1. **Función simple en `src/app/helpers`** (NO en `LoginService`, para no agregar otra dependencia al ciclo `HandleErrorsResponseService` ↔ `LoginService`): `sesionVigente(): boolean` lee el token de `localStorage`, lo decodifica con `jwt_decode` y compara solo `exp` (segundos) con la hora actual, sin márgenes. Sin token, token ilegible o vencido → `false`. La usan `LoginService`, los guards y el manejador de errores.
2. `AuthenticationGuard`: permitir solo si `sesionVigente()`; si no, `localStorage.clear()`, ir a `auth/login` y devolver `false`.
3. `LoginGuard`: mandar a `home` solo si `sesionVigente()`; si hay token vencido, limpiar `localStorage` y mostrar el login (devolver `true`).
4. `HandleErrorsResponseService.handleError`, caso `status 0`: si `!sesionVigente()`, tratarlo como 401 (limpiar, toast "Sesión expirada…", ir al login). Con sesión válida, se mantiene el comportamiento actual.
5. `PageNotFoundComponent`, botón **Reintentar**: con sesión válida, `location.back()`; si no hay historial (enlace directo), ir a `/`. Con sesión vencida, limpiar e ir al login. No usar `window.location.reload()` sobre la pantalla de conexión.
6. `login.service.ts` (línea ~115, único uso): **quitar** `catchError(this.handleErrorsRespService.handleError)` (pierde el `this` y lanza `TypeError`; el interceptor ya maneja ese error) y **quitar `retry(2)`** (repite un login fallido). No usar `.bind`.
7. `login.component.ts` (líneas ~43–58): el `(err) => { this.isLoading = false; }` quedó **fuera** del `subscribe`; moverlo dentro, como segundo argumento, para que el botón no quede cargando si el login falla. (Incluido en esta tarea por estar en el mismo flujo de login.)
8. **API – vencimiento a la medianoche LOCAL (decisión del usuario 04/11/2026).** Hoy, en `MVSystemApi/Model_Negocio/Seguridad/JwtService.cs`, `var expires = DateTime.UtcNow.Date.AddDays(1);` vence a la medianoche **UTC** = **8:00 p. m. hora local**, y expulsa a quien trabaja de noche. Cambiar a la medianoche local (UTC-4, la misma diferencia fija que usa todo el sistema):
```csharp
            // Vence a la medianoche hora local (UTC-4); el valor queda en UTC (Kind = Utc)
            var ahoraLocal = DateTime.UtcNow.AddHours(-4);
            var expires = ahoraLocal.Date.AddDays(1).AddHours(4);
```
   Verificar que `expires` conserve `DateTimeKind.Utc` (lo conserva: viene de `UtcNow`). El front no cambia: sigue leyendo `exp` del token.
- Riesgo conocido: si el reloj de la PC está muy adelantado, el guard podría ver vencido un token recién emitido; se acepta (caso raro) comparando solo `exp` sin márgenes.
- Aceptación: (a) con un token vencido en `localStorage`, abrir el sistema → va directo al login, sin pantalla de conexión ni bucle; (b) con sesión válida y el API apagado → pantalla de conexión; al encender el API, "Reintentar" vuelve a la pantalla donde estaba; (c) con token vencido y el API apagado → login, no la pantalla de conexión; (d) el logout y el 401 siguen igual; (e) iniciar sesión a las 8:30 p. m. → el token vence a las 12:00 a. m. (revisar `exp` en jwt.io o en `expires` de la respuesta); (f) login con clave incorrecta → mensaje de error, el botón deja de cargar y se envía **una sola** petición (Network); (g) front y API compilan sin errores.

### Tarea 21 – Registrar un accesorio que ya existe pero está inactivo: reactivarlo al sumar
**Decisión del usuario (04/11/2026).** Si el accesorio está inactivo y la consulta no lo muestra, el usuario intenta crearlo de nuevo. Sin pantallas ni botones nuevos: se usa la ventana de coincidencias que ya existe.
- **Front** (`src/app/MVSystem/pages/Accesorios/accesorios-registro/`): en la ventana de coincidencias, **permitir elegir** también los accesorios con `estado !== 'A'` (hoy el radio está `[disabled]`). Mostrarlos con una marca visible "Inactivo – se reactivará al sumar". El resto del flujo (cantidad, actualizar precios) no cambia.
- **SQL** `dbo.Accesorio_Sumar_Existencia` (en `SCRIPT_MODULO_LAPTOPS_COMPLETO.sql`): quitar el `THROW 50804` para inactivos. Si el accesorio está inactivo, en el mismo `UPDATE` poner `Estado = 'A'` además de sumar la cantidad. El histórico (foto antes del cambio) y el movimiento (`ENTRADA`) quedan como hoy; cambiar la nota del movimiento a `'Reactivado y suma de cantidad desde el registro de accesorios'` cuando estaba inactivo.
- **API:** sin cambios de contrato. La réplica a la espejo ya se hace con `_sync.Ejecutar("Accesorio_Sumar_Existencia", ...)`, así que la reactivación también llega a la espejo.
- No mostrar inactivos en la consulta de accesorios (no se agrega filtro). Los accesorios renombrados "(… VIEJO …)" por la limpieza de duplicados no aparecen porque su descripción ya no coincide.
- Aceptación: (a) desactivar un accesorio desde Editar; (b) intentar registrarlo de nuevo → aparece en coincidencias marcado "Inactivo – se reactivará al sumar" y se puede elegir; (c) al sumar 3 → queda activo con la cantidad sumada, con registro en histórico y en movimientos; (d) en la espejo queda igual (activo y misma cantidad); (e) los activos siguen funcionando igual; (f) compila sin errores. Actualizar el script completo solo en la rama `claude` (base de pruebas).

## 3. Entrega de cada tarea
1. Archivos creados/modificados (rutas).
2. Scripts SQL a ejecutar y en qué orden (siempre en **principal y espejo**).
3. Cómo probarla (pasos concretos).
4. Supuestos o dudas.
5. Confirmar que la rama es `claude` y que `ventura` no se tocó.
6. **No continuar** con la siguiente tarea.
