using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using MVSystemApi.Model_Negocio;

namespace MVSystemApi.MonitorSync
{
    /// <summary>Envia los avisos por correo (canal principal) y, si esta habilitado, por WhatsApp.</summary>
    public class NotificadorAlertas
    {
        private readonly MonitorOptions _opciones;
        private readonly WhatsAppService _whatsApp;
        private readonly ILogger<NotificadorAlertas> _logger;

        public NotificadorAlertas(IOptions<MonitorOptions> opciones, WhatsAppService whatsApp, ILogger<NotificadorAlertas> logger)
        {
            _opciones = opciones.Value;
            _whatsApp = whatsApp;
            _logger = logger;
        }

        /// <summary>Devuelve un texto breve con lo que paso en cada canal (sirve para la prueba y para el log).</summary>
        public async Task<string> EnviarAsync(string asunto, string cuerpo, string textoCorto)
        {
            var correo = await EnviarCorreoAsync(asunto, cuerpo);
            var whatsApp = await EnviarWhatsAppAsync(textoCorto);
            return $"correo: {correo}; whatsapp: {whatsApp}";
        }

        private async Task<string> EnviarCorreoAsync(string asunto, string cuerpo)
        {
            var c = _opciones.Correo;
            if (!c.Habilitado) return "deshabilitado";
            if (string.IsNullOrWhiteSpace(c.Host) || string.IsNullOrWhiteSpace(c.Usuario) || string.IsNullOrWhiteSpace(c.Password) || c.Destinatarios.Count == 0)
            {
                _logger.LogWarning("[Monitor] Correo habilitado pero falta configuracion (Host, Usuario, Password o Destinatarios)");
                return "falta configuracion";
            }

            try
            {
                using var smtp = new SmtpClient(c.Host, c.Port)
                {
                    EnableSsl = c.UsarSsl,
                    Credentials = new NetworkCredential(c.Usuario, c.Password),
                    Timeout = 30000,
                };
                using var mensaje = new MailMessage
                {
                    From = new MailAddress(string.IsNullOrWhiteSpace(c.Remitente) ? c.Usuario : c.Remitente, "MVSystem Monitor"),
                    Subject = asunto,
                    Body = cuerpo,
                    IsBodyHtml = false,
                };
                foreach (var destinatario in c.Destinatarios.Where(d => !string.IsNullOrWhiteSpace(d)))
                    mensaje.To.Add(destinatario.Trim());

                await smtp.SendMailAsync(mensaje);
                _logger.LogInformation("[Monitor] Correo enviado: {Asunto}", asunto);
                return "enviado";
            }
            catch (Exception ex)
            {
                // Nunca se registra la contrasena
                _logger.LogError(ex, "[Monitor] No se pudo enviar el correo '{Asunto}'", asunto);
                return "error: " + ex.Message;
            }
        }

        private async Task<string> EnviarWhatsAppAsync(string texto)
        {
            var w = _opciones.WhatsApp;
            if (!w.Habilitado) return "deshabilitado";
            if (w.Numeros.Count == 0) return "falta configuracion";

            try
            {
                // El servidor de WhatsApp (Node) debe estar activo y vinculado; si no, se omite sin afectar el correo
                if (!await _whatsApp.VerificarEstado())
                {
                    _logger.LogWarning("[Monitor] WhatsApp habilitado pero el servidor no esta disponible; se omite");
                    return "servidor no disponible";
                }

                int enviados = 0;
                foreach (var numero in w.Numeros.Where(n => !string.IsNullOrWhiteSpace(n)))
                    if (await _whatsApp.EnviarMensaje(numero, texto)) enviados++;

                return $"enviado a {enviados} de {w.Numeros.Count}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Monitor] No se pudo enviar el WhatsApp");
                return "error: " + ex.Message;
            }
        }
    }
}
