using DTO;
using MVSystemApi.Interfaz;
using System.Data;

namespace MVSystemApi.Model
{
    public class Accesorios_Negocio
    {
        private readonly IAccesoDatos Ac;

  
        public Accesorios_Negocio(IAccesoDatos db)
        {
            Ac = db;
        }
        internal object Accesorio_Insert(Accesorio accesorio)
        {
            try
            {
                var solicit = (from dt in Ac.Accesorio_Insert(accesorio).AsEnumerable()
                               select new Accesorio_Respuesta
                               {
                                   Descripcion_Accesorio = Convert.ToString(dt["Descripcion_Accesorio"]),
                                   Cantidad = Convert.ToInt32(dt["Cantidad"]),
                                   Precio_Por_Mayor = Convert.ToDecimal(dt["Precio_Por_Mayor"]),
                                   Precio_Detalle = Convert.ToDecimal(dt["Precio_Detalle"]),
                                 
                               }).ToList().FirstOrDefault();

                return solicit;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        internal object ConsultaAccesorio(int Codigo)
        {
            try
            {
                var solicit = (from dt in Ac.ConsultaAccesorio(Codigo).AsEnumerable()
                               select new Accesorio_Respuesta
                               {
                                   Descripcion_Accesorio = Convert.ToString(dt["Descripcion_Accesorio"]),
                                   Cantidad = Convert.ToInt32(dt["Cantidad"]),
                                   Precio_Por_Mayor = Convert.ToDecimal(dt["Precio_Por_Mayor"]),
                                   Precio_Detalle = Convert.ToDecimal(dt["Precio_Detalle"]),

                               }).ToList().FirstOrDefault();

                return solicit;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        internal object GetAllAccesorios(Paginate paginate,string accesorio, int almacen)
        {
            try
            {
                var resp = (from dt in Ac.GetAccesorios(paginate, accesorio, almacen).AsEnumerable()
                          select new AccesorioDTO
                          {
                              ID = Convert.ToInt32(dt["id"]),
                              Codigo = Convert.ToString(dt["Codigo"]),
                              DescripcionAccesorio = Convert.ToString(dt["Descripcion_Accesorio"]),
                              Modelo = Convert.ToString(dt["Modelo"]),
                              Cantidad = Convert.ToInt32(dt["Cantidad"]),
                              CostoEquipo = Convert.ToInt32(dt["Costo_Equipo"]),
                              PrecioDetalle = Convert.ToInt32(dt["Precio_Detalle"]),
                              PrecioPorMayor = Convert.ToInt32(dt["Precio_Por_Mayor"]),
                              Disponible = Convert.ToString(dt["Disponible"]),
                              DisponibleDetalle = Convert.ToString(dt["Nota"]),
                              Almacen = Convert.ToString(dt["Almacen"]),
                              CodBarra = Convert.ToInt32(dt["Codigo_Barra"]),
                              TotalInventario = Convert.ToInt32(dt["TotalInventario"]),
                              LastLine = Convert.ToInt32(dt["Ultima_Linea"]),
                              Line = Convert.ToInt32(dt["Linea"]),
                              TotalRecord = Convert.ToInt32(dt["Cantidad_Registros"])
                          }).ToList();
                return resp;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        /// <summary>
        /// Retornna una accesorios de equips vendidos
        /// Author: Xavier Mejia
        /// </summary>
        /// <param name="paginate"> paginación</param>
        /// <param name="accesorio"> Descripción/nombre del accesorio</param>
        /// <param name="almacen"> Id del almacen</param>
        /// <returns></returns>
        internal object GetAccesoriosVendidos(Paginate paginate, string accesorio, int almacen)
        {
            try
            {
                var resp = (from dt in Ac.GetAccesoriosVendidos(paginate, accesorio, almacen).AsEnumerable()
                            select new AccesorioVendidoDTO
                            {
                                Factura = Convert.ToString(dt["Factura"]),
                                Descripcion = Convert.ToString(dt["Descripcion"]),
                                Cantidad = Convert.ToInt32(dt["Cantidad"]),
                                Costo = Convert.ToInt32(dt["Costo"]),
                                Precio = Convert.ToInt32(dt["precio"]),
                                Sucursal = Convert.ToString(dt["Sucursal"]),
                                FechaRegistro = Convert.ToDateTime(dt["Fecha_registro"]),
                                LastLine = Convert.ToInt32(dt["Ultima_Linea"]),
                                Line = Convert.ToInt32(dt["Linea"]),
                                TotalRecord = Convert.ToInt32(dt["Cantidad_Registros"]),
                                TotalInventario = Convert.ToInt32(dt["TotalInventario"]),
                                TotalFacturado = Convert.ToDecimal(dt["TotalFacturado"]),
                                TotalGanancia = Convert.ToDecimal(dt["TotalGanancia"]),
                                TotalItbis = Convert.ToDecimal(dt["TotalItbis"])
                            }).ToList();
                return resp;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        internal object GetAccesorioById( int id)
        {
            try
            {
                var resp = (from dt in Ac.GetAccesorioById(id).AsEnumerable()
                            select new AccesorioModelId
                            {
                                ID = Convert.ToInt32(dt["ID_Accesorio"]),
                                Codigo = Convert.ToString(dt["Codigo"]),
                                DescripcionAccesorio = Convert.ToString(dt["Descripcion_Accesorio"]),
                                Cantidad = Convert.ToInt32(dt["Cantidad"]),
                                CostoEquipo = Convert.ToInt32(dt["Costo_Equipo"]),
                                PrecioDetalle = Convert.ToInt32(dt["Precio_Detalle"]),
                                PrecioPorMayor = Convert.ToInt32(dt["Precio_Por_Mayor"]),
                                Estado = Convert.ToString(dt["Estado"]),
                            }).FirstOrDefault();
                return resp;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
