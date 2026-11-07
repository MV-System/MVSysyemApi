# PLAN – Módulo de Laptops (configuración, cambio de piezas y custodia)

> **Instrucción para el agente:** Este documento es la especificación completa del módulo.
> Ejecuta **UNA SOLA TAREA** por sesión (la que se te indique: "Ejecuta la Tarea N").
> No adelantes tareas siguientes. Al terminar, entrega el resumen pedido en la sección **"Entrega de cada tarea"** y detente.

---

## 1. Contexto del negocio

MVSystem administra y factura equipos (celulares, laptops, etc.). Las laptops usadas se registran como **Equipos**
(su número de serie se guarda en el campo `Imei`). Cuando un cliente pide una mejora (ej. RAM 8GB → 12GB), el técnico
cambia la pieza. Hoy **no queda registro** de la pieza retirada → **fuga de inventario**.

Objetivo del módulo:
1. La laptop **sigue siendo un Equipo** (mismo flujo de inventario, transferencias, facturación, utilidades).
2. La **configuración variable** (RAM, almacenamiento) se guarda **por equipo**, no en el modelo.
3. Todo cambio de pieza queda registrado y **ajusta el costo** de la laptop.
4. La pieza retirada queda **en custodia del técnico** hasta que **otro usuario** confirme que la recibió.

## 2. Reglas de negocio (OBLIGATORIAS)

| # | Regla |
|---|-------|
| R1 | El **Modelo** solo describe lo fijo (ej. Marca `HP`, Modelo `240 G7 Intel Celeron N4020`). RAM y almacenamiento **NO** van en el modelo. |
| R2 | Un modelo es laptop si su `ID_Grupo` = grupo **"Laptops"** en `dbo.Grupos_Cata`. Nunca comparar por texto de descripción; obtener el ID por la descripción una vez y usar el ID. |
| R3 | Las piezas (RAM, SSD, HDD) son **Accesorios** normales, bajo la marca **"Componentes"**, con modelos por tipo de pieza (ej. `RAM DDR4 SODIMM`) y la capacidad/velocidad en la descripción del accesorio (ej. `8GB 3200`). |
| R4 | Cambio de pieza: **Costo nuevo laptop = Costo actual − Costo pieza retirada + Costo pieza instalada** (campo `Costo_Equipo` de `Equipos`). |
| R5 | El costo de la pieza retirada se toma del **costo del accesorio en catálogo** (`Costo_Equipo` del accesorio). El técnico NO lo digita. |
| R6 | La pieza instalada **no se factura aparte**: queda dentro de la laptop. La factura lleva una sola línea (la laptop) con la descripción `Modelo + configuración actual`. |
| R7 | Solo se permiten cambios de pieza en laptops **disponibles (no vendidas)**. (Cambios post-venta = fuera de alcance de este plan.) |
| R8 | Pieza retirada → estado `PENDIENTE` (en custodia del técnico). **No suma al inventario** hasta que se confirme la recepción. |
| R9 | La recepción la confirma un usuario **distinto** al técnico. Validar en el SP. Al confirmar: estado `RECIBIDA` y `Cantidad + 1` del accesorio en el almacén del cambio. |
| R10 | Si el cliente se lleva la pieza retirada: estado `ENTREGADA_CLIENTE`. Como la pieza se va con la laptop, **se vuelve a sumar su costo** a la laptop (anula el descuento de R4). Solo permitido mientras la laptop no esté vendida. |
| R11 | Si se agrega una pieza sin retirar otra (slot libre), no hay pieza retirada: `ID_Accesorio_Retirado = NULL`, estado `NO_APLICA`. |
| R12 | Toda escritura en BD debe replicarse a la **BD espejo** con `_sync.Ejecutar("<SP>", cmd.Parameters)`, igual que el código existente. |
| R13 | Toda operación de cambio/recepción debe ser **transaccional** (todo o nada). |

## 3. Convenciones del proyecto (respetarlas)

