# Módulo de Seguridad - MVSystemApi
## Documentación Completa

### 🎯 Objetivo General
Crear un módulo de seguridad completo para MVSystemApi con funcionalidad de:
- ✅ Gestión de Usuarios (CRUD, autenticación, cambio de contraseña)
- ✅ Gestión de Permisos (CRUD, asignación)
- ✅ Gestión de Perfiles/Roles (CRUD, asignación de permisos)
- ✅ Asignación de Permisos a Usuarios (directos e heredados)
- ✅ Gestión de Departamentos y Módulos (tablas de soporte)
- ✅ Gestión de Sistemas

---

## 📋 Estructura de Base de Datos (seg schema)

### Tablas Utilizadas:
1. **seg.Usuarios**
   - Codigo (PK)
   - Nombre, Login, Password (encrypted)
   - Perfil (FK → seg.Perfiles.Codigo) - nullable
   - Departamento (FK → seg.Departamentos.Codigo)
   - Estado

2. **seg.Perfiles**
   - Codigo (PK)
   - Nombre, Descripcion
   - Sistema (FK → seg.Sistemas.Codigo)
   - Estado

3. **seg.Permisos**
   - Codigo (PK)
   - Nombre, Descripcion
   - Modulo (FK → seg.Modulos.Codigo)
   - Basico (nchar)
   - Estado

4. **seg.PermisosPerfiles** (Junction: Perfil → Permisos)
   - Perfil (FK)
   - Permiso (FK)
   - Estado

5. **seg.PermisosUsuario** (Junction: Usuario → Permisos directos)
   - Usuario (FK)
   - Permiso (FK)
   - Estado

6. **seg.Modulos**
   - Codigo (PK)
   - Nombre
   - Sistema (FK → seg.Sistemas.Codigo)
   - Estado

7. **seg.Sistemas**
   - Codigo (PK)
   - Nombre, Descripcion
   - Estado

8. **seg.Departamentos**
   - Codigo (PK)
   - Nombre, Descripcion
   - Estado

9. **seg.Estados** (Tabla de referencia)
   - Codigo (PK)
   - Nombre

---

## 🏗️ Arquitectura Implementada

### DTOs (c:\mvsystem\MVSystemApi\DTO\)

#### Usuario
- `CrearUsuarioDTO`: Login, Nombre, Contraseña, Perfil, Departamento
- `ActualizarUsuarioDTO`: Nombre, Perfil, Departamento, Estado
- `CambioContrasenaDTO`: CodigoUsuario, ContraseñaActual, ContraseñaNueva
- `UsuarioResponseDTO`: Datos de respuesta con Perfil y Departamento
- `UsuarioConPermisosDTO`: Incluye permisos directos e heredados

#### Permiso
- `CrearPermisoDTO`: Nombre, Descripcion, Modulo
- `ActualizarPermisoDTO`: Nombre, Descripcion, Modulo, Estado
- `PermisoResponseDTO`: Incluye información del módulo

#### Perfil
- `CrearPerfilDTO`: Nombre, Descripcion, Sistema
- `ActualizarPerfilDTO`: Nombre, Descripcion, Estado
- `PerfilResponseDTO`: Incluye lista de permisos asignados

#### Asignación
- `AsignacionPermisoDTO`: Usuario, Lista de Permisos
- `AsignacionPerfilDTO`: Usuario, Perfil
- `DesasignacionPermisoDTO`: Usuario, Permiso

#### Apoyo
- `DepartamentoDTO`: Codigo, Nombre, Descripcion
- `ModuloDTO`: Codigo, Nombre, Sistema, NombreSistema
- `SistemaResponseDTO`: Codigo, Nombre, Descripcion

---

## 🔧 Servicios Implementados

### 1. UsuariosService
**Ubicación**: `MVSystemApi\Model_Negocio\Seguridad\UsuariosService.cs`

```csharp
// Métodos principales
ObtenerTodosUsuarios()                          // Retorna lista de usuarios con perfiles
ObtenerUsuarioPorCodigo(int codigo)            // Usuario con permisos
CrearUsuario(CrearUsuarioDTO dto)              // Valida login único, encripta contraseña
ActualizarUsuario(ActualizarUsuarioDTO dto)    // Actualiza datos del usuario
CambiarContrasena(CambioContrasenaDTO dto)     // Valida contraseña actual
CambiarContrasenaAdmin(int codigo, string nueva) // Admin override
DesactivarUsuario(int codigo)                  // Estado = 0
ActivarUsuario(int codigo)                     // Estado = 1
ObtenerPermisosDirectos(int codigo)            // De seg.PermisosUsuario
ObtenerPermisosDelPerfil(int codigo)           // De seg.PermisosPerfiles
ObtenerNombresPermisos(List<int> permisos)     // Traduce códigos a nombres
```

