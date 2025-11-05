using System.ComponentModel.DataAnnotations;

namespace MVSystemApi.Model
{
    /// <summary>
    /// DTO para recibir logs de tracking desde el frontend
    /// </summary>
    public class TrackingLogDto
    {
        [Required]
        public string Usuario { get; set; }

        [Required]
        public string Modulo { get; set; }

        [Required]
        public string Accion { get; set; }

        public string Metadata { get; set; }
    }

    /// <summary>
    /// DTO para enviar mensajes de WhatsApp
    /// </summary>
    public class WhatsAppMessageDto
    {
        [Required]
        [Phone]
        public string PhoneNumber { get; set; }

        [Required]
        public string Message { get; set; }
    }

    /// <summary>
    /// DTO de respuesta de operaciones de tracking
    /// </summary>
    public class TrackingResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
    }

    /// <summary>
    /// DTO para configuración de notificaciones
    /// </summary>
    public class TrackingNotificationConfig
    {
        public string Modulo { get; set; }
        public bool NotificarAcceso { get; set; }
        public bool NotificarAcciones { get; set; }
        public string NumeroWhatsApp { get; set; }
    }
}
