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
    /// <summary>
    /// Clase auxiliar para procesar ajustes en cierre de caja con pagos mixtos
    /// </summary>
    internal class AjusteTipoPago
    {
        public decimal MontoAnadido { get; set; }
        public decimal MontoEfectivoAnadido { get; set; }
        public int CantidadFacturasAnadidas { get; set; }
    }

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


            var pageTable = Ac.GetFacturas(consulta);
            var result = (from dt in pageTable.AsEnumerable()
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
                              NcfTipoNumero = dt.Table.Columns.Contains("NFCTipoNumero") ? Convert.ToInt32(dt["NFCTipoNumero"]) : 0,
                              TipoComprobante = dt.Table.Columns.Contains("TipoComprobante") ? Convert.ToString(dt["TipoComprobante"]) : "Sin tipo",
                              Telefono = Convert.ToInt64(dt["Telefono"]),
                              ClienteId = Convert.ToInt32(dt["ID_Cliente"]),
                              TipoPago = Convert.ToString(dt["Tipo de Pago"]),
                              SubTotal = Convert.ToDecimal(dt["SubTotal"]),
                              Descuento = Convert.ToDecimal(dt["Descuento"]),
                              Total = Convert.ToDecimal(dt["Total"]),
                              Itbis = Convert.ToDecimal(dt["Itbis"]),
                              Abono = dt.Table.Columns.Contains("Abono") ? Convert.ToDecimal(dt["Abono"]) : 0m,
                              Nota = Convert.ToString(dt["Nota"]),
                              FechaFacturacion = Convert.ToDateTime(dt["Fecha de la Factura"]),
                              LastLine = Convert.ToInt32(dt["Ultima_Linea"]),
                              Line = Convert.ToInt32(dt["Linea"]),
                              TotalRecord = Convert.ToInt32(dt["Cantidad_Registros"]),
                              TotalFacturado = Convert.ToDecimal(dt["TotalFacturado"])
                          }).ToList();

            if (result.Count == 0)
            {
                return result;
            }

            var totalFacturado = result[0].TotalFacturado;
            var totalRecord = result[0].TotalRecord;

            decimal totalItbis;
            decimal totalSinItbis;

            if (pageTable.Columns.Contains("TotalItbis"))
            {
                totalItbis = Convert.ToDecimal(pageTable.Rows[0]["TotalItbis"]);
            }
            else
            {
                var aggregateRows = pageTable.AsEnumerable();

                if (totalRecord > pageTable.Rows.Count)
                {
                    var aggregateFilter = new FacturaFilter
                    {
                        NumeroFactura = consulta.NumeroFactura,
                        Criterio = consulta.Criterio,
                        Almacen = consulta.Almacen,
                        TipoPago = consulta.TipoPago,
                        empresa = consulta.empresa,
                        IdEmpresa = consulta.IdEmpresa,
                        IdSucursal = consulta.IdSucursal,
                        EstadoFactura = consulta.EstadoFactura,
                        ComprobanteCheck = consulta.ComprobanteCheck,
                        IdVendedor = consulta.IdVendedor,
                        IdTipoPago = consulta.IdTipoPago,
                        Usuario = consulta.Usuario,
                        FechaInicio = consulta.FechaInicio,
                        FechaFinal = consulta.FechaFinal,
                        PageIndex = 1,
                        PageSize = totalRecord
                    };

                    var aggregateTable = Ac.GetFacturas(aggregateFilter);
                    aggregateRows = aggregateTable.AsEnumerable();
                }

                totalItbis = aggregateRows.Sum(CalcularItbisFactura);
            }

            if (pageTable.Columns.Contains("TotalSinItbis"))
            {
                totalSinItbis = Convert.ToDecimal(pageTable.Rows[0]["TotalSinItbis"]);
            }
            else
            {
                totalSinItbis = Math.Max(totalFacturado - totalItbis, 0m);
            }
            foreach (var factura in result)
            {
                factura.TotalItbis = totalItbis;
                factura.TotalSinItbis = totalSinItbis;
            }

            return result;
        }

        private decimal CalcularItbisFactura(DataRow row)
        {
            if (row.Table.Columns.Contains("Itbis"))
            {
                var itbis = Convert.ToDecimal(row["Itbis"]);
                if (itbis > 0)
                {
                    return itbis;
                }
            }

            var subTotal = row.Table.Columns.Contains("SubTotal") ? Convert.ToDecimal(row["SubTotal"]) : 0m;
            var descuento = row.Table.Columns.Contains("Descuento") ? Convert.ToDecimal(row["Descuento"]) : 0m;
            var total = row.Table.Columns.Contains("Total") ? Convert.ToDecimal(row["Total"]) : 0m;
            var baseSinItbis = subTotal - descuento;
            var itbisCalculado = total - baseSinItbis;

            return itbisCalculado > 0 ? itbisCalculado : 0m;
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
        /// <summary>
        /// Obtiene el reporte de cierre de caja considerando pagos mixtos
        /// Si hay facturas MIXTO, desglosa los pagos y suma cada componente correctamente
        /// </summary>
        internal object GetCierreCajaReporte(FacturaFilter filtro)
        {
            try
            {
                const int ID_TIPO_PAGO_MIXTO = 6;
                const int ID_TIPO_PAGO_CONTADO = 1;
                const int ID_TIPO_PAGO_CREDITO = 2;

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

                // Diccionario para acumular los detalles por tipo de pago
                var detallesPorTipo = new Dictionary<int, CierreCajaDetalleReporte>();

                // Procesar los datos del reporte base
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
                        MontoAbonos = Convert.ToDecimal(row["MontoAbonos"] ?? 0),
                        SubTotal = Convert.ToDecimal(row["SubTotal"]),
                        Descuento = Convert.ToDecimal(row["Descuento"]),
                        Itbis = Convert.ToDecimal(row["Itbis"]),
                        TotalFacturado = rowTotalFacturado,
                        EfectivoReal = efectivoReal,
                        SaldoPendiente = saldoPendiente,
                        PorcentajeDelTotal = 0,
                        IsAbono = Convert.ToInt32(row["IsAbono"]) == 1
                    };

                    detallesPorTipo[idTipoPago] = detalle;
                }

                // Guardar el conteo real de facturas ANTES del desglose mixto
                // (cada factura se cuenta una sola vez en su tipo de pago original)
                int cantidadRealFacturas = detallesPorTipo.Values.Sum(x => x.CantidadFacturas);

                // PROCESAMIENTO DE PAGOS MIXTOS
                if (detallesPorTipo.ContainsKey(ID_TIPO_PAGO_MIXTO))
                {
                    var detalleMixto = detallesPorTipo[ID_TIPO_PAGO_MIXTO];

                    // Obtener todas las facturas MIXTO en el período
                    var facturasMixto = Ac.GetFacturasMixtoEnPeriodo(filtro);

                    if (facturasMixto != null && facturasMixto.Rows.Count > 0)
                    {
                        // Para cada factura MIXTO, obtener sus desglose
                        decimal totalEfectivoExtraido = 0;
                        decimal totalMixtoAjustado = 0;
                        var ajustesPorTipo = new Dictionary<int, AjusteTipoPago>();

                        foreach (DataRow facturaRow in facturasMixto.Rows)
                        {
                            int numeroFactura = Convert.ToInt32(facturaRow["Numero_Factura"]);
                            decimal totalFacturaMixto = Convert.ToDecimal(facturaRow["Total"]);

                            // Obtener el desglose de pagos para esta factura
                            var desgloses = Ac.GetDetallesPagoFactura(numeroFactura, "F");

                            if (desgloses != null && desgloses.Count > 0)
                            {
                                foreach (var desglose in desgloses)
                                {
                                    int tipoPagoDesglose = desglose.ID_Tipo_Pago;
                                    decimal montoDesglose = desglose.Monto_Pago;
                                    string descDesglose = tiposPagos.ContainsKey(tipoPagoDesglose)
                                        ? tiposPagos[tipoPagoDesglose].Descripcion_Tipo_Pagos
                                        : $"Tipo {tipoPagoDesglose}";

                                    // Si el desglose es CONTADO, extraerlo del MIXTO y sumarlo a CONTADO
                                    if (tipoPagoDesglose == ID_TIPO_PAGO_CONTADO)
                                    {
                                        totalEfectivoExtraido += montoDesglose;

                                        // Registrar el ajuste
                                        if (!ajustesPorTipo.ContainsKey(ID_TIPO_PAGO_CONTADO))
                                        {
                                            ajustesPorTipo[ID_TIPO_PAGO_CONTADO] = new AjusteTipoPago();
                                        }
                                        ajustesPorTipo[ID_TIPO_PAGO_CONTADO].MontoEfectivoAnadido += montoDesglose;
                                        ajustesPorTipo[ID_TIPO_PAGO_CONTADO].CantidadFacturasAnadidas++;
                                    }
                                    else if (tipoPagoDesglose != ID_TIPO_PAGO_MIXTO)
                                    {
                                        // Otros tipos de pago (BANRESERVAS, POPULAR, etc.)
                                        if (!ajustesPorTipo.ContainsKey(tipoPagoDesglose))
                                        {
                                            ajustesPorTipo[tipoPagoDesglose] = new AjusteTipoPago();
                                        }
                                        ajustesPorTipo[tipoPagoDesglose].MontoAnadido += montoDesglose;
                                        ajustesPorTipo[tipoPagoDesglose].CantidadFacturasAnadidas++;
                                    }
                                }
                            }
                            else
                            {
                                // Sin desglose disponible
                            }

                            totalMixtoAjustado += totalFacturaMixto;
                        }

                        // Aplicar los ajustes
                        // 1. Ajustar el MIXTO: restar el monto extraído del total, y poner EfectivoReal en 0
                        //    (porque todo el CONTADO fue extraído del desglose, lo que queda son otros tipos de pago)
                        detalleMixto.EfectivoReal = 0;
                        detalleMixto.TotalFacturado -= totalEfectivoExtraido;

                        // 2. Sumar el efectivo extraído a CONTADO
                        if (!detallesPorTipo.ContainsKey(ID_TIPO_PAGO_CONTADO))
                        {
                            // Si no existe CONTADO, crearlo
                            detallesPorTipo[ID_TIPO_PAGO_CONTADO] = new CierreCajaDetalleReporte
                            {
                                TipoPago = tiposPagos.ContainsKey(ID_TIPO_PAGO_CONTADO)
                                    ? tiposPagos[ID_TIPO_PAGO_CONTADO].Descripcion_Tipo_Pagos
                                    : "Contado",
                                CantidadFacturas = 0,
                                MontoAbonos = 0,
                                SubTotal = 0,
                                Descuento = 0,
                                Itbis = 0,
                                TotalFacturado = 0,
                                EfectivoReal = 0,
                                SaldoPendiente = 0
                            };
                        }

                        if (ajustesPorTipo.ContainsKey(ID_TIPO_PAGO_CONTADO))
                        {
                            var ajuste = ajustesPorTipo[ID_TIPO_PAGO_CONTADO];
                            var detalleContado = detallesPorTipo[ID_TIPO_PAGO_CONTADO];
                            detalleContado.EfectivoReal += ajuste.MontoEfectivoAnadido;
                            detalleContado.TotalFacturado += ajuste.MontoEfectivoAnadido;
                            detalleContado.CantidadFacturas += ajuste.CantidadFacturasAnadidas;

                            // Registrar que vino del desglose MIXTO
                            detalleContado.MontoDesdeDesgloseMixto = ajuste.MontoEfectivoAnadido;
                            detalleContado.CantidadFacturasDesdeMixto = ajuste.CantidadFacturasAnadidas;
                        }

                        // 3. Crear/actualizar entradas para otros tipos de pago del desglose
                        foreach (var ajuste in ajustesPorTipo)
                        {
                            if (ajuste.Key != ID_TIPO_PAGO_CONTADO) // Ya lo procesamos arriba
                            {
                                bool esNuevaLinea = !detallesPorTipo.ContainsKey(ajuste.Key);

                                if (esNuevaLinea)
                                {
                                    detallesPorTipo[ajuste.Key] = new CierreCajaDetalleReporte
                                    {
                                        TipoPago = tiposPagos.ContainsKey(ajuste.Key)
                                            ? tiposPagos[ajuste.Key].Descripcion_Tipo_Pagos
                                            : $"Tipo {ajuste.Key}",
                                        CantidadFacturas = 0,
                                        MontoAbonos = 0,
                                        SubTotal = 0,
                                        Descuento = 0,
                                        Itbis = 0,
                                        TotalFacturado = 0,
                                        EfectivoReal = 0,  // Los desgloses MIXTO de estos tipos no son efectivo
                                        SaldoPendiente = 0
                                    };
                                }

                                var detalleOtro = detallesPorTipo[ajuste.Key];
                                detalleOtro.TotalFacturado += ajuste.Value.MontoAnadido;
                                // ⚠️ IMPORTANTE: Para líneas que vienen de desglose MIXTO, EfectivoReal es 0
                                // porque estos pagos (BANRESERVAS, POPULAR, etc.) no son efectivo en caja
                                if (esNuevaLinea)
                                {
                                    detalleOtro.EfectivoReal = 0;
                                }
                                detalleOtro.CantidadFacturas += ajuste.Value.CantidadFacturasAnadidas;

                                // Registrar que vino del desglose MIXTO
                                detalleOtro.MontoDesdeDesgloseMixto = ajuste.Value.MontoAnadido;
                                detalleOtro.CantidadFacturasDesdeMixto = ajuste.Value.CantidadFacturasAnadidas;
                            }
                        }

                        // IMPORTANTE: Eliminar MIXTO del reporte final porque fue completamente desglosado
                        // MIXTO es solo un contenedor temporal; en el reporte se muestra la composición real
                        if (detallesPorTipo.ContainsKey(ID_TIPO_PAGO_MIXTO))
                        {
                            detallesPorTipo.Remove(ID_TIPO_PAGO_MIXTO);
                        }
                    }
                }

                // IMPORTANTE: Asegurar que SOLO CRÉDITO tiene saldo pendiente
                // Todos los demás tipos de pago deben tener SaldoPendiente = 0
                foreach (var entrada in detallesPorTipo)
                {
                    if (entrada.Key != ID_TIPO_PAGO_CREDITO)
                    {
                        entrada.Value.SaldoPendiente = 0;
                    }
                }

                // Convertir a lista y calcular porcentajes
                var detalles = detallesPorTipo.Values.ToList();
                decimal totalFacturado = detalles.Sum(x => x.TotalFacturado);
                decimal totalEfectivoReal = detalles.Sum(x => x.EfectivoReal);
                decimal totalAbonos = detalles.Sum(x => x.MontoAbonos);
                decimal totalSaldoPendiente = detalles.Sum(x => x.SaldoPendiente);

                if (totalFacturado > 0)
                {
                    foreach (var detalle in detalles)
                    {
                        detalle.PorcentajeDelTotal = Math.Round((detalle.TotalFacturado / totalFacturado) * 100, 2);
                    }
                }

                // Crear resumen final (cantidadRealFacturas se calculó antes del desglose mixto)
                var resumen = new CierreCajaReporteResumen
                {
                    FechaInicio = DateTime.Parse(filtro.FechaInicio),
                    FechaFinal = DateTime.Parse(filtro.FechaFinal),
                    Almacen = filtro.Almacen.ToString(),
                    CantidadTotalFacturas = cantidadRealFacturas,
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
