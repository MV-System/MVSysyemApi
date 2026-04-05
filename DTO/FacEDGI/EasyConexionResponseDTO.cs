namespace DTO.FacEDGI
{
    /// <summary>
    /// DTO para la respuesta recibida desde DGI a través de EasyConexión API
    /// Contiene el resultado del envío de la factura
    /// </summary>
    public class EasyConexionResponseDTO
    {
        /// <summary>
        /// Indica si la operación fue exitosa
        /// Valor: true si se procesó correctamente, false en caso de error
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// ID interno asignado por EasyConexión
        /// Usado para rastrear la factura en el sistema externo
        /// </summary>
        public int DocumentId { get; set; }

        /// <summary>
        /// Número de comprobante retornado por DGI
        /// Puede ser igual al enviado o diferente según el sistema DGI
        /// </summary>
        public string VoucherNumber { get; set; }

        /// <summary>
        /// Estado actual del comprobante
        /// Valores esperados: "PEND" (pendiente envío a DGII), "SENT", etc.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Mensaje descriptivo del resultado
        /// Ej: "Created. The system will send it to DGII automatically."
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// ID de seguimiento en DGII
        /// Puede ser null si aún no se ha confirmado el envío
        /// </summary>
        public string TrackId { get; set; }
    }
}
