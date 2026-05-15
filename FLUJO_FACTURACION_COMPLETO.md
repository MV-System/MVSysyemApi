# 📊 FLUJO COMPLETO DE FACTURACIÓN DE EQUIPOS - MVSystemApi

## 1️⃣ CONTROLADOR PRINCIPAL: FacturacionController.cs

### Endpoint HTTP
```
[HttpPost] /Factura/PostFactura
```

### Clase
- **Ubicación**: `MVSystemApi\Controllers\FacturacionController.cs`
- **Método**: `PostFactura(FacturaRegistroDTO request)` (línea 44)
- **Tipo**: `async Task<ActionResult>`

### Inyecciones de Dependencia
```csharp
public FacturacionController(
    Facturas_Negocio _en,                    // Lógica de negocio (DEPRECADO)
    IAccesoDatos accesoDatos,                // Acceso a datos ✅ USADO
    IFacEDGIService facEDGIService,          // Servicio electrónico (DGII)
    IServiceScopeFactory serviceScopeFactory,// Factory de servicios
    ILogger<FacturacionController> logger    // Logging
)
```

---

## 2️⃣ FLUJO PASO A PASO DE FACTURACIÓN

### PASO 1: Validación de Pagos (línea 50-58)
```csharp
if (request.IdTipoPago == 6) // Pago Mixto
    // Valida:
    - Mínimo 2 métodos de pago
    - Suma de montos = Total factura
    - Montos positivos
    - Sin tipos de pago duplicados
```

### PASO 2: Conversión DTO → Modelo Facturas (línea 65-116)
- Mapea `FacturaRegistroDTO` a objeto `Facturas`
- Convierte `DetalleFacturaDTO` a lista de `DetalleFactura`
- **Campos de Factura**:
  - NumeroFactura (inicia en 0, se genera en BD)
  - IdCliente, IdVendedor, IdTipoFactura, IdTipoPago, IdAlmacen
  - SubTotal, Descuento, Itbis, Total, Abono
  - Usuario, FechaRegistro, fechaLimitePago
  - RNC, Nota, Estado='A' (Activo), empresa, IdEmpresa

### PASO 3: Preparación de Detalles de Pago (línea 118-130)
- Si es pago mixto: Usa lista `request.DetallesPagos`
- Si es pago simple: Crea lista con 1 elemento del tipo `IdTipoPago`

### PASO 4: Llamada a `AccesoDatos.PostFacturaConPagos()` (línea 128)
```csharp
var resultFactura = _accesoDatos.PostFacturaConPagos(factura, detallesPago);
```

### PASO 5: Envío a DGII (línea 138-156)
- Llama método asíncrono `EnviarFacturaADGISincrono()`
- Si DGI rechaza: Anula la factura automáticamente
- Si aprueba: Factura queda confirmada

---

## 3️⃣ MÉTODO CORE: `PostFacturaConPagos()` en AccesoDatos.cs

### Ubicación
`MVSystemApi\Model_Negocio\AccesoDatos.cs` (línea 1706)

### Estructura General
```csharp
public Facturas PostFacturaConPagos(Facturas factura, List<DetallePagoDTO> detallesPagos)
{
    // Transacción BD PRINCIPAL
    // Sync BD ESPEJO
}
```

---

### ✅ FASE 1: TRANSACCIÓN EN BD PRINCIPAL (línea 1709-1793)

#### 1️⃣ BEGIN TRANSACTION en BD Principal (línea 1710)
```csharp
using var connection = new SqlConnection(_connectionStrings);
connection.Open();
using var transaction = connection.BeginTransaction();
```

