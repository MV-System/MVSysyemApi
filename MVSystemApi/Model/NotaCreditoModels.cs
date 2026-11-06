using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace MVSystemApi.Model
{
    /// <summary>
    /// Datos de la factura de origen que necesita la nota de crédito del proveedor 1 (EasyConexión),
    /// leídos directamente de las tablas de la empresa (Facturas, Detalle_Facturas y pagos).
    /// </summary>
    public class FacturaOrigenNotaCredito
    {
        public int NumeroFactura { get; set; }
        public int Sucursal { get; set; }
        /// <summary>Literal real de la factura (Facturas.Literal, char(1)).</summary>
        public string Literal { get; set; }
        /// <summary>'A' activa, 'X' anulada.</summary>
        public string Estado { get; set; }
        /// <summary>e-NCF de la factura (Facturas.NCF_NUMERO). Vacío si no tiene comprobante electrónico.</summary>
        public string NcfNumero { get; set; }
        public int NcfTipoNumero { get; set; }
        public int IdCliente { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Descuento { get; set; }
        public decimal Itbis { get; set; }
        public decimal Total { get; set; }
        public string RNC { get; set; }
        public DateTime FechaRegistro { get; set; }
        public int IdTipoPago { get; set; }
        public DateTime? TiempoCredito { get; set; }
        public List<DetalleFactura> Detalles { get; set; } = new List<DetalleFactura>();
        public List<DetallesPagos_Factura> Pagos { get; set; } = new List<DetallesPagos_Factura>();
    }

    /// <summary>Respuesta de la emisión de una nota de crédito.</summary>
    public class NotaCreditoResultado
    {
        /// <summary>e-NCF de la nota de crédito.</summary>
        public string Ncf { get; set; }
        /// <summary>APROBADA | PENDIENTE | RECHAZADA | ANULADA (la factura ya estaba rechazada por la DGII: se anuló sin nota de crédito)</summary>
        public string Estado { get; set; }
        /// <summary>true si la factura de origen ya está anulada (la anulación automática es la Parte 3).</summary>
        public bool Anulada { get; set; }
        public string Mensaje { get; set; }
    }

    /// <summary>Error de negocio de la nota de crédito: el mensaje se muestra al usuario tal cual.</summary>
    public class NotaCreditoException : Exception
    {
        public NotaCreditoException(string mensaje) : base(mensaje) { }
    }

    /// <summary>
    /// Candado de aplicación en SQL Server (sp_getapplock, dueño Session) sobre una conexión que se mantiene abierta
    /// mientras dura el flujo. Se libera con DisposeAsync (sp_releaseapplock) y al cerrar la conexión si el API se cae.
    /// </summary>
    public sealed class CandadoAplicacion : IAsyncDisposable
    {
        private readonly SqlConnection _conexion;
        private readonly string _recurso;

        public CandadoAplicacion(SqlConnection conexion, string recurso)
        {
            _conexion = conexion;
            _recurso = recurso;
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await using var cmd = new SqlCommand("sp_releaseapplock", _conexion) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@Resource", _recurso);
                cmd.Parameters.AddWithValue("@LockOwner", "Session");
                await cmd.ExecuteNonQueryAsync();
            }
            catch
            {
                // Si falla, el candado se libera igual al cerrar la conexión
            }
            finally
            {
                await _conexion.DisposeAsync();
            }
        }
    }
}
