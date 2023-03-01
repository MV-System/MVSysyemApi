using MVSystemApi.Interfaz;
using MVSystemApi.Model;
using System.Data;

namespace MVSystemApi.Model_Negocio
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Class Cotizacion_Negocio
    /// Author: Xavier Mejia
    /// Date: 21.FEB.2023
    ///</summary>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    public class Cotizacion_Negocio
    {
        private readonly IAccesoDatos Ac;

        public Cotizacion_Negocio(IAccesoDatos Ad)
        {
            Ac = Ad;
        }
        internal object GetCotizacionFacturaConsulta(string telefono)
        {
            var result = (from dt in Ac.GetCotizacionFacturaConsulta(telefono).AsEnumerable()
                          select new CotizacionAmount
                          {
                              NumeroFactura = Convert.ToInt32(dt["Numero_Factura"]),
                              Cliente = Convert.ToString(dt["Cliente"]),
                              SubTotal = Convert.ToDecimal(dt["SubTotal"]),
                              Descuento = Convert.ToDecimal(dt["Descuento"]),
                              Total = Convert.ToDecimal(dt["Total"]),
                              Itbis = Convert.ToDecimal(dt["Itbis"]),
                              Abono = Convert.ToDecimal(dt["Abono"]),
                          }).ToList();
            return result;
        }
        internal object GetDetalleCotizacionConsulta(int numeroFactura, int sucursal)
        {
            var result = (from dt in Ac.GetDetalleCotizacionConsulta(numeroFactura, sucursal).AsEnumerable()
                          select new DetalleFactura
                          {
                              NumeroFactura = Convert.ToInt32(dt["Numero_Factura"]),
                              IdEquipo = Convert.ToString(dt["ID_Equipo"]),
                              Descripcion = Convert.ToString(dt["Descripcion"]), 
                              Cantidad = Convert.ToInt32(dt["cantidad"]),
                              Precio = Convert.ToInt32(dt["precio"]),
                              SubTotal = Convert.ToDecimal(dt["SubTotal"]),
                              Descuento = Convert.ToDecimal(dt["Descuento"]),
                              Total = Convert.ToDecimal(dt["Total"]),
                              IdTipo = Convert.ToInt32(dt["ID_Tipo"]),
                          }).ToList();
            return result;

        }

    }
}
