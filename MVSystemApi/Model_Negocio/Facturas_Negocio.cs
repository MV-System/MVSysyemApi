using DTO;
using MVSystemApi.Interfaz;
using MVSystemApi.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace MVSystemApi.Model_Negocio
{
    public class Facturas_Negocio
    {
        private readonly IAccesoDatos Ac;

        public Facturas_Negocio(IAccesoDatos Ad)
        {
            Ac = Ad;
        }
        internal object GetDetalleFacturaConsulta(int numeroFactura, int sucursal)
        {
            var result = (from dt in Ac.GetDetalleFacturaConsulta(numeroFactura, sucursal).AsEnumerable()
                          select new DetalleFactura
                          {
                              IdEquipo = Convert.ToString(dt["ID_Equipo"]),
                              NumeroFactura = Convert.ToInt32(dt["Numero_Factura"]),
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
        internal object GetFacturas(FacturaFilter consulta)
        {


            var result = (from dt in Ac.GetFacturas(consulta).AsEnumerable()
                          select new FacturaConsulta
                          {
                              NumeroFactura = Convert.ToInt32(dt["Numero de Factura"]),
                              Nombres = Convert.ToString(dt["Nombres"]),
                              Apellidos= Convert.ToString(dt["Apellidos"]),
                              TipoFactura = Convert.ToString(dt["Tipo De Factura"]),
                              Vendedor = Convert.ToString(dt["Vendedor"]),
                              Almacen = Convert.ToString(dt["Almacen"]),
                              Rnc = Convert.ToString(dt["RNC"]),
                              Ncf = Convert.ToString(dt["NCF"]),
                              Telefono = Convert.ToInt64(dt["Telefono"]),
                              ClienteId = Convert.ToInt32(dt["ID_Cliente"]),
                              TipoPago = Convert.ToString(dt["Tipo de Pago"]),
                              SubTotal = Convert.ToDecimal(dt["SubTotal"]),
                              Descuento = Convert.ToDecimal(dt["Descuento"]),
                              Total = Convert.ToDecimal(dt["Total"]),
                              Itbis = Convert.ToDecimal(dt["Itbis"]),
                              Nota = Convert.ToString(dt["Nota"]),
                              FechaFacturacion = Convert.ToDateTime(dt["Fecha de la Factura"]),
                              LastLine = Convert.ToInt32(dt["Ultima_Linea"]),
                              Line = Convert.ToInt32(dt["Linea"]),
                              TotalRecord = Convert.ToInt32(dt["Cantidad_Registros"]),
                              TotalFacturado = Convert.ToDecimal(dt["TotalFacturado"])
                          }).ToList();
            return result;
        }
        internal object GetFacturaNumeroUltimo()
        {
            var Numero_Factura = 0;
            var result = (from dt in Ac.GetFacturaNumeroUltimo().AsEnumerable()
                          select Numero_Factura = Convert.ToInt32(dt["Numero_Factura"]) + 1);

                        

            return result;
        }
        //internal object PostFactura(Facturas Factura)
        //{
        //    var result = (from dt in Ac.PostFactura(Factura).AsEnumerable()
        //                  select new Facturas
        //                  {
        //                      Imei = Convert.ToString(dt["Imei"]),
        //                      Modelo = Convert.ToString(dt["Modelo"]),
        //                      Marca = Convert.ToString(dt["Marca"]),
        //                      Mensaje = Convert.ToString(dt["Mensaje"])

        //                  }).ToList();
        //    var result = "Factura Generada";

        //    return result;
        //}
        //internal object Post_Factura(Facturas Factura, int Cotizacion_Numero, SqlTransaction tran = null)
        //{
        //    var result = (from dt in Ac.Post_Factura(Factura, Cotizacion_Numero, tran).AsEnumerable()
        //                  select new Facturas
        //                  {
        //                      //Imei = Convert.ToString(dt["Imei"]),
        //                      //Modelo = Convert.ToString(dt["Modelo"]),
        //                      //Marca = Convert.ToString(dt["Marca"]),
        //                      //Mensaje = Convert.ToString(dt["Mensaje"])

        //                  }).ToList();
        //    var result = "Factura Generada";

        //    return result;
        //}

    }
}
