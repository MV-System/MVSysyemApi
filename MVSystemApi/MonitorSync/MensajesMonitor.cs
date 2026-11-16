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

        /// <summary>Los correos del monitor van en HTML; el notificador lo detecta por este inicio.</summary>
        public const string EstiloHtml = "<html><body style=\"font-family:Segoe UI,Arial,sans-serif;font-size:14px;color:#222\">";

        private static string E(string s) => System.Net.WebUtility.HtmlEncode(s ?? "");

        private const string Celda = "border:1px solid #ccc;padding:5px 9px;";

        private static string TablaCantidades(ResultadoRevision revision, List<Hallazgo> hallazgos)
        {
            if (revision == null || revision.Empresas.Count == 0) return "";
            var sb = new StringBuilder();
            sb.Append("<h3>Cantidades disponibles</h3>");
            sb.Append("<table style=\"border-collapse:collapse\">");
            sb.Append($"<tr style=\"background:#e9eef5\"><th rowspan=\"2\" style=\"{Celda}text-align:left\">Empresa</th>");
            sb.Append($"<th colspan=\"2\" style=\"{Celda}\">Equipos</th><th colspan=\"2\" style=\"{Celda}\">Accesorios (modelos / existencia)</th>");
            sb.Append($"<th rowspan=\"2\" style=\"{Celda}\">Estado</th></tr>");
            sb.Append($"<tr style=\"background:#e9eef5\"><th style=\"{Celda}\">Principal</th><th style=\"{Celda}\">Espejo</th><th style=\"{Celda}\">Principal</th><th style=\"{Celda}\">Espejo</th></tr>");
            foreach (var e in revision.Empresas.OrderBy(x => x.Nombre))
            {
                var dif = hallazgos.Count(h => h.Empresa == e.Nombre);
                var distinto = dif > 0 || e.EquiposPrincipal != e.EquiposEspejo || e.AccesoriosPrincipal != e.AccesoriosEspejo || e.ExistenciaPrincipal != e.ExistenciaEspejo;
                var fondo = distinto ? "background:#fde8e8;" : "";
                var estado = distinto ? $"&#10006; {(dif > 0 ? dif.ToString() : "")}" : "&#10004;";
                sb.Append($"<tr style=\"{fondo}\"><td style=\"{Celda}\">{E(e.Nombre)}</td>");
                sb.Append($"<td style=\"{Celda}text-align:right\">{e.EquiposPrincipal:N0}</td><td style=\"{Celda}text-align:right\">{e.EquiposEspejo:N0}</td>");
                sb.Append($"<td style=\"{Celda}text-align:right\">{e.AccesoriosPrincipal:N0} / {e.ExistenciaPrincipal:N0}</td><td style=\"{Celda}text-align:right\">{e.AccesoriosEspejo:N0} / {e.ExistenciaEspejo:N0}</td>");
                sb.Append($"<td style=\"{Celda}text-align:center\">{estado}</td></tr>");
            }
            sb.Append("</table>");
            return sb.ToString();
        }

        private static string DetalleHtml(IEnumerable<Hallazgo> hallazgos, int maxPorCategoria)
        {
            var sb = new StringBuilder();
            foreach (var empresa in hallazgos.GroupBy(h => h.Empresa).OrderBy(g => g.Key))
            {
                sb.Append($"<p><b>{E(empresa.Key)}</b></p><ul>");
                foreach (var categoria in empresa.GroupBy(h => h.Categoria).OrderBy(g => g.Key))
                {
                    var lista = categoria.ToList();
                    sb.Append($"<li>{E(categoria.Key)} ({lista.Count})<ul>");
                    foreach (var h in lista.Take(maxPorCategoria)) sb.Append($"<li>{E(h.Detalle)}</li>");
                    if (lista.Count > maxPorCategoria) sb.Append($"<li>... y {lista.Count - maxPorCategoria} mas</li>");
                    sb.Append("</ul></li>");
                }
                sb.Append("</ul>");
            }
            return sb.ToString();
        }

        public static (string Asunto, string Cuerpo, string Corto) Nuevos(List<Hallazgo> nuevos, ResultadoRevision revision, int maxPorCategoria)
        {
            var asunto = $"[MVSystem] Sincronizacion: {nuevos.Count} diferencia(s) nueva(s)";
            var html = new StringBuilder(EstiloHtml);
            html.Append($"<p>Se detectaron <b>{nuevos.Count}</b> diferencia(s) entre la base principal y su espejo ({FechaRD()}, hora RD).<br>");
            html.Append("Persistieron en revisiones seguidas, asi que no son una sincronizacion en curso.</p>");
            html.Append(TablaCantidades(revision, nuevos));
            html.Append("<h3>Diferencias nuevas</h3>");
            html.Append(DetalleHtml(nuevos, maxPorCategoria));
            html.Append("<p>Para investigar: consola DbCompare (opcion 1 para ver diferencias, 2 para el historial de los equipos, 11 para los accesorios).</p>");

            return (asunto, html.ToString(), Corto("Nuevas diferencias de sincronizacion", nuevos));
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

            var sb = new StringBuilder(EstiloHtml);
            sb.Append($"<p>Resumen diario ({FechaRD()}, hora RD). Este correo confirma que el monitor sigue en funcionamiento.</p>");
            if (ultima != null) sb.Append(TablaCantidades(ultima, pendientes));
            if (pendientes.Count == 0)
                sb.Append("<p>Sin diferencias pendientes.</p>");
            else
            {
                sb.Append($"<h3>Diferencias pendientes ({pendientes.Count})</h3>");
                sb.Append(DetalleHtml(pendientes, maxPorCategoria));
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
