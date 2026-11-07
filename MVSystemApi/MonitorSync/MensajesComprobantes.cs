using System.Text;

namespace MVSystemApi.MonitorSync
{
    /// <summary>Texto de los avisos de comprobantes con problemas: empresa, factura, comprobante (e-NCF) y el error.</summary>
    public static class MensajesComprobantes
    {
        private static string FechaRD() => DateTime.UtcNow.AddHours(-4).ToString("dd/MM/yyyy HH:mm");

        private static string Proveedor(int p) => p == 2 ? "MicroStar (proveedor 2)" : "EasyConexión (proveedor 1)";

        private static string Documento(ItemSeguimiento i) => i.EsNotaCredito ? "Nota de crédito" : "Factura";

        private static string Recortar(string texto, int max = 400)
        {
            if (string.IsNullOrWhiteSpace(texto)) return "(sin detalle)";
            texto = texto.Replace("\r", " ").Replace("\n", " ").Trim();
            return texto.Length <= max ? texto : texto.Substring(0, max) + "…";
        }

        private static string Cuerpo(ItemSeguimiento i, string linea1, string estado, string detalle)
        {
            var sb = new StringBuilder();
            sb.AppendLine(linea1);
            sb.AppendLine();
            sb.AppendLine($"Empresa:     {i.Empresa} (RNC {i.Rnc})");
            sb.AppendLine($"Documento:   {Documento(i)} #{i.NumeroFactura}" + (string.IsNullOrWhiteSpace(i.Literal) ? "" : $" (literal {i.Literal.Trim()})"));
            sb.AppendLine($"Comprobante: {i.Encf}" + (i.TipoNcf > 0 ? $" (tipo {i.TipoNcf})" : ""));
            sb.AppendLine($"Proveedor:   {Proveedor(i.Proveedor)}");
            if (!string.IsNullOrWhiteSpace(i.Usuario)) sb.AppendLine($"Usuario:     {i.Usuario}");
            sb.AppendLine($"Estado:      {estado}");
            sb.AppendLine($"Detalle:     {Recortar(detalle)}");
            sb.AppendLine($"Hora (RD):   {FechaRD()}");
            return sb.ToString();
        }

        private static string Corto(ItemSeguimiento i, string que) =>
            $"MVSystem: {que} - {i.Empresa}, {Documento(i).ToLowerInvariant()} #{i.NumeroFactura}, {i.Encf}.";

        public static (string Asunto, string Cuerpo, string Corto) Problema(ItemSeguimiento i, string estado, string detalle) =>
            ($"[MVSystem] Comprobante con problema: {i.Empresa} - {Documento(i).ToLowerInvariant()} #{i.NumeroFactura} ({i.Encf})",
             Cuerpo(i, "El proveedor reporta un problema con un comprobante electrónico.", estado, detalle),
             Corto(i, "comprobante con problema (" + estado + ")"));

        public static (string Asunto, string Cuerpo, string Corto) Observaciones(ItemSeguimiento i, string detalle) =>
            ($"[MVSystem] Comprobante aceptado con observaciones: {i.Empresa} - {Documento(i).ToLowerInvariant()} #{i.NumeroFactura} ({i.Encf})",
             Cuerpo(i, "La DGII aceptó el comprobante pero con observaciones. Revíselo con el proveedor.", "Aceptado con observaciones", detalle),
             Corto(i, "aceptado con observaciones"));

        public static (string Asunto, string Cuerpo, string Corto) SinRespuesta(ItemSeguimiento i, int minutos) =>
            ($"[MVSystem] Comprobante sin respuesta: {i.Empresa} - {Documento(i).ToLowerInvariant()} #{i.NumeroFactura} ({i.Encf})",
             Cuerpo(i, $"El comprobante sigue sin respuesta del proveedor después de {minutos} minutos.", "Sin respuesta", "El proveedor no ha dado un estado final."),
             Corto(i, $"sin respuesta en {minutos} min"));

        public static (string Asunto, string Cuerpo, string Corto) Resuelto(ItemSeguimiento i) =>
            ($"[MVSystem] Comprobante resuelto: {i.Empresa} - {Documento(i).ToLowerInvariant()} #{i.NumeroFactura} ({i.Encf})",
             Cuerpo(i, "El comprobante que había dado problemas ya fue aceptado por la DGII.", "Aceptado", "Resuelto."),
             Corto(i, "resuelto"));
    }
}
