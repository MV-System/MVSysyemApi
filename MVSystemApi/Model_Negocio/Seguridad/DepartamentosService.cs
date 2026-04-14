using DTO;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace MVSystemApi.Model_Negocio.Seguridad
{
    public class DepartamentosService
    {
        private readonly string _connectionStringSeguridad;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DepartamentosService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
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

        public async Task<List<DepartamentoResponseDTO>> ObtenerTodos()
        {
            var departamentos = new List<DepartamentoResponseDTO>();

            using var conn = new SqlConnection(_connectionStringSeguridad);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT Codigo, Nombre, Descripcion, Estado, FechaRegistro, FechaModificado
                FROM seg.Departamentos
                ORDER BY Nombre", conn);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                departamentos.Add(new DepartamentoResponseDTO
                {
                    Codigo = (int)reader["Codigo"],
                    Nombre = reader["Nombre"].ToString(),
                    Descripcion = reader["Descripcion"].ToString(),
                    Estado = reader["Estado"] == DBNull.Value ? null : (int)reader["Estado"],
                    FechaRegistro = reader["FechaRegistro"] == DBNull.Value ? null : (DateTime)reader["FechaRegistro"],
                    FechaModificado = reader["FechaModificado"] == DBNull.Value ? null : (DateTime)reader["FechaModificado"]
                });
            }

            return departamentos;
        }

        public async Task<DepartamentoResponseDTO> ObtenerPorCodigo(int codigo)
        {
            using var conn = new SqlConnection(_connectionStringSeguridad);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT Codigo, Nombre, Descripcion, Estado, FechaRegistro, FechaModificado
                FROM seg.Departamentos
                WHERE Codigo = @Codigo", conn);

            cmd.Parameters.AddWithValue("@Codigo", codigo);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return new DepartamentoResponseDTO
            {
                Codigo = (int)reader["Codigo"],
                Nombre = reader["Nombre"].ToString(),
                Descripcion = reader["Descripcion"].ToString(),
                Estado = reader["Estado"] == DBNull.Value ? null : (int)reader["Estado"],
                FechaRegistro = reader["FechaRegistro"] == DBNull.Value ? null : (DateTime)reader["FechaRegistro"],
                FechaModificado = reader["FechaModificado"] == DBNull.Value ? null : (DateTime)reader["FechaModificado"]
            };
        }

        public async Task<int> Crear(CrearDepartamentoDTO dto)
        {
            if (await NombreExiste(dto.Nombre))
                throw new Exception($"El departamento '{dto.Nombre}' ya existe");

            using var conn = new SqlConnection(_connectionStringSeguridad);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(@"
                INSERT INTO seg.Departamentos (Nombre, Descripcion, Estado, FechaRegistro, EstadoRegistro)
                VALUES (@Nombre, @Descripcion, @Estado, GETDATE(), 'A');
                SELECT SCOPE_IDENTITY();", conn);

            cmd.Parameters.AddWithValue("@Nombre", dto.Nombre);
            cmd.Parameters.AddWithValue("@Descripcion", dto.Descripcion ?? "");
            cmd.Parameters.AddWithValue("@Estado", dto.Estado ?? 1);

            var resultado = await cmd.ExecuteScalarAsync();
            var codigoDepartamento = Convert.ToInt32(resultado);
            return codigoDepartamento;
        }

        public async Task<bool> Actualizar(ActualizarDepartamentoDTO dto)
        {
            using var conn = new SqlConnection(_connectionStringSeguridad);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(@"
                UPDATE seg.Departamentos
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
            using var conn = new SqlConnection(_connectionStringSeguridad);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("DELETE FROM seg.Departamentos WHERE Codigo = @Codigo", conn);
            cmd.Parameters.AddWithValue("@Codigo", codigo);

            var filasAfectadas = await cmd.ExecuteNonQueryAsync();
            return filasAfectadas > 0;
        }

        private async Task<bool> NombreExiste(string nombre)
        {
            using var conn = new SqlConnection(_connectionStringSeguridad);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("SELECT COUNT(*) FROM seg.Departamentos WHERE Nombre = @Nombre", conn);
            cmd.Parameters.AddWithValue("@Nombre", nombre);

            var resultado = await cmd.ExecuteScalarAsync();
            return (int)resultado > 0;
        }
    }
}
