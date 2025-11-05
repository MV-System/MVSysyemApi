# 📊 Sistema de Tracking - Todos los Módulos

## ✅ Implementación Completada

Se ha agregado tracking a **8 módulos principales** del sistema MVSystem. Todas las configuraciones de notificación están **DESACTIVADAS por defecto** en la base de datos.

---

## 📋 Módulos con Tracking Implementado

### 1️⃣ **Utilidades** 
📁 Archivo: `src/app/MVSystem/pages/utilidades/utilidades-consulta/utilidades.component.ts`

**Acciones rastreadas:**
- ✅ `Acceso` - Cuando el usuario entra al módulo
- ✅ `Consultar` - Cuando consulta utilidades con filtros de fecha y sucursal

**Metadatos capturados:**
```typescript
{
  seccion: 'consulta',
  vista: 'principal',
  fechaInicio: '2024-01-01',
  fechaFin: '2024-12-31',
  sucursal: 1
}
```

---

### 2️⃣ **Equipos Vendidos**
📁 Archivo: `src/app/MVSystem/pages/Equipo/Equipo-consulta/Equipo-consulta-vendido/equipo-consulta-vendido.component.ts`

**Acciones rastreadas:**
- ✅ `Acceso` - Cuando el usuario entra al módulo
- ✅ `Consultar` - Cuando consulta equipos vendidos con filtros

**Metadatos capturados:**
```typescript
{
  seccion: 'consulta',
  vista: 'principal',
  fechaInicio: '2024-01-01',
  fechaFin: '2024-12-31',
  sucursal: 1,
  modelo: 15,
  suplidor: 3
}
```

---

### 3️⃣ **Equipos Disponibles**
📁 Archivo: `src/app/MVSystem/pages/Equipo/Equipo-consulta/Equipo-consulta-disponible/equipo-disponible.component.ts`

**Acciones rastreadas:**
- ✅ `Acceso` - Cuando el usuario entra al módulo
- ✅ `Consultar` - Cuando consulta inventario disponible
- ✅ `Buscar IMEI` - Cuando busca por IMEI específico (metadato: imei)

**Metadatos capturados:**
```typescript
{
  seccion: 'consulta',
  vista: 'principal',
  imei: '123456789012345',
  modelo: 15,
  sucursal: 1,
  suplidor: 3
}
```

---

### 4️⃣ **Facturas** (Consulta)
📁 Archivo: `src/app/MVSystem/pages/facturacion/facturacion-consulta/facturacion-consulta.component.ts`

**Acciones rastreadas:**
- ✅ `Acceso` - Cuando el usuario entra al módulo
- ✅ `Consultar` - Cuando consulta facturas
- ✅ `Ver Detalle` - Cuando abre el detalle de una factura (puedes implementar)
- ✅ `Anular` - Cuando anula una factura (puedes implementar)

**Metadatos capturados:**
```typescript
{
  seccion: 'consulta',
  vista: 'principal',
  fechaInicio: '2024-01-01',
  fechaFin: '2024-12-31',
  sucursal: 1,
  estado: 'A' // A=Activas, N=Anuladas
}
```

---

### 5️⃣ **Accesorios** (Consulta)
📁 Archivo: `src/app/MVSystem/pages/Accesorios/accesorio-consulta/accesorio-consulta.component.ts`

**Acciones rastreadas:**
- ✅ `Acceso` - Cuando el usuario entra al módulo
- ✅ `Consultar` - Cuando consulta accesorios

**Metadatos capturados:**
```typescript
{
  seccion: 'consulta',
  vista: 'principal',
  accesorio: 'Funda',
  sucursal: 1
}
```

---

### 6️⃣ **Registro de Equipos**
📁 Archivo: `src/app/MVSystem/pages/Equipo/Equipo-registro/equipo-registro.component.ts`

**Acciones rastreadas:**
- ✅ `Acceso` - Cuando el usuario entra al módulo
- ✅ `Crear` - Cuando registra un nuevo equipo

**Metadatos capturados:**
```typescript
{
  seccion: 'registro',
  vista: 'formulario',
  imei: '123456789012345',
  modelo: 15
}
```

---

### 7️⃣ **Registro de Accesorios**
📁 Archivo: `src/app/MVSystem/pages/Accesorios/accesorios-registro/accesorios-registro.component.ts`

