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
