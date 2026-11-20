using MVSystemApi.Interfaz;
using MVSystemApi.Model;
using System.Data;

namespace MVSystemApi.Model_Negocio
{
    public class Utilidades_Negocio
    {
        private readonly IAccesoDatos Ac;

        public Utilidades_Negocio(IAccesoDatos db)
        {
            Ac = db;
        }

        internal object GetUtilidades(int? sucursal, DateTime FI, DateTime FF)
        {

            try
            {
                var Lista = (from dt in Ac.GetUtilidades(sucursal,FI,FF).AsEnumerable()
                             select new Utilidades
                             {
                                 Costo_Equipos = Convert.ToDecimal(dt["Costo_Equipos"]),
                                 Venta_Equipos = Convert.ToDecimal(dt["Venta_Equipos"]),
                                 Itbis_Equipos = Convert.ToDecimal(dt["Itbis_Equipos"]),
                                 Ganancia_Equipos = Convert.ToDecimal(dt["Ganancia_Equipos"]),
                                 Costo_Accesorios = Convert.ToDecimal(dt["Costo_Accesorios"]),
                                 Venta_Accesorios = Convert.ToDecimal(dt["Venta_Accesorios"]),
                                 Itbis_Accesorios = Convert.ToDecimal(dt["Itbis_Accesorios"]),
                                 Ganancia_Accesorios = Convert.ToDecimal(dt["Ganancia_Accesorios"]),
                                 Costo_Reparaciones = Convert.ToDecimal(dt["Costo_Reparaciones"]),
                                 Venta_Reparaciones = Convert.ToDecimal(dt["Venta_Reparaciones"]),
                                 Itbis_Reparaciones = Convert.ToDecimal(dt["Itbis_Reparaciones"]),
                                 Ganancia_Reparaciones = Convert.ToDecimal(dt["Ganancia_Reparaciones"]),
                                 Costo_Desbloqueos = Convert.ToDecimal(dt["Costo_Desbloqueos"]),
                                 Venta_Desbloqueos = Convert.ToDecimal(dt["Venta_Desbloqueos"]),
                                 Itbis_Desbloqueos = Convert.ToDecimal(dt["Itbis_Desbloqueos"]),
                                 Ganancia_Desbloqueos = Convert.ToDecimal(dt["Ganancia_Desbloqueos"]),
                                 Abonos_Credito = Convert.ToDecimal(dt["Abonos_Credito"]),
                                 Retencion_Tarjeta = dt.Table.Columns.Contains("Retencion_Tarjeta") && dt["Retencion_Tarjeta"] != DBNull.Value ? Convert.ToDecimal(dt["Retencion_Tarjeta"]) : 0m,

                                 // ID_Tipo_Pagos = Convert.ToInt32(dt["ID_Tipo_Pagos"]),
                                 // Descripcion_Tipo_Pagos = Convert.ToString(dt["Descripcion_Tipo_Pagos"]),

                             }).FirstOrDefault();

                return Lista;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        internal List<DTO.UtilidadAbonoDetalleDTO> GetDetalleAbonos(int? sucursal, DateTime FI, DateTime FF)
        {
            var tabla = Ac.GetDetalleAbonosUtilidades(sucursal, FI, FF);
            return tabla.AsEnumerable().Select(fila => new DTO.UtilidadAbonoDetalleDTO
            {
                Id = Convert.ToInt32(fila["Id"]),
                IdCliente = fila["IdCliente"] == DBNull.Value ? 0 : Convert.ToInt32(fila["IdCliente"]),
                Cliente = Convert.ToString(fila["Cliente"]),
                NumeroFactura = Convert.ToInt32(fila["NumeroFactura"]),
                FechaAbono = Convert.ToDateTime(fila["FechaAbono"]),
                MontoAbono = Convert.ToDecimal(fila["MontoAbono"]),
                RetencionAbono = fila.Table.Columns.Contains("RetencionAbono") && fila["RetencionAbono"] != DBNull.Value ? Convert.ToDecimal(fila["RetencionAbono"]) : 0m,
                RetencionFactura = fila.Table.Columns.Contains("RetencionFactura") && fila["RetencionFactura"] != DBNull.Value ? Convert.ToDecimal(fila["RetencionFactura"]) : 0m,
                FacturaSaldada = Convert.ToBoolean(fila["FacturaSaldada"]),
                EsAbonoQueSaldaFactura = Convert.ToBoolean(fila["EsAbonoQueSaldaFactura"]),
                CostoFactura = fila["CostoFactura"] == DBNull.Value ? 0m : Convert.ToDecimal(fila["CostoFactura"]),
                TotalFactura = fila["TotalFactura"] == DBNull.Value ? 0m : Convert.ToDecimal(fila["TotalFactura"]),
                GananciaAbono = fila["GananciaAbono"] == DBNull.Value ? 0m : Convert.ToDecimal(fila["GananciaAbono"]),
                Usuario = Convert.ToString(fila["Usuario"]),
                Nota = Convert.ToString(fila["Nota"])
            }).ToList();
        }
        internal async Task<object> GetUtilidadesAsync(int? sucursal, DateTime FI, DateTime FF)
        {
            try
            {
                var data = await Task.Run(() => Ac.GetUtilidades(sucursal, FI, FF).AsEnumerable());

                var lista = (from dt in data
                             select new Utilidades
                             {
                                 Costo_Equipos = Convert.ToDecimal(dt["Costo_Equipos"]),
                                 Venta_Equipos = Convert.ToDecimal(dt["Venta_Equipos"]),
                                 Itbis_Equipos = Convert.ToDecimal(dt["Itbis_Equipos"]),
                                 Ganancia_Equipos = Convert.ToDecimal(dt["Ganancia_Equipos"]),
                                 Costo_Accesorios = Convert.ToDecimal(dt["Costo_Accesorios"]),
                                 Venta_Accesorios = Convert.ToDecimal(dt["Venta_Accesorios"]),
                                 Itbis_Accesorios = Convert.ToDecimal(dt["Itbis_Accesorios"]),
                                 Ganancia_Accesorios = Convert.ToDecimal(dt["Ganancia_Accesorios"]),
                                 Costo_Reparaciones = Convert.ToDecimal(dt["Costo_Reparaciones"]),
                                 Venta_Reparaciones = Convert.ToDecimal(dt["Venta_Reparaciones"]),
                                 Itbis_Reparaciones = Convert.ToDecimal(dt["Itbis_Reparaciones"]),
                                 Ganancia_Reparaciones = Convert.ToDecimal(dt["Ganancia_Reparaciones"]),
                                 Costo_Desbloqueos = Convert.ToDecimal(dt["Costo_Desbloqueos"]),
                                 Venta_Desbloqueos = Convert.ToDecimal(dt["Venta_Desbloqueos"]),
                                 Itbis_Desbloqueos = Convert.ToDecimal(dt["Itbis_Desbloqueos"]),
                                 Ganancia_Desbloqueos = Convert.ToDecimal(dt["Ganancia_Desbloqueos"]),
                                 Abonos_Credito = Convert.ToDecimal(dt["Abonos_Credito"]),
                                 Retencion_Tarjeta = dt.Table.Columns.Contains("Retencion_Tarjeta") && dt["Retencion_Tarjeta"] != DBNull.Value ? Convert.ToDecimal(dt["Retencion_Tarjeta"]) : 0m,
                             }).FirstOrDefault();

                return lista;
            }
            catch (Exception ex)
            {
                throw ex; // Considera usar "throw;" para mantener la pila de excepciones
            }
        }
    }
}
