namespace MVSystemApi.MonitorSync
{
    /// <summary>
    /// Configuracion del monitor de sincronizacion (seccion "Monitor" de appsettings).
    /// Los secretos (contrasena del correo) NO van en el repositorio: se configuran como variables de la
    /// aplicacion en el servidor (por ejemplo Monitor__Correo__Password).
    /// </summary>
    public class MonitorOptions
    {
        /// <summary>Apagado por defecto: publicar el API no empieza a enviar alertas hasta configurarlo.</summary>
        public bool Habilitado { get; set; } = false;

        /// <summary>Cada cuantos minutos se hace la revision liviana.</summary>
        public int IntervaloMinutos { get; set; } = 15;

        /// <summary>Hora (hora de Republica Dominicana, UTC-4) de la revision completa nocturna. Formato HH:mm.</summary>
        public string HoraRevisionCompleta { get; set; } = "02:00";

        /// <summary>Hora del resumen diario (tambien confirma que el monitor sigue vivo). Formato HH:mm.</summary>
        public string HoraResumenDiario { get; set; } = "07:00";

        /// <summary>Revisiones seguidas en que debe persistir una diferencia antes de avisarla (evita avisar por una
        /// sincronizacion que apenas esta en curso).</summary>
        public int ConfirmacionesMinimas { get; set; } = 2;

        /// <summary>Maximo de elementos detallados por categoria en un aviso.</summary>
        public int MaxItemsPorAviso { get; set; } = 25;

        public List<ParEmpresa> Pares { get; set; } = new List<ParEmpresa>
        {
            // Rol "principal" = la base donde esta la verdad; "espejo" = la copia. Ver ValidadorEspejos de DbCompare.
            new ParEmpresa { Nombre = "GAPMOVILES",      IdPrincipal = 30, IdEspejo = 1 },
            new ParEmpresa { Nombre = "BESTPHONE_STORE", IdPrincipal = 4,  IdEspejo = 33 },
            new ParEmpresa { Nombre = "TECNOSTYLE",      IdPrincipal = 18, IdEspejo = 32 },
        };

        public CorreoOptions Correo { get; set; } = new CorreoOptions();
        public WhatsAppAlertaOptions WhatsApp { get; set; } = new WhatsAppAlertaOptions();

        /// <summary>Usuarios que pueden usar GET /Monitor/Probar. Vacio = nadie.</summary>
        public List<string> UsuariosAutorizados { get; set; } = new List<string>();
    }

    public class ParEmpresa
    {
        public string Nombre { get; set; }
        /// <summary>ID_Empresa (en SEGURIDAD.Empresas) de la base principal.</summary>
        public int IdPrincipal { get; set; }
        /// <summary>ID_Empresa (en SEGURIDAD.Empresas) de la base espejo.</summary>
        public int IdEspejo { get; set; }
    }

    public class CorreoOptions
    {
        public bool Habilitado { get; set; } = false;
        public string Host { get; set; } = "smtp.gmail.com";
        public int Port { get; set; } = 587;
        public bool UsarSsl { get; set; } = true;
        public string Usuario { get; set; }
        public string Password { get; set; }
        /// <summary>Si queda vacio se usa el Usuario.</summary>
        public string Remitente { get; set; }
        public List<string> Destinatarios { get; set; } = new List<string>();
    }

    public class WhatsAppAlertaOptions
    {
        /// <summary>Canal opcional: usa el WhatsAppService existente (necesita el servidor Node activo y vinculado).</summary>
        public bool Habilitado { get; set; } = false;
        public List<string> Numeros { get; set; } = new List<string>();
    }
}
