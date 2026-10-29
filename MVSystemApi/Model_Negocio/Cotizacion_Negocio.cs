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
        internal object GetCotizaciones(CotizacionConsultaFiltro filtro)
        {
            var result = (from dt in Ac.GetCotizaciones(filtro).AsEnumerable()
                          select new CotizacionConsulta
                          {
                              Codigo = Convert.ToInt32(dt["Codigo"]),
                              NumeroFactura = Convert.ToInt32(dt["NumeroFactura"]),
                              IdSucursal = Convert.ToInt32(dt["IdSucursal"]),
                              Literal = Convert.ToString(dt["Literal"]),
                              ClienteId = Convert.ToInt32(dt["ClienteId"]),
                              Nombres = Convert.ToString(dt["Nombres"]),
                              Apellidos = Convert.ToString(dt["Apellidos"]),
                              Telefono = Convert.ToString(dt["Telefono"]),
                              TipoFactura = Convert.ToString(dt["TipoFactura"]),
                              Vendedor = Convert.ToString(dt["Vendedor"]),
                              TipoPago = Convert.ToString(dt["TipoPago"]),
                              SubTotal = Convert.ToDecimal(dt["SubTotal"]),
                              Descuento = Convert.ToDecimal(dt["Descuento"]),
                              Itbis = Convert.ToDecimal(dt["Itbis"]),
                              Abono = Convert.ToDecimal(dt["Abono"]),
                              Total = Convert.ToDecimal(dt["Total"]),
                              TotalCotizado = Convert.ToDecimal(dt["TotalCotizado"]),
                              Nota = Convert.ToString(dt["Nota"]),
                              FechaFacturacion = Convert.ToDateTime(dt["FechaFacturacion"]),
                              Estado = Convert.ToString(dt["Estado"]),
                              Facturado = Convert.ToString(dt["Facturado"]),
                              Line = Convert.ToInt32(dt["Linea"]),
                              TotalRecord = Convert.ToInt32(dt["TotalRecord"]),
                              LastLine = Convert.ToInt32(dt["Ultima_Linea"])
                          }).ToList();
            return result;
        }

        internal CotizacionModel GetCotizacionPorCodigo(int codigo)
        {
            var cabecera = Ac.GetCotizacionPorCodigo(codigo);
            if (cabecera.Rows.Count == 0) return null;

            var row = cabecera.Rows[0];
            var sucursal = Convert.ToInt32(row["IdSucursal"]);
            var cotizacion = new CotizacionModel
            {
                Codigo = Convert.ToInt32(row["Codigo"]),
                NumeroFactura = Convert.ToInt32(row["NumeroFactura"]),
                IdCliente = Convert.ToInt32(row["IdCliente"]),
                IdTipoFactura = Convert.ToInt32(row["IdTipoFactura"]),
                IdVendedor = Convert.ToInt32(row["IdVendedor"]),
                IdTipoPago = Convert.ToInt32(row["IdTipoPago"]),
                IdSucursal = sucursal.ToString(),
                IdAlmacen = sucursal,
                SubTotal = Convert.ToDecimal(row["SubTotal"]),
                Descuento = Convert.ToDecimal(row["Descuento"]),
                Itbis = Convert.ToDecimal(row["Itbis"]),
                Abono = Convert.ToDecimal(row["Abono"]),
                Total = Convert.ToDecimal(row["Total"]),
                CantidadArticulos = Convert.ToInt32(row["CantidadArticulos"]),
                Nota = Convert.ToString(row["Nota"]),
                Usuario = Convert.ToString(row["Usuario"]),
                FechaRegistro = Convert.ToDateTime(row["FechaRegistro"]).ToString("yyyy-MM-dd HH:mm:ss"),
                FechaFacturacion = Convert.ToDateTime(row["FechaRegistro"]).ToString("yyyy-MM-dd HH:mm:ss"),
                FechaLimitePago = row.IsNull("FechaLimitePago") ? string.Empty : Convert.ToDateTime(row["FechaLimitePago"]).ToString("yyyy-MM-dd"),
                NcfTipoNumero = Convert.ToString(row["NcfTipoNumero"]),
                Rnc = Convert.ToString(row["Rnc"]),
                Nombre = Convert.ToString(row["Nombre"]),
                Apellido = Convert.ToString(row["Apellido"]),
                Telefono = string.IsNullOrWhiteSpace(Convert.ToString(row["Telefono"])) ? 0 : Convert.ToInt64(row["Telefono"]),
                Vendedor = Convert.ToString(row["Vendedor"]),
                Articulos = (from dt in Ac.GetDetalleCotizacionPorCodigo(codigo).AsEnumerable()
                             select new Articulo
                             {
                                 NumeroFactura = Convert.ToInt32(dt["NumeroFactura"]),
                                 IdEquipo = Convert.ToString(dt["IdEquipo"]),
                                 Descripcion = Convert.ToString(dt["Descripcion"]),
                                 IdTipo = Convert.ToInt32(dt["IdTipo"]),
                                 IdGarantia = Convert.ToInt32(dt["IdGarantia"]),
                                 Cantidad = Convert.ToInt32(dt["Cantidad"]),
                                 Precio = Convert.ToDecimal(dt["Precio"]),
                                 SubTotal = Convert.ToDecimal(dt["SubTotal"]),
                                 Descuento = Convert.ToDecimal(dt["Descuento"]),
                                 Itbis = Convert.ToDecimal(dt["Itbis"]),
                                 Total = Convert.ToDecimal(dt["Total"])
                             }).ToList()
            };

            return cotizacion;
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
