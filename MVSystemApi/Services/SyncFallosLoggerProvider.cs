namespace MVSystemApi.Services
{
    /// <summary>
    /// Guarda en un archivo permanente cada linea de log con [SYNC-FALLO] (fallos al copiar a la base espejo).
    /// En Azure App Service va a %HOME%\LogFiles\sync-fallos-AAAAMM.log (se ve en Kudu y no se borra a las 12 horas);
    /// en local, a la carpeta logs del proyecto. Nunca lanza: si no puede escribir, sigue sin afectar la operacion.
    /// </summary>
    public sealed class SyncFallosLoggerProvider : ILoggerProvider
    {
        private static readonly object Candado = new();
        private readonly string _carpeta;

        public SyncFallosLoggerProvider(string contentRoot)
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            var logFiles = string.IsNullOrEmpty(home) ? null : Path.Combine(home, "LogFiles");
            _carpeta = logFiles != null && Directory.Exists(logFiles) ? logFiles : Path.Combine(contentRoot, "logs");
        }

        public ILogger CreateLogger(string categoria) => new Registro(this);

        public void Dispose() { }

        private void Escribir(string linea)
        {
            try
            {
                Directory.CreateDirectory(_carpeta);
                var ahora = DateTime.UtcNow.AddHours(-4);   // hora de RD
                var archivo = Path.Combine(_carpeta, $"sync-fallos-{ahora:yyyyMM}.log");
                lock (Candado)
                    File.AppendAllText(archivo, $"{ahora:yyyy-MM-dd HH:mm:ss} {linea}{Environment.NewLine}");
            }
            catch { /* el log nunca debe afectar la operacion */ }
        }

        private sealed class Registro : ILogger
        {
            private readonly SyncFallosLoggerProvider _proveedor;
            public Registro(SyncFallosLoggerProvider proveedor) => _proveedor = proveedor;

            public IDisposable BeginScope<TState>(TState state) => null;

            public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                if (!IsEnabled(logLevel)) return;
                var mensaje = formatter(state, exception);
                if (mensaje == null || !mensaje.Contains("[SYNC-FALLO]")) return;
                _proveedor.Escribir(exception == null ? mensaje : $"{mensaje} | {exception.GetType().Name}: {exception.Message}");
            }
        }
    }
}
