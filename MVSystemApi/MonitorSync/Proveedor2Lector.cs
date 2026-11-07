using System.Data.SqlClient;

namespace MVSystemApi.MonitorSync
{
    /// <summary>Estado de un comprobante en la tabla remota del proveedor 2.</summary>
    public record EstadoRemoto(string Estatus, string Error, string CodigoSeguridad);

    /// <summary>Fila de la tabla remota del proveedor 2 que todavía no está aceptada.</summary>
    public record FilaProveedor2(string Rnc, string Encf, int TipoNcf, string NumDocumento, string TipoDocumento, string Estatus, string Error, DateTime FechaRegistro);

    /// <summary>
    /// Lectura de PIVOT_FACTURAS_ENC en la base remota del proveedor 2 y de las empresas del proveedor 2 en SEGURIDAD.
    /// SOLO LECTURA (solo ejecuta SELECT). No usa AccesoDatos: el monitor corre sin sesión de usuario.
    /// </summary>
    public class Proveedor2Lector
    {
        private readonly string _conexionProveedor2;
        private readonly string _conexionSeguridad;
        private readonly ILogger<Proveedor2Lector> _logger;

        public Proveedor2Lector(IConfiguration configuration, ILogger<Proveedor2Lector> logger)
        {
            _conexionProveedor2 = configuration.GetConnectionString("Proveedor2MicroStar") ?? string.Empty;
            _conexionSeguridad = configuration.GetConnectionString("MVSystemSeguridad") ?? string.Empty;
            _logger = logger;
        }

        public bool Disponible => !string.IsNullOrWhiteSpace(_conexionProveedor2);

        /// <summary>ACEPTADA = ACEPTADO; cualquier rechazo = RECHAZADO_DGII; el resto en mayúsculas sin espacios sobrantes.</summary>
        public static string Normalizar(string estatus)
        {
            var e = (estatus ?? "").Trim().ToUpperInvariant();
            if (e == "ACEPTADA") return "ACEPTADO";
            if (e.StartsWith("RECHAZ")) return "RECHAZADO_DGII";
            return e;
        }

        public async Task<EstadoRemoto> LeerAsync(string encf, string rnc, CancellationToken ct)
        {
            if (!Disponible) return null;
            using var cn = new SqlConnection(_conexionProveedor2);
            await cn.OpenAsync(ct);
            using var cmd = new SqlCommand(
                "SELECT TOP (1) ESTATUS, ESTATUS_ERROR, ENCF_CODIGO_SEGURIDAD FROM PIVOT_FACTURAS_ENC WHERE ENCF = @encf AND RNC_EMISOR = @rnc", cn)
            { CommandTimeout = 60 };
            cmd.Parameters.AddWithValue("@encf", encf);
            cmd.Parameters.AddWithValue("@rnc", rnc);
            using var rd = await cmd.ExecuteReaderAsync(ct);
            if (!await rd.ReadAsync(ct)) return null;
            return new EstadoRemoto(
                Normalizar(rd.IsDBNull(0) ? "" : rd.GetString(0)),
                rd.IsDBNull(1) ? "" : Convert.ToString(rd.GetValue(1)) ?? "",
                rd.IsDBNull(2) ? "" : rd.GetString(2));
        }

        /// <summary>RNC -> nombre de las empresas activas, certificadas y con proveedor 2 (las únicas que se vigilan).</summary>
        public async Task<Dictionary<string, string>> EmpresasAsync(CancellationToken ct)
        {
            var resultado = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(_conexionSeguridad)) return resultado;
            using var cn = new SqlConnection(_conexionSeguridad);
            await cn.OpenAsync(ct);
            using var cmd = new SqlCommand(@"
SELECT RNC, MAX(ISNULL(Nombre_Empresa_Mostrar, Nombre_Empresa)) AS Nombre
  FROM Empresas
 WHERE IdProveedorFacturacionElectronica = 2
   AND ISNULL(Certificado, 0) = 1
   AND RNC IS NOT NULL
 GROUP BY RNC", cn);
            using var rd = await cmd.ExecuteReaderAsync(ct);
            while (await rd.ReadAsync(ct))
            {
                var rnc = Convert.ToString(rd.GetValue(0))?.Trim();
                if (!string.IsNullOrWhiteSpace(rnc))
                    resultado[rnc] = rd.IsDBNull(1) ? rnc : rd.GetString(1).Trim();
            }
            return resultado;
        }

        /// <summary>Comprobantes recientes de esos RNC que todavía no están aceptados.</summary>
        public async Task<List<FilaProveedor2>> NoAceptadosAsync(IReadOnlyCollection<string> rncs, int dias, CancellationToken ct)
        {
            var filas = new List<FilaProveedor2>();
            if (!Disponible || rncs.Count == 0) return filas;

            using var cn = new SqlConnection(_conexionProveedor2);
            await cn.OpenAsync(ct);
            using var cmd = new SqlCommand { Connection = cn, CommandTimeout = 120 };
            var nombres = new List<string>();
            int n = 0;
            foreach (var rnc in rncs)
            {
                var p = "@r" + n++;
                nombres.Add(p);
                cmd.Parameters.AddWithValue(p, rnc);
            }
            cmd.Parameters.AddWithValue("@desde", DateTime.UtcNow.AddHours(-4).AddDays(-Math.Max(1, dias)));
            cmd.CommandText = $@"
SELECT RNC_EMISOR, ENCF, TIPO_NCF, NUM_DOCUMENTO, TIPO_DOCUMENTO, ESTATUS, ESTATUS_ERROR, FECHA_REGISTRO
  FROM PIVOT_FACTURAS_ENC
 WHERE RNC_EMISOR IN ({string.Join(",", nombres)})
   AND FECHA_REGISTRO >= @desde
   AND ISNULL(ESTATUS, '') NOT IN ('ACEPTADO', 'ACEPTADA')";

            using var rd = await cmd.ExecuteReaderAsync(ct);
            while (await rd.ReadAsync(ct))
            {
                filas.Add(new FilaProveedor2(
                    rd.GetString(0).Trim(),
                    rd.IsDBNull(1) ? "" : rd.GetString(1).Trim(),
                    rd.IsDBNull(2) ? 0 : Convert.ToInt32(rd.GetValue(2)),
                    rd.IsDBNull(3) ? "" : rd.GetString(3).Trim(),
                    rd.IsDBNull(4) ? "" : rd.GetString(4).Trim(),
                    Normalizar(rd.IsDBNull(5) ? "" : rd.GetString(5)),
                    rd.IsDBNull(6) ? "" : Convert.ToString(rd.GetValue(6)) ?? "",
                    rd.IsDBNull(7) ? DateTime.MinValue : rd.GetDateTime(7)));
            }
            return filas;
        }
    }
}
