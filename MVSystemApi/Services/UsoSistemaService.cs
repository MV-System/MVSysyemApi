using System.Data.SqlClient;
using MVSystemApi.Model;

namespace MVSystemApi.Services
{
    /// <summary>
    /// Guarda y consulta el uso del sistema (pantallas, botones y sesiones) en la base SEGURIDAD.
    /// Las fechas se guardan en hora de Republica Dominicana (UTC-4), igual que TrackingLogs.
    /// </summary>
    public class UsoSistemaService
    {
        private const int MaxEventosPorAviso = 50;
        private readonly string _connectionString;

        public UsoSistemaService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MVSystemSeguridad");
        }

        /// <summary>Actualiza la sesion (la crea si no existe) y guarda los eventos pendientes.</summary>
        public async Task RegistrarAsync(UsoRegistroDto dto, string usuario, string empresa, string ip, string userAgent)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            using (var sesion = new SqlCommand(@"
                MERGE dbo.SesionesUsuario AS destino
                USING (SELECT @SesionId AS SesionId) AS origen ON destino.SesionId = origen.SesionId
                WHEN MATCHED THEN
                    UPDATE SET UltimaActividad = DATEADD(HOUR, -4, GETDATE()), Ruta = @Ruta, Activa = 1
                WHEN NOT MATCHED THEN
                    INSERT (SesionId, Usuario, Empresa, Inicio, UltimaActividad, Ruta, DireccionIP, UserAgent, Activa)
                    VALUES (@SesionId, @Usuario, @Empresa, DATEADD(HOUR, -4, GETDATE()), DATEADD(HOUR, -4, GETDATE()), @Ruta, @Ip, @UserAgent, 1);",
                connection, transaction))
            {
                sesion.Parameters.AddWithValue("@SesionId", dto.SesionId);
                sesion.Parameters.AddWithValue("@Usuario", Truncar(usuario, 100));
                sesion.Parameters.AddWithValue("@Empresa", (object)Truncar(empresa, 150) ?? DBNull.Value);
                sesion.Parameters.AddWithValue("@Ruta", (object)Truncar(dto.Ruta, 200) ?? DBNull.Value);
                sesion.Parameters.AddWithValue("@Ip", (object)Truncar(ip, 50) ?? DBNull.Value);
                sesion.Parameters.AddWithValue("@UserAgent", (object)Truncar(userAgent, 500) ?? DBNull.Value);
                await sesion.ExecuteNonQueryAsync();
            }

            foreach (var e in (dto.Eventos ?? new List<UsoEventoDto>()).Take(MaxEventosPorAviso))
            {
                var tipo = e.Tipo == "B" ? "B" : e.Tipo == "P" ? "P" : null;
                var ruta = Truncar(e.Ruta, 200);
                if (tipo == null || string.IsNullOrWhiteSpace(ruta)) continue;

                using var insertar = new SqlCommand(@"
                    INSERT INTO dbo.UsoSistema (Usuario, Empresa, Tipo, Ruta, Detalle, FechaHora)
                    VALUES (@Usuario, @Empresa, @Tipo, @Ruta, @Detalle, DATEADD(HOUR, -4, GETDATE()));",
                    connection, transaction);
                insertar.Parameters.AddWithValue("@Usuario", Truncar(usuario, 100));
                insertar.Parameters.AddWithValue("@Empresa", (object)Truncar(empresa, 150) ?? DBNull.Value);
                insertar.Parameters.AddWithValue("@Tipo", tipo);
                insertar.Parameters.AddWithValue("@Ruta", ruta);
                insertar.Parameters.AddWithValue("@Detalle", tipo == "B" ? (object)Truncar(e.Detalle, 150) ?? DBNull.Value : DBNull.Value);
                await insertar.ExecuteNonQueryAsync();
            }