#### 2️⃣ Insertar Factura - SP `facturas_Insert` (línea 1715-1747)
```sql
EXEC facturas_Insert
    @ID_Cliente = request.IdCliente
    @ID_Vendedor = request.IdVendedor
    @ID_Tipo_Factura = request.IdTipoFactura
    @ID_Tipo_Pagos = request.IdTipoPago
    @ID_Almacen = request.IdAlmacen
    @Cantidad_Articulos = request.CantidadArticulos
    @Itbis = request.Itbis
    @NFCTipoNumero = request.NcfTipoNumero
    @RNC = request.RNC
    @SubTotal = request.SubTotal
    @Abono = request.Abono
    @Tiempo_Credito = request.fechaLimitePago
    @Total = request.Total
    @Descuento = request.Descuento
    @Nota = request.Nota
    @Estado = 'A'
    @Usuario = request.Usuario
    @fecha_registro = request.FechaRegistro
    @Numero_factura = OUTPUT ← Retorna NumeroFactura
```

**Resultado**: Genera y retorna `NumeroFactura` (auto-incremental)

#### 3️⃣ Insertar Detalles de Factura - SP `Detalle_Factura_Insert` (línea 1749-1777)
```sql
POR CADA articulo/equipo en DetalleFacturaList:
    EXEC Detalle_Factura_Insert
        @Numero_Factura = factura.NumeroFactura
        @ID_Equipo = item.IdEquipo              ← IMEI del equipo
        @Descripcion = item.Descripcion
        @ID_Tipo = item.IdTipo
        @ID_Garantia = item.IdGarantia
        @ID_Tipo_Factura = item.IdTipoFactura
        @ID_Vendedor = item.IdVendedor
        @SubTotal = item.SubTotal
        @cantidad = item.Cantidad
        @precio = item.Precio
        @Descuento = item.Descuento
        @Itbis = item.Itbis
        @Total = item.Total
        @Estado = 'A'
        @Usuario = item.Usuario
        @fecha_registro = item.FechaRegistro
```

**Tablas Afectadas**:
- `Facturas` (INSERT)
- `Detalle_Facturas` (INSERT) - línea de factura con IMEI del equipo

#### 4️⃣ Insertar Detalles de Pago - SP `InsertDetallesPagoFactura` (línea 1778-1792)
```sql
POR CADA método de pago:
    EXEC InsertDetallesPagoFactura
        @Numero_Factura = factura.NumeroFactura
        @ID_Tipo_Pago = item.ID_Tipo_Pago
        @Monto_Pago = item.Monto_Pago
        @Numero_Referencia = item.Numero_Referencia
        @Observacion = item.Observacion
```

**Tablas Afectadas**:
- `Detalles_Pago` (INSERT)

#### 5️⃣ COMMIT Transaction (línea 1793)
```csharp
transaction.Commit();
// Todas las inserciones se confirman en BD Principal
```