**Características**:
- Encriptación AES para contraseñas
- Validación de login duplicado
- Retorna DTO con Perfil y Departamento resueltos
- Manejo de valores null para campos opcionales

---

### 2. PermisosService
**Ubicación**: `MVSystemApi\Model_Negocio\Seguridad\PermisosService.cs`

```csharp
ObtenerTodosPermisos()                    // Todos los permisos con módulo
ObtenerPermisosPorModulo(int codigoModulo) // Permisos de un módulo específico
ObtenerPermisoPorCodigo(int codigo)       // Detalle de un permiso
CrearPermiso(CrearPermisoDTO dto)         // Valida nombre único
ActualizarPermiso(ActualizarPermisoDTO dto) // Actualiza datos
EliminarPermiso(int codigo)               // Cascada: elimina de PermisosPerfiles y PermisosUsuario
```

**Características**:
- Validación de nombre único
- Joins automáticos con seg.Modulos para obtener NombreModulo
- Eliminación en cascada para mantener integridad referencial

---

### 3. PerfilesService (anteriormente RolesService)
**Ubicación**: `MVSystemApi\Model_Negocio\Seguridad\PerfilesService.cs`

```csharp
ObtenerTodosPerfiles()                    // Todos los perfiles con permisos
ObtenerPerfilPorCodigo(int codigo)        // Perfil con lista de permisos
CrearPerfil(CrearPerfilDTO dto)           // Crea bajo un sistema específico
ActualizarPerfil(ActualizarPerfilDTO dto) // Actualiza nombre y estado
AsignarPermisosAlPerfil(int codigoPerfil, List<int> permisosIds)
                                          // Reemplaza permisos del perfil
EliminarPerfil(int codigo)                // Cascada: elimina PermisosPerfiles
```

**Características**:
- Asocia perfiles a sistemas
- Asignación de permisos sin duplicados
- Retorna permisos como lista en la respuesta

---

### 4. AsignacionesService
**Ubicación**: `MVSystemApi\Model_Negocio\Seguridad\AsignacionesService.cs`

```csharp
// Asignación directa
AsignarPermisosUsuario(AsignacionPermisoDTO dto)      // Inserta en PermisosUsuario
AsignarPerfilUsuario(AsignacionPerfilDTO dto)         // Actualiza Usuarios.Perfil

// Obtención de permisos
ObtenerPermisosEfectivos(int codigo)     // UNION: Directos + Heredados
ObtenerPermisosDirectos(int codigo)      // Solo de PermisosUsuario
ObtenerPermisosHeredados(int codigo)     // Solo de PermisosPerfiles (via Perfil)

// Verificación
UsuarioTienePermiso(int codigo, int permisoId) // Boolean check
ObtenerPerfilUsuario(int codigo)         // Retorna perfil con sus permisos

// Desasignación
DesasignarPermisoUsuario(DesasignacionPermisoDTO dto)
DesasignarPerfilUsuario(DesasignacionPerfilDTO dto)
```

**Características**:
- ✅ **Modelo de herencia de permisos**: Usuario tiene 1 Perfil → Perfil tiene muchos Permisos
- ✅ **Permisos directos**: Asignados directamente al usuario (PermisosUsuario)
- ✅ **Permisos heredados**: Vienen del Perfil asignado (PermisosPerfiles)
- ✅ **Permisos efectivos**: UNION de directos + heredados
- Query DISTINCT para evitar duplicados cuando tiene ambos tipos

---

### 5. DepartamentosService
**Ubicación**: `MVSystemApi\Model_Negocio\Seguridad\DepartamentosService.cs`

```csharp
ObtenerTodos()                               // Todos los departamentos
ObtenerPorCodigo(int codigo)                 // Detalle de departamento
Crear(CrearDepartamentoDTO dto)             // Valida nombre único
Actualizar(ActualizarDepartamentoDTO dto)   // Actualiza datos
Eliminar(int codigo)                         // Elimina departamento
```

---

### 6. ModulosService
**Ubicación**: `MVSystemApi\Model_Negocio\Seguridad\ModulosService.cs`