            transaction.Commit();
        }

        public async Task SalirAsync(Guid sesionId, string usuario)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var cmd = new SqlCommand(
                "UPDATE dbo.SesionesUsuario SET Activa = 0, UltimaActividad = DATEADD(HOUR, -4, GETDATE()) WHERE SesionId = @SesionId AND Usuario = @Usuario",
                connection);
            cmd.Parameters.AddWithValue("@SesionId", sesionId);
            cmd.Parameters.AddWithValue("@Usuario", Truncar(usuario, 100));
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>Sesiones con actividad reciente (por defecto, en los ultimos 5 minutos).</summary>
        public async Task<List<UsoConectadoDto>> ConectadosAsync(int minutos)
        {
            var lista = new List<UsoConectadoDto>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var cmd = new SqlCommand(@"
                SELECT s.Usuario, s.Empresa, s.Ruta, s.Inicio, s.UltimaActividad, s.DireccionIP,
                       (SELECT TOP (1) u.Nombre FROM dbo.Usuarios u WHERE u.Login = s.Usuario) AS NombreUsuario
                FROM dbo.SesionesUsuario s
                WHERE s.Activa = 1 AND s.UltimaActividad >= DATEADD(MINUTE, -@Minutos, DATEADD(HOUR, -4, GETDATE()))
                ORDER BY s.UltimaActividad DESC", connection);
            cmd.Parameters.AddWithValue("@Minutos", minutos);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                lista.Add(new UsoConectadoDto
                {
                    Usuario = reader.GetString(0),
                    Empresa = reader.IsDBNull(1) ? null : reader.GetString(1),
                    Ruta = reader.IsDBNull(2) ? null : reader.GetString(2),
                    Inicio = reader.GetDateTime(3),
                    UltimaActividad = reader.GetDateTime(4),
                    DireccionIP = reader.IsDBNull(5) ? null : reader.GetString(5),
                    NombreUsuario = reader.IsDBNull(6) ? null : reader.GetString(6),
                });
            }
            return lista;
        }

        /// <summary>Uso agrupado por pantalla (y por boton) en un rango de fechas, opcionalmente de una sola empresa.</summary>
        public async Task<List<UsoResumenDto>> ResumenAsync(DateTime desde, DateTime hasta, string empresa)
        {
            var lista = new List<UsoResumenDto>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var cmd = new SqlCommand(@"
                SELECT Tipo, Ruta, ISNULL(Detalle, '') AS Detalle,
                       COUNT(*) AS Visitas, COUNT(DISTINCT Usuario) AS Usuarios, MAX(FechaHora) AS Ultima
                FROM dbo.UsoSistema
                WHERE FechaHora >= @Desde AND FechaHora < @Hasta
                  AND (@Empresa IS NULL OR Empresa = @Empresa)
                GROUP BY Tipo, Ruta, ISNULL(Detalle, '')
                ORDER BY COUNT(*) DESC", connection);
            cmd.Parameters.AddWithValue("@Desde", desde.Date);
            cmd.Parameters.AddWithValue("@Hasta", hasta.Date.AddDays(1));
            cmd.Parameters.AddWithValue("@Empresa", string.IsNullOrWhiteSpace(empresa) ? DBNull.Value : empresa);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                lista.Add(new UsoResumenDto
                {
                    Tipo = reader.GetString(0),
                    Ruta = reader.GetString(1),
                    Detalle = reader.GetString(2),
                    Visitas = reader.GetInt32(3),
                    Usuarios = reader.GetInt32(4),
                    Ultima = reader.GetDateTime(5),
                });
            }
            return lista;
        }

        /// <summary>Quien abrio una pantalla (o pulso un boton) y cuantas veces, en un rango de fechas.</summary>
        public async Task<List<UsoDetalleUsuarioDto>> DetalleAsync(string tipo, string ruta, string detalle, DateTime desde, DateTime hasta, string empresa)
        {
            var lista = new List<UsoDetalleUsuarioDto>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var cmd = new SqlCommand(@"
                SELECT x.Usuario, x.Empresa, x.Veces, x.Ultima,
                       (SELECT TOP (1) u.Nombre FROM dbo.Usuarios u WHERE u.Login = x.Usuario) AS NombreUsuario
                FROM (
                    SELECT Usuario, Empresa, COUNT(*) AS Veces, MAX(FechaHora) AS Ultima
                    FROM dbo.UsoSistema
                    WHERE FechaHora >= @Desde AND FechaHora < @Hasta
                      AND Tipo = @Tipo AND Ruta = @Ruta AND ISNULL(Detalle, '') = @Detalle
                      AND (@Empresa IS NULL OR Empresa = @Empresa)
                    GROUP BY Usuario, Empresa
                ) x
                ORDER BY x.Veces DESC, x.Usuario", connection);
            cmd.Parameters.AddWithValue("@Desde", desde.Date);
            cmd.Parameters.AddWithValue("@Hasta", hasta.Date.AddDays(1));
            cmd.Parameters.AddWithValue("@Tipo", tipo == "B" ? "B" : "P");
            cmd.Parameters.AddWithValue("@Ruta", ruta ?? "");
            cmd.Parameters.AddWithValue("@Detalle", detalle ?? "");
            cmd.Parameters.AddWithValue("@Empresa", string.IsNullOrWhiteSpace(empresa) ? DBNull.Value : empresa);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                lista.Add(new UsoDetalleUsuarioDto
                {
                    Usuario = reader.GetString(0),
                    Empresa = reader.IsDBNull(1) ? null : reader.GetString(1),
                    Veces = reader.GetInt32(2),
                    Ultima = reader.GetDateTime(3),
                    NombreUsuario = reader.IsDBNull(4) ? null : reader.GetString(4),
                });
            }
            return lista;
        }

        /// <summary>Empresas con uso registrado, para el filtro de la pantalla.</summary>
        public async Task<List<string>> EmpresasAsync()
        {
            var lista = new List<string>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var cmd = new SqlCommand("SELECT DISTINCT Empresa FROM dbo.UsoSistema WHERE Empresa IS NOT NULL ORDER BY Empresa", connection);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync()) lista.Add(reader.GetString(0));
            return lista;
        }

        /// <summary>Fecha del primer registro de uso, para saber desde cuando hay datos confiables.</summary>
        public async Task<DateTime?> PrimerRegistroAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var cmd = new SqlCommand("SELECT MIN(FechaHora) FROM dbo.UsoSistema", connection);
            var valor = await cmd.ExecuteScalarAsync();
            return valor == null || valor == DBNull.Value ? (DateTime?)null : (DateTime)valor;
        }

        private static string Truncar(string texto, int max) =>
            string.IsNullOrEmpty(texto) ? texto : (texto.Length <= max ? texto : texto.Substring(0, max));
    }
}