**Repos**
- API: `MVSystemApi` (.NET, SQL Server con procedimientos almacenados).
- Front: `MVSystem` (Angular 12).

**SQL**
- Scripts sueltos en la raíz del repo API, con nombre `CREATE_<Nombre>.sql` / `UPDATE_<Nombre>.sql` y encabezado de comentario (feature, propósito, fecha). Ver `CREATE_Grupos_Equipos.sql` como ejemplo.
- Scripts **idempotentes** (`IF NOT EXISTS`, `CREATE OR ALTER PROCEDURE`).
- Esquema `dbo`. Catálogos usan `Estado CHAR(1) 'A'/'I'`, `Usuario VARCHAR(60)`, `Fecha_Registro DATETIME DEFAULT GETDATE()`.
- ⚠️ Los SP existentes **no están todos en el repo**, viven en la BD. Antes de modificar un SP existente (`ALTER`), el agente debe pedir al usuario su definición actual (`sp_helptext 'NombreSP'`) y **no romper columnas ni parámetros existentes**: solo agregar.

**API**
- Acceso a datos: métodos en `MVSystemApi/Model_Negocio/AccesoDatos.cs` (ADO.NET, `SqlCommand` + `CommandType.StoredProcedure` + `DataTable`), expuestos por `IAccesoDatos`.
- Controladores en `MVSystemApi/Controllers`, patrón `try { ... return Ok(result); } catch (Exception ex) { return BadRequest(ex.Message); }`.
- Modelos en `MVSystemApi/Model`, DTOs en el proyecto `DTO`.
- Crear un controlador nuevo `LaptopsController` (`[Route("api/[controller]")]`, `[Authorize]`).

**Front**
- Servicios en `src/app/core/services`, interfaces en `src/app/core/interfaces`, URLs en `src/app/config/url.ts` (`APIURL`).
- Pantallas de equipos en `src/app/MVSystem/pages/Equipo` (lazy module `equipo.module.ts`, rutas en `equipo-routing.module.ts`), menú en `pages/home/sidebar`.
- Usar `HttpParams` para query strings, `ngx-toastr` para mensajes, `ng-select` para combos.

## 4. Modelo de datos (nuevo)

```sql
-- Configuración actual por equipo (1 fila por laptop)
dbo.Equipo_Configuracion (
  ID_Configuracion     INT IDENTITY PK,
  Imei                 VARCHAR(30) NOT NULL UNIQUE,   -- serial de la laptop (= Equipos.Imei)
  Ram_GB               INT NULL,
  Ram_Tipo             VARCHAR(30) NULL,              -- ej. 'DDR4 SODIMM 3200'
  Almacenamiento_GB    INT NULL,
  Almacenamiento_Tipo  VARCHAR(30) NULL,              -- 'SSD SATA' | 'SSD NVMe' | 'HDD'
  Notas                VARCHAR(250) NULL,
  Usuario              VARCHAR(60) NOT NULL,
  Fecha_Registro       DATETIME NOT NULL DEFAULT GETDATE(),
  Usuario_Modifica     VARCHAR(60) NULL,
  Fecha_Modifica       DATETIME NULL
)

-- Historial de cambios de pieza + custodia
dbo.Equipo_Cambio_Pieza (
  ID_Cambio               INT IDENTITY PK,
  Imei                    VARCHAR(30) NOT NULL,
  Tipo_Pieza              VARCHAR(20) NOT NULL,      -- 'RAM' | 'ALMACENAMIENTO'
  ID_Accesorio_Instalado  BIGINT NULL,
  Costo_Instalado         DECIMAL(18,2) NOT NULL DEFAULT 0,
  ID_Accesorio_Retirado   BIGINT NULL,
  Costo_Retirado          DECIMAL(18,2) NOT NULL DEFAULT 0,
  Config_Anterior         VARCHAR(100) NULL,         -- texto ej. '8GB / 256GB SSD SATA'
  Config_Nueva            VARCHAR(100) NULL,
  Costo_Equipo_Anterior   DECIMAL(18,2) NOT NULL,
  Costo_Equipo_Nuevo      DECIMAL(18,2) NOT NULL,
  ID_Almacen              INT NOT NULL,
  ID_PreFactura           INT NULL,
  ID_Reparacion           INT NULL,
  Tecnico                 VARCHAR(60) NOT NULL,
  Estado_Custodia         VARCHAR(20) NOT NULL,      -- 'PENDIENTE' | 'RECIBIDA' | 'ENTREGADA_CLIENTE' | 'NO_APLICA'
  Usuario_Registro        VARCHAR(60) NOT NULL,
  Fecha_Registro          DATETIME NOT NULL DEFAULT GETDATE(),
  Usuario_Recepcion       VARCHAR(60) NULL,
  Fecha_Recepcion         DATETIME NULL,
  Nota_Recepcion          VARCHAR(250) NULL
)
-- Índices: (Imei), (Estado_Custodia, Tecnico), (Fecha_Registro)
```

