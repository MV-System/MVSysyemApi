using System;
using System.Collections.Generic;

namespace MVSystemApi.Model
{
    /// <summary>
    /// Detalle de cierre de caja por cada tipo de pago
    /// </summary>
    public class CierreCajaDetalleReporte
    {
        /// <summary>
        /// Tipo de pago (dinámico según empresa: Contado, Crédito, Transferencia, Tarjeta, BANRESERVAS, POPULAR, etc.)
        /// </summary>
        public string TipoPago { get; set; }

        /// <summary>
        /// Total de facturas emitidas con este tipo de pago
        /// </summary>
        public int CantidadFacturas { get; set; }

        /// <summary>
        /// Cantidad de facturas de crédito que recibieron abono (solo para TipoPago = "Crédito")
        /// </summary>
        public int CantidadAbonosRecibidos { get; set; }

        /// <summary>
        /// Monto total de abonos recibidos (solo para TipoPago = "Crédito")
        /// </summary>
        public decimal MontoAbonos { get; set; }

        /// <summary>
        /// Subtotal de todas las facturas
        /// </summary>
        public decimal SubTotal { get; set; }

        /// <summary>
        /// Total de descuentos aplicados
        /// </summary>
        public decimal Descuento { get; set; }

        /// <summary>
        /// Total de ITBIS
        /// </summary>
        public decimal Itbis { get; set; }

        /// <summary>
        /// Total facturado con este tipo de pago
        /// </summary>
        public decimal TotalFacturado { get; set; }

        /// <summary>
        /// CLAVE: Efectivo real que entró a la caja
        /// Para Contado y otros: es el total facturado
        /// Para Crédito: son solo los abonos recibidos
        /// </summary>
        public decimal EfectivoReal { get; set; }

        /// <summary>
        /// Saldo pendiente de cobrar (solo para Crédito)
        /// = TotalFacturado - EfectivoReal
        /// </summary>
        public decimal SaldoPendiente { get; set; }

        /// <summary>
        /// Porcentaje del total facturado
        /// </summary>
        public decimal PorcentajeDelTotal { get; set; }

        /// <summary>
        /// Monto que vino del desglose de pagos MIXTO
        /// Se usa para mostrar etiquetas de trazabilidad como "mixto: RD$100"
        /// </summary>
        public decimal MontoDesdeDesgloseMixto { get; set; } = 0;

        /// <summary>
        /// Cantidad de facturas MIXTO que contribuyeron a este tipo de pago
        /// </summary>
        public int CantidadFacturasDesdeMixto { get; set; } = 0;

        /// <summary>
        /// Indicador de si esta fila representa un abono (pago) vs una venta
        /// true = es un abono (pago realizado a factura de crédito)
        /// false = es una venta
        /// </summary>
        public bool IsAbono { get; set; } = false;
    }

    /// <summary>
    /// Resumen general del cierre de caja
    /// </summary>
    public class CierreCajaReporteResumen
    {
        /// <summary>
        /// Fecha inicio del período
        /// </summary>
        public DateTime FechaInicio { get; set; }

        /// <summary>
        /// Fecha final del período
        /// </summary>
        public DateTime FechaFinal { get; set; }

        /// <summary>
        /// Nombre del almacén/sucursal
        /// </summary>
        public string Almacen { get; set; }

        /// <summary>
        /// Nombre del vendedor (si aplica filtro)
        /// </summary>
        public string Vendedor { get; set; }

        /// <summary>
        /// Nombre de la empresa
        /// </summary>
        public string Empresa { get; set; }

        /// <summary>
        /// Total de facturas en el período
        /// </summary>
        public int CantidadTotalFacturas { get; set; }

        /// <summary>
        /// Total facturado (suma de todos los tipos de pago)
        /// Incluye crédito aún no pagado
        /// </summary>
        public decimal TotalFacturado { get; set; }

        /// <summary>
        /// CLAVE: Total de dinero que realmente entró a la caja
        /// = Contado + Transferencias + Abonos a Crédito + Otros medios realizados
        /// </summary>
        public decimal TotalEfectivoReal { get; set; }

        /// <summary>
        /// Total de abonos recibidos (dinero en caja por facturas de crédito)
        /// </summary>
        public decimal TotalAbonos { get; set; }

        /// <summary>
        /// Total pendiente de cobrar (crédito sin pagar)
        /// </summary>
        public decimal TotalSaldoPendiente { get; set; }

        /// <summary>
        /// Detalles dinámicos por cada tipo de pago
        /// La cantidad de elementos varía según la empresa
        /// </summary>
        public List<CierreCajaDetalleReporte> DetallesPorTipoPago { get; set; } = new List<CierreCajaDetalleReporte>();

        /// <summary>
        /// Datos de la empresa para el encabezado del reporte
        /// </summary>
        public DatosEmpresa DatosEmpresaFactura { get; set; }
    }
}
