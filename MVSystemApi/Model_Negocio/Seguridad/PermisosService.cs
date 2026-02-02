using DTO;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Claims;

namespace MVSystemApi.Model_Negocio.Seguridad
{
    public class PermisosService
    {
        private readonly string _connectionStringSeguridad;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PermisosService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
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
        /// Obtiene todos los permisos del sistema
        /// </summary>
        public async Task<List<PermisoResponseDTO>> ObtenerTodosPermisos()
        {
            var permisos = new List<PermisoResponseDTO>();
            var connectionString = await ObtenerConnectionStringEmpresa();

            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT p.Codigo, p.Nombre, p.Descripcion, p.Modulo, m.Nombre as NombreModulo,
                       p.Basico, p.Estado, p.FechaRegistro, p.FechaModificado
                FROM seg.Permisos p
                LEFT JOIN seg.Modulos m ON p.Modulo = m.Codigo
                ORDER BY p.Nombre", conn);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                permisos.Add(new PermisoResponseDTO
                {
                    Codigo = (int)reader["Codigo"],
                    Nombre = reader["Nombre"].ToString(),
                    Descripcion = reader["Descripcion"].ToString(),
                    Modulo = reader["Modulo"] == DBNull.Value ? null : (int)reader["Modulo"],
                    NombreModulo = reader["NombreModulo"].ToString(),
                    Basico = reader["Basico"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["Basico"]),
                    Estado = reader["Estado"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["Estado"]),
                    FechaRegistro = reader["FechaRegistro"] == DBNull.Value ? null : (DateTime)reader["FechaRegistro"],
                    FechaModificado = reader["FechaModificado"] == DBNull.Value ? null : (DateTime)reader["FechaModificado"]
                });
            }