## 5. Tareas

Cada tarea indica: **Repo**, **Qué hacer**, **Criterio de aceptación**.

### FASE 1 – Base

**Tarea 1 – Grupos "Laptops" y "Componentes"**
- Repo: API (solo SQL). Archivo: `CREATE_Laptops_Fase1_Grupos.sql`.
- Insertar en `dbo.Grupos_Cata` los grupos `Laptops` y `Componentes` si no existen.
- Aceptación: correr el script 2 veces no duplica registros; `Grupos_Consulta_Combo` los devuelve.
- Nota: los modelos de laptops existentes se reclasifican al grupo "Laptops" desde la pantalla de Modelos (ya soportado) — no hacerlo por script.

**Tarea 2 – Marca y modelos de componentes**
- Repo: API (solo SQL). Archivo: `CREATE_Laptops_Fase1_Componentes.sql`.
- Crear marca `Componentes` (verificar la tabla real de marcas que usa `Marcas_Insert`) y modelos con `ID_Grupo` = Componentes:
  `RAM DDR3 SODIMM`, `RAM DDR4 SODIMM`, `RAM DDR5 SODIMM`, `SSD SATA 2.5`, `SSD M.2 SATA`, `SSD M.2 NVMe`, `HDD 2.5`.
- Aceptación: idempotente; los modelos aparecen en el combo de modelos al registrar un accesorio.

### FASE 2 – Configuración por equipo

**Tarea 3 – Tabla y SPs de configuración**
- Repo: API (SQL). Archivo: `CREATE_Equipo_Configuracion.sql`.
- Crear `dbo.Equipo_Configuracion` (sección 4).
- SP `Equipo_Configuracion_Guardar` (upsert por `Imei`; valida que el equipo exista y que su modelo sea del grupo Laptops).
- SP `Equipo_Configuracion_Consultar @Imei`.
- Función `dbo.fn_Equipo_Configuracion_Texto(@Imei)` → `'12GB DDR4 / 256GB SSD SATA'` (NULL si no hay config).
- Aceptación: guardar dos veces el mismo IMEI actualiza (no duplica); un equipo que no es laptop devuelve error claro.

**Tarea 4 – API de configuración**
- Repo: API. Crear `LaptopsController` + métodos en `AccesoDatos`/`IAccesoDatos` + modelo `EquipoConfiguracion`.
- Endpoints: `POST api/Laptops/Configuracion` (guardar), `GET api/Laptops/Configuracion?imei=`.
- Usar `_sync.Ejecutar` en el guardado (R12).
- Aceptación: probado en Swagger; compila sin warnings nuevos.

