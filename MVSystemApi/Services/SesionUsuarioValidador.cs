using System.Collections.Concurrent;
using System.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;

namespace MVSystemApi.Services
{
    /// <summary>
    /// Decide si el token de un usuario sigue valiendo: el usuario debe estar activo (Estado = 1) y el token no debe haberse
    /// emitido antes de Usuarios.TokensValidosDesde. El estado se guarda 30 s en memoria por usuario; al desactivar o
    /// desconectar se invalida esa entrada para que el corte sea inmediato en este servidor.
    /// Si SEGURIDAD no responde se usa el ultimo estado conocido (o se deja pasar si nunca se leyo): una caida de la base
    /// no debe sacar a todos los usuarios.
    /// </summary>
    public class SesionUsuarioValidador
    {
        private sealed record EstadoUsuario(bool Activo, DateTime? ValidosDesdeUtc);

        private static readonly TimeSpan Vigencia = TimeSpan.FromSeconds(30);
        private readonly string _connectionString;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SesionUsuarioValidador> _logger;
        private readonly ConcurrentDictionary<int, EstadoUsuario> _ultimoConocido = new();

        public SesionUsuarioValidador(IConfiguration configuration, IMemoryCache cache, ILogger<SesionUsuarioValidador> logger)
        {
            _connectionString = configuration.GetConnectionString("MVSystemSeguridad");
            _cache = cache;
            _logger = logger;
        }

        /// <summary>emitidoUtc = momento en que se emitio el token (claim iat); null en tokens anteriores a este control.</summary>
        public async Task<bool> EsValidaAsync(int codigoUsuario, DateTime? emitidoUtc)
        {
            var estado = await ObtenerEstadoAsync(codigoUsuario);
            if (estado == null) return true;               // sin dato (error de base y nunca leido): se deja pasar
            if (!estado.Activo) return false;
            if (estado.ValidosDesdeUtc.HasValue && emitidoUtc.HasValue && emitidoUtc.Value < estado.ValidosDesdeUtc.Value) return false;
            return true;
        }

        private async Task<EstadoUsuario> ObtenerEstadoAsync(int codigoUsuario)
        {
            var clave = $"sesion-usuario:{codigoUsuario}";
            if (_cache.TryGetValue(clave, out EstadoUsuario enCache)) return enCache;

            try
            {
                using var cn = new SqlConnection(_connectionString);
                await cn.OpenAsync();
                using var cmd = new SqlCommand("SELECT Estado, TokensValidosDesde FROM dbo.Usuarios WHERE Codigo = @Codigo", cn);
                cmd.Parameters.AddWithValue("@Codigo", codigoUsuario);
                using var rd = await cmd.ExecuteReaderAsync();

                EstadoUsuario estado;
                if (await rd.ReadAsync())
                {
                    var activo = !rd.IsDBNull(0) && Convert.ToInt32(rd.GetValue(0)) == 1;
                    DateTime? desde = rd.IsDBNull(1) ? null : DateTime.SpecifyKind(rd.GetDateTime(1), DateTimeKind.Utc);
                    estado = new EstadoUsuario(activo, desde);
                }
                else
                {
                    estado = new EstadoUsuario(false, null);   // el usuario ya no existe
                }

                _ultimoConocido[codigoUsuario] = estado;
                _cache.Set(clave, estado, Vigencia);
                return estado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Sesion] No se pudo validar el usuario {Codigo}; se usa el ultimo estado conocido", codigoUsuario);
                var ultimo = _ultimoConocido.TryGetValue(codigoUsuario, out var conocido) ? conocido : null;
                _cache.Set(clave, ultimo, Vigencia);   // no reintentar (ni registrar el error) en cada peticion durante 30 s
                return ultimo;
            }
        }

        /// <summary>Hace que el siguiente token del usuario se revalide de inmediato (tras desactivar o desconectar).</summary>
        public void Invalidar(int codigoUsuario) => _cache.Remove($"sesion-usuario:{codigoUsuario}");

        /// <summary>Desconecta a un usuario por su login: sus tokens actuales dejan de valer. false si el login no existe.</summary>
        public async Task<bool> DesconectarAsync(string login)
        {
            using var cn = new SqlConnection(_connectionString);
            await cn.OpenAsync();
            int? codigo;
            using (var buscar = new SqlCommand("SELECT TOP (1) Codigo FROM dbo.Usuarios WHERE Login = @Login", cn))
            {
                buscar.Parameters.AddWithValue("@Login", login);
                var r = await buscar.ExecuteScalarAsync();
                codigo = r == null || r == DBNull.Value ? null : Convert.ToInt32(r);
            }
            if (codigo == null) return false;

            using (var corte = new SqlCommand("UPDATE dbo.Usuarios SET TokensValidosDesde = GETUTCDATE() WHERE Codigo = @Codigo", cn))
            {
                corte.Parameters.AddWithValue("@Codigo", codigo.Value);
                await corte.ExecuteNonQueryAsync();
            }
            using (var sesiones = new SqlCommand("UPDATE dbo.SesionesUsuario SET Activa = 0 WHERE Usuario = @Login AND Activa = 1", cn))
            {
                sesiones.Parameters.AddWithValue("@Login", login);
                await sesiones.ExecuteNonQueryAsync();
            }
            Invalidar(codigo.Value);
            return true;
        }
    }
}
