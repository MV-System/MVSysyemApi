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
                              Itbis = Convert.ToDecimal(dt["Itbis"]),
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
        internal object GetFacturasReporte(FacturaFilter consulta)
        {


            var result = (from dt in Ac.GetFacturasReporte(consulta).AsEnumerable()
                          select new FacturaConsulta
                          {
                              NumeroFactura = Convert.ToInt32(dt["Numero de Factura"]),
                              Nombres = Convert.ToString(dt["Nombres"]),
                              Apellidos = Convert.ToString(dt["Apellidos"]),
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
                              Abono = Convert.ToDecimal(dt["Abono"]),
                              Itbis = Convert.ToDecimal(dt["Itbis"]),
                              Nota = Convert.ToString(dt["Nota"]),
                              FechaFacturacion = Convert.ToDateTime(dt["Fecha de la Factura"]),
                              //LastLine = Convert.ToInt32(dt["Ultima_Linea"]),
                              //Line = Convert.ToInt32(dt["Linea"]),
                              //TotalRecord = Convert.ToInt32(dt["Cantidad_Registros"]),
                              TotalFacturado = Convert.ToDecimal(dt["TotalFacturado"])
                          }).ToList();
            return result;
        }
        internal object GetFacturaNumeroUltimo()
        {
            var Numero_Factura = 0;
            var result = (from dt in Ac.GetFacturaNumeroUltimo().AsEnumerable()
                          select Numero_Factura = Convert.ToInt32(dt["Numero_Factura"]) + 1).FirstOrDefault();

                        

            return result;
        }

        internal object EditaDetalleFactura(EditaDetalleFactura EditaDetalleFactura)
        {
            try
            {
                // Llamamos al procedimiento almacenado que devuelve el mensaje
                var mensaje = Ac.EditaDetalleFactura(EditaDetalleFactura);  // Este método debería devolver un string (mensaje)

                // Creación de un objeto de tipo EquipoEliminado con el mensaje
                var result = new FacturaEditada
                {
                    mensaje = mensaje.ToString()  // Asignar el mensaje al objeto
                };

                return result;  // Retornamos el objeto con el mensaje
            }
            catch (Exception ex)
            {
                // Manejo de excepciones
                throw new Exception("Error al intentar eliminar el equipo.", ex);
            }
        }

        internal object RebajaEquipo(RebajaEquipoModel model)
        {
            try
            {
                // Llamamos al procedimiento almacenado que devuelve el mensaje
                var mensaje = Ac.RebajaEquipo(model);  // Este método debería devolver un string (mensaje)

                // Creación de un objeto de tipo EquipoEliminado con el mensaje
                var result = new FacturaEditada
                {
                    mensaje = mensaje.ToString()  // Asignar el mensaje al objeto
                };

                return result;  // Retornamos el objeto con el mensaje
            }
            catch (Exception ex)
            {
                // Manejo de excepciones
                throw new Exception("Error al intentar eliminar el equipo.", ex);
            }
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

        /// <summary>
        /// Obtiene el reporte de cierre de caja con desglose por tipo de pago
        /// </summary>
        internal object GetCierreCajaReporte(FacturaFilter filtro)
        {
            try
            {
                // Obtener datos de la base de datos
                var dt = Ac.GetCierreCajaReport(filtro);
                
                if (dt == null || dt.Rows.Count == 0)
                {
                    return new CierreCajaReporteResumen
                    {
                        FechaInicio = DateTime.Parse(filtro.FechaInicio),
                        FechaFinal = DateTime.Parse(filtro.FechaFinal),
                        Almacen = filtro.Almacen.ToString(),
                        DetallesPorTipoPago = new List<CierreCajaDetalleReporte>(),
                        TotalFacturado = 0,
                        TotalEfectivoReal = 0,
                        TotalAbonos = 0,
                        TotalSaldoPendiente = 0
                    };
                }

                // Obtener tipos de pagos para tener las descripciones
                var tiposPagosData = Ac.Tipos_Pagos_Cata_Consulta_Combo();
                var tiposPagos = new Dictionary<int, Tipos_Pagos>();
                
                foreach (DataRow row in tiposPagosData.Rows)
                {
                    var idTipo = Convert.ToInt32(row["ID_Tipo_Pagos"]);
                    tiposPagos[idTipo] = new Tipos_Pagos
                    {
                        ID_Tipo_Pagos = idTipo,
                        Descripcion_Tipo_Pagos = Convert.ToString(row["Descripcion_Tipo_Pagos"])
                    };
                }

                // Mapear datos a DTO
                var detalles = new List<CierreCajaDetalleReporte>();
                decimal totalFacturado = 0, totalEfectivoReal = 0, totalAbonos = 0, totalSaldoPendiente = 0;

                foreach (DataRow row in dt.Rows)
                {
                    var idTipoPago = Convert.ToInt32(row["ID_Tipo_Pagos"]);
                    var tipoPagoDesc = tiposPagos.ContainsKey(idTipoPago) 
                        ? tiposPagos[idTipoPago].Descripcion_Tipo_Pagos 
                        : "Desconocido";

                    var efectivoReal = Convert.ToDecimal(row["EfectivoReal"]);
                    var saldoPendiente = Convert.ToDecimal(row["SaldoPendiente"]);
                    var rowTotalFacturado = Convert.ToDecimal(row["TotalFacturado"]);

                    var detalle = new CierreCajaDetalleReporte
                    {
                        TipoPago = tipoPagoDesc,
                        CantidadFacturas = Convert.ToInt32(row["CantidadFacturas"]),
                        CantidadAbonosRecibidos = Convert.ToInt32(row["CantidadAbonosRecibidos"] ?? 0),
                        MontoAbonos = efectivoReal, // Abonos recibidos (efectivo real para crédito)
                        SubTotal = Convert.ToDecimal(row["SubTotal"]),
                        Descuento = Convert.ToDecimal(row["Descuento"]),
                        Itbis = Convert.ToDecimal(row["Itbis"]),
                        TotalFacturado = rowTotalFacturado,
                        EfectivoReal = efectivoReal,
                        SaldoPendiente = saldoPendiente,
                        PorcentajeDelTotal = 0 // Se calcula después
                    };

                    detalles.Add(detalle);

                    totalFacturado += detalle.TotalFacturado;
                    totalEfectivoReal += efectivoReal;
                    totalAbonos += efectivoReal;
                    totalSaldoPendiente += saldoPendiente;
                }

                // Calcular porcentajes
                if (totalFacturado > 0)
                {
                    foreach (var detalle in detalles)
                    {
                        detalle.PorcentajeDelTotal = Math.Round((detalle.TotalFacturado / totalFacturado) * 100, 2);
                    }
                }

                // Crear resumen
                var resumen = new CierreCajaReporteResumen
                {
                    FechaInicio = DateTime.Parse(filtro.FechaInicio),
                    FechaFinal = DateTime.Parse(filtro.FechaFinal),
                    Almacen = filtro.Almacen.ToString(),
                    CantidadTotalFacturas = detalles.Sum(x => x.CantidadFacturas),
                    TotalFacturado = totalFacturado,
                    TotalEfectivoReal = totalEfectivoReal,
                    TotalAbonos = totalAbonos,
                    TotalSaldoPendiente = totalSaldoPendiente,
                    DetallesPorTipoPago = detalles
                };

                return resumen;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener el cierre de caja: " + ex.Message, ex);
            }
        }

    }
}