**Tarea 5 – Front: configuración en registro de equipo**
- Repo: Front. Pantalla `pages/Equipo/Equipo-registro`.
- Al seleccionar un modelo del grupo Laptops, mostrar campos: RAM (GB), tipo RAM, almacenamiento (GB), tipo almacenamiento (combo: SSD SATA / SSD NVMe / HDD), notas. Obligatorios para laptops; ocultos para otros grupos.
- Al guardar el equipo con éxito, llamar `POST api/Laptops/Configuracion`.
- Aceptación: registrar un celular funciona igual que antes; registrar una laptop sin RAM/almacenamiento no deja guardar.

**Tarea 6 – Descripción dinámica en Equipos disponibles**
- Repo: API (SQL + modelo) y Front.
- Modificar `Proc_Equipo_Disponible_Consulta` (pedir definición actual) para devolver columna nueva `Configuracion = dbo.fn_Equipo_Configuracion_Texto(Imei)`. No quitar ni renombrar columnas.
- Mostrar en la lista: `Modelo · Configuracion` cuando exista.
- Aceptación: celulares se ven igual; laptops muestran su configuración.

**Tarea 7 – Descripción dinámica en facturación, prefactura y vendidos**
- Igual que la Tarea 6 en: búsqueda de equipo para facturar (`Equipo_Busca_Disponible`), prefactura, reporte de factura (`SP_Reporte_Factura`) y `Proc_Equipo_Consulta_Vendidos`.
- Aceptación: la factura impresa de una laptop muestra `Modelo · 12GB DDR4 / 256GB SSD`.

### FASE 3 – Cambio de pieza

**Tarea 8 – Tabla de cambios**
- Repo: API (SQL). Archivo: `CREATE_Equipo_Cambio_Pieza.sql`. Crear tabla e índices (sección 4).

**Tarea 9 – SP de cambio de pieza**
- SP `Equipo_Cambio_Pieza_Registrar` con parámetros: `@Imei, @Tipo_Pieza, @ID_Accesorio_Instalado, @ID_Accesorio_Retirado (NULL), @Destino_Retirada ('CUSTODIA'|'CLIENTE'), @Tecnico, @ID_Almacen, @ID_PreFactura (NULL), @ID_Reparacion (NULL), @Usuario`.
- Dentro de `BEGIN TRAN ... COMMIT` con `TRY/CATCH` + `ROLLBACK`:
  1. Validar: equipo existe, es laptop (R2), no está vendido (R7), está en `@ID_Almacen`.
  2. Validar stock del accesorio instalado en el almacén (`Cantidad > 0`) y restar 1.
  3. Obtener costos de catálogo de ambos accesorios (R5).
  4. Calcular costo nuevo (R4). Si `@Destino_Retirada = 'CLIENTE'` no restar el costo retirado (R10).
  5. Actualizar `Equipos.Costo_Equipo`.
  6. Actualizar `Equipo_Configuracion` (RAM o almacenamiento según `@Tipo_Pieza`, tomando capacidad/tipo de la pieza instalada — definir con el usuario si se toma de campos del accesorio o se envían como parámetros `@Nueva_Capacidad_GB`, `@Nuevo_Tipo`; **recomendado: parámetros**).
  7. Insertar en `Equipo_Cambio_Pieza` con `Estado_Custodia`: `PENDIENTE` (custodia), `ENTREGADA_CLIENTE` (cliente) o `NO_APLICA` (sin retirada).
  8. Devolver el registro insertado.
- Aceptación (probar en SQL): stock baja 1; costo de la laptop cumple R4; configuración actualizada; sin stock → error y nada cambia.

**Tarea 10 – API de cambio de pieza**
- `POST api/Laptops/CambioPieza`, `GET api/Laptops/Historial?imei=`. Ejecutar con transacción y `_sync.Ejecutar` (R12, R13).

