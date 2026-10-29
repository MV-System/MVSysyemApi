using System.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace MVSystemApi.MonitorSync
{
    /// <summary>
    /// Compara, para cada empresa, la base principal con su espejo. SOLO LECTURA (solo ejecuta SELECT).
    /// Las cadenas de conexion de cada base se leen de SEGURIDAD.Empresas (StringCon); nunca se registran.
    /// </summary>
    public class RevisorSincronizacion
    {
        private readonly MonitorOptions _opciones;
        private readonly string _connSeguridad;
        private readonly ILogger<RevisorSincronizacion> _logger;

        public RevisorSincronizacion(IOptions<MonitorOptions> opciones, IConfiguration configuration, ILogger<RevisorSincronizacion> logger)
        {
            _opciones = opciones.Value;
            _connSeguridad = configuration.GetConnectionString("MVSystemSeguridad") ?? string.Empty;
            _logger = logger;
        }

        public async Task<ResultadoRevision> RevisarAsync(AlcanceRevision alcance, CancellationToken ct = default)
        {
            var resultado = new ResultadoRevision { Alcance = alcance };

            foreach (var par in _opciones.Pares)
            {
                try
                {
                    await RevisarParAsync(par, alcance, resultado, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Monitor] No se pudo revisar la empresa {Empresa}", par.Nombre);
                    resultado.Errores.Add($"{par.Nombre}: {ex.Message}");
                    resultado.EmpresasConError.Add(par.Nombre);
                    resultado.Hallazgos.Add(new Hallazgo
                    {
                        Clave = $"{par.Nombre}|ERROR_REVISION",
                        Empresa = par.Nombre,
                        Categoria = "ERROR_REVISION",
                        Detalle = $"No se pudo revisar la empresa: {ex.Message}",
                        Alcance = AlcanceRevision.Liviana,
                    });
                }
            }

            return resultado;
        }

        private async Task RevisarParAsync(ParEmpresa par, AlcanceRevision alcance, ResultadoRevision resultado, CancellationToken ct)
        {
            var connP = await ObtenerConexionAsync(par.IdPrincipal, ct);
            var connE = await ObtenerConexionAsync(par.IdEspejo, ct);

            // ---------- 1. Inventario disponible ----------
            var dispP = await CargarImeisDisponiblesAsync(connP, ct);
            var dispE = await CargarImeisDisponiblesAsync(connE, ct);
            resultado.Resumen.Add($"{par.Nombre}: disponibles principal {dispP.Count} / espejo {dispE.Count}");

            var soloP = dispP.Where(i => !dispE.Contains(i)).ToList();
            var soloE = dispE.Where(i => !dispP.Contains(i)).ToList();

            var estadoEnEspejo = soloP.Count > 0 ? await CargarEstadosAsync(connE, soloP, ct) : new Dictionary<string, string>();
            var estadoEnPrincipal = soloE.Count > 0 ? await CargarEstadosAsync(connP, soloE, ct) : new Dictionary<string, string>();

            foreach (var imei in soloP)
            {
                if (estadoEnEspejo.TryGetValue(imei, out var estado))
                    Agregar(resultado, par, "DISPONIBLE_SOLO_PRINCIPAL", imei, $"IMEI {imei}: disponible en la principal y '{estado}' en el espejo", AlcanceRevision.Liviana);
                else
                    Agregar(resultado, par, "FALTA_EN_ESPEJO", imei, $"IMEI {imei}: disponible en la principal y NO existe en el espejo", AlcanceRevision.Liviana);
            }
            foreach (var imei in soloE)
            {
                if (estadoEnPrincipal.TryGetValue(imei, out var estado))
                    Agregar(resultado, par, "DISPONIBLE_SOLO_ESPEJO", imei, $"IMEI {imei}: disponible en el espejo y '{estado}' en la principal", AlcanceRevision.Liviana);
                else
                    Agregar(resultado, par, "FALTA_EN_PRINCIPAL", imei, $"IMEI {imei}: disponible en el espejo y NO existe en la principal", AlcanceRevision.Liviana);
            }

            // ---------- 2. Catalogo de modelos ----------
            await RevisarModelosAsync(par, connP, connE, resultado, ct);

            // ---------- 3. Costos y precios (solo en la revision completa) ----------
            if (alcance == AlcanceRevision.Completa)
                await RevisarValoresAsync(par, connP, connE, resultado, ct);
        }

        private async Task RevisarModelosAsync(ParEmpresa par, string connP, string connE, ResultadoRevision resultado, CancellationToken ct)
        {
            var modP = await CargarModelosAsync(connP, ct);
            var modE = await CargarModelosAsync(connE, ct);

            var porDescP = modP.ToLookup(m => Normalizar(m.Value), m => m.Key);
            var porDescE = modE.ToLookup(m => Normalizar(m.Value), m => m.Key);
            var yaReportadas = new HashSet<string>();

            foreach (var (id, desc) in modP.OrderBy(m => m.Key))
            {
                if (modE.TryGetValue(id, out var descE))
                {
                    if (Normalizar(desc) != Normalizar(descE))
                        Agregar(resultado, par, "MODELO_DESCRIPCION_DISTINTA", id.ToString(),
                            $"ID {id}: principal '{desc}' / espejo '{descE}'", AlcanceRevision.Liviana);
                }
                else if (porDescE.Contains(Normalizar(desc)))
                {
                    var idsE = porDescE[Normalizar(desc)].ToList();
                    if (yaReportadas.Add(Normalizar(desc)))
                        Agregar(resultado, par, "MODELO_CON_OTRO_ID", Normalizar(desc),
                            $"'{desc}': ID {id} en la principal / ID {string.Join("/", idsE)} en el espejo", AlcanceRevision.Liviana);
                }
                else
                {
                    Agregar(resultado, par, "MODELO_SOLO_PRINCIPAL", id.ToString(), $"ID {id} '{desc}' no existe en el espejo", AlcanceRevision.Liviana);
                }
            }

            foreach (var (id, desc) in modE.OrderBy(m => m.Key).Where(m => !modP.ContainsKey(m.Key)))
            {
                if (porDescP.Contains(Normalizar(desc)))
                {
                    var idsP = porDescP[Normalizar(desc)].ToList();
                    if (yaReportadas.Add(Normalizar(desc)))
                        Agregar(resultado, par, "MODELO_CON_OTRO_ID", Normalizar(desc),
                            $"'{desc}': ID {string.Join("/", idsP)} en la principal / ID {id} en el espejo", AlcanceRevision.Liviana);
                }
                else
                {
                    Agregar(resultado, par, "MODELO_SOLO_ESPEJO", id.ToString(), $"ID {id} '{desc}' no existe en la principal", AlcanceRevision.Liviana);
                }
            }
        }

        private async Task RevisarValoresAsync(ParEmpresa par, string connP, string connE, ResultadoRevision resultado, CancellationToken ct)
        {
            var eqP = await CargarValoresAsync(connP, ct);
            var eqE = await CargarValoresAsync(connE, ct);

            foreach (var (imei, p) in eqP)
            {
                if (!eqE.TryGetValue(imei, out var e)) continue; // lo que falta ya lo reporta la revision liviana

                if (p.IdModelo != e.IdModelo)
                    Agregar(resultado, par, "EQUIPO_MODELO_DISTINTO", imei, $"IMEI {imei}: modelo principal {p.IdModelo} / espejo {e.IdModelo}", AlcanceRevision.Completa);
                if (p.Costo != e.Costo)
                    Agregar(resultado, par, "COSTO_DISTINTO", imei, $"IMEI {imei}: costo principal {p.Costo:N2} / espejo {e.Costo:N2}", AlcanceRevision.Completa);
                if (p.PrecioDetalle != e.PrecioDetalle)
                    Agregar(resultado, par, "PRECIO_DETALLE_DISTINTO", imei, $"IMEI {imei}: precio detalle principal {p.PrecioDetalle:N2} / espejo {e.PrecioDetalle:N2}", AlcanceRevision.Completa);
                if (p.PrecioMayor != e.PrecioMayor)
                    Agregar(resultado, par, "PRECIO_MAYOR_DISTINTO", imei, $"IMEI {imei}: precio por mayor principal {p.PrecioMayor:N2} / espejo {e.PrecioMayor:N2}", AlcanceRevision.Completa);
            }
        }

        private static void Agregar(ResultadoRevision resultado, ParEmpresa par, string categoria, string id, string detalle, AlcanceRevision alcance)
        {
            resultado.Hallazgos.Add(new Hallazgo
            {
                Clave = $"{par.Nombre}|{categoria}|{id}",
                Empresa = par.Nombre,
                Categoria = categoria,
                Detalle = detalle,
                Alcance = alcance,
            });
        }

        private static string Normalizar(string s) => (s ?? string.Empty).Trim().ToUpperInvariant();

        // ---------------- Acceso a datos (solo SELECT) ----------------

        private async Task<string> ObtenerConexionAsync(int idEmpresa, CancellationToken ct)
        {
            using var cn = new SqlConnection(_connSeguridad);
            await cn.OpenAsync(ct);
            using var cmd = new SqlCommand("SELECT TOP (1) StringCon FROM Empresas WHERE ID_Empresa = @id", cn);
            cmd.Parameters.AddWithValue("@id", idEmpresa);
            var cadena = (await cmd.ExecuteScalarAsync(ct))?.ToString();
            if (string.IsNullOrWhiteSpace(cadena))
                throw new InvalidOperationException($"No hay cadena de conexion para la empresa {idEmpresa} en SEGURIDAD.");
            return cadena;
        }

        private static async Task<HashSet<string>> CargarImeisDisponiblesAsync(string conn, CancellationToken ct)
        {
            var set = new HashSet<string>();
            using var cn = new SqlConnection(conn);
            await cn.OpenAsync(ct);
            using var cmd = new SqlCommand("SELECT Imei FROM Equipos WHERE Disponible = 'S' AND Imei IS NOT NULL", cn) { CommandTimeout = 120 };
            using var rd = await cmd.ExecuteReaderAsync(ct);
            while (await rd.ReadAsync(ct))
                set.Add(Normalizar(rd.GetString(0)));
            return set;
        }

        private static async Task<Dictionary<string, string>> CargarEstadosAsync(string conn, List<string> imeis, CancellationToken ct)
        {
            var resultado = new Dictionary<string, string>();
            using var cn = new SqlConnection(conn);
            await cn.OpenAsync(ct);
            foreach (var bloque in imeis.Chunk(500))
            {
                using var cmd = new SqlCommand { Connection = cn, CommandTimeout = 120 };
                var nombres = new List<string>();
                for (int i = 0; i < bloque.Length; i++)
                {
                    nombres.Add("@p" + i);
                    cmd.Parameters.AddWithValue("@p" + i, bloque[i]);
                }
                cmd.CommandText = $"SELECT Imei, Disponible FROM Equipos WHERE Imei IN ({string.Join(",", nombres)})";
                using var rd = await cmd.ExecuteReaderAsync(ct);
                while (await rd.ReadAsync(ct))
                    resultado[Normalizar(rd.GetString(0))] = Normalizar(rd.IsDBNull(1) ? "?" : rd.GetString(1));
            }
            return resultado;
        }

        private static async Task<Dictionary<int, string>> CargarModelosAsync(string conn, CancellationToken ct)
        {
            var dic = new Dictionary<int, string>();
            using var cn = new SqlConnection(conn);
            await cn.OpenAsync(ct);
            using var cmd = new SqlCommand("SELECT ID_Modelo, Descripcion FROM Modelos", cn) { CommandTimeout = 120 };
            using var rd = await cmd.ExecuteReaderAsync(ct);
            while (await rd.ReadAsync(ct))
                dic[rd.GetInt32(0)] = rd.IsDBNull(1) ? string.Empty : rd.GetString(1);
            return dic;
        }

        private sealed class ValoresEquipo
        {
            public int IdModelo;
            public decimal Costo;
            public decimal PrecioDetalle;
            public decimal PrecioMayor;
        }

        private static async Task<Dictionary<string, ValoresEquipo>> CargarValoresAsync(string conn, CancellationToken ct)
        {
            var dic = new Dictionary<string, ValoresEquipo>();
            using var cn = new SqlConnection(conn);
            await cn.OpenAsync(ct);
            using var cmd = new SqlCommand(
                "SELECT Imei, ID_Modelo, Costo_Equipo, Precio_Detalle, Precio_Por_Mayor FROM Equipos WHERE Disponible = 'S' AND Imei IS NOT NULL", cn)
            { CommandTimeout = 180 };
            using var rd = await cmd.ExecuteReaderAsync(ct);
            while (await rd.ReadAsync(ct))
            {
                dic[Normalizar(rd.GetString(0))] = new ValoresEquipo
                {
                    IdModelo = rd.IsDBNull(1) ? 0 : rd.GetInt32(1),
                    Costo = rd.IsDBNull(2) ? 0 : Convert.ToDecimal(rd.GetValue(2)),
                    PrecioDetalle = rd.IsDBNull(3) ? 0 : Convert.ToDecimal(rd.GetValue(3)),
                    PrecioMayor = rd.IsDBNull(4) ? 0 : Convert.ToDecimal(rd.GetValue(4)),
                };
            }
            return dic;
        }
    }
}