**🔴 IMPORTANTE**: ⚠️ Los SPs `facturas_Insert` y/o `Detalle_Factura_Insert` 
**PROBABLEMENTE CONTENGAN LÓGICA PARA ACTUALIZAR `Equipos.Disponible` A 'N'**
(No está en el código C# visible, sino internamente en los SPs)

---

### ✅ FASE 2: SINCRONIZACIÓN CON BD ESPEJO (línea 1795-1808)

#### FUERA de Transacción (después de COMMIT)

**Importante**: Esta parte ejecuta en conexión diferente (BD Espejo)

```csharp
if (factura.DetalleFacturaList != null)
{
    POR CADA equipo en la factura:
    
    _sync.EjecutarQuery(
        SQL: "UPDATE Equipos SET Disponible = 'N', Nota_Adicional = 'inverso' WHERE IMEI = @IMEI",
        Parámetro: @IMEI = item.IdEquipo
    )
}
```

**Sentencia SQL BD Espejo**:
```sql
UPDATE Equipos 
SET 
    Disponible = 'N',                  -- Marca como NO disponible
    Nota_Adicional = 'inverso'         -- Flag para rastreo sincronización
WHERE 
    IMEI = <ID_EQUIPO_FACTURADO>
```

**Tabla Afectada**: `Equipos` (UPDATE)

---

## 4️⃣ SERVICIO DE SINCRONIZACIÓN: SyncService.cs

### Ubicación
`MVSystemApi\Services\SyncService.cs`

### Inicialización
```csharp
public SyncService(
    IHttpContextAccessor httpContextAccessor,
    SeguridadService seguridad,
    IMemoryCache memoryCache,
    ILogger<SyncService> logger
)
```

### Obtención de Connection String BD Espejo
1. Lee claim JWT: `User.FindFirst("IdEmpresaE")?.Value`
2. Si `IdEmpresaE > 0`: Existe BD Espejo
3. Valida cache (TryGetValue: `SyncConn:{idEmpresaE}`)
4. Si NO en cache: Obtiene de `SeguridadService.GetConnStringByEmpresa()`
5. Cachea por 13 minutos

### Métodos de Ejecución

#### Método 1: `Ejecutar()` - Para Stored Procedures
```csharp
public void Ejecutar(string spName, SqlParameterCollection parameters)
{
    // Ejecuta SP en BD espejo
}
```

#### Método 2: `EjecutarQuery()` - Para SQL Directo ✅ USADO EN FACTURACIÓN
```csharp
public void EjecutarQuery(string sql, params SqlParameter[] parameters)
{
    // Ejecuta UPDATE en BD espejo
}
```

**Características**:
- Ejecuta en conexión separada (BD Espejo)
- NO participa en transacción de BD Principal
- Log de TODAS las operaciones
- Captura errores sin romper flujo principal

---

## 5️⃣ ANULACIÓN DE FACTURA: Método `PostAnulaFactura()`

### Flujo Inverso (línea 1815-1850)

#### PASO 1: Ejecutar SP en BD Principal
```csharp
SqlCommand cmd = new SqlCommand("Proc_Anular_Factura", cn);
cmd.Parameters.Add("@NumeroFactura", Factura.NumeroFactura);
cmd.Parameters.Add("@IdSucursal", Factura.IDsucursal);
cmd.Parameters.Add("@Usuario", Factura.Usuario);
cmd.ExecuteNonQuery();
```

#### PASO 2: Leer IMEIs de la Factura
```sql
SELECT ID_Equipo 
FROM Detalle_Facturas 
WHERE Numero_Factura = @nf
```

#### PASO 3: Marcar Equipos como Disponibles en BD Espejo
```sql
UPDATE Equipos 
SET 
    Disponible = 'S',                  -- Vuelve a marcar como disponible
    Nota_Adicional = 'inverso'
WHERE 
    IMEI = <ID_EQUIPO>
```

**Efecto Final**: Libera los equipos facturas de vuelta para poder ser vendidos

---

## 6️⃣ TABLA DE EQUIPOS - CAMPOS MODIFICADOS

### Campo: `Disponible`
```
Valores:
  'S' = Sí, disponible para venta
  'N' = No disponible (ya fue facturado/vendido)
```

### Campo: `Nota_Adicional`
```
Valores durante sync:
  'inverso' = Marcado como invertida la disponibilidad (para rastreo)
```

### Eventos que Cambian `Disponible`

| Evento | BD Principal | BD Espejo | Disponible | Nota_Adicional |
|--------|-------------|-----------|-----------|-----------------|
| **Facturación** | ¿En SP? | UPDATE | N | inverso |
| **Anulación** | ¿Revert? | UPDATE | S | inverso |
| **Transferencia Salida** | ¿En SP? | UPDATE | N | inverso |
| **Transferencia Entrada** | ¿En SP? | UPDATE | S | inverso |
| **Cambio IMEI Manual** | SP especial | UPDATE | Var | inverso |

---

## 7️⃣ TABLAS MODIFICADAS EN BD PRINCIPAL

### Tablas Afectadas por `PostFacturaConPagos()`

| Tabla | Operación | Descripción |
|-------|-----------|-------------|
| **Facturas** | INSERT | Se inserta nueva factura |
| **Detalle_Facturas** | INSERT | Se inserta línea de factura con IMEI del equipo |
| **Detalles_Pago** | INSERT | Se inserta detalles de pago mixto |
| **Equipos** | UPDATE? | ⚠️ Probablemente en SP `facturas_Insert` o `Detalle_Factura_Insert` |

### Tablas Afectadas por SyncService en BD ESPEJO

| Tabla | Operación | Descripción |
|-------|-----------|-------------|
| **Equipos** | UPDATE | Marca Disponible='N' después de facturación |

---

## 8️⃣ FLUJO VISUAL COMPLETO

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    CLIENTE (Frontend/API)                               │
│                   POST /Factura/PostFactura                             │
│              FacturaRegistroDTO con DetalleFacturaList                  │
└──────────────────────────┬──────────────────────────────────────────────┘
                           │
                           ▼
        ┌──────────────────────────────────────┐
        │   FacturacionController.PostFactura  │
        │   ✅ Valida pago mixto               │
        │   ✅ Convierte DTO → Modelo          │
        │   ✅ Prepara detalles de pago        │
        └──────────────────────┬───────────────┘
                               │
                               ▼
        ┌──────────────────────────────────────────────────────────┐
        │     AccesoDatos.PostFacturaConPagos()                    │
        │                                                          │
        │  ┌─── BD PRINCIPAL (con transacción) ─────────────────┐ │
        │  │                                                      │ │
        │  │  1. BEGIN TRANSACTION                               │ │
        │  │                                                      │ │
        │  │  2. INSERT Facturas (SP: facturas_Insert)            │ │
        │  │     └─ Retorna: NumeroFactura                       │ │
        │  │                                                      │ │
        │  │  3. INSERT Detalle_Facturas (SP: Detalle_Factura_Insert) │
        │  │     ├─ PER CADA EQUIPO FACTURADO                   │ │
        │  │     └─ Inserta IMEI del equipo                      │ │
        │  │                                                      │ │
        │  │  4. INSERT Detalles_Pago (SP: InsertDetallesPagoFactura) │
        │  │     └─ PER CADA MÉTODO DE PAGO                     │ │
        │  │                                                      │ │
        │  │  ⚠️  PROBABLE: SP ACTUALIZA Equipos.Disponible='N'   │ │
        │  │     (Lógica dentro del SP, no visible en C#)        │ │
        │  │                                                      │ │
        │  │  5. COMMIT TRANSACTION                              │ │
        │  │                                                      │ │
        │  └──────────────────────────────────────────────────────┘ │
        │                                                          │
        │  ┌─── BD ESPEJO (conexión separada) ──────────────────┐ │
        │  │                                                      │ │
        │  │  6. FOREACH equipo en factura:                      │ │
        │  │     ├─ SyncService.EjecutarQuery()                  │ │
        │  │     └─ UPDATE Equipos                               │ │
        │  │         SET Disponible='N', Nota_Adicional='inverso'│ │
        │  │         WHERE IMEI = @IMEI                          │ │
        │  │                                                      │ │
        │  │  7. Sin transacción, sin rollback automático        │ │
        │  │                                                      │ │
        │  └──────────────────────────────────────────────────────┘ │
        │                                                          │
        └──────────────────────┬───────────────────────────────────┘
                               │
                               ▼
        ┌──────────────────────────────────────┐
        │  FacturacionController               │
        │  EnviarFacturaADGISincrono()        │
        │  ✅ Envía a DGI                      │
        │  ✅ Valida respuesta                │
        │  ❌ Si rechaza: Anula factura       │
        └──────────────────────┬───────────────┘
                               │
                               ▼
        ┌──────────────────────────────────────┐
        │  Respuesta JSON                      │
        │  {                                   │
        │    factura: { ... },                │
        │    mensaje: "Factura guardada"      │
        │  }                                   │
        └──────────────────────────────────────┘
```

---

## 9️⃣ CASOS DE ESTUDIO

### Caso 1: Facturación Normal
```
1. Usuario factura 2 equipos (IMEI: A1, A2)
2. Se inserta factura #001, detalles con IMEIs A1, A2
3. SyncService marca A1='N', A2='N' en BD Espejo
4. Equipos no aparecen en búsqueda de disponibles
```

### Caso 2: Pago Mixto
```
1. Usuario pagará 70% efectivo + 30% cheque
2. Controller valida que suma = total
3. Crea 2 registros en Detalles_Pago
4. Ambos se insertan en transacción
5. Factura queda con 2 formas de pago vinculadas
```

### Caso 3: Anulación
```
1. Se anula factura #001
2. SP Proc_Anular_Factura la marca inactiva en BD Principal
3. Se leen IMEIs (A1, A2) de Detalle_Facturas
4. SyncService marca A1='S', A2='S' en BD Espejo
5. Equipos vuelven a disponible para venta
```

### Caso 4: Sin BD Espejo
```
1. Usuario sin IdEmpresaE en JWT
2. SyncService retorna early (sin operación)
3. BD Principal se actualiza normalmente
4. BD Espejo NO se afecta (no existe)
```

---

## 🔟 OBSERVACIONES CRÍTICAS

### ❓ PREGUNTA ABIERTA #1
**¿Dónde exactamente se marca `Equipos.Disponible='N'` en BD Principal?**

Opciones:
1. ✅ **MÁS PROBABLE**: Dentro del SP `Detalle_Factura_Insert` o `facturas_Insert`
2. ❌ NO está en código C# visible de `PostFacturaConPagos()`
3. ❌ NO hay UPDATE explícito en AccesoDatos

**Evidencia**: El SP recibe `@ID_Equipo`, es lógico que lo marque como no disponible

### ❓ PREGUNTA ABIERTA #2
**¿Cuál es el orden de ejecución de INSERT vs UPDATE en los SPs?**

Probable flujo:
```sql
-- Detalle_Factura_Insert
1. INSERT INTO Detalle_Facturas (Numero_Factura, ID_Equipo, ...)
2. UPDATE Equipos SET Disponible='N' WHERE IMEI=@ID_Equipo
```

### 🔴 PUNTO CRÍTICO
**La sincronización con BD Espejo OCURRE FUERA de la transacción**

```
Riesgo: Si SyncService falla, BD Principal queda con equipo facturado
        pero BD Espejo sigue mostrándolo disponible
Mitigación: SyncService logea errores pero NO interrumpe flujo principal
```

---

## 1️⃣1️⃣ CONCLUSIÓN - RESUMEN EJECUTIVO

### Flujo de Facturación en 3 Pasos

```
PASO 1: TRANSACCIÓN BD PRINCIPAL
├─ BEGIN TRANSACTION
├─ INSERT Factura (genera NumeroFactura)
├─ INSERT Detalles_Factura (POR CADA EQUIPO)
├─ INSERT Detalles_Pago (POR CADA MÉTODO PAGO)
├─ [PROBABLE: UPDATE Equipos.Disponible='N' EN SPs]
└─ COMMIT TRANSACTION ✅

PASO 2: SINCRONIZACIÓN BD ESPEJO
├─ [FUERA DE TRANSACCIÓN]
├─ FOREACH equipo facturado
├─ SyncService.EjecutarQuery()
├─ UPDATE Equipos SET Disponible='N' in BD Espejo
└─ Log errors pero sigue adelante

PASO 3: ENVÍO DGI
├─ EnviarFacturaADGISincrono()
├─ ✅ Si aprueba: Factura confirmada
└─ ❌ Si rechaza: Anula factura automáticamente
```

### Campos Modificados en Tabla Equipos
```
Disponible:      'S' → 'N'
Nota_Adicional:  '' → 'inverso'
```

### Tablas Afectadas
```
BD Principal          │ BD Espejo
─────────────────────┼──────────────
Facturas (INSERT)     │ 
Detalle_Facturas      │ 
Detalles_Pago (INSERT)│
Equipos (UPDATE?)     │ Equipos (UPDATE)
```

---

**📌 ÚLTIMA ACTUALIZACIÓN**: Mayo 7, 2026
**✅ ESTADO**: Análisis Completo - Listo para Implementación