**Tarea 11 – Front: pantalla de cambio de pieza**
- Nueva ruta `equipo/laptop-cambio-pieza` + opción en el menú de Equipos.
- Flujo: escanear/buscar serial → muestra modelo, configuración y costo actual → elegir tipo de pieza → elegir accesorio instalado (filtrado por marca Componentes y stock en el almacén) → elegir accesorio retirado (opcional) → destino de la retirada → técnico (combo de usuarios) → **resumen con costo anterior/nuevo** → confirmar.
- Aceptación: tras confirmar, la pantalla muestra el cambio en el historial de la laptop.

### FASE 4 – Custodia

**Tarea 12 – Confirmar recepción**
- SP `Equipo_Cambio_Pieza_Recibir @ID_Cambio, @Usuario, @Nota`: valida estado `PENDIENTE` y `@Usuario <> Tecnico` (R9); suma 1 al accesorio retirado en `ID_Almacen`; marca `RECIBIDA`. Transaccional.
- Endpoint `POST api/Laptops/Custodia/Recibir` + botón en la pantalla de pendientes (Tarea 14).

**Tarea 13 – Entregada al cliente**
- SP `Equipo_Cambio_Pieza_EntregarCliente @ID_Cambio, @Usuario, @Nota`: solo desde `PENDIENTE` y si la laptop **no está vendida**; vuelve a sumar `Costo_Retirado` al `Costo_Equipo` (R10); marca `ENTREGADA_CLIENTE`.
- Endpoint `POST api/Laptops/Custodia/EntregarCliente`.

**Tarea 14 – Pantalla de pendientes por técnico**
- SP `Equipo_Cambio_Pieza_Pendientes @Tecnico (NULL), @ID_Almacen (NULL)` → lista con horas transcurridas.
- Ruta `equipo/laptop-custodia`: tabla agrupable por técnico, columna de antigüedad (rojo si supera el límite), botones Recibir / Entregada al cliente.

### FASE 5 – Control

**Tarea 15 – Historial de la laptop**
- Vista de historial (todos los cambios con estado de custodia) accesible desde el detalle del equipo y desde la pantalla de cambio.

**Tarea 16 – Alerta / bloqueo por pendientes**
- Configuración en `appsettings.json`: `"Laptops": { "HorasMaxCustodia": 24, "BloquearTecnicoConPendientesVencidos": true }`.
- Si está activo, `Equipo_Cambio_Pieza_Registrar` rechaza nuevos cambios para un técnico con pendientes vencidos (mensaje claro).

**Tarea 17 – Verificar utilidades**
- Revisar de dónde toma el costo `Ver_Utilidades_Precio_Detalle` (¿de `Equipos.Costo_Equipo` o de una copia en `Detalle_Factura`?).
- Caso de prueba: laptop costo 15,000, retira RAM 1,000, instala RAM 1,800 → costo 15,800; vender a 22,500 → utilidad debe ser **6,700**. Reportar resultado; no cambiar SP de utilidades sin aprobación.

**Tarea 18 – Permisos**
- Crear permisos en el módulo de Seguridad: `LAPTOP_CAMBIO_PIEZA`, `LAPTOP_CUSTODIA_RECIBIR`, `LAPTOP_CUSTODIA_CONSULTA`. Aplicarlos al menú (ngx-permissions) y a los endpoints.

### FASE 6 – Limpieza

**Tarea 19 – Migración de modelos existentes**
- Script de **solo lectura** que liste modelos del grupo Laptops con RAM/almacenamiento en la descripción y proponga: descripción limpia + RAM + almacenamiento (regex sobre patrones tipo `8ram`, `8gb`, `256gb`, `1tb`).
- Entregar el resultado para **revisión manual**. La actualización real se hace en un segundo script solo después de aprobación.

## 6. Fuera de alcance (no implementar)
- Cambios de pieza en laptops ya vendidas (post-venta).
- Seguimiento por número de serie de cada pieza.

## 7. Entrega de cada tarea
Al terminar una tarea, el agente responde con:
1. Archivos creados/modificados (rutas).
2. Cómo probarla (pasos o script de prueba).
3. Dudas o supuestos que tomó.
4. **No continuar** con la siguiente tarea.
