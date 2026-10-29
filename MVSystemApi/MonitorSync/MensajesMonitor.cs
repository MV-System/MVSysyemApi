using System.Text;

namespace MVSystemApi.MonitorSync
{
    /// <summary>Texto de los avisos (correo completo y version corta para WhatsApp).</summary>
    public static class MensajesMonitor
    {
        private static string FechaRD() => DateTime.UtcNow.AddHours(-4).ToString("dd/MM/yyyy HH:mm");

        public static string Detalle(IEnumerable<Hallazgo> hallazgos, int maxPorCategoria)
        {
            var sb = new StringBuilder();
            foreach (var empresa in hallazgos.GroupBy(h => h.Empresa).OrderBy(g => g.Key))
            {
                sb.AppendLine($"== {empresa.Key} ==");
                foreach (var categoria in empresa.GroupBy(h => h.Categoria).OrderBy(g => g.Key))
                {
                    var lista = categoria.ToList();
                    sb.AppendLine($"  {categoria.Key} ({lista.Count}):");
                    foreach (var h in lista.Take(maxPorCategoria))
                        sb.AppendLine($"    - {h.Detalle}");
                    if (lista.Count > maxPorCategoria)
                        sb.AppendLine($"    ... y {lista.Count - maxPorCategoria} mas");
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public static (string Asunto, string Cuerpo, string Corto) Nuevos(List<Hallazgo> nuevos, ResultadoRevision revision, int maxPorCategoria)
        {
            var asunto = $"[MVSystem] Sincronizacion: {nuevos.Count} diferencia(s) nueva(s)";
            var sb = new StringBuilder();
            sb.AppendLine($"Se detectaron {nuevos.Count} diferencia(s) entre la base principal y su espejo ({FechaRD()}, hora RD).");
            sb.AppendLine("Persistieron en revisiones seguidas, asi que no son una sincronizacion en curso.");
            sb.AppendLine();
            sb.Append(Detalle(nuevos, maxPorCategoria));
            sb.AppendLine("Cantidades disponibles:");
            foreach (var linea in revision.Resumen) sb.AppendLine("  " + linea);
            sb.AppendLine();
            sb.AppendLine("Para investigar: consola DbCompare (opcion 1 para ver diferencias, 2 para el historial de los equipos).");

            return (asunto, sb.ToString(), Corto("Nuevas diferencias de sincronizacion", nuevos));
        }

        public static (string Asunto, string Cuerpo, string Corto) Resueltos(List<Hallazgo> resueltos)
        {
            var asunto = $"[MVSystem] Sincronizacion: {resueltos.Count} diferencia(s) resuelta(s)";
            var sb = new StringBuilder();
            sb.AppendLine($"Ya no aparecen {resueltos.Count} diferencia(s) avisadas antes ({FechaRD()}, hora RD):");
            sb.AppendLine();
            foreach (var g in resueltos.GroupBy(h => $"{h.Empresa} - {h.Categoria}").OrderBy(g => g.Key))
                sb.AppendLine($"  {g.Key}: {g.Count()}");

            return (asunto, sb.ToString(), $"MVSystem: {resueltos.Count} diferencia(s) de sincronizacion resuelta(s).");
        }

        public static (string Asunto, string Cuerpo, string Corto) ResumenDiario(ResultadoRevision ultima, List<Hallazgo> pendientes, int maxPorCategoria)
        {
            var asunto = pendientes.Count == 0
                ? "[MVSystem] Resumen diario de sincronizacion: todo en orden"
                : $"[MVSystem] Resumen diario de sincronizacion: {pendientes.Count} diferencia(s) pendiente(s)";

            var sb = new StringBuilder();
            sb.AppendLine($"Resumen diario ({FechaRD()}, hora RD). Este correo confirma que el monitor sigue en funcionamiento.");
            sb.AppendLine();
            if (ultima != null)
            {
                sb.AppendLine("Cantidades disponibles en la ultima revision:");
                foreach (var linea in ultima.Resumen) sb.AppendLine("  " + linea);
                sb.AppendLine();
            }
            if (pendientes.Count == 0)
            {
                sb.AppendLine("Sin diferencias pendientes.");
            }
            else
            {
                sb.AppendLine($"Diferencias pendientes ({pendientes.Count}):");
                sb.AppendLine();
                sb.Append(Detalle(pendientes, maxPorCategoria));
            }

            var corto = pendientes.Count == 0
                ? "MVSystem: sincronizacion en orden (resumen diario)."
                : Corto("Diferencias de sincronizacion pendientes", pendientes);
            return (asunto, sb.ToString(), corto);
        }

        /// <summary>Texto breve para WhatsApp: conteo por empresa y categoria.</summary>
        private static string Corto(string titulo, List<Hallazgo> hallazgos)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"MVSystem - {titulo} ({hallazgos.Count}):");
            foreach (var g in hallazgos.GroupBy(h => $"{h.Empresa}: {h.Categoria}").OrderBy(g => g.Key).Take(10))
                sb.AppendLine($"- {g.Key} x{g.Count()}");
            sb.AppendLine("Detalle en el correo.");
            return sb.ToString();
        }
    }
}
