using System;
using System.ComponentModel.DataAnnotations;

namespace MVSystemApi.Model
{
    /// <summary>
    /// Modelo de sesión de inventario
    /// </summary>
    public class InventarioSesion
    {
        [Key]
        public long ID_Sesion { get; set; }

        [Required]
        public int ID_Almacen { get; set; }

        [Required]
        public int ID_Empresa { get; set; }

        [Required]
        public int ID_Sucursal { get; set; }

        [Required]
        [StringLength(20)]
        public string Estado { get; set; } // EN_PROCESO, PAUSADO, FINALIZADO, CANCELADO

        public int Total_Esperados { get; set; }
        public int Total_Escaneados { get; set; }
        public int Total_Faltantes { get; set; }
        public int Total_Sobrantes { get; set; }
        public decimal? Porcentaje_Completado { get; set; }

        public DateTime Fecha_Inicio { get; set; }
        public DateTime? Fecha_Pausa { get; set; }
        public DateTime? Fecha_Reanudacion { get; set; }
        public DateTime? Fecha_Finalizacion { get; set; }
        public int? Minutos_Transcurridos { get; set; }

        [Required]
        [StringLength(50)]
        public string Usuario_Responsable { get; set; }

        [StringLength(50)]
        public string Usuario_Finalizo { get; set; }

        [StringLength(1000)]
        public string Observaciones { get; set; }

        [StringLength(500)]
        public string Motivo_Cancelacion { get; set; }

        public DateTime Fecha_Registro { get; set; }
        public DateTime? Fecha_Actualizacion { get; set; }

        // Propiedades de navegación (no mapeadas a BD)
        public string Nombre_Almacen { get; set; }

        public InventarioSesion()
        {
            Estado = "EN_PROCESO";
            Fecha_Registro = DateTime.Now;
            Total_Esperados = 0;
            Total_Escaneados = 0;
            Total_Faltantes = 0;
            Total_Sobrantes = 0;
        }
    }

    /// <summary>
    /// Modelo de equipo en inventario
    /// </summary>
    public class InventarioEquipo
    {
        [Key]
        public long ID_Registro { get; set; }

        [Required]
        public long ID_Sesion { get; set; }

        [Required]
        public long ID_Equipo { get; set; }

        [Required]
        [StringLength(30)]
        public string IMEI { get; set; }

        public bool Fue_Escaneado { get; set; }
        public DateTime? Fecha_Escaneo { get; set; }

        [StringLength(50)]
        public string Usuario_Escaneo { get; set; }

        public int? Orden_Escaneo { get; set; }

        [Required]
        [StringLength(20)]
        public string Tipo_Hallazgo { get; set; } // ESPERADO, ENCONTRADO, FALTANTE, SOBRANTE

        [StringLength(50)]
        public string Estado_Anterior_Equipo { get; set; }

        public int? ID_Almacen_Anterior { get; set; }

        public DateTime Fecha_Registro { get; set; }

        // Propiedades de navegación (no mapeadas a BD)
        public string Modelo { get; set; }
        public string Marca { get; set; }

        public InventarioEquipo()
        {
            Fue_Escaneado = false;
            Tipo_Hallazgo = "ESPERADO";
            Fecha_Registro = DateTime.Now;
        }
    }

    /// <summary>
    /// DTO para iniciar nueva sesión de inventario
    /// </summary>
    public class IniciarInventarioDTO
    {
        [Required]
        public int ID_Almacen { get; set; }

        [Required]
        public int ID_Empresa { get; set; }

        [Required]
        public int ID_Sucursal { get; set; }

        [Required]
        [StringLength(50)]
        public string Usuario { get; set; }
    }

    /// <summary>
    /// DTO para registrar escaneo de equipo
    /// </summary>
    public class RegistrarEscaneoDTO
    {
        [Required]
        public long ID_Sesion { get; set; }

        [Required]
        [StringLength(30)]
        public string IMEI { get; set; }

        [Required]
        [StringLength(50)]
        public string Usuario { get; set; }
    }

    /// <summary>
    /// DTO para finalizar inventario
    /// </summary>
    public class FinalizarInventarioDTO
    {
        [Required]
        public long ID_Sesion { get; set; }

        [Required]
        [StringLength(50)]
        public string Usuario { get; set; }

        [StringLength(1000)]
        public string Observaciones { get; set; }
    }

    /// <summary>
    /// DTO para pausar/reanudar inventario
    /// </summary>
    public class GestionarInventarioDTO
    {
        [Required]
        public long ID_Sesion { get; set; }

        [Required]
        [StringLength(50)]
        public string Usuario { get; set; }
    }

    /// <summary>
    /// Respuesta de registro de escaneo
    /// </summary>
    public class EscaneoResponse
    {
        public string Resultado { get; set; } // ENCONTRADO, SOBRANTE, ERROR
        public string Mensaje { get; set; }
        public InventarioEquipo Equipo { get; set; }
    }

    /// <summary>
    /// Respuesta de iniciar inventario
    /// </summary>
    public class IniciarInventarioResponse
    {
        public long ID_Sesion { get; set; }
        public int Total_Esperados { get; set; }
    }

    /// <summary>
    /// Respuesta de finalizar inventario
    /// </summary>
    public class FinalizarInventarioResponse
    {
        public string Resultado { get; set; }
        public string Mensaje { get; set; }
        public int Total_Faltantes { get; set; }
    }
}