```csharp
ObtenerTodos()                           // Todos los módulos
ObtenerPorCodigo(int codigo)             // Detalle del módulo
ObtenerPorSistema(int codigoSistema)     // Módulos de un sistema
Crear(CrearModuloDTO dto)                // Módulo bajo un sistema
Actualizar(ActualizarModuloDTO dto)      // Actualiza datos
Eliminar(int codigo)                     // Cascada: elimina Permisos asociados
```

---

### 7. SistemasService
**Ubicación**: `MVSystemApi\Model_Negocio\Seguridad\SistemasService.cs`

```csharp
ObtenerTodos()                       // Todos los sistemas
ObtenerPorCodigo(int codigo)         // Detalle del sistema
Crear(CrearSistemaDTO dto)           // Nuevo sistema
Actualizar(ActualizarSistemaDTO dto) // Actualiza datos
Eliminar(int codigo)                 // Elimina sistema
```

---

## 🎮 Controladores (API Endpoints)

### UsuariosController
```
GET    /api/usuarios                     - Obtener todos
GET    /api/usuarios/{codigo}            - Obtener por ID
POST   /api/usuarios                     - Crear usuario
PUT    /api/usuarios/{codigo}            - Actualizar usuario
DELETE /api/usuarios/{codigo}            - Eliminar usuario
POST   /api/usuarios/cambiar-contrasena  - Cambiar contraseña
POST   /api/usuarios/admin-cambio        - Admin reset de contraseña
```

### PermisosController
```
GET    /api/permisos                     - Obtener todos
GET    /api/permisos/{codigo}            - Obtener por ID
GET    /api/permisos/por-modulo/{codigoModulo} - Permisos de módulo
POST   /api/permisos                     - Crear permiso
PUT    /api/permisos/{codigo}            - Actualizar permiso
DELETE /api/permisos/{codigo}            - Eliminar permiso
```

### RolesController (o PerfilesController)
```
GET    /api/roles                        - Obtener todos
GET    /api/roles/{codigo}               - Obtener por ID
POST   /api/roles                        - Crear perfil
PUT    /api/roles/{codigo}               - Actualizar perfil
POST   /api/roles/{codigo}/permisos      - Asignar permisos
DELETE /api/roles/{codigo}               - Eliminar perfil
```

### AsignacionesController
```
POST   /api/asignaciones/usuario-permisos   - Asignar permisos directos
POST   /api/asignaciones/usuario-perfil     - Asignar perfil
DELETE /api/asignaciones/usuario-permiso    - Desasignar permiso
GET    /api/asignaciones/{codigo}/efectivos - Permisos efectivos
GET    /api/asignaciones/{codigo}/directos  - Permisos directos
GET    /api/asignaciones/{codigo}/heredados - Permisos heredados
POST   /api/asignaciones/{codigo}/verifica  - Verificar permiso
```

### DepartamentosController
```
GET    /api/departamentos                - Obtener todos
GET    /api/departamentos/{codigo}       - Obtener por ID
POST   /api/departamentos                - Crear departamento
PUT    /api/departamentos/{codigo}       - Actualizar
DELETE /api/departamentos/{codigo}       - Eliminar
```

### ModulosController
```
GET    /api/modulos                      - Obtener todos
GET    /api/modulos/{codigo}             - Obtener por ID
GET    /api/modulos/por-sistema/{codigoSistema} - Módulos de sistema
POST   /api/modulos                      - Crear módulo
PUT    /api/modulos/{codigo}             - Actualizar módulo
DELETE /api/modulos/{codigo}             - Eliminar módulo
```

### SistemasController
```
GET    /api/sistemas                     - Obtener todos
GET    /api/sistemas/{codigo}            - Obtener por ID
POST   /api/sistemas                     - Crear sistema
PUT    /api/sistemas/{codigo}            - Actualizar sistema
DELETE /api/sistemas/{codigo}            - Eliminar sistema
```

---

## 🔐 Seguridad

### Autenticación
- JWT Bearer Token (configurado en Program.cs)
- Todos los endpoints requieren `[Authorize]`

### Encriptación de Contraseñas
- Método AES de clase `AES` (disponible en el proyecto)
- Almacenadas encriptadas en base de datos
- Desencriptadas solo en memoria para validación

### Validaciones
- Login único para usuarios
- Nombre único para permisos, perfiles, departamentos
- Validación de contraseña actual en cambios de usuario
- Estado validado (activo/inactivo)

---

## 📦 Configuración en Program.cs

