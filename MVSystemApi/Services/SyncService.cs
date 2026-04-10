using Microsoft.Extensions.Caching.Memory;
using MVSystemApi.Interfaz;
using MVSystemApi.Model_Negocio.Seguridad;
using System.Data;
using System.Data.SqlClient;

namespace MVSystemApi.Services
{
    public class SyncService : ISyncService
    {
        private readonly string _connectionStringSync;
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
                        _logger.LogWarning("[Sync] No se encontró StringCon para empresa espejo {IdEmpresaE}", idEmpresaE);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Sync] Error inicializando SyncService");
            }
        }

        public void Ejecutar(string spName, SqlParameterCollection parameters)
        {
            if (_connectionStringSync == null) return;

            _logger.LogInformation("[Sync] Ejecutando SP: {SP} en BD espejo", spName);

            try
            {
                using var conn = new SqlConnection(_connectionStringSync);
                conn.Open();
                using var cmd = new SqlCommand(spName, conn);
                cmd.CommandType = CommandType.StoredProcedure;

                foreach (SqlParameter p in parameters)
                {
                    var clone = new SqlParameter(p.ParameterName, p.SqlDbType, p.Size)
                    {
                        Value = p.Value ?? DBNull.Value,
                        Direction = p.Direction
                    };
                    cmd.Parameters.Add(clone);
                    _logger.LogDebug("[Sync] Param: {Name} = {Value}", p.ParameterName, p.Value);
                }

                cmd.ExecuteNonQuery();
                _logger.LogInformation("[Sync] SP {SP} ejecutado exitosamente en BD espejo", spName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Sync] ERROR ejecutando SP: {SP} en BD espejo", spName);
            }
        }

        public void EjecutarQuery(string sql, params SqlParameter[] parameters)
        {
            if (_connectionStringSync == null) return;

            _logger.LogInformation("[Sync] Ejecutando query en BD espejo: {SQL}", sql);

            try
            {
                using var conn = new SqlConnection(_connectionStringSync);
                conn.Open();
                using var cmd = new SqlCommand(sql, conn);

                foreach (var p in parameters)
                {
                    cmd.Parameters.Add(p);
                    _logger.LogDebug("[Sync] Param: {Name} = {Value}", p.ParameterName, p.Value);
                }

                var rows = cmd.ExecuteNonQuery();
                _logger.LogInformation("[Sync] Query ejecutado en BD espejo, {Rows} filas afectadas", rows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Sync] ERROR ejecutando query en BD espejo");
            }
        }
    }
}
