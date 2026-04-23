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
                                 Costo_Equipos = Convert.ToInt32(dt["Costo_Equipos"]),
                                 Venta_Equipos = Convert.ToInt32(dt["Venta_Equipos"]),
                                 Itbis_Equipos = Convert.ToInt32(dt["Itbis_Equipos"]),
                                 Ganancia_Equipos = Convert.ToInt32(dt["Ganancia_Equipos"]),
                                 Costo_Accesorios = Convert.ToInt32(dt["Costo_Accesorios"]),
                                 Venta_Accesorios = Convert.ToInt32(dt["Venta_Accesorios"]),
                                 Itbis_Accesorios = Convert.ToInt32(dt["Itbis_Accesorios"]),
                                 Ganancia_Accesorios = Convert.ToInt32(dt["Ganancia_Accesorios"]),
                                 Costo_Reparaciones = Convert.ToInt32(dt["Costo_Reparaciones"]),
                                 Venta_Reparaciones = Convert.ToInt32(dt["Venta_Reparaciones"]),
                                 Itbis_Reparaciones = Convert.ToInt32(dt["Itbis_Reparaciones"]),
                                 Ganancia_Reparaciones = Convert.ToInt32(dt["Ganancia_Reparaciones"]),
                                 Costo_Desbloqueos = Convert.ToInt32(dt["Costo_Desbloqueos"]),
                                 Venta_Desbloqueos = Convert.ToInt32(dt["Venta_Desbloqueos"]),
                                 Itbis_Desbloqueos = Convert.ToInt32(dt["Itbis_Desbloqueos"]),
                                 Ganancia_Desbloqueos = Convert.ToInt32(dt["Ganancia_Desbloqueos"]),

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
        internal async Task<object> GetUtilidadesAsync(int? sucursal, DateTime FI, DateTime FF)
        {
            try
            {
                var data = await Task.Run(() => Ac.GetUtilidades(sucursal, FI, FF).AsEnumerable());

                var lista = (from dt in data
                             select new Utilidades
                             {
                                 Costo_Equipos = Convert.ToInt32(dt["Costo_Equipos"]),
                                 Venta_Equipos = Convert.ToInt32(dt["Venta_Equipos"]),
                                 Itbis_Equipos = Convert.ToInt32(dt["Itbis_Equipos"]),
                                 Ganancia_Equipos = Convert.ToInt32(dt["Ganancia_Equipos"]),
                                 Costo_Accesorios = Convert.ToInt32(dt["Costo_Accesorios"]),
                                 Venta_Accesorios = Convert.ToInt32(dt["Venta_Accesorios"]),
                                 Itbis_Accesorios = Convert.ToInt32(dt["Itbis_Accesorios"]),
                                 Ganancia_Accesorios = Convert.ToInt32(dt["Ganancia_Accesorios"]),
                                 Costo_Reparaciones = Convert.ToInt32(dt["Costo_Reparaciones"]),
                                 Venta_Reparaciones = Convert.ToInt32(dt["Venta_Reparaciones"]),
                                 Itbis_Reparaciones = Convert.ToInt32(dt["Itbis_Reparaciones"]),
                                 Ganancia_Reparaciones = Convert.ToInt32(dt["Ganancia_Reparaciones"]),
                                 Costo_Desbloqueos = Convert.ToInt32(dt["Costo_Desbloqueos"]),
                                 Venta_Desbloqueos = Convert.ToInt32(dt["Venta_Desbloqueos"]),
                                 Itbis_Desbloqueos = Convert.ToInt32(dt["Itbis_Desbloqueos"]),
                                 Ganancia_Desbloqueos = Convert.ToInt32(dt["Ganancia_Desbloqueos"]),
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