```csharp
// Servicios de Seguridad registrados
builder.Services.AddScoped<UsuariosService>();
builder.Services.AddScoped<PermisosService>();
builder.Services.AddScoped<RolesService>();        // Mismo que PerfilesService
builder.Services.AddScoped<AsignacionesService>();
builder.Services.AddScoped<DepartamentosService>();
builder.Services.AddScoped<ModulosService>();
builder.Services.AddScoped<SistemasService>();

// Connection String
builder.Services.AddDbContext<SEGURIDADContext>(opt => 
    opt.UseSqlServer(builder.Configuration.GetConnectionString("MVSystemSeguridad")));

// Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(...)
```

---

## 🧪 Ejemplo de Flujo Completo

### 1. Crear Sistema
```
POST /api/sistemas
{
  "nombre": "Facturación",
  "descripcion": "Sistema de Facturación"
}
```

### 2. Crear Módulos del Sistema
```
POST /api/modulos
{
  "nombre": "Clientes",
  "sistema": 1
}
```

### 3. Crear Permisos del Módulo
```
POST /api/permisos
{
  "nombre": "Crear Cliente",
  "descripcion": "Permite crear nuevos clientes",
  "modulo": 1
}
```

### 4. Crear Perfil (Rol)
```
POST /api/roles
{
  "nombre": "Gerente de Ventas",
  "descripcion": "Rol para gerentes",
  "sistema": 1
}
```

### 5. Asignar Permisos al Perfil
```
POST /api/roles/1/permisos
{
  "permisosIds": [1, 2, 3]
}
```

### 6. Crear Departamento
```
POST /api/departamentos
{
  "nombre": "Ventas",
  "descripcion": "Departamento de Ventas"
}
```

### 7. Crear Usuario
```
POST /api/usuarios
{
  "login": "juan.perez",
  "nombre": "Juan Pérez",
  "password": "SecurePassword123",
  "perfil": 1,
  "departamento": 1
}
```

### 8. Asignar Perfil a Usuario
```
POST /api/asignaciones/usuario-perfil
{
  "usuario": 1,
  "perfil": 1
}
```

### 9. Obtener Permisos Efectivos del Usuario
```
GET /api/asignaciones/1/efectivos
Respuesta: [ { codigo: 1, nombre: "Crear Cliente" }, ... ]
```

### 10. Verificar Permiso Específico
```
POST /api/asignaciones/1/verifica
{
  "permisoId": 1
}
Respuesta: { tienePermiso: true }
```

---

## ✅ Estado del Desarrollo

### Completado
- ✅ 7 Servicios de negocio implementados
- ✅ 7 DTOs creados y actualizados
- ✅ 7 Controladores creados
- ✅ Registración de servicios en Program.cs
- ✅ Encriptación de contraseñas
- ✅ Modelo de permisos (directos + heredados)
- ✅ Validaciones en cascada
- ✅ Documentación XML en endpoints
- ✅ Proyecto compila sin errores

### Listo para
- 🚀 Testing en Postman/Thunder Client
- 🚀 Integración con frontend Angular
- 🚀 Validaciones adicionales de negocio
- 🚀 Logs y auditoría de cambios

---

## 🔗 Relaciones de Tablas

```
Usuarios (1) ──Perfil──> (1) Perfiles
    │                           │
    │                           │ (M)
    │ (M)                       │
    └─PermisosUsuario ─┐    PermisosPerfiles
                       │        │
                       └─(M)─ Permisos ─(1) Modulos ─(1) Sistemas
                                │
                                └─(M) Usuarios (Departamento) ← Departamentos
```

---

## 📝 Notas Importantes

1. **Herencia de Permisos**: Un usuario obtiene permisos de dos fuentes:
   - Directos: Asignados específicamente en PermisosUsuario
   - Heredados: Del Perfil asignado en Usuarios.Perfil

2. **Cascada de Eliminación**: Se implementó eliminación en cascada para:
   - Permisos: Elimina registros en PermisosPerfiles y PermisosUsuario
   - Perfiles: Elimina registros en PermisosPerfiles
   - Módulos: Elimina Permisos asociados

3. **Validaciones Únicas**: 
   - Login de usuarios debe ser único
   - Nombre de permisos debe ser único
   - Nombre de perfiles debe ser único por sistema

4. **Encriptación**: Las contraseñas se almacenan encriptadas en BD y se comparan desencriptadas en memoria.

---

**Fecha de Implementación**: 2024
**Framework**: ASP.NET Core
**Database**: SQL Server
**Schema**: seg (seguridad)
