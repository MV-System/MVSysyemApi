namespace MVSystemApi.MonitorSync
{
    /// <summary>
    /// Seguimiento en memoria de las diferencias detectadas: decide cuales son nuevas (ya confirmadas en
    /// varias revisiones seguidas) y cuales se resolvieron, para no avisar dos veces lo mismo.
    /// Se pierde al reiniciar el API (un aviso pendiente puede repetirse una vez). Asume una sola instancia.
    /// </summary>
    public class EstadoAlertas
    {
        private sealed class Seguimiento
        {
            public Hallazgo Hallazgo;
            public int Veces;
            public bool Notificado;
        }

        private readonly object _candado = new object();
        private readonly Dictionary<string, Seguimiento> _items = new Dictionary<string, Seguimiento>();

        /// <param name="confirmacionesMinimas">Revisiones seguidas necesarias para avisar un hallazgo de la revision liviana.
        /// Los de la revision completa (una vez por dia) se avisan de inmediato.</param>
        public (List<Hallazgo> Nuevos, List<Hallazgo> Resueltos) Procesar(ResultadoRevision revision, int confirmacionesMinimas)
        {
            var nuevos = new List<Hallazgo>();
            var resueltos = new List<Hallazgo>();
            var actuales = revision.Hallazgos
                .GroupBy(h => h.Clave)
                .ToDictionary(g => g.Key, g => g.First());

            lock (_candado)
            {
                foreach (var h in actuales.Values)
                {
                    if (!_items.TryGetValue(h.Clave, out var s))
                    {
                        s = new Seguimiento();
                        _items[h.Clave] = s;
                    }
                    s.Hallazgo = h;
                    s.Veces++;

                    int requeridas = h.Alcance == AlcanceRevision.Completa ? 1 : Math.Max(1, confirmacionesMinimas);
                    if (!s.Notificado && s.Veces >= requeridas)
                    {
                        s.Notificado = true;
                        nuevos.Add(h);
                    }
                }

                foreach (var clave in _items.Keys.ToList())
                {
                    if (actuales.ContainsKey(clave)) continue;

                    var s = _items[clave];
                    // La revision liviana no puede dar por resuelto algo que solo detecta la completa
                    if (s.Hallazgo.Alcance > revision.Alcance) continue;
                    // Si la empresa no se pudo revisar, no se sabe si se resolvio
                    if (revision.EmpresasConError.Contains(s.Hallazgo.Empresa)) continue;

                    if (s.Notificado) resueltos.Add(s.Hallazgo);
                    _items.Remove(clave);
                }
            }

            return (nuevos, resueltos);
        }

        /// <summary>Diferencias ya avisadas que siguen pendientes.</summary>
        public List<Hallazgo> Pendientes()
        {
            lock (_candado)
                return _items.Values.Where(s => s.Notificado).Select(s => s.Hallazgo).ToList();
        }
    }
}
