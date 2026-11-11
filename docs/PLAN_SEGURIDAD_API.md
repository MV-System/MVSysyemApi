# Plan – Seguridad del API

Plan independiente. Rama de trabajo: la que indique el usuario (no tocar `ventura` ni producción sin su autorización).
Revisión hecha sobre `ventura` el 10/11/2026. **Se trabaja una parte a la vez; entregar y esperar aprobación antes de seguir.**

## Hallazgos (resumen)
| # | Hallazgo | Prioridad |
|---|---|---|
| 1 | Secretos dentro de `MVSystemApi/appsettings.json` en el repositorio (llave JWT, API keys de EasyConexion prod y dev, cadenas de conexión de SEGURIDAD y MicroStar); 11 commits de historial | Alta |
| 2 | Los permisos solo se validan en el front: 47 `[Authorize(Roles=...)]` comentados; cualquier usuario con sesión puede llamar cualquier endpoint | Alta |
| 3 | Un usuario desactivado o desconectado sigue entrando hasta que vence su token (medianoche) | Alta |
| 4 | CORS abierto: `WithOrigins("*")` | Media |
| 5 | 188 lugares devuelven `ex.Message` al cliente | Media |
| 6 | Login sin límite de intentos | Media |
| 7 | 4 consultas SQL armadas con texto (hoy seguras: solo números o nombres de parámetros) | Baja |
| 8 | Swagger solo en Development (correcto); confirmar que producción no corre como Development | Baja |

Lo que ya está bien: todos los endpoints exigen token (`MapControllers().RequireAuthorization()`); el JWT valida emisor, audiencia, firma y vencimiento; los permisos del usuario ya viajan en el token como claims `role`.

---

## Parte 1 – Sacar los secretos del repositorio (Alta)
**Lo hace el usuario en Azure; el agente solo prepara el código.** No escribir ni mostrar ningún secreto en commits, logs ni mensajes.
1. **Código:** dejar en `appsettings.json` solo las llaves vacías (`""`) de: `JWT:Key`, `ConnectionStrings:*` con contraseña, `FacEDGI:ApiKey`, `FacEDGI:ApiKey_Dev`, `Monitor:Correo:Password` y cualquier otro secreto que aparezca. El valor real se lee de la configuración del App Service (`JWT__Key`, `ConnectionStrings__MVSystemSeguridad`, `FacEDGI__ApiKey`, …), que .NET ya sobreescribe sin cambios de código.
2. **Al arrancar:** si `JWT:Key` está vacía o tiene menos de 32 caracteres, el API **no arranca** y lo registra (evita firmar tokens con una llave vacía).
3. **Desarrollo local:** usar `dotnet user-secrets` o un `appsettings.Development.json` **fuera de git** (agregarlo a `.gitignore`; ya se excluye de la publicación).
4. **Lista para el usuario** (en la entrega): cada variable a crear en el App Service, sin valores.
5. **Rotación (la hace el usuario, después de publicar):** nueva llave JWT (todos vuelven a iniciar sesión), nuevas API keys en EasyConexion, nueva contraseña del usuario SQL de SEGURIDAD y del de MicroStar. Hasta rotarlas, las actuales siguen expuestas en el historial de git.
- Aceptación: (a) `appsettings.json` sin ningún secreto; (b) con las variables en el App Service el API funciona igual (login, facturación electrónica de ambos proveedores, monitor); (c) sin `JWT:Key` el API no arranca y lo dice en el log; (d) compila.

## Parte 2 – Validar permisos en el API (Alta)
**Estado (10/11/2026): Grupo A escrito por Claude en la copia local (sin commit). Falta: compilar, probar y commit.**

**Corrección al plan:** `[Authorize(Roles = "...")]` **no sirve** en este API: pasa por `RolesAuthorizationHandler` → `RoleService.GetPermisos()`, que consulta el modelo EF viejo (`Usuarios.RolUsuarios/PermisoUsuarios`) por login, no las tablas `seg.*` actuales (probablemente por eso se comentaron los 47). Se usa un filtro nuevo, **`[RequierePermiso(...)]`** (`MVSystemApi/Handlers/RequierePermisoAttribute.cs`), que lee los permisos del **token** (claims `role`, igual que `LaptopsController.TienePermiso`). Basta con tener **uno** de los permisos listados. Sin permiso → 403 `{ error: "No tiene permiso para esta acción." }` (el front ya muestra ese mensaje).

