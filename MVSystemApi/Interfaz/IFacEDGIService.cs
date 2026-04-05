using DTO.FacEDGI;
using MVSystemApi.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MVSystemApi.Interfaz
{
    /// <summary>
    /// Interfaz para el servicio de facturación electrónica (FacEDGI - EasyConexión)
    /// Define el contrato para comunicarse con la DGI a través de EasyConexión API
    /// </summary>
    public interface IFacEDGIService
    {
        /// <summary>
        /// Envía una factura a la DGI a través de EasyConexión API
        /// </summary>
        /// <param name="factura">Datos de la factura (cabecera)</param>
        /// <param name="detalles">Lista de detalles/líneas de la factura</param>
        /// <param name="pagos">Lista de métodos de pago utilizados</param>
        /// <param name="usuario">Usuario que envía la factura (para auditoría)</param>
        /// <param name="rncEmisor">RNC del emisor (extraído del token JWT)</param>
        /// <param name="nombreEmisor">Nombre de la empresa emisora (extraído del token JWT)</param>
        /// <returns>Response de EasyConexión con datos del envío</returns>
        Task<EasyConexionResponseDTO> EnviarFacturaADGII(
            Facturas factura,
            List<DetalleFactura> detalles,
            List<DetallesPagos_Factura> pagos,
            string usuario,
            string rncEmisor,
            string nombreEmisor);

        /// <summary>
        /// Obtiene el log de envío de una factura específica
        /// </summary>
        /// <param name="numeroFactura">Número de la factura</param>
        /// <param name="literal">Literal de la sucursal</param>
        /// <returns>Log de factura electrónica o null si no existe</returns>
        Task<LogFacturaElectronica> GetLogFacturaElectronica(int numeroFactura, string literal);

        /// <summary>
        /// Obtiene todas las facturas con estado PEND (pendiente envío a DGII)
        /// </summary>
        /// <returns>Lista de logs con estado PEND</returns>
        Task<List<LogFacturaElectronica>> GetLogFacturasElectronicasPendientes();

        /// <summary>
        /// Actualiza el estado de un log de factura electrónica
        /// </summary>
        /// <param name="log">Log actualizado</param>
        /// <returns>true si se actualizó exitosamente</returns>
        Task<bool> UpdateLogFacturaElectronica(LogFacturaElectronica log);

        /// <summary>
        /// Inserta un nuevo registro de log para una factura envío
        /// </summary>
        /// <param name="log">Log a insertar</param>
        /// <returns>true si se insertó exitosamente</returns>
        Task<bool> InsertLogFacturaElectronica(LogFacturaElectronica log);
    }
}
