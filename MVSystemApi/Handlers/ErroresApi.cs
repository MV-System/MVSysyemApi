using MVSystemApi.Model;

namespace MVSystemApi.Handlers
{
    /// <summary>
    /// Decide que mensaje de una excepcion puede ver el usuario. Los mensajes de negocio (validaciones, THROW 50xxx de los SP,
    /// NotaCreditoException, Exception con texto propio) se muestran tal cual. Los tecnicos (errores de SQL, referencias nulas,
    /// red, etc.) se registran completos en el log y el usuario solo ve un mensaje generico con una referencia para soporte.
    /// </summary>
    public static class ErroresApi
    {
        public const string MensajeGenerico = "Ocurrió un error. Intente de nuevo o contacte a soporte.";

        private static ILogger _logger;

        private static readonly string[] MarcasTecnicas =
        {
            "invalid object name", "invalid column name", "incorrect syntax", "violation of", "cannot insert", "cannot open database",
            "login failed", "conversion failed", "the statement has been terminated", "string or binary data", "deadlock",
            "timeout expired", "a network-related", "provider:", "microsoft.data", "system.data", "object reference",
            "sqlexception", "connection string", "   at ", "could not find stored procedure", "procedure or function",
        };

        public static void Inicializar(ILoggerFactory fabrica) => _logger = fabrica.CreateLogger("MVSystemApi.Errores");

        public static string NuevoId() => Guid.NewGuid().ToString("N").Substring(0, 8);

        /// <summary>Mensaje seguro para mostrar al usuario. Si es tecnico lo registra en el log con su referencia.</summary>
        public static string Mensaje(Exception ex)
        {
            if (ex == null) return MensajeGenerico;
            if (EsDeNegocio(ex)) return ex.Message;

            var id = NuevoId();
            Registrar(ex, id);
            return $"{MensajeGenerico} (Ref: {id})";
        }

        public static void Registrar(Exception ex, string id) =>
            _logger?.LogError(ex, "[Error {Id}] {Mensaje}", id, ex?.Message);

        private static bool EsDeNegocio(Exception ex)
        {
            if (string.IsNullOrWhiteSpace(ex.Message)) return false;
            if (EsTecnico(ex.Message)) return false;

            if (ex is NotaCreditoException) return true;

            // THROW propios de los procedimientos almacenados (numero >= 50000)
            if (ex is System.Data.SqlClient.SqlException s1) return s1.Number >= 50000;
            if (ex is Microsoft.Data.SqlClient.SqlException s2) return s2.Number >= 50000;

            // Exception / ApplicationException con texto propio: asi se lanzan las validaciones de negocio
            var tipo = ex.GetType();
            return tipo == typeof(Exception) || tipo == typeof(ApplicationException);
        }

        private static bool EsTecnico(string mensaje)
        {
            var m = mensaje.ToLowerInvariant();
            foreach (var marca in MarcasTecnicas)
                if (m.Contains(marca)) return true;
            return false;
        }
    }
}
