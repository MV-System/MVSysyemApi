using DTO;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace MVSystemApi.Model_Negocio.Seguridad
{
    public class PerfilesService
    {
        private readonly string _connectionStringSeguridad;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PerfilesService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _connectionStringSeguridad = configuration.GetConnectionString("MVSystemSeguridad");
            _httpContextAccessor = httpContextAccessor;
        }

        private async Task<string> ObtenerConnectionStringEmpresa()
        {
            var idEmpresa = _httpContextAccessor.HttpContext?.User?.FindFirst("IdEmpresa")?.Value;
            
            if (string.IsNullOrEmpty(idEmpresa))
                throw new Exception("No se encontró IdEmpresa en el token");

            using var conn = new SqlConnection(_connectionStringSeguridad);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT StringCon FROM dbo.Empresas WHERE ID_Empresa = @IdEmpresa", conn);
            cmd.Parameters.AddWithValue("@IdEmpresa", int.Parse(idEmpresa));

            var result = await cmd.ExecuteScalarAsync();
            if (result == null)
                throw new Exception($"No se encontró connection string para la empresa {idEmpresa}");

            return result.ToString();
        }

        /// <summary>
        /// Obtiene todos los perfiles del sistema
        /// </summary>
        public async Task<List<PerfilDTO>> ObtenerTodosPerfiles()
        {
            var perfiles = new List<PerfilDTO>();

            using var conn = new SqlConnection(_connectionStringSeguridad);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT Codigo, Nombre, Descripcion, Estado, FechaRegistro, FechaModificado
                FROM seg.Perfiles
                WHERE Estado = 1
                ORDER BY Nombre", conn);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                perfiles.Add(new PerfilDTO
                {
                    Codigo = (int)reader["Codigo"],
                    Nombre = reader["Nombre"].ToString(),
                    Descripcion = reader["Descripcion"].ToString(),
                    Estado = reader["Estado"] == DBNull.Value ? null : (int)reader["Estado"],
                    FechaRegistro = reader["FechaRegistro"] == DBNull.Value ? null : (DateTime)reader["FechaRegistro"],
                    FechaModificado = reader["FechaModificado"] == DBNull.Value ? null : (DateTime)reader["FechaModificado"]
                });
            }

            return perfiles;
        }

        /// <summary>
        /// Obtiene un perfil específico por su código
        /// </summary>
        public async Task<PerfilDTO> ObtenerPerfilPorCodigo(int codigoPerfil)
        {
            using var conn = new SqlConnection(_connectionStringSeguridad);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT Codigo, Nombre, Descripcion, Estado, FechaRegistro, FechaModificado
                FROM seg.Perfiles
                WHERE Codigo = @Codigo", conn);

            cmd.Parameters.AddWithValue("@Codigo", codigoPerfil);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return new PerfilDTO
            {
                Codigo = (int)reader["Codigo"],
                Nombre = reader["Nombre"].ToString(),
                Descripcion = reader["Descripcion"].ToString(),
                Estado = reader["Estado"] == DBNull.Value ? null : (int)reader["Estado"],
                FechaRegistro = reader["FechaRegistro"] == DBNull.Value ? null : (DateTime)reader["FechaRegistro"],
                FechaModificado = reader["FechaModificado"] == DBNull.Value ? null : (DateTime)reader["FechaModificado"]
            };
        }

        /// <summary>
        /// Crea un nuevo perfil
        /// </summary>
        public async Task<int> CrearPerfil(CrearPerfilDTO dto)
        {
            using var conn = new SqlConnection(_connectionStringSeguridad);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(@"
                INSERT INTO seg.Perfiles (Nombre, Descripcion, Sistema, Estado, FechaRegistro, EstadoRegistro)
                VALUES (@Nombre, @Descripcion, @Sistema, @Estado, GETDATE(), 'A');
                SELECT SCOPE_IDENTITY();", conn);

            cmd.Parameters.AddWithValue("@Nombre", dto.Nombre);
            cmd.Parameters.AddWithValue("@Descripcion", dto.Descripcion ?? "");
            cmd.Parameters.AddWithValue("@Sistema", dto.Sistema > 0 ? (object)dto.Sistema : DBNull.Value);
            cmd.Parameters.AddWithValue("@Estado", dto.Estado ?? 1);

            var resultado = await cmd.ExecuteScalarAsync();
            var codigoPerfil = Convert.ToInt32(resultado);
            return codigoPerfil;
        }

        /// <summary>
        /// Actualiza un perfil existente
        /// </summary>
        public async Task<bool> ActualizarPerfil(ActualizarPerfilDTO dto)
        {
            using var conn = new SqlConnection(_connectionStringSeguridad);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(@"
                UPDATE seg.Perfiles
                SET Nombre = @Nombre,
                    Descripcion = @Descripcion,
                    Estado = @Estado,
                    FechaModificado = GETDATE()
                WHERE Codigo = @Codigo", conn);

            cmd.Parameters.AddWithValue("@Codigo", dto.Codigo);
            cmd.Parameters.AddWithValue("@Nombre", dto.Nombre ?? "");
            cmd.Parameters.AddWithValue("@Descripcion", dto.Descripcion ?? "");
            cmd.Parameters.AddWithValue("@Estado", dto.Estado ?? 1);

            var filasAfectadas = await cmd.ExecuteNonQueryAsync();
            return filasAfectadas > 0;
        }

        /// <summary>
        /// Asigna permisos a un perfil
        /// </summary>
        public async Task<bool> AsignarPermisosAlPerfil(int codigoPerfil, List<int> permisosIds)
        {
            using var conn = new SqlConnection(_connectionStringSeguridad);
            await conn.OpenAsync();

            using var transaction = conn.BeginTransaction();
            try
            {
                // Eliminar permisos actuales del perfil
                using var cmdDelete = new SqlCommand(
                    "DELETE FROM seg.PermisosPerfiles WHERE Perfil = @Perfil", conn, transaction);
                cmdDelete.Parameters.AddWithValue("@Perfil", codigoPerfil);
                await cmdDelete.ExecuteNonQueryAsync();

                // Insertar nuevos permisos
                foreach (var permisoId in permisosIds)
                {
                    using var cmdInsert = new SqlCommand(
                        @"INSERT INTO seg.PermisosPerfiles (Perfil, Permiso, Estado, FechaRegistro, EstadoRegistro)
                        VALUES (@Perfil, @Permiso, 1, GETDATE(), 'A')", conn, transaction);
                    cmdInsert.Parameters.AddWithValue("@Perfil", codigoPerfil);
                    cmdInsert.Parameters.AddWithValue("@Permiso", permisoId);
                    await cmdInsert.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Elimina un perfil y sus permisos asociados
        /// </summary>
        public async Task<bool> EliminarPerfil(int codigo)
        {
            using var conn = new SqlConnection(_connectionStringSeguridad);
            await conn.OpenAsync();

            using var transaction = conn.BeginTransaction();
            try
            {
                // Eliminar relaciones de PermisosPerfiles
                using var cmdDeletePermisos = new SqlCommand(
                    "DELETE FROM seg.PermisosPerfiles WHERE Perfil = @Perfil", conn, transaction);
                cmdDeletePermisos.Parameters.AddWithValue("@Perfil", codigo);
                await cmdDeletePermisos.ExecuteNonQueryAsync();

                // Eliminar el perfil
                using var cmdDelete = new SqlCommand(
                    "DELETE FROM seg.Perfiles WHERE Codigo = @Codigo", conn, transaction);
                cmdDelete.Parameters.AddWithValue("@Codigo", codigo);
                var filasAfectadas = await cmdDelete.ExecuteNonQueryAsync();

                transaction.Commit();
                return filasAfectadas > 0;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