### Grupo A – hecho (revisar)
| Endpoint | Permiso(s) | Por qué |
|---|---|---|
| `GET api/Usuarios` | VerUsuarios, AsignarPermisos, LAPTOP_CAMBIO_PIEZA, LAPTOP_CUSTODIA_CONSULTA, LAPTOP_CUSTODIA_RECIBIR | también lo usan Asignaciones y Custodia (lista de técnicos) |
| `GET/POST/PUT/DELETE api/Usuarios/{codigo}`, `reset-contrasena`, `activar`, `eliminar` | VerUsuarios | pantalla Usuarios |
| `POST api/Usuarios/cambiar-contrasena` | (sesión) | cambia la propia clave; **se quitó el log que guardaba la contraseña actual y la nueva** |
| `POST login/ChangePassword` | (sesión) | **corregido:** el código del usuario sale del token; antes cualquiera podía cambiar la clave de otro enviando su código |
| `api/Roles/*` | VerRoles | |
| `GET api/Permisos/*` | VerPermisos, AsignarPermisos, VerUsuarios, VerRoles | lo usan Permisos, Asignaciones y Usuarios |
| `POST/PUT/DELETE api/Permisos` | VerPermisos | |
| `api/Perfiles/*` | VerUsuarios, AsignarPermisos | |
| `GET api/Departamentos/*` | VerDepartamentos, VerUsuarios | Usuarios carga el combo |
| `POST/PUT/DELETE api/Departamentos` | VerDepartamentos | |
| `GET api/Modulos/*` | VerModulos, VerPermisos | Permisos carga el combo |
| `POST/PUT/DELETE api/Modulos` | VerModulos | |
| `GET api/Sistemas/*` | VerSistemas, VerModulos | |
| `POST/PUT/DELETE api/Sistemas` | VerSistemas | |
| `api/Asignaciones/*` | AsignarPermisos | |
| `api/UsoSistema/Conectados, Resumen, Detalle, Empresas, PrimerRegistro` | ModoDesarrollador | `Registrar` y `Salir` quedan para todos (tracking) |
| `Factura/PostAnulaFactura` | VNT_DETALLE_FACTURA_BTN_ANULAR | el comentado decía MNU_MANT_FACTURACION, que **no** es el del botón |
| `Factura/EditaDetalleFactura` (cambiar IMEI) | VNT_DETALLE_FACTURA_BTN_CAMBIA_IMEI | |
| `Factura/DetalleFacturaRebajaEquipo` | VNT_DETALLE_FACTURA_BTN_REBAJA | |
| `NotaCredito/PostNotaCredito`, `PostNotaCreditoProveedor2` | VNT_DETALLE_FACTURA_BTN_NOTA_CREDITO | `ReportNotaCredito` (reimprimir) queda sin permiso, como en el front |
| `Utilidades/*` | verUtilidadesToolStripMenuItem | |
| `api/Laptops/*`, `api/Avisos` (admin), movimientos de accesorios | ya validaban en código | sin cambios |
- **Front:** `dashboard.component.ts` ya no consulta Utilidades si el usuario no tiene el permiso (antes lo hacía siempre aunque el panel estuviera oculto; con el API nuevo saldría "No tiene permisos" al abrir el inicio).
- **Lo que debe hacer el agente:** compilar API y front; probar con un usuario **con** y otro **sin** cada permiso; confirmar en `seg.Permisos` que existen todos los nombres de la tabla (consulta abajo); commit en la rama que indique el usuario.
  ```sql
  SELECT Nombre FROM seg.Permisos WHERE Nombre IN ('VerUsuarios','VerRoles','VerPermisos','VerDepartamentos','VerModulos','VerSistemas','AsignarPermisos','ModoDesarrollador','LAPTOP_CAMBIO_PIEZA','LAPTOP_CUSTODIA_CONSULTA','LAPTOP_CUSTODIA_RECIBIR','VNT_DETALLE_FACTURA_BTN_ANULAR','VNT_DETALLE_FACTURA_BTN_NOTA_CREDITO','VNT_DETALLE_FACTURA_BTN_CAMBIA_IMEI','VNT_DETALLE_FACTURA_BTN_REBAJA','verUtilidadesToolStripMenuItem');  -- deben salir 16
  ```
