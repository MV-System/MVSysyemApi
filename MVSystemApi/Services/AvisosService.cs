using System.Data.SqlClient;
using MVSystemApi.Model;

namespace MVSystemApi.Services
{
    /// <summary>
    /// Avisos y novedades que se muestran a los usuarios al entrar o mientras tienen el sistema abierto.
    /// Se guardan en la base SEGURIDAD; las fechas estan en hora de Republica Dominicana (UTC-4).
    /// </summary>
    public class AvisosService
    {
        private const string Columnas =
            "a.IdAviso, a.Tipo, a.Prioridad, a.Titulo, a.Mensaje, a.Donde, a.Destino, a.DestinoValor, a.Permiso, " +
            "a.FechaInicio, a.FechaVence, a.Activo, a.CreadoPor, a.FechaCreacion";

        private readonly string _connectionString;

        public AvisosService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MVSystemSeguridad");
        }

        /// <summary>
        /// Avisos vigentes que el usuario todavia no ha visto y que le corresponden
        /// (por destino y, si lo piden, por permiso). Los urgentes primero.
        /// </summary>
        public async Task<List<AvisoDto>> PendientesAsync(string usuario, string empresa, ISet<string> permisos)
        {
            var candidatos = new List<AvisoDto>();
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using var cmd = new SqlCommand($@"
                    SELECT {Columnas}
                    FROM dbo.Avisos a
                    WHERE a.Activo = 1
                      AND a.FechaInicio <= DATEADD(HOUR, -4, GETDATE())
                      AND (a.FechaVence IS NULL OR a.FechaVence > DATEADD(HOUR, -4, GETDATE()))
                      AND NOT EXISTS (SELECT 1 FROM dbo.AvisosVistos v WHERE v.IdAviso = a.IdAviso AND v.Usuario = @Usuario)
                    ORDER BY a.Prioridad DESC, a.FechaInicio DESC", connection);
                cmd.Parameters.AddWithValue("@Usuario", usuario ?? "");
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync()) candidatos.Add(Leer(reader));
            }

