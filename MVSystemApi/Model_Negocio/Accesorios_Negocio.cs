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
                var resp = (from dt in Ac.GetAllAccesorios(paginate, accesorio, almacen).AsEnumerable()
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
