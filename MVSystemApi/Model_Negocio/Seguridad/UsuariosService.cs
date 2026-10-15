using DTO;
using Microsoft.Data.SqlClient;
using MVSystemApi.Model;
using MVSystemApi.Model.Seguridad;
using System.Data;
using System.Security.Claims;

namespace MVSystemApi.Model_Negocio.Seguridad
{
    public class UsuariosService
    {
        private readonly string _connectionStringSeguridad;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UsuariosService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _connectionStringSeguridad = configuration.GetConnectionString("MVSystemSeguridad");
            _httpContextAccessor = httpContextAccessor;
        }

        private int ObtenerIdEmpresaActual()
        {
            var idEmpresaClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("IdEmpresa")?.Value;

            if (!int.TryParse(idEmpresaClaim, out var idEmpresa) || idEmpresa <= 0)
                throw new InvalidOperationException("No se encontró un IdEmpresa válido en el token");

            return idEmpresa;
        }

        private int ObtenerIdEmpresaEspejoActual()
        {
            var idEmpresaClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("IdEmpresaE")?.Value;

            if (!int.TryParse(idEmpresaClaim, out var idEmpresa) || idEmpresa < 0)
                throw new InvalidOperationException("No se encontró un IdEmpresaE válido en el token");

            return idEmpresa;
        }

        private async Task<string> ObtenerConnectionStringEmpresa()
        {
            var idEmpresa = ObtenerIdEmpresaActual();

            using var conn = new SqlConnection(_connectionStringSeguridad);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT StringCon FROM dbo.Empresas WHERE ID_Empresa = @IdEmpresa", conn);
            cmd.Parameters.AddWithValue("@IdEmpresa", idEmpresa);

            var result = await cmd.ExecuteScalarAsync();
            if (result == null)
                throw new Exception($"No se encontró connection string para la empresa {idEmpresa}");

            return result.ToString();
        }

        /// <summary>
        /// Obtiene todos los usuarios del sistema filtrados por empresa del usuario logueado
        /// Lee usuarios y roles de SEGURIDAD, filtra por sucursal del usuario logueado
        /// </summary>
        public async Task<List<UsuarioResponseDTO>> ObtenerTodosUsuarios()
        {
            var usuarios = new List<UsuarioResponseDTO>();
            
            // Obtener IdEmpresa del token
            var idEmpresa = ObtenerIdEmpresaActual();

            using var conn = new SqlConnection(_connectionStringSeguridad);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT u.Codigo, u.Nombre, u.Login, u.Descripcion, u.Perfil, p.Nombre as NombrePerfil,
                       u.Departamento, d.Nombre as NombreDepartamento, u.Estado, u.Id_Sucursal, u.Id_Empresa,
                       u.FechaRegistro, u.FechaModificado
                FROM [Usuarios] u
                LEFT JOIN seg.Perfiles p ON u.Perfil = p.Codigo
                LEFT JOIN seg.Departamentos d ON u.Departamento = d.Codigo
                WHERE u.Id_Empresa = @IdEmpresa
                ORDER BY u.Nombre", conn);
            
            cmd.Parameters.AddWithValue("@IdEmpresa", idEmpresa);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                usuarios.Add(new UsuarioResponseDTO
                {
                    Codigo = (int)reader["Codigo"],
                    Nombre = reader["Nombre"].ToString(),
                    Login = reader["Login"].ToString(),
                    Descripcion = reader["Descripcion"].ToString(),
                    Perfil = reader["Perfil"] == DBNull.Value ? null : (int)reader["Perfil"],
                    NombrePerfil = reader["NombrePerfil"].ToString(),
                    Departamento = reader["Departamento"] == DBNull.Value ? null : (int)reader["Departamento"],
                    NombreDepartamento = reader["NombreDepartamento"].ToString(),
                    Estado = reader["Estado"] == DBNull.Value ? null : (int)reader["Estado"],
                    Id_Empresa = reader["Id_Empresa"] == DBNull.Value ? null : (int)reader["Id_Empresa"],
                    Id_Sucursal = reader["Id_Sucursal"] == DBNull.Value ? null : (int)reader["Id_Sucursal"],
                    FechaRegistro = reader["FechaRegistro"] == DBNull.Value ? null : (DateTime)reader["FechaRegistro"],
                    FechaModificado = reader["FechaModificado"] == DBNull.Value ? null : (DateTime)reader["FechaModificado"]
                });
            }