            return candidatos.Where(a => LeCorresponde(a, usuario, empresa, permisos)).ToList();
        }

        public async Task MarcarVistosAsync(IEnumerable<int> ids, string usuario)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            foreach (var id in ids.Distinct().Take(50))
            {
                using var cmd = new SqlCommand(@"
                    INSERT INTO dbo.AvisosVistos (IdAviso, Usuario, FechaVisto)
                    SELECT a.IdAviso, @Usuario, DATEADD(HOUR, -4, GETDATE())
                    FROM dbo.Avisos a
                    WHERE a.IdAviso = @IdAviso
                      AND NOT EXISTS (SELECT 1 FROM dbo.AvisosVistos v WHERE v.IdAviso = a.IdAviso AND v.Usuario = @Usuario)",
                    connection);
                cmd.Parameters.AddWithValue("@IdAviso", id);
                cmd.Parameters.AddWithValue("@Usuario", usuario ?? "");
                await cmd.ExecuteNonQueryAsync();
            }
        }

        /// <summary>Todos los avisos, los mas nuevos primero, con cuantos usuarios lo han visto.</summary>
        public async Task<List<AvisoDto>> ListarAsync()
        {
            var lista = new List<AvisoDto>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var cmd = new SqlCommand($@"
                SELECT {Columnas}, (SELECT COUNT(*) FROM dbo.AvisosVistos v WHERE v.IdAviso = a.IdAviso) AS Vistos
                FROM dbo.Avisos a
                ORDER BY a.IdAviso DESC", connection);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var aviso = Leer(reader);
                aviso.Vistos = reader.GetInt32(14);
                lista.Add(aviso);
            }
            return lista;
        }

        public async Task<int> CrearAsync(AvisoNuevoDto dto, string creador)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var cmd = new SqlCommand(@"
                INSERT INTO dbo.Avisos
                    (Tipo, Prioridad, Titulo, Mensaje, Donde, Destino, DestinoValor, Permiso, FechaInicio, FechaVence, Activo, CreadoPor, FechaCreacion)
                VALUES
                    (@Tipo, @Prioridad, @Titulo, @Mensaje, @Donde, @Destino, @DestinoValor, @Permiso,
                     ISNULL(@FechaInicio, DATEADD(HOUR, -4, GETDATE())), @FechaVence, 1, @CreadoPor, DATEADD(HOUR, -4, GETDATE()));
                SELECT CAST(SCOPE_IDENTITY() AS INT);", connection);
            cmd.Parameters.AddWithValue("@Tipo", dto.Tipo);
            cmd.Parameters.AddWithValue("@Prioridad", dto.Prioridad);
            cmd.Parameters.AddWithValue("@Titulo", dto.Titulo);
            cmd.Parameters.AddWithValue("@Mensaje", dto.Mensaje);
            cmd.Parameters.AddWithValue("@Donde", Nulo(dto.Donde));
            cmd.Parameters.AddWithValue("@Destino", dto.Destino);
            cmd.Parameters.AddWithValue("@DestinoValor", Nulo(dto.DestinoValor));
            cmd.Parameters.AddWithValue("@Permiso", Nulo(dto.Permiso));
            cmd.Parameters.AddWithValue("@FechaInicio", (object)dto.FechaInicio ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FechaVence", (object)dto.FechaVence ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CreadoPor", creador ?? "");
            return (int)await cmd.ExecuteScalarAsync();
        }

        public async Task<bool> CambiarActivoAsync(int idAviso, bool activo)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var cmd = new SqlCommand("UPDATE dbo.Avisos SET Activo = @Activo WHERE IdAviso = @IdAviso", connection);
            cmd.Parameters.AddWithValue("@Activo", activo);
            cmd.Parameters.AddWithValue("@IdAviso", idAviso);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        /// <summary>Quien vio un aviso y cuando.</summary>
        public async Task<List<AvisoVistoUsuarioDto>> VistosAsync(int idAviso)
        {
            var lista = new List<AvisoVistoUsuarioDto>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var cmd = new SqlCommand(@"
                SELECT v.Usuario, (SELECT TOP (1) u.Nombre FROM dbo.Usuarios u WHERE u.Login = v.Usuario) AS NombreUsuario, v.FechaVisto
                FROM dbo.AvisosVistos v
                WHERE v.IdAviso = @IdAviso
                ORDER BY v.FechaVisto DESC", connection);
            cmd.Parameters.AddWithValue("@IdAviso", idAviso);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                lista.Add(new AvisoVistoUsuarioDto
                {
                    Usuario = reader.GetString(0),
                    NombreUsuario = reader.IsDBNull(1) ? null : reader.GetString(1),
                    FechaVisto = reader.GetDateTime(2),
                });
            }
            return lista;
        }

        private static bool LeCorresponde(AvisoDto a, string usuario, string empresa, ISet<string> permisos)
        {
            if (!string.IsNullOrWhiteSpace(a.Permiso) && !permisos.Contains(a.Permiso)) return false;

            switch (a.Destino)
            {
                case "EMPRESA":
                    return string.Equals(a.DestinoValor?.Trim(), empresa?.Trim(), StringComparison.OrdinalIgnoreCase);
                case "USUARIOS":
                    return (a.DestinoValor ?? "")
                        .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Any(u => string.Equals(u.Trim(), usuario, StringComparison.OrdinalIgnoreCase));
                default:
                    return true;
            }
        }

        private static AvisoDto Leer(SqlDataReader r) => new AvisoDto
        {
            IdAviso = r.GetInt32(0),
            Tipo = r.GetString(1),
            Prioridad = r.GetString(2).Trim(),
            Titulo = r.GetString(3),
            Mensaje = r.GetString(4),
            Donde = r.IsDBNull(5) ? null : r.GetString(5),
            Destino = r.GetString(6),
            DestinoValor = r.IsDBNull(7) ? null : r.GetString(7),
            Permiso = r.IsDBNull(8) ? null : r.GetString(8),
            FechaInicio = r.GetDateTime(9),
            FechaVence = r.IsDBNull(10) ? (DateTime?)null : r.GetDateTime(10),
            Activo = r.GetBoolean(11),
            CreadoPor = r.GetString(12),
            FechaCreacion = r.GetDateTime(13),
        };

        private static object Nulo(string texto) =>
            string.IsNullOrWhiteSpace(texto) ? DBNull.Value : texto.Trim();
    }
}