            return permisos;
        }

        /// <summary>
        /// Obtiene permisos de un módulo específico
        /// </summary>
        public async Task<List<PermisoResponseDTO>> ObtenerPermisosPorModulo(int codigoModulo)
        {
            var permisos = new List<PermisoResponseDTO>();
            var connectionString = await ObtenerConnectionStringEmpresa();

            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT p.Codigo, p.Nombre, p.Descripcion, p.Modulo, m.Nombre as NombreModulo,
                       p.Basico, p.Estado, p.FechaRegistro, p.FechaModificado
                FROM seg.Permisos p
                LEFT JOIN seg.Modulos m ON p.Modulo = m.Codigo
                WHERE p.Modulo = @Modulo AND p.Estado = 1
                ORDER BY p.Nombre", conn);

            cmd.Parameters.AddWithValue("@Modulo", codigoModulo);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                permisos.Add(new PermisoResponseDTO
                {
                    Codigo = (int)reader["Codigo"],
                    Nombre = reader["Nombre"].ToString(),
                    Descripcion = reader["Descripcion"].ToString(),
                    Modulo = reader["Modulo"] == DBNull.Value ? null : (int)reader["Modulo"],
                    NombreModulo = reader["NombreModulo"].ToString(),
                    Basico = reader["Basico"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["Basico"]),
                    Estado = reader["Estado"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["Estado"]),
                    FechaRegistro = reader["FechaRegistro"] == DBNull.Value ? null : (DateTime)reader["FechaRegistro"],
                    FechaModificado = reader["FechaModificado"] == DBNull.Value ? null : (DateTime)reader["FechaModificado"]
                });
            }

            return permisos;
        }

        /// <summary>
        /// Obtiene un permiso específico
        /// </summary>
        public async Task<PermisoResponseDTO> ObtenerPermisoPorCodigo(int codigoPermiso)
        {
            var connectionString = await ObtenerConnectionStringEmpresa();

            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT p.Codigo, p.Nombre, p.Descripcion, p.Modulo, m.Nombre as NombreModulo,
                       p.Basico, p.Estado, p.FechaRegistro, p.FechaModificado
                FROM seg.Permisos p
                LEFT JOIN seg.Modulos m ON p.Modulo = m.Codigo
                WHERE p.Codigo = @Codigo", conn);

            cmd.Parameters.AddWithValue("@Codigo", codigoPermiso);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return new PermisoResponseDTO
            {
                Codigo = (int)reader["Codigo"],
                Nombre = reader["Nombre"].ToString(),
                Descripcion = reader["Descripcion"].ToString(),
                Modulo = reader["Modulo"] == DBNull.Value ? null : (int)reader["Modulo"],
                NombreModulo = reader["NombreModulo"].ToString(),
                Basico = reader["Basico"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["Basico"]),
                Estado = reader["Estado"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["Estado"]),
                FechaRegistro = reader["FechaRegistro"] == DBNull.Value ? null : (DateTime)reader["FechaRegistro"],
                FechaModificado = reader["FechaModificado"] == DBNull.Value ? null : (DateTime)reader["FechaModificado"]
            };
        }

        /// <summary>
        /// Crea un nuevo permiso
        /// </summary>
        public async Task<int> CrearPermiso(CrearPermisoDTO permisoDTO)
        {
            if (await NombrePermisoExiste(permisoDTO.Nombre))
                throw new Exception($"El permiso '{permisoDTO.Nombre}' ya existe");

            var connectionString = await ObtenerConnectionStringEmpresa();

            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(@"
                INSERT INTO seg.Permisos (Nombre, Descripcion, Modulo, Basico, Estado, FechaRegistro)
                VALUES (@Nombre, @Descripcion, @Modulo, @Basico, @Estado, GETDATE());
                SELECT SCOPE_IDENTITY();", conn);

            cmd.Parameters.AddWithValue("@Nombre", permisoDTO.Nombre);
            cmd.Parameters.AddWithValue("@Descripcion", permisoDTO.Descripcion ?? "");
            cmd.Parameters.AddWithValue("@Modulo", permisoDTO.Modulo);
            cmd.Parameters.AddWithValue("@Basico", permisoDTO.Basico ?? 0);
            cmd.Parameters.AddWithValue("@Estado", permisoDTO.Estado ?? 1);

            var resultado = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(resultado);
        }

        /// <summary>
        /// <summary>
        /// Actualiza un permiso
        /// </summary>
        public async Task<bool> ActualizarPermiso(ActualizarPermisoDTO permisoDTO)
        {
            Console.WriteLine($"🔍 ActualizarPermiso - Datos recibidos: Código={permisoDTO.Codigo}, Nombre={permisoDTO.Nombre}, Basico={permisoDTO.Basico}, Estado={permisoDTO.Estado}");

            var connectionString = await ObtenerConnectionStringEmpresa();

            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            // Si Estado no viene o es inválido, dejar el actual
            string queryUpdate = @"
                UPDATE seg.Permisos 
                SET Nombre = @Nombre, 
                    Descripcion = @Descripcion,
                    Modulo = @Modulo,
                    Basico = @Basico";

            // Solo actualizar Estado si viene válido
            if (permisoDTO.Estado.HasValue && permisoDTO.Estado > 0)
            {
                queryUpdate += ", Estado = @Estado";
            }

            queryUpdate += ", FechaModificado = GETDATE() WHERE Codigo = @Codigo";

            using var cmd = new SqlCommand(queryUpdate, conn);

            cmd.Parameters.AddWithValue("@Codigo", permisoDTO.Codigo);
            cmd.Parameters.AddWithValue("@Nombre", permisoDTO.Nombre ?? "");
            cmd.Parameters.AddWithValue("@Descripcion", permisoDTO.Descripcion ?? "");
            cmd.Parameters.AddWithValue("@Modulo", permisoDTO.Modulo ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Basico", permisoDTO.Basico ?? 0);

            if (permisoDTO.Estado.HasValue && permisoDTO.Estado > 0)
            {
                cmd.Parameters.AddWithValue("@Estado", permisoDTO.Estado);
            }

            var filasAfectadas = await cmd.ExecuteNonQueryAsync();
            Console.WriteLine($"✅ Filas afectadas por UPDATE: {filasAfectadas}");
            return filasAfectadas > 0;
        }

        /// <summary>
        /// Elimina un permiso
        /// </summary>
        public async Task<bool> EliminarPermiso(int codigoPermiso)
        {
            var connectionString = await ObtenerConnectionStringEmpresa();

            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            // Primero eliminar las relaciones
            using var cmdRelaciones = new SqlCommand(@"
                DELETE FROM seg.PermisosPerfiles WHERE Permiso = @Codigo;
                DELETE FROM seg.PermisosUsuario WHERE Permiso = @Codigo;", conn);

            cmdRelaciones.Parameters.AddWithValue("@Codigo", codigoPermiso);
            await cmdRelaciones.ExecuteNonQueryAsync();

            // Luego eliminar el permiso
            using var cmd = new SqlCommand("DELETE FROM seg.Permisos WHERE Codigo = @Codigo", conn);
            cmd.Parameters.AddWithValue("@Codigo", codigoPermiso);

            var filasAfectadas = await cmd.ExecuteNonQueryAsync();
            return filasAfectadas > 0;
        }

        private async Task<bool> NombrePermisoExiste(string nombrePermiso)
        {
            var connectionString = await ObtenerConnectionStringEmpresa();

            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("SELECT COUNT(*) FROM seg.Permisos WHERE Nombre = @Nombre", conn);
            cmd.Parameters.AddWithValue("@Nombre", nombrePermiso);

            var resultado = await cmd.ExecuteScalarAsync();
            return (int)resultado > 0;
        }
    }
}
