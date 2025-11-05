using System;
using System.ComponentModel.DataAnnotations;

namespace MVSystemApi.Model
{
    /// <summary>
    /// Modelo de registro de actividad de usuario en el sistema
    /// </summary>
    public class TrackingLog
    {
        [Key]
        public int IdTracking { get; set; }

        [Required]
        [StringLength(100)]
        public string Usuario { get; set; }

        [Required]
        [StringLength(200)]
        public string Modulo { get; set; }

        [Required]
        [StringLength(200)]
        public string Accion { get; set; }

        public DateTime FechaHora { get; set; }

        [StringLength(500)]
        public string Metadata { get; set; }

        [StringLength(50)]
        public string DireccionIP { get; set; }

        [StringLength(500)]
        public string UserAgent { get; set; }

        public bool NotificacionEnviada { get; set; }

        public DateTime? FechaNotificacion { get; set; }

        public TrackingLog()
        {
            FechaHora = DateTime.Now;
            NotificacionEnviada = false;
        }
    }
}
