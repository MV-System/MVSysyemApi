using DTO;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace MVSystemApi.Model_Negocio.Seguridad
{
    public class ModulosService
    {
        private readonly string _connectionStringSeguridad;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ModulosService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
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

        public async Task<List<ModuloResponseDTO>> ObtenerTodos()
        {
            var modulos = new List<ModuloResponseDTO>();

            using var conn = new SqlConnection(_connectionStringSeguridad);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT m.Codigo, m.Nombre, m.Descripcion, m.Sistema, s.Nombre as NombreSistema,
                       m.Estado, m.FechaRegistro, m.FechaModificado
                FROM seg.Modulos m
                LEFT JOIN seg.Sistemas s ON m.Sistema = s.Codigo
                ORDER BY m.Nombre", conn);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                modulos.Add(new ModuloResponseDTO
                {
                    Codigo = (int)reader["Codigo"],
                    Nombre = reader["Nombre"].ToString(),
                    Descripcion = reader["Descripcion"].ToString(),
                    Sistema = reader["Sistema"] == DBNull.Value ? null : (int)reader["Sistema"],
                    NombreSistema = reader["NombreSistema"].ToString(),
                    Estado = reader["Estado"] == DBNull.Value ? null : (int)reader["Estado"],
                    FechaRegistro = reader["FechaRegistro"] == DBNull.Value ? null : (DateTime)reader["FechaRegistro"],
                    FechaModificado = reader["FechaModificado"] == DBNull.Value ? null : (DateTime)reader["FechaModificado"]
                });
            }

            return modulos;
        }

        public async Task<ModuloResponseDTO> ObtenerPorCodigo(int codigo)
        {
            using var conn = new SqlConnection(_connectionStringSeguridad);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT m.Codigo, m.Nombre, m.Descripcion, m.Sistema, s.Nombre as NombreSistema,
                       m.Estado, m.FechaRegistro, m.FechaModificado
                FROM seg.Modulos m
                LEFT JOIN seg.Sistemas s ON m.Sistema = s.Codigo
                WHERE m.Codigo = @Codigo", conn);

            cmd.Parameters.AddWithValue("@Codigo", codigo);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return new ModuloResponseDTO
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
        }

        public async Task<List<ModuloResponseDTO>> ObtenerPorSistema(int codigoSistema)
        {
            var modulos = new List<ModuloResponseDTO>();

            using var conn = new SqlConnection(_connectionStringSeguridad);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT m.Codigo, m.Nombre, m.Descripcion, m.Sistema, s.Nombre as NombreSistema,
                       m.Estado, m.FechaRegistro, m.FechaModificado
                FROM seg.Modulos m
                LEFT JOIN seg.Sistemas s ON m.Sistema = s.Codigo
                WHERE m.Sistema = @Sistema AND m.Estado = 1
                ORDER BY m.Nombre", conn);

            cmd.Parameters.AddWithValue("@Sistema", codigoSistema);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                modulos.Add(new ModuloResponseDTO
                {
                    Codigo = (int)reader["Codigo"],
                    Nombre = reader["Nombre"].ToString(),
                    Descripcion = reader["Descripcion"].ToString(),
                    Sistema = reader["Sistema"] == DBNull.Value ? null : (int)reader["Sistema"],
                    NombreSistema = reader["NombreSistema"].ToString(),
                    Estado = reader["Estado"] == DBNull.Value ? null : (int)reader["Estado"],
                    FechaRegistro = reader["FechaRegistro"] == DBNull.Value ? null : (DateTime)reader["FechaRegistro"],
                    FechaModificado = reader["FechaModificado"] == DBNull.Value ? null : (DateTime)reader["FechaModificado"]
                });
            }

            return modulos;
        }

        public async Task<int> Crear(CrearModuloDTO dto)
        {
            if (await NombreExiste(dto.Nombre))
                throw new Exception($"El módulo '{dto.Nombre}' ya existe");

            using var conn = new SqlConnection(_connectionStringSeguridad);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(@"
                INSERT INTO seg.Modulos (Nombre, Descripcion, Sistema, Estado, FechaRegistro, EstadoRegistro)
                VALUES (@Nombre, @Descripcion, @Sistema, @Estado, GETDATE(), 'A');
                SELECT SCOPE_IDENTITY();", conn);

            cmd.Parameters.AddWithValue("@Nombre", dto.Nombre);
            cmd.Parameters.AddWithValue("@Descripcion", dto.Descripcion ?? "");
            cmd.Parameters.AddWithValue("@Sistema", dto.Sistema);
            cmd.Parameters.AddWithValue("@Estado", dto.Estado ?? 1);

            var resultado = await cmd.ExecuteScalarAsync();
            var codigoModulo = Convert.ToInt32(resultado);
            return codigoModulo;
        }

        public async Task<bool> Actualizar(ActualizarModuloDTO dto)
        {
            using var conn = new SqlConnection(_connectionStringSeguridad);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(@"
                UPDATE seg.Modulos
                SET Nombre = @Nombre,
                    Descripcion = @Descripcion,
                    Sistema = @Sistema,
                    Estado = @Estado,
                    FechaModificado = GETDATE()
                WHERE Codigo = @Codigo", conn);

            cmd.Parameters.AddWithValue("@Codigo", dto.Codigo);
            cmd.Parameters.AddWithValue("@Nombre", dto.Nombre ?? "");
            cmd.Parameters.AddWithValue("@Descripcion", dto.Descripcion ?? "");
            cmd.Parameters.AddWithValue("@Sistema", dto.Sistema ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Estado", dto.Estado ?? 1);

            var filasAfectadas = await cmd.ExecuteNonQueryAsync();
            return filasAfectadas > 0;
        }

        public async Task<bool> Eliminar(int codigo)
        {
            using var conn = new SqlConnection(_connectionStringSeguridad);
            await conn.OpenAsync();

            // Primero eliminar permisos relacionados
            using var cmdPermisos = new SqlCommand("DELETE FROM seg.Permisos WHERE Modulo = @Codigo", conn);
            cmdPermisos.Parameters.AddWithValue("@Codigo", codigo);
            await cmdPermisos.ExecuteNonQueryAsync();

            // Luego eliminar el módulo
            using var cmd = new SqlCommand("DELETE FROM seg.Modulos WHERE Codigo = @Codigo", conn);
            cmd.Parameters.AddWithValue("@Codigo", codigo);

            var filasAfectadas = await cmd.ExecuteNonQueryAsync();
            return filasAfectadas > 0;
        }

        private async Task<bool> NombreExiste(string nombre)
        {
            using var conn = new SqlConnection(_connectionStringSeguridad);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("SELECT COUNT(*) FROM seg.Modulos WHERE Nombre = @Nombre", conn);
            cmd.Parameters.AddWithValue("@Nombre", nombre);

            var resultado = await cmd.ExecuteScalarAsync();
            return (int)resultado > 0;
        }
    }
}