**Acciones rastreadas:**
- ✅ `Acceso` - Cuando el usuario entra al módulo
- ✅ `Crear` - Cuando registra un nuevo accesorio

**Metadatos capturados:**
```typescript
{
  seccion: 'registro',
  vista: 'formulario',
  descripcion: 'Protector de Pantalla',
  cantidad: 50
}
```

---

### 8️⃣ **Dashboard IA** (YA IMPLEMENTADO ANTERIORMENTE)
📁 Archivo: `src/app/MVSystem/pages/dashboard/dashboard.component.ts`

**Acciones rastreadas:**
- ✅ `Acceso` - Cuando el usuario entra al módulo
- ✅ `Consultar Utilidades Día`
- ✅ `Consultar Utilidades Semana`
- ✅ `Consultar Utilidades Mes`

---

## 🗄️ Configuración en Base de Datos

### Script de Instalación
📄 **Archivo:** `Scripts/03_InsertTrackingConfig_AllModules.sql`

**Ejecutar en:** Base de datos `SEGURIDAD`

### Estado Inicial
🔴 **TODAS las notificaciones están DESACTIVADAS** (`NotificarWhatsApp = 0`)

### Estructura de la Tabla
```sql
TrackingNotificacionConfig
├── IdConfig (PK)
├── Modulo (varchar, nullable)
├── Accion (varchar, nullable)
├── NotificarWhatsApp (bit) -- 0 = NO notifica, 1 = SÍ notifica
├── Activo (bit)             -- 0 = Regla inactiva, 1 = Regla activa
├── FechaCreacion
└── FechaModificacion
```

---

## 🔧 Cómo Activar Notificaciones

### ✅ Ejemplo 1: Notificar cuando crean una factura
```sql
UPDATE TrackingNotificacionConfig 
SET NotificarWhatsApp = 1 
WHERE Modulo = 'Facturas' AND Accion = 'Crear'
```

### ✅ Ejemplo 2: Notificar cuando eliminan un equipo
```sql
UPDATE TrackingNotificacionConfig 
SET NotificarWhatsApp = 1 
WHERE Modulo = 'Registro Equipos' AND Accion = 'Eliminar'
```

### ✅ Ejemplo 3: Notificar TODO el módulo de Facturas
```sql
UPDATE TrackingNotificacionConfig 
SET NotificarWhatsApp = 1 
WHERE Modulo = 'Facturas'
```

### ✅ Ejemplo 4: Notificar accesos a Equipos Disponibles
```sql
UPDATE TrackingNotificacionConfig 
SET NotificarWhatsApp = 1 
WHERE Modulo = 'Equipos Disponibles' AND Accion = 'Acceso'
```

### ✅ Ejemplo 5: Notificar cuando consultan accesorios
```sql
UPDATE TrackingNotificacionConfig 
SET NotificarWhatsApp = 1 
WHERE Modulo = 'Accesorios' AND Accion = 'Consultar'
```

---

## 📱 Mensaje de WhatsApp (Ejemplo)

Cuando se activa una notificación, llegaría un mensaje como:

```
🔔 Interacción del Usuario

👤 Usuario: Juan Pérez
📍 Módulo: Facturas
⚡ Acción: Crear

📊 Detalles:
• Fecha: 2024-01-15 14:30:45
• Sucursal: 1
• Estado: A

🌐 IP: 192.168.1.100
```

---

## 🎯 Nombres de Módulos en Base de Datos

⚠️ **IMPORTANTE:** Los nombres deben coincidir **EXACTAMENTE** con los usados en el código Angular:

| Módulo en Angular          | Nombre en BD            |
|---------------------------|------------------------|
| `'Utilidades'`            | `Utilidades`           |
| `'Equipos Vendidos'`      | `Equipos Vendidos`     |
| `'Equipos Disponibles'`   | `Equipos Disponibles`  |
| `'Facturas'`              | `Facturas`             |
| `'Accesorios'`            | `Accesorios`           |
| `'Registro Equipos'`      | `Registro Equipos`     |
| `'Registro Accesorios'`   | `Registro Accesorios`  |
| `'Dashboard IA'`          | `Dashboard IA`         |

---

## 📝 Acciones Disponibles por Módulo

### Utilidades
- `Acceso`
- `Consultar`
- `Exportar Excel`

