using Microsoft.Extensions.Caching.Memory;
using MVSystemApi.Interfaz;
using MVSystemApi.Model_Negocio.Seguridad;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace MVSystemApi.Services
{
    public class SyncService : ISyncService
    {
        // Reintentos ante errores transitorios (red, Azure SQL ocupado, deadlock)
        private const int MaxIntentos = 3;
        private static readonly int[] PausasMs = { 300, 1000 };

        // Errores transitorios habituales de SQL Server / Azure SQL
        private static readonly HashSet<int> ErroresTransitorios = new()
        {
            -2,                     // timeout
            20, 64, 233,            // conexion perdida / cerrada
            1205,                   // deadlock
            4060,                   // base no disponible (failover)
            10053, 10054, 10060,    // red
            10928, 10929,           // limites de recursos
            40197, 40501, 40540, 40613, // servicio ocupado / reconfigurando
            49918, 49919, 49920     // demasiadas operaciones
        };

        private readonly string _connectionStringSync;
        // Se llena solo cuando el usuario DEBIA tener espejo (IdEmpresaE distinto de 0) y no se pudo preparar
        private readonly string _motivoEspejoNoDisponible;
        private readonly ILogger<SyncService> _logger;

        public SyncService(IHttpContextAccessor httpContextAccessor, SeguridadService seguridad, IMemoryCache memoryCache, ILogger<SyncService> logger)
        {
            _logger = logger;

            try
            {
                var user = httpContextAccessor.HttpContext?.User;
                if (user?.Identity?.IsAuthenticated != true)
                {
                    _logger.LogInformation("[Sync] Usuario no autenticado, sync desactivado");
                    return;
                }

                var userName = user.Identity.Name;
                var idEmpresaE = user.FindFirst("IdEmpresaE")?.Value;
                _logger.LogInformation("[Sync] Usuario: {User}, IdEmpresaE: {IdEmpresaE}", userName, idEmpresaE ?? "null");

                if (string.IsNullOrEmpty(idEmpresaE) || idEmpresaE == "0")
                {
                    _logger.LogInformation("[Sync] Usuario {User} NO tiene BD espejo configurada", userName);
                    return;
                }

                var keySync = $"SyncConn:{idEmpresaE}";
                if (memoryCache.TryGetValue<string>(keySync, out var cached))
                {
                    _connectionStringSync = cached;
                    _logger.LogInformation("[Sync] Connection string espejo cargado de cache para empresa {IdEmpresaE}", idEmpresaE);
                }
                else
                {
                    var connString = seguridad.GetConnStringByEmpresa(int.Parse(idEmpresaE));
                    if (!string.IsNullOrEmpty(connString))
                    {
                        memoryCache.Set(keySync, connString, TimeSpan.FromMinutes(13));
                        _connectionStringSync = connString;
                        _logger.LogInformation("[Sync] Connection string espejo obtenido de BD para empresa {IdEmpresaE}", idEmpresaE);
                    }
                    else
                    {
                        _motivoEspejoNoDisponible = $"no se encontro StringCon para la empresa espejo {idEmpresaE} (usuario {userName})";
                        _logger.LogWarning("[Sync] No se encontró StringCon para empresa espejo {IdEmpresaE}", idEmpresaE);
                    }
                }
            }
            catch (Exception ex)
            {
                _motivoEspejoNoDisponible = $"error preparando la conexion al espejo: {ex.Message}";
                _logger.LogError(ex, "[Sync] Error inicializando SyncService");
            }
        }

        public bool Ejecutar(string spName, SqlParameterCollection parameters)
        {
            if (!HayEspejo($"SP {spName}")) return !EsperabaEspejo;

            _logger.LogInformation("[Sync] Ejecutando SP: {SP} en BD espejo", spName);

            // Copia de los parametros: un SqlParameter no puede pertenecer a dos comandos (necesario para reintentar)
            var copias = ClonarParametros(parameters);

            // Un SP puede no ser idempotente (por ejemplo un INSERT): un timeout durante la ejecucion
            // NO se reintenta porque pudo haberse aplicado. Los fallos al conectar si se reintentan.
            return ConReintentos($"SP {spName} ({Formatear(copias)})", reintentarTimeoutEnEjecucion: false, (conn, ejecutando) =>
            {
                using var cmd = new SqlCommand(spName, conn) { CommandType = CommandType.StoredProcedure };
                foreach (var p in copias)
                    cmd.Parameters.Add(Duplicar(p));
                ejecutando();
                cmd.ExecuteNonQuery();
            });
        }

        public bool EjecutarQuery(string sql, params SqlParameter[] parameters)
        {
            if (!HayEspejo("query")) return !EsperabaEspejo;

            _logger.LogInformation("[Sync] Ejecutando query en BD espejo: {SQL}", sql);

            return ConReintentos($"query '{sql}' ({Formatear(parameters)})", reintentarTimeoutEnEjecucion: true, (conn, ejecutando) =>
            {
                using var cmd = new SqlCommand(sql, conn);
                foreach (var p in parameters)
                    cmd.Parameters.Add(Duplicar(p));
                ejecutando();
                var rows = cmd.ExecuteNonQuery();
                _logger.LogInformation("[Sync] Query ejecutado en BD espejo, {Rows} filas afectadas", rows);
            });
        }

        public bool EjecutarQueries(IReadOnlyList<(string Sql, SqlParameter[] Parametros)> consultas)
        {
            if (consultas == null || consultas.Count == 0) return true;
            if (!HayEspejo($"{consultas.Count} consultas")) return !EsperabaEspejo;

            _logger.LogInformation("[Sync] Ejecutando {Cantidad} consultas en BD espejo (una conexion, una transaccion)", consultas.Count);

            var resumen = string.Join(" | ", consultas.Select(c => Formatear(c.Parametros)));

            // Todo o nada; son UPDATE idempotentes, por eso se puede reintentar el lote completo
            return ConReintentos($"lote de {consultas.Count} consultas ({Recortar(resumen, 400)})", reintentarTimeoutEnEjecucion: true, (conn, ejecutando) =>
            {
                using var tx = conn.BeginTransaction();
                try
                {
                    ejecutando();
                    int filas = 0;
                    foreach (var (sql, parametros) in consultas)
                    {
                        using var cmd = new SqlCommand(sql, conn, tx);
                        foreach (var p in parametros)
                            cmd.Parameters.Add(Duplicar(p));
                        filas += cmd.ExecuteNonQuery();
                    }
                    tx.Commit();
                    _logger.LogInformation("[Sync] Lote ejecutado en BD espejo, {Filas} filas afectadas", filas);
                }
                catch
                {
                    try { tx.Rollback(); } catch { /* la conexion ya estaba cerrada */ }
                    throw;
                }
            });
        }

        // ---------------------------------------------------------------

        private bool EsperabaEspejo => _motivoEspejoNoDisponible != null;

        /// <summary>
        /// true si hay espejo al cual escribir. Si el usuario no tiene espejo configurado no es un error.
        /// Si lo tenia y no se pudo preparar, se deja constancia con [SYNC-FALLO].
        /// </summary>
        private bool HayEspejo(string operacion)
        {
            if (_connectionStringSync != null) return true;

            if (EsperabaEspejo)
                _logger.LogError("[SYNC-FALLO] No se aplico {Operacion} en el espejo: {Motivo}", operacion, _motivoEspejoNoDisponible);

            return false;
        }

        /// <summary>
        /// Abre la conexion y ejecuta la operacion con reintentos ante errores transitorios.
        /// El callback recibe un Action que debe invocar justo antes de ejecutar el comando, para saber si el
        /// error ocurrio al conectar (siempre reintentable) o durante la ejecucion.
        /// </summary>
        private bool ConReintentos(string descripcion, bool reintentarTimeoutEnEjecucion, Action<SqlConnection, Action> operacion)
        {
            for (int intento = 1; intento <= MaxIntentos; intento++)
            {
                bool ejecutando = false;
                try
                {
                    using var conn = new SqlConnection(_connectionStringSync);
                    conn.Open();
                    operacion(conn, () => ejecutando = true);

                    if (intento > 1)
                        _logger.LogWarning("[Sync] {Descripcion} se aplico en el intento {Intento}", descripcion, intento);
                    else
                        _logger.LogInformation("[Sync] {Descripcion} ejecutado exitosamente en BD espejo", descripcion);
                    return true;
                }
                catch (SqlException ex) when (intento < MaxIntentos && EsReintentable(ex, ejecutando, reintentarTimeoutEnEjecucion))
                {
                    _logger.LogWarning(ex, "[Sync] Error transitorio ({Numero}) en {Descripcion}; reintento {Intento}/{Max}", ex.Number, descripcion, intento, MaxIntentos);
                    Thread.Sleep(PausasMs[Math.Min(intento - 1, PausasMs.Length - 1)]);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[SYNC-FALLO] No se pudo aplicar en el espejo (intento {Intento}/{Max}): {Descripcion}", intento, MaxIntentos, descripcion);
                    return false;
                }
            }
            return false;
        }

        private static bool EsReintentable(SqlException ex, bool yaEjecutando, bool reintentarTimeoutEnEjecucion)
        {
            // Timeout durante la ejecucion de algo no idempotente: pudo haberse aplicado, no se repite
            if (ex.Number == -2 && yaEjecutando && !reintentarTimeoutEnEjecucion) return false;

            foreach (SqlError error in ex.Errors)
                if (ErroresTransitorios.Contains(error.Number)) return true;

            return ErroresTransitorios.Contains(ex.Number);
        }

        private static List<SqlParameter> ClonarParametros(SqlParameterCollection parametros)
        {
            var lista = new List<SqlParameter>();
            foreach (SqlParameter p in parametros)
            {
                lista.Add(new SqlParameter(p.ParameterName, p.SqlDbType, p.Size)
                {
                    Value = p.Value ?? DBNull.Value,
                    Direction = p.Direction
                });
            }
            return lista;
        }

        // Cada intento usa parametros nuevos (un SqlParameter solo puede estar en una coleccion a la vez)
        private static SqlParameter Duplicar(SqlParameter p) => (SqlParameter)((ICloneable)p).Clone();

        private static string Formatear(IEnumerable<SqlParameter> parametros)
        {
            var sb = new StringBuilder();
            foreach (var p in parametros)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(p.ParameterName).Append('=').Append(p.Value == null || p.Value is DBNull ? "NULL" : p.Value);
            }
            return Recortar(sb.ToString(), 300);
        }

        private static string Recortar(string s, int max) => s.Length <= max ? s : s.Substring(0, max) + "...";
    }
}