            return usuarios;
        }

        /// <summary>
        /// Obtiene un usuario específico con sus permisos
        /// </summary>
        public async Task<UsuarioConPermisosDTO> ObtenerUsuarioPorCodigo(int codigoUsuario)
        {
            using var conn = new SqlConnection(_connectionStringSeguridad);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT u.Codigo, u.Nombre, u.Login, u.Estado, u.Perfil, p.Nombre as NombrePerfil
                FROM [Usuarios] u
                LEFT JOIN seg.Perfiles p ON u.Perfil = p.Codigo
                WHERE u.Codigo = @Codigo", conn);

            cmd.Parameters.AddWithValue("@Codigo", codigoUsuario);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            var usuario = new UsuarioConPermisosDTO
            {
                Codigo = (int)reader["Codigo"],
                Nombre = reader["Nombre"].ToString(),
                Login = reader["Login"].ToString(),
                Estado = reader["Estado"] == DBNull.Value ? null : (int)reader["Estado"],
                Perfil = reader["Perfil"] == DBNull.Value ? null : (int)reader["Perfil"],
                NombrePerfil = reader["NombrePerfil"].ToString()
            };

            await reader.CloseAsync();

            // Obtener permisos directos del usuario
            usuario.PermisosDirectosIds = await ObtenerPermisosDirectos(codigoUsuario, conn);
            usuario.PermisosDirectosNombres = await ObtenerNombresPermisos(usuario.PermisosDirectosIds, conn);

            // Obtener permisos heredados del perfil
            if (usuario.Perfil.HasValue)
            {
                usuario.PermisosHereadosIds = await ObtenerPermisosDelPerfil(usuario.Perfil.Value, conn);
                usuario.PermisosHereadosNombres = await ObtenerNombresPermisos(usuario.PermisosHereadosIds, conn);
            }

            return usuario;
        }

        /// <summary>
        /// Crea un nuevo usuario
        /// </summary>
        public async Task<int> CrearUsuario(CrearUsuarioDTO usuarioDTO)
        {
            var idEmpresa = ObtenerIdEmpresaActual();
            var idEmpresaEspejo = ObtenerIdEmpresaEspejoActual();

            // Validar que el login no exista
            if (await LoginExiste(usuarioDTO.Login))
                throw new Exception($"El login '{usuarioDTO.Login}' ya existe en el sistema");

            using var conn = new SqlConnection(_connectionStringSeguridad);
            await conn.OpenAsync();

            var passwordEncriptada = AES.Encrypt(usuarioDTO.Password);

            using var cmd = new SqlCommand(@"
                INSERT INTO [Usuarios] (Nombre, Login, Password, Departamento, Descripcion, Perfil, Id_Sucursal, Id_Empresa, IdEmpresaE, Estado, FechaRegistro)
                VALUES (@Nombre, @Login, @Password, @Departamento, @Descripcion, @Perfil, @Id_Sucursal, @Id_Empresa, @IdEmpresaE, @Estado, GETDATE());
                SELECT SCOPE_IDENTITY();", conn);

            cmd.Parameters.AddWithValue("@Nombre", usuarioDTO.Nombre);
            cmd.Parameters.AddWithValue("@Login", usuarioDTO.Login);
            cmd.Parameters.AddWithValue("@Password", passwordEncriptada);
            cmd.Parameters.AddWithValue("@Departamento", usuarioDTO.Departamento);
            cmd.Parameters.AddWithValue("@Descripcion", usuarioDTO.Descripcion ?? "");
            cmd.Parameters.AddWithValue("@Perfil", usuarioDTO.Perfil ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Id_Sucursal", usuarioDTO.Id_Sucursal ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Id_Empresa", idEmpresa);
            cmd.Parameters.AddWithValue("@IdEmpresaE", idEmpresaEspejo);
            cmd.Parameters.AddWithValue("@Estado", usuarioDTO.Estado ?? 1);

            var resultado = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(resultado);
        }

        /// <summary>
        /// Actualiza un usuario
        /// </summary>
        public async Task<bool> ActualizarUsuario(ActualizarUsuarioDTO usuarioDTO)
        {
            var idEmpresa = ObtenerIdEmpresaActual();

            using var conn = new SqlConnection(_connectionStringSeguridad);
            await conn.OpenAsync();

            // Si Estado no viene, conserva el valor actual; 0 es un estado válido (inactivo).
            string updateStatement = @"
                UPDATE [Usuarios]
                SET Nombre = @Nombre,
                    Descripcion = @Descripcion,
                    Perfil = @Perfil,
                    Departamento = @Departamento,
                    Id_Sucursal = @Id_Sucursal";

            if (usuarioDTO.Estado.HasValue)
            {
                updateStatement += ", Estado = @Estado";
            }

            updateStatement += ", FechaModificado = GETDATE() WHERE Codigo = @Codigo AND Id_Empresa = @IdEmpresa";

            using var cmd = new SqlCommand(updateStatement, conn);

            cmd.Parameters.AddWithValue("@Codigo", usuarioDTO.Codigo);
            cmd.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            cmd.Parameters.AddWithValue("@Nombre", usuarioDTO.Nombre ?? "");
            cmd.Parameters.AddWithValue("@Descripcion", usuarioDTO.Descripcion ?? "");
            cmd.Parameters.AddWithValue("@Perfil", usuarioDTO.Perfil ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Departamento", usuarioDTO.Departamento ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Id_Sucursal", usuarioDTO.Id_Sucursal ?? (object)DBNull.Value);

            if (usuarioDTO.Estado.HasValue)
            {
                cmd.Parameters.AddWithValue("@Estado", usuarioDTO.Estado);
            }

            var filasAfectadas = await cmd.ExecuteNonQueryAsync();
            return filasAfectadas > 0;
        }

        /// <summary>
        /// Cambia la contraseña de un usuario
        /// </summary>
        public async Task<bool> CambiarContrasena(CambioContrasenaDTO cambioDTO)
        {
            try
            {
                if (cambioDTO.ContrasenaNueva != cambioDTO.ConfirmarContrasena)
                    throw new Exception("Las contraseñas no coinciden");

                // Usar la conexión de SEGURIDAD directamente
                using var conn = new SqlConnection(_connectionStringSeguridad);
                await conn.OpenAsync();

                // Obtener contraseña actual
                using var cmd = new SqlCommand("SELECT Password FROM [Usuarios] WHERE Codigo = @Codigo", conn);
                cmd.Parameters.AddWithValue("@Codigo", cambioDTO.CodigoUsuario);

                var passwordEncriptadaObj = await cmd.ExecuteScalarAsync();
                
                if (passwordEncriptadaObj == null)
                    throw new Exception("Usuario no encontrado");
                
                var passwordDesencriptada = AES.Decrypt(passwordEncriptadaObj.ToString());
                if (cambioDTO.ContrasenaActual != passwordDesencriptada)
                    throw new Exception("La contraseña actual es incorrecta");

                var passwordNuevaEncriptada = AES.Encrypt(cambioDTO.ContrasenaNueva);

                // Actualizar contraseña en SEGURIDAD
                using var cmdUpdate = new SqlCommand(
                    "UPDATE [Usuarios] SET Password = @PasswordNueva, FechaModificado = GETDATE() WHERE Codigo = @Codigo", conn);

                cmdUpdate.Parameters.AddWithValue("@Codigo", cambioDTO.CodigoUsuario);
                cmdUpdate.Parameters.AddWithValue("@PasswordNueva", passwordNuevaEncriptada);

                var filasAfectadas = await cmdUpdate.ExecuteNonQueryAsync();
                return filasAfectadas > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al cambiar contraseña: {ex.Message}");
            }
        }

        /// <summary>
        /// Cambia la contraseña de un usuario (Admin)
        /// </summary>
        public async Task<bool> CambiarContrasenaAdmin(int codigoUsuario, string nuevaContrasena)
        {
            try
            {
                // Obtener el login del usuario desde SEGURIDAD
                using var connSeguridad = new SqlConnection(_connectionStringSeguridad);
                await connSeguridad.OpenAsync();

                using var cmdLogin = new SqlCommand("SELECT Login FROM [Usuarios] WHERE Codigo = @Codigo", connSeguridad);
                cmdLogin.Parameters.AddWithValue("@Codigo", codigoUsuario);
                var login = await cmdLogin.ExecuteScalarAsync();
                
                if (login == null)
                    throw new Exception("Usuario no encontrado en la base de datos SEGURIDAD");

                var passwordEncriptada = AES.Encrypt(nuevaContrasena);

                // Actualizar la contraseña en la base de datos SEGURIDAD
                using var cmdUpdate = new SqlCommand(
                    "UPDATE [Usuarios] SET Password = @Password, FechaModificado = GETDATE() WHERE Codigo = @Codigo", connSeguridad);

                cmdUpdate.Parameters.AddWithValue("@Codigo", codigoUsuario);
                cmdUpdate.Parameters.AddWithValue("@Password", passwordEncriptada);

                var filasAfectadas = await cmdUpdate.ExecuteNonQueryAsync();
                
                return filasAfectadas > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al cambiar contraseña del administrador: {ex.Message}");
            }
        }

        /// <summary>
        /// Desactiva un usuario
        /// </summary>
        public async Task<bool> DesactivarUsuario(int codigoUsuario)
        {
            var connectionString = await ObtenerConnectionStringEmpresa();

            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(
                "UPDATE [Usuarios] SET Estado = 0, FechaModificado = GETDATE() WHERE Codigo = @Codigo", conn);

            cmd.Parameters.AddWithValue("@Codigo", codigoUsuario);
            var filasAfectadas = await cmd.ExecuteNonQueryAsync();
            return filasAfectadas > 0;
        }

        /// <summary>
        /// Activa un usuario
        /// </summary>
        public async Task<bool> ActivarUsuario(int codigoUsuario)
        {
            var connectionString = await ObtenerConnectionStringEmpresa();

            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(
                "UPDATE [Usuarios] SET Estado = 1, FechaModificado = GETDATE() WHERE Codigo = @Codigo", conn);

            cmd.Parameters.AddWithValue("@Codigo", codigoUsuario);
            var filasAfectadas = await cmd.ExecuteNonQueryAsync();
            return filasAfectadas > 0;
        }

        /// <summary>
        /// Elimina completamente un usuario (y sus relaciones de permisos)
        /// </summary>
        public async Task<bool> EliminarUsuario(int codigoUsuario)
        {
            var connectionString = await ObtenerConnectionStringEmpresa();

            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();
            using var transaction = conn.BeginTransaction();

            try
            {
                // 1. Eliminar permisos del usuario
                using var cmdDeletePermisos = new SqlCommand(
                    "DELETE FROM seg.PermisosUsuario WHERE Usuario = @Codigo", conn, transaction);
                cmdDeletePermisos.Parameters.AddWithValue("@Codigo", codigoUsuario);
                await cmdDeletePermisos.ExecuteNonQueryAsync();

                // 2. Eliminar el usuario
                using var cmdDeleteUsuario = new SqlCommand(
                    "DELETE FROM [Usuarios] WHERE Codigo = @Codigo", conn, transaction);
                cmdDeleteUsuario.Parameters.AddWithValue("@Codigo", codigoUsuario);
                var filasAfectadas = await cmdDeleteUsuario.ExecuteNonQueryAsync();

                if (filasAfectadas == 0)
                {
                    transaction.Rollback();
                    return false;
                }

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception($"Error al eliminar usuario: {ex.Message}");
            }
        }

        // ===== Métodos privados =====

        private async Task<bool> LoginExiste(string login)
        {
            using var conn = new SqlConnection(_connectionStringSeguridad);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("SELECT COUNT(*) FROM [Usuarios] WHERE Login = @Login", conn);
            cmd.Parameters.AddWithValue("@Login", login);

            var resultado = await cmd.ExecuteScalarAsync();
            return (int)resultado > 0;
        }

        private async Task<List<int>> ObtenerPermisosDirectos(int codigoUsuario, SqlConnection conn)
        {
            var permisos = new List<int>();

            using var cmd = new SqlCommand(@"
                SELECT DISTINCT pm.Codigo
                FROM seg.PermisosUsuario pu
                INNER JOIN seg.Permisos pm ON pu.Permiso = pm.Codigo
                WHERE pu.Usuario = @UsuarioId AND pu.Estado = 1", conn);

            cmd.Parameters.AddWithValue("@UsuarioId", codigoUsuario);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                permisos.Add((int)reader["Codigo"]);
            }

            return permisos;
        }

        private async Task<List<int>> ObtenerPermisosDelPerfil(int codigoPerfil, SqlConnection conn)
        {
            var permisos = new List<int>();

            using var cmd = new SqlCommand(@"
                SELECT DISTINCT pm.Codigo
                FROM seg.PermisosPerfiles pp
                INNER JOIN seg.Permisos pm ON pp.Permiso = pm.Codigo
                WHERE pp.Perfil = @PerfilId AND pp.Estado = 1", conn);

            cmd.Parameters.AddWithValue("@PerfilId", codigoPerfil);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                permisos.Add((int)reader["Codigo"]);
            }

            return permisos;
        }

        private async Task<List<string>> ObtenerNombresPermisos(List<int> permisosIds, SqlConnection conn)
        {
            var nombres = new List<string>();

            if (permisosIds.Count == 0)
                return nombres;

            var ids = string.Join(",", permisosIds);

            using var cmd = new SqlCommand($"SELECT Nombre FROM seg.Permisos WHERE Codigo IN ({ids})", conn);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                nombres.Add(reader["Nombre"].ToString());
            }

            return nombres;
        }
    }
}