### Equipos Vendidos
- `Acceso`
- `Consultar`
- `Filtrar`
- `Exportar Excel`

### Equipos Disponibles
- `Acceso`
- `Consultar`
- `Buscar IMEI`
- `Filtrar`
- `Exportar Excel`

### Facturas
- `Acceso`
- `Consultar`
- `Ver Detalle`
- `Crear`
- `Anular`
- `Exportar Excel`

### Accesorios
- `Acceso`
- `Consultar`
- `Filtrar`
- `Exportar Excel`

### Registro Equipos
- `Acceso`
- `Crear`
- `Editar`
- `Eliminar`
- `Guardar`

### Registro Accesorios
- `Acceso`
- `Crear`
- `Editar`
- `Eliminar`
- `Guardar`

---

## 🚀 Pasos para Completar la Implementación

1. ✅ **Ejecutar el script SQL**
   ```bash
   # En SQL Server Management Studio o Azure Data Studio
   # Abrir: Scripts/03_InsertTrackingConfig_AllModules.sql
   # Ejecutar en base de datos: SEGURIDAD
   ```

2. ✅ **Reiniciar el API .NET**
   ```bash
   # Detener la aplicación
   # Volver a compilar y ejecutar
   ```

3. ✅ **Navegar a los módulos en Angular**
   - Ir a Utilidades
   - Ir a Equipos Vendidos
   - Ir a Facturas
   - etc.

4. ✅ **Verificar en la base de datos**
   ```sql
   SELECT TOP 100 *
   FROM TrackingLogs
   ORDER BY FechaHora DESC
   ```

5. ✅ **Activar notificaciones que necesites**
   ```sql
   -- Ejemplo: Solo notificar creación de facturas
   UPDATE TrackingNotificacionConfig 
   SET NotificarWhatsApp = 1 
   WHERE Modulo = 'Facturas' AND Accion = 'Crear'
   ```

6. ✅ **Probar notificación**
   - Crear una factura
   - Esperar mensaje de WhatsApp en el teléfono 18293808751

---

## 🔍 Consultas Útiles

### Ver todas las configuraciones activas
```sql
SELECT 
    ISNULL(Modulo, '(Todos)') AS Modulo,
    ISNULL(Accion, '(Todas)') AS Accion,
    CASE WHEN NotificarWhatsApp = 1 THEN '✅ SÍ' ELSE '❌ NO' END AS Notifica,
    CASE WHEN Activo = 1 THEN '✅ Activa' ELSE '❌ Inactiva' END AS Estado
FROM TrackingNotificacionConfig
WHERE Activo = 1
ORDER BY Modulo, Accion
```

### Ver interacciones de hoy
```sql
SELECT 
    Usuario,
    Modulo,
    Accion,
    FechaHora,
    NotificacionEnviada
FROM TrackingLogs
WHERE CAST(FechaHora AS DATE) = CAST(GETDATE() AS DATE)
ORDER BY FechaHora DESC
```

### Ver módulos más usados
```sql
SELECT 
    Modulo,
    COUNT(*) AS TotalInteracciones
FROM TrackingLogs
GROUP BY Modulo
ORDER BY TotalInteracciones DESC
```

### Ver usuarios más activos
```sql
SELECT 
    Usuario,
    COUNT(*) AS TotalInteracciones
FROM TrackingLogs
GROUP BY Usuario
ORDER BY TotalInteracciones DESC
```

---

## 💡 Tips de Uso

1. **No recompilar el API:** Los cambios en `TrackingNotificacionConfig` aplican inmediatamente
2. **Usar NULL para comodines:** 
   - `Modulo = NULL` → aplica a todos los módulos
   - `Accion = NULL` → aplica a todas las acciones
3. **Prioridad de reglas:** Exactas > Módulo específico > Acción específica > Genéricas
4. **Desactivar temporalmente:** Cambiar `Activo = 0` en lugar de eliminar el registro

---

## 📞 Soporte

Si necesitas agregar tracking a otros módulos:

1. Inyectar `UserTrackingService` en el constructor
2. Agregar `trackPageView()` en `ngOnInit()`
3. Agregar `trackAction()` en métodos clave
4. Insertar configuración en `TrackingNotificacionConfig`

---

**Última actualización:** 2024-01-15  
**Versión:** 1.0  
**Autor:** Sistema de Tracking MVSystem
