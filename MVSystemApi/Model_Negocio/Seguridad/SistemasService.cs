using DTO;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace MVSystemApi.Model_Negocio.Seguridad
{
    public class SistemasService
    {
        private readonly string _connectionStringSeguridad;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SistemasService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
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

        public async Task<List<SistemaResponseDTO>> ObtenerTodos()
        {
            var sistemas = new List<SistemaResponseDTO>();
            var connectionString = await ObtenerConnectionStringEmpresa();

            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT Codigo, Nombre, Descripcion, Estado, FechaRegistro, FechaModificado
                FROM seg.Sistemas
                ORDER BY Nombre", conn);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                sistemas.Add(new SistemaResponseDTO
                {
                    Codigo = (int)reader["Codigo"],
                    Nombre = reader["Nombre"].ToString(),
                    Descripcion = reader["Descripcion"].ToString(),
                    Estado = reader["Estado"] == DBNull.Value ? null : (int)reader["Estado"],
                    FechaRegistro = reader["FechaRegistro"] == DBNull.Value ? null : (DateTime)reader["FechaRegistro"],
                    FechaModificado = reader["FechaModificado"] == DBNull.Value ? null : (DateTime)reader["FechaModificado"]
                });
            }

            return sistemas;
        }

        public async Task<SistemaResponseDTO> ObtenerPorCodigo(int codigo)
        {
            var connectionString = await ObtenerConnectionStringEmpresa();

            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT Codigo, Nombre, Descripcion, Estado, FechaRegistro, FechaModificado
                FROM seg.Sistemas
                WHERE Codigo = @Codigo", conn);

            cmd.Parameters.AddWithValue("@Codigo", codigo);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return new SistemaResponseDTO
            {
                Codigo = (int)reader["Codigo"],
                Nombre = reader["Nombre"].ToString(),
                Descripcion = reader["Descripcion"].ToString(),
                Estado = reader["Estado"] == DBNull.Value ? null : (int)reader["Estado"],
                FechaRegistro = reader["FechaRegistro"] == DBNull.Value ? null : (DateTime)reader["FechaRegistro"],
                FechaModificado = reader["FechaModificado"] == DBNull.Value ? null : (DateTime)reader["FechaModificado"]
            };
        }

        public async Task<int> Crear(CrearSistemaDTO dto)
        {
            if (await NombreExiste(dto.Nombre))
                throw new Exception($"El sistema '{dto.Nombre}' ya existe");

            var connectionString = await ObtenerConnectionStringEmpresa();

            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(@"
                INSERT INTO seg.Sistemas (Nombre, Descripcion, Estado, FechaRegistro)
                VALUES (@Nombre, @Descripcion, @Estado, GETDATE());
                SELECT SCOPE_IDENTITY();", conn);

            cmd.Parameters.AddWithValue("@Nombre", dto.Nombre);
            cmd.Parameters.AddWithValue("@Descripcion", dto.Descripcion ?? "");
            cmd.Parameters.AddWithValue("@Estado", dto.Estado ?? 1);

            var resultado = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(resultado);
        }

        public async Task<bool> Actualizar(ActualizarSistemaDTO dto)
        {
            var connectionString = await ObtenerConnectionStringEmpresa();

            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(@"
                UPDATE seg.Sistemas
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

        public async Task<bool> Eliminar(int codigo)
        {
            var connectionString = await ObtenerConnectionStringEmpresa();

            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("DELETE FROM seg.Sistemas WHERE Codigo = @Codigo", conn);
            cmd.Parameters.AddWithValue("@Codigo", codigo);

            var filasAfectadas = await cmd.ExecuteNonQueryAsync();
            return filasAfectadas > 0;
        }

        private async Task<bool> NombreExiste(string nombre)
        {
            var connectionString = await ObtenerConnectionStringEmpresa();

            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("SELECT COUNT(*) FROM seg.Sistemas WHERE Nombre = @Nombre", conn);
            cmd.Parameters.AddWithValue("@Nombre", nombre);

            var resultado = await cmd.ExecuteScalarAsync();
            return (int)resultado > 0;
        }
    }
}
