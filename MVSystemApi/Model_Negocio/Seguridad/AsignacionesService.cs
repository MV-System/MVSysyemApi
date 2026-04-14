using DTO;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace MVSystemApi.Model_Negocio.Seguridad
{
    public class AsignacionesService
    {
        private readonly string _connectionStringSeguridad;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AsignacionesService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _connectionStringSeguridad = configuration.GetConnectionString("MVSystemSeguridad");
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Modifica los permisos de un usuario (agrega y elimina según sea necesario)
        /// </summary>
        public async Task<bool> ModificarPermisosUsuario(AsignacionPermisoDTO asignacionDTO)
        {
            using var conn = new SqlConnection(_connectionStringSeguridad);
            await conn.OpenAsync();

            // Obtener permisos actuales del usuario (incluyendo los desactivados para mantener consistencia)
            var permisosActuales = new List<int>();
            using (var cmdActuales = new SqlCommand(
                "SELECT Permiso FROM seg.PermisosUsuario WHERE Usuario = @Usuario", conn))
            {
                cmdActuales.Parameters.AddWithValue("@Usuario", asignacionDTO.CodigoUsuario);
                using var reader = await cmdActuales.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    permisosActuales.Add((int)reader["Permiso"]);
                }
            }

            // Permisos a eliminar (están en actuales pero no en nuevos)
            var permisosAEliminar = permisosActuales.Where(p => !asignacionDTO.PermisosIds.Contains(p)).ToList();
            
            // Permisos a agregar (están en nuevos pero no en actuales)
            var permisosAAsignar = asignacionDTO.PermisosIds.Where(p => !permisosActuales.Contains(p)).ToList();

            // Eliminar permisos
            foreach (var permisoId in permisosAEliminar)
            {
                using var cmdDelete = new SqlCommand(
                    "DELETE FROM seg.PermisosUsuario WHERE Usuario = @Usuario AND Permiso = @Permiso", conn);
                cmdDelete.Parameters.AddWithValue("@Usuario", asignacionDTO.CodigoUsuario);
                cmdDelete.Parameters.AddWithValue("@Permiso", permisoId);
                await cmdDelete.ExecuteNonQueryAsync();
            }

            // Agregar permisos
            foreach (var permisoId in permisosAAsignar)
            {
                using var cmdInsert = new SqlCommand(
                    "INSERT INTO seg.PermisosUsuario (Usuario, Permiso, Estado, FechaRegistro) VALUES (@Usuario, @Permiso, 1, GETDATE())", conn);
                cmdInsert.Parameters.AddWithValue("@Usuario", asignacionDTO.CodigoUsuario);
                cmdInsert.Parameters.AddWithValue("@Permiso", permisoId);
                await cmdInsert.ExecuteNonQueryAsync();
            }

            return true;
        }

        /// <summary>
        /// Obtiene el perfil del usuario logueado desde el token JWT
        /// </summary>
        private int ObtenerPerfilUsuarioLogueado()
        {
            var perfilClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("Perfil")?.Value;
            return !string.IsNullOrEmpty(perfilClaim) && int.TryParse(perfilClaim, out var perfil) ? perfil : 0;
        }

        /// <summary>
        /// Obtiene información del permiso incluyendo si es básico
        /// </summary>
        private async Task<(bool Existe, bool? EsBasico)> ObtenerInfoPermiso(int codigoPermiso, SqlConnection conn)
        {
            using var cmd = new SqlCommand("SELECT Basico FROM seg.Permisos WHERE Codigo = @Codigo", conn);
            cmd.Parameters.AddWithValue("@Codigo", codigoPermiso);
            
            var result = await cmd.ExecuteScalarAsync();
            if (result == null)
                return (false, null);
            
            bool? esBasico = result == DBNull.Value ? null : (bool?)(Convert.ToInt32(result) == 1);
            return (true, esBasico);
        }

        /// <summary>
        /// Asigna permisos directamente a un usuario (solo agrega, no elimina)
        /// Validación: Solo perfil 3 puede asignar permisos no básicos
        /// </summary>
        public async Task<bool> AsignarPermisosUsuario(AsignacionPermisoDTO asignacionDTO)
        {
            using var conn = new SqlConnection(_connectionStringSeguridad);
            await conn.OpenAsync();

            // Obtener perfil del usuario logueado
            int perfilUsuarioLogueado = ObtenerPerfilUsuarioLogueado();

            // Insertar solo los permisos que no existan ya
            foreach (var permisoId in asignacionDTO.PermisosIds)
            {
                // Verificar información del permiso (si es básico o no)
                var (permiso_existe, es_basico) = await ObtenerInfoPermiso(permisoId, conn);
                
                if (!permiso_existe)
                    throw new Exception($"El permiso {permisoId} no existe en el sistema");

                // Validación: Si el permiso NO es básico y el usuario no es perfil 3, rechazar
                if (es_basico == false && perfilUsuarioLogueado != 3)
                    throw new Exception($"No tienes permiso para asignar el permiso '{permisoId}'. Solo usuarios con perfil 3 pueden asignar permisos no básicos.");

                // Verificar si el permiso ya existe
                using var cmdCheck = new SqlCommand(
                    "SELECT COUNT(*) FROM seg.PermisosUsuario WHERE Usuario = @Usuario AND Permiso = @Permiso", conn);
                cmdCheck.Parameters.AddWithValue("@Usuario", asignacionDTO.CodigoUsuario);
                cmdCheck.Parameters.AddWithValue("@Permiso", permisoId);

                var existe = (int)await cmdCheck.ExecuteScalarAsync() > 0;

                if (!existe)
                {
                    using var cmdInsert = new SqlCommand(
                        "INSERT INTO seg.PermisosUsuario (Usuario, Permiso, Estado, FechaRegistro) VALUES (@Usuario, @Permiso, 1, GETDATE())", conn);

                    cmdInsert.Parameters.AddWithValue("@Usuario", asignacionDTO.CodigoUsuario);
                    cmdInsert.Parameters.AddWithValue("@Permiso", permisoId);

                    await cmdInsert.ExecuteNonQueryAsync();
                }
            }

            return true;
        }

        /// <summary>
        /// Asigna un perfil a un usuario
        /// </summary>
        public async Task<bool> AsignarPerfilUsuario(AsignacionPerfilDTO asignacionDTO)
        {
            using var conn = new SqlConnection(_connectionStringSeguridad);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(@"
                UPDATE [Usuarios] 
                SET Perfil = @Perfil, FechaModificado = GETDATE()
                WHERE Codigo = @Codigo", conn);

            cmd.Parameters.AddWithValue("@Codigo", asignacionDTO.CodigoUsuario);
            cmd.Parameters.AddWithValue("@Perfil", asignacionDTO.CodigoPerfil);

            var filasAfectadas = await cmd.ExecuteNonQueryAsync();
            return filasAfectadas > 0;
        }

        /// <summary>
        /// Desasigna un permiso de un usuario
        /// </summary>
        public async Task<bool> DesasignarPermisoUsuario(DesasignacionPermisoDTO desasignacionDTO)
        {
            try
            {
                using var conn = new SqlConnection(_connectionStringSeguridad);
                await conn.OpenAsync();

                // Primero verificar que el permiso existe
                using (var cmdVerify = new SqlCommand(
                    "SELECT COUNT(*) FROM seg.PermisosUsuario WHERE Usuario = @Usuario AND Permiso = @Permiso", conn))
                {
                    cmdVerify.Parameters.AddWithValue("@Usuario", desasignacionDTO.CodigoUsuario);
                    cmdVerify.Parameters.AddWithValue("@Permiso", desasignacionDTO.CodigoPermiso);
                    
                    var existe = (int)await cmdVerify.ExecuteScalarAsync() > 0;
                    
                    if (!existe)
                    {
                        return false;
                    }
                }

                // Si existe, proceder a eliminarlo
                using var cmd = new SqlCommand(
                    "DELETE FROM seg.PermisosUsuario WHERE Usuario = @Usuario AND Permiso = @Permiso", conn);

                cmd.Parameters.AddWithValue("@Usuario", desasignacionDTO.CodigoUsuario);
                cmd.Parameters.AddWithValue("@Permiso", desasignacionDTO.CodigoPermiso);

                var filasAfectadas = await cmd.ExecuteNonQueryAsync();
                
                return filasAfectadas > 0;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Desasigna el perfil de un usuario
        /// </summary>
        public async Task<bool> DesasignarPerfilUsuario(DesasignacionPerfilDTO desasignacionDTO)
        {
            using var conn = new SqlConnection(_connectionStringSeguridad);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(
                "UPDATE [Usuarios] SET Perfil = NULL, FechaModificado = GETDATE() WHERE Codigo = @Codigo", conn);

            cmd.Parameters.AddWithValue("@Codigo", desasignacionDTO.CodigoUsuario);

            var filasAfectadas = await cmd.ExecuteNonQueryAsync();
            return filasAfectadas > 0;
        }

        /// <summary>
        /// Obtiene los permisos efectivos de un usuario (directos + heredados del perfil) con datos completos
        /// </summary>
        public async Task<List<Dictionary<string, object>>> ObtenerPermisosEfectivosConDatos(int codigoUsuario)
        {
            var permisos = new List<Dictionary<string, object>>();

            using var conn = new SqlConnection(_connectionStringSeguridad);
            await conn.OpenAsync();

            // Obtener permisos DIRECTOS del usuario
            using var cmd1 = new SqlCommand(@"
                SELECT DISTINCT p.Codigo, p.Nombre, p.Descripcion, m.Nombre as NombreModulo, 
                       p.Basico, p.Estado, p.FechaRegistro
                FROM seg.PermisosUsuario pu
                INNER JOIN seg.Permisos p ON pu.Permiso = p.Codigo
                LEFT JOIN seg.Modulos m ON p.Modulo = m.Codigo
                WHERE pu.Usuario = @Usuario AND pu.Estado = 1
                ORDER BY p.Nombre", conn);
            
            cmd1.Parameters.AddWithValue("@Usuario", codigoUsuario);
            
            using var reader1 = await cmd1.ExecuteReaderAsync();
            while (await reader1.ReadAsync())
            {
                var permiso = new Dictionary<string, object>
                {
                    { "codigo", reader1["Codigo"] },
                    { "nombre", reader1["Nombre"]?.ToString() ?? "" },
                    { "descripcion", reader1["Descripcion"]?.ToString() ?? "" },
                    { "nombreModulo", reader1["NombreModulo"]?.ToString() ?? "" },
                    { "basico", reader1["Basico"] },
                    { "estado", reader1["Estado"] },
                    { "fechaRegistro", reader1["FechaRegistro"] }
                };
                permisos.Add(permiso);
            }

            await reader1.CloseAsync();

            // Obtener permisos HEREDADOS del perfil del usuario
            using var cmd2 = new SqlCommand(@"
                SELECT DISTINCT p.Codigo, p.Nombre, p.Descripcion, m.Nombre as NombreModulo, 
                       p.Basico, p.Estado, p.FechaRegistro
                FROM [Usuarios] u
                INNER JOIN seg.Perfiles pf ON u.Perfil = pf.Codigo
                INNER JOIN seg.PermisosPerfiles pp ON pf.Codigo = pp.Perfil
                INNER JOIN seg.Permisos p ON pp.Permiso = p.Codigo
                LEFT JOIN seg.Modulos m ON p.Modulo = m.Codigo
                WHERE u.Codigo = @Usuario AND u.Perfil IS NOT NULL AND pp.Estado = 1
                ORDER BY p.Nombre", conn);
            
            cmd2.Parameters.AddWithValue("@Usuario", codigoUsuario);
            
            using var reader2 = await cmd2.ExecuteReaderAsync();
            while (await reader2.ReadAsync())
            {
                var codigoPermiso = (int)reader2["Codigo"];
                
                // Evitar duplicados si el permiso ya está en la lista (asignado directamente)
                if (permisos.Any(p => (int)p["codigo"] == codigoPermiso))
                    continue;
                
                var permiso = new Dictionary<string, object>
                {
                    { "codigo", codigoPermiso },
                    { "nombre", reader2["Nombre"]?.ToString() ?? "" },
                    { "descripcion", reader2["Descripcion"]?.ToString() ?? "" },
                    { "nombreModulo", reader2["NombreModulo"]?.ToString() ?? "" },
                    { "basico", reader2["Basico"] },
                    { "estado", reader2["Estado"] },
                    { "fechaRegistro", reader2["FechaRegistro"] }
                };
                permisos.Add(permiso);
            }

            return permisos;
        }

        /// <summary>
        /// Obtiene los permisos efectivos de un usuario (directos + heredados del perfil) - Solo IDs
        /// </summary>
        private async Task<List<int>> ObtenerPermisosEfectivosIds(int codigoUsuario)
        {
            var permisos = new List<int>();

            using var conn = new SqlConnection(_connectionStringSeguridad);
            await conn.OpenAsync();

            // Obtener permisos DIRECTOS del usuario
            using var cmd1 = new SqlCommand(@"
                SELECT DISTINCT Permiso
                FROM seg.PermisosUsuario
                WHERE Usuario = @Usuario", conn);

            cmd1.Parameters.AddWithValue("@Usuario", codigoUsuario);

            using var reader1 = await cmd1.ExecuteReaderAsync();
            while (await reader1.ReadAsync())
            {
                permisos.Add((int)reader1["Permiso"]);
            }

            await reader1.CloseAsync();

            // Obtener permisos HEREDADOS del perfil del usuario
            using var cmd2 = new SqlCommand(@"
                SELECT DISTINCT pp.Permiso
                FROM [Usuarios] u
                INNER JOIN seg.Perfiles pf ON u.Perfil = pf.Codigo
                INNER JOIN seg.PermisosPerfiles pp ON pf.Codigo = pp.Perfil
                WHERE u.Codigo = @Usuario AND u.Perfil IS NOT NULL", conn);

            cmd2.Parameters.AddWithValue("@Usuario", codigoUsuario);

            using var reader2 = await cmd2.ExecuteReaderAsync();
            while (await reader2.ReadAsync())
            {
                var permisoId = (int)reader2["Permiso"];
                
                // Evitar duplicados
                if (!permisos.Contains(permisoId))
                    permisos.Add(permisoId);
            }

            return permisos;
        }

        /// <summary>
        /// Verifica si un usuario tiene un permiso específico
        /// </summary>
        public async Task<bool> UsuarioTienePermiso(int codigoUsuario, int codigoPermiso)
        {
            var permisos = await ObtenerPermisosEfectivosIds(codigoUsuario);
            return permisos.Contains(codigoPermiso);
        }

        /// <summary>
        /// Obtiene solo permisos directos del usuario con datos completos
        /// </summary>
        public async Task<List<Dictionary<string, object>>> ObtenerPermisosDirectosConDatos(int codigoUsuario)
        {
            var permisos = new List<Dictionary<string, object>>();

            using var conn = new SqlConnection(_connectionStringSeguridad);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT DISTINCT p.Codigo, p.Nombre, p.Descripcion, m.Nombre as NombreModulo, 
                       p.Basico, p.Estado, p.FechaRegistro
                FROM seg.PermisosUsuario pu
                INNER JOIN seg.Permisos p ON pu.Permiso = p.Codigo
                LEFT JOIN seg.Modulos m ON p.Modulo = m.Codigo
                WHERE pu.Usuario = @Usuario
                ORDER BY p.Nombre", conn);

            cmd.Parameters.AddWithValue("@Usuario", codigoUsuario);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var permiso = new Dictionary<string, object>
                {
                    { "codigo", reader["Codigo"] },
                    { "nombre", reader["Nombre"]?.ToString() ?? "" },
                    { "descripcion", reader["Descripcion"]?.ToString() ?? "" },
                    { "nombreModulo", reader["NombreModulo"]?.ToString() ?? "" },
                    { "basico", reader["Basico"] },
                    { "estado", reader["Estado"] },
                    { "fechaRegistro", reader["FechaRegistro"] }
                };
                permisos.Add(permiso);
            }

            return permisos;
        }

        /// <summary>
        /// Obtiene solo permisos heredados del perfil con datos completos
        /// </summary>
        public async Task<List<Dictionary<string, object>>> ObtenerPermisosHeredadosConDatos(int codigoUsuario)
        {
            var permisos = new List<Dictionary<string, object>>();

            using var conn = new SqlConnection(_connectionStringSeguridad);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT DISTINCT p.Codigo, p.Nombre, p.Descripcion, m.Nombre as NombreModulo, 
                       p.Basico, p.Estado, p.FechaRegistro
                FROM [Usuarios] u
                INNER JOIN seg.Perfiles pf ON u.Perfil = pf.Codigo
                INNER JOIN seg.PermisosPerfiles pp ON pf.Codigo = pp.Perfil
                INNER JOIN seg.Permisos p ON pp.Permiso = p.Codigo
                LEFT JOIN seg.Modulos m ON p.Modulo = m.Codigo
                WHERE u.Codigo = @Usuario
                ORDER BY p.Nombre", conn);

            cmd.Parameters.AddWithValue("@Usuario", codigoUsuario);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var permiso = new Dictionary<string, object>
                {
                    { "codigo", reader["Codigo"] },
                    { "nombre", reader["Nombre"]?.ToString() ?? "" },
                    { "descripcion", reader["Descripcion"]?.ToString() ?? "" },
                    { "nombreModulo", reader["NombreModulo"]?.ToString() ?? "" },
                    { "basico", reader["Basico"] },
                    { "estado", reader["Estado"] },
                    { "fechaRegistro", reader["FechaRegistro"] }
                };
                permisos.Add(permiso);
            }

            return permisos;
        }

        /// <summary>
        /// Obtiene el perfil de un usuario con sus permisos
        /// </summary>
        public async Task<PerfilResponseDTO> ObtenerPerfilUsuario(int codigoUsuario)
        {
            using var conn = new SqlConnection(_connectionStringSeguridad);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT pf.Codigo, pf.Nombre, pf.Descripcion, pf.Sistema, s.Nombre as NombreSistema,
                       pf.Estado, pf.FechaRegistro, pf.FechaModificado
                FROM [Usuarios] u
                LEFT JOIN seg.Perfiles pf ON u.Perfil = pf.Codigo
                LEFT JOIN seg.Sistemas s ON pf.Sistema = s.Codigo
                WHERE u.Codigo = @Codigo", conn);

            cmd.Parameters.AddWithValue("@Codigo", codigoUsuario);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync() || reader.IsDBNull(0))
                return null;

            var perfil = new PerfilResponseDTO
            {
                Codigo = (int)reader["Codigo"],
                Nombre = reader["Nombre"].ToString(),
                Descripcion = reader["Descripcion"].ToString(),
                Sistema = reader["Sistema"] == DBNull.Value ? null : (int)reader["Sistema"],
                NombreSistema = reader["NombreSistema"].ToString(),
                Estado = reader["Estado"] == DBNull.Value ? null : (int)reader["Estado"],
                FechaRegistro = reader["FechaRegistro"] == DBNull.Value ? null : (DateTime)reader["FechaRegistro"],
                FechaModificado = reader["FechaModificado"] == DBNull.Value ? null : (DateTime)reader["FechaModificado"]
            };

            await reader.CloseAsync();

            // Obtener permisos del perfil
            perfil.PermisosIds = await ObtenerPermisosDelPerfil(perfil.Codigo, conn);

            return perfil;
        }

        private async Task<List<int>> ObtenerPermisosDelPerfil(int codigoPerfil, SqlConnection conn)
        {
            var permisos = new List<int>();

            using var cmd = new SqlCommand(@"
                SELECT Permiso 
                FROM seg.PermisosPerfiles 
                WHERE Perfil = @Perfil", conn);

            cmd.Parameters.AddWithValue("@Perfil", codigoPerfil);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                permisos.Add((int)reader["Permiso"]);
            }

            return permisos;
        }
    }
}
