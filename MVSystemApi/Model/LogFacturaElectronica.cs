using System;

namespace MVSystemApi.Model
{
    /// <summary>
    /// Modelo para auditar envíos de facturas electrónicas a DGI
    /// Registra cada intento de envío, respuesta y estado actual
    /// </summary>
    public class LogFacturaElectronica
    {
        /// <summary>
        /// ID único del registro de log
        /// Identificador primario
        /// </summary>
        public int IDLog { get; set; }

        /// <summary>
        /// Número de factura
        /// Referencia a Facturas.Numero_Factura (junto con Literal forma identificador único)
        /// </summary>
        public int Numero_Factura { get; set; }

        /// <summary>
        /// Literal de la sucursal
        /// Referencia a Facturas.Literal (junto con Numero_Factura forma identificador único)
        /// Identifica a qué sucursal pertenece la factura
        /// Ejemplo: "A", "B", "C", etc.
        /// </summary>
        public string Literal { get; set; }

        /// <summary>
        /// ID del documento asignado por EasyConexión
        /// Obtenido de EasyConexionResponseDTO.DocumentId
        /// Usado para rastreo en el sistema externo
        /// </summary>
        public int? DocumentoId_EasyConexion { get; set; }

        /// <summary>
        /// Número de comprobante en DGI
        /// Obtenido de EasyConexionResponseDTO.VoucherNumber
        /// Formato: E32XXXXXXXXX
        /// </summary>
        public string VoucherNumber_DGII { get; set; }

        /// <summary>
        /// Estado actual del envío
        /// Valores posibles:
        /// - "PEND": Pendiente envío a DGII
        /// - "ENVIADA": Enviada exitosamente
        /// - "ERROR": Error en el envío
        /// - "CONFIRMADA": Confirmada por DGII
        /// </summary>
        public string Estado { get; set; }

        /// <summary>
        /// Respuesta JSON completa de EasyConexión
        /// Se guarda para auditoría y debugging
        /// Contiene toda la información retornada por el API
        /// </summary>
        public string Respuesta_JSON { get; set; }

        /// <summary>
        /// Fecha y hora del último intento de envío
        /// Permite rastrear cuándo se intentó enviar
        /// </summary>
        public DateTime Fecha_Envio { get; set; }

        /// <summary>
        /// Fecha y hora de confirmación en DGII
        /// Null si aún no se ha confirmado
        /// Se actualiza cuando cambia a estado confirmado
        /// </summary>
        public DateTime? Fecha_Confirmacion { get; set; }

        /// <summary>
        /// Descripción de error si el envío falló
        /// Contiene el mensaje de excepción o error del API
        /// Null si el envío fue exitoso
        /// </summary>
        public string Error_Descripcion { get; set; }

        /// <summary>
        /// Usuario que generó la factura
        /// Referencia al usuario del sistema
        /// </summary>
        public string Usuario { get; set; }

        /// <summary>
        /// Fecha de registro del log
        /// Fecha en la que se creó este registro (automática - DEFAULT GETDATE())
        /// </summary>
        public DateTime Fecha_Registro { get; set; }

        // Propiedades de navegación (si usa Entity Framework)
        // public virtual Facturas Factura { get; set; }
    }
}