- **Visto de paso (no se tocó):** el front hace `PUT api/roles` sin el id y el API espera `PUT api/roles/{rolId}`; editar un rol probablemente ya falla hoy. Las contraseñas se guardan cifradas con AES (reversible), no con hash; queda para otra tarea.

### Grupo B – pendiente
Los 47 `[Authorize(Roles=...)]` comentados (Equipos 13, Inventario 9, Clientes 8, Facturación 8, otros 9): **no descomentarlos**; reemplazarlos por `[RequierePermiso(...)]` con el permiso que usa el front para esa pantalla o botón, uno por uno, verificando el nombre en `seg.Permisos`. Entregar la misma tabla endpoint → permiso antes de activar.
- Aceptación (A y B): (a) usuario sin el permiso que llama directo al endpoint → 403; (b) usuario con el permiso → funciona igual que hoy; (c) ninguna pantalla que hoy funciona para un perfil deja de funcionar; (d) compila.

## Parte 3 – Cortar el acceso al instante (Alta)
Es el pendiente "Desconectar usuario" (PENDIENTES_LAPTOPS):
1. Columna `TokensValidosDesde` (datetime, NULL) en `seg.Usuarios` de SEGURIDAD.
2. Al validar cada token (`JwtBearerEvents.OnTokenValidated`): si el usuario está **inactivo** o el token se emitió **antes** de `TokensValidosDesde` → 401. Caché de ~30 s por usuario para no consultar en cada petición.
3. "Desconectar" en Uso del sistema y desactivar un usuario ponen `TokensValidosDesde = ahora`. Permiso `USO_SISTEMA_DESCONECTAR`.
4. El front ya maneja el 401 (borra la sesión y manda al login); mensaje "Su sesión fue cerrada por el administrador".
- Aceptación: (a) desactivar un usuario → pierde el acceso en ≤ 30 s en todos sus equipos; (b) desconectar → igual, y puede volver a entrar; (c) los demás usuarios no se afectan; (d) compila.

## Parte 4 – CORS, errores y login (Media)
1. **CORS:** reemplazar `WithOrigins("*")` por la lista de dominios del front, leída de configuración (`Cors:Origenes`). En Development se permite `localhost`.
2. **Errores:** un manejador global que registre la excepción completa en el log y devuelva `{ error: "Ocurrió un error. Intente de nuevo o contacte a soporte.", id: <código> }`. Ir reemplazando `return BadRequest(new { error = ex.Message })` por ese manejador. **Excepción:** los mensajes de negocio que el usuario debe ver (THROW 50xxx de los SP, validaciones de DGII, "Esta factura ya se está procesando", etc.) se siguen mostrando tal cual.
3. **Login:** límite de intentos (p. ej. 5 fallidos por usuario y por IP en 15 min → espera de 15 min) con el rate limiter de ASP.NET o un contador en memoria. Mensaje: "Demasiados intentos. Intente en unos minutos."
- Aceptación: (a) el front publicado funciona; una página de otro dominio no puede llamar al API; (b) un error inesperado no muestra detalles internos y queda en el log con su id; (c) 6.º intento fallido → bloqueado; el login correcto después del tiempo funciona; (d) compila.

## Parte 5 – Limpieza (Baja)
1. Pasar a parámetros las 4 consultas armadas con texto: `AccesoDatos.cs` (UPDATE de Equipos con `Disponible` y el `IN` de IMEI), `UsuariosService.ObtenerNombresPermisos` (`IN ({ids})`), `RevisorSincronizacion.cs` (`IN` de IMEI).
2. Confirmar en el App Service que `ASPNETCORE_ENVIRONMENT` no sea `Development` (si lo es, Swagger y la página de errores detallados quedan públicos).
- Aceptación: mismas respuestas que hoy; compila.

---

## Entrega de cada parte
1. Archivos creados/modificados (rutas).
2. Scripts SQL a ejecutar y dónde (SEGURIDAD, o principal y espejo).
3. Variables de configuración nuevas (solo nombres, nunca valores).
4. Cómo probarla (pasos concretos).
5. Supuestos o dudas.
6. **No continuar** con la siguiente parte sin aprobación.
