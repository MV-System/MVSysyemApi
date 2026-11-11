using System.Collections.Concurrent;

namespace MVSystemApi.Handlers
{
    /// <summary>
    /// Limita los intentos fallidos de login, en memoria. Por usuario: 5 fallos en 15 minutos bloquean ese usuario 15 minutos.
    /// Por IP el umbral es mayor (20): una tienda comparte una sola IP entre todos sus empleados y un solo usuario que se
    /// equivoca no debe bloquear a los demas. Un login correcto reinicia el contador del usuario.
    /// </summary>
    public class LimiteIntentosLogin
    {
        private sealed class Registro
        {
            public int Fallos;
            public DateTime Inicio;
            public DateTime? BloqueadoHasta;
        }

        private const int MaxFallosUsuario = 5;
        private const int MaxFallosIp = 20;
        private static readonly TimeSpan Ventana = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan Bloqueo = TimeSpan.FromMinutes(15);

        private readonly ConcurrentDictionary<string, Registro> _registros = new();

        private static string ClaveUsuario(string login) => "u:" + (login ?? "").Trim().ToLowerInvariant();
        private static string ClaveIp(string ip) => "ip:" + (ip ?? "").Trim();

        /// <summary>true si el usuario o la IP estan bloqueados; minutos = espera restante aproximada.</summary>
        public bool EstaBloqueado(string login, string ip, out int minutos)
        {
            minutos = 0;
            var ahora = DateTime.UtcNow;
            foreach (var clave in new[] { ClaveUsuario(login), ClaveIp(ip) })
            {
                if (_registros.TryGetValue(clave, out var r) && r.BloqueadoHasta.HasValue && r.BloqueadoHasta.Value > ahora)
                {
                    minutos = Math.Max(minutos, (int)Math.Ceiling((r.BloqueadoHasta.Value - ahora).TotalMinutes));
                }
            }
            return minutos > 0;
        }

        /// <summary>Registra un fallo. Devuelve cuantos fallos lleva el usuario en la ventana y cuantos intentos le quedan.</summary>
        public (int Fallos, int Restantes) RegistrarFallo(string login, string ip)
        {
            var fallos = Sumar(ClaveUsuario(login), MaxFallosUsuario);
            Sumar(ClaveIp(ip), MaxFallosIp);
            Limpiar();
            return (fallos, Math.Max(0, MaxFallosUsuario - fallos));
        }

        public static int MinutosDeBloqueo => (int)Bloqueo.TotalMinutes;

        public void RegistrarExito(string login) => _registros.TryRemove(ClaveUsuario(login), out _);

        private int Sumar(string clave, int maximo)
        {
            var ahora = DateTime.UtcNow;
            var r = _registros.GetOrAdd(clave, _ => new Registro { Inicio = ahora });
            lock (r)
            {
                if (ahora - r.Inicio > Ventana) { r.Fallos = 0; r.Inicio = ahora; r.BloqueadoHasta = null; }
                r.Fallos++;
                if (r.Fallos >= maximo) r.BloqueadoHasta = ahora + Bloqueo;
                return r.Fallos;
            }
        }

        private void Limpiar()
        {
            if (_registros.Count < 5000) return;
            var ahora = DateTime.UtcNow;
            foreach (var par in _registros)
                if (ahora - par.Value.Inicio > Ventana && (!par.Value.BloqueadoHasta.HasValue || par.Value.BloqueadoHasta < ahora))
                    _registros.TryRemove(par.Key, out _);
        }
    }
}
