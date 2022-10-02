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
        internal Pagination<AccesorioDTO> GetAllAccesorios(Paginate paginate,string accesorio, int almacen)
        {
            List<AccesorioDTO> accesorios = new List<AccesorioDTO>();
            try
            {
              var dr = Ac.GetAllAccesorios(accesorio, almacen);

                while (dr.Read())
                {
                    AccesorioDTO accesorioDTO = new AccesorioDTO
                    {
                        ID = Convert.ToInt32(dr["ID_Accesorio"]),
                        Codigo = Convert.ToString(dr["Codigo"]),
                        DescripcionAccesorio = Convert.ToString(dr["Descripcion_Accesorio"]),
                        Modelo = Convert.ToString(dr["Modelo"]),
                        Cantidad = Convert.ToInt32(dr["Cantidad"]),
                        CostoEquipo = Convert.ToInt32(dr["Costo_Equipo"]),
                        PrecioDetalle = Convert.ToInt32(dr["Precio_Detalle"]),
                        PrecioPorMayor = Convert.ToInt32(dr["Precio_Por_Mayor"]),
                        Disponible = Convert.ToString(dr["Disponible"]),
                        DisponibleDetalle = Convert.ToString(dr["Nota"]),
                        Almacen = Convert.ToString(dr["Almacen"]),
                        CodBarra = Convert.ToInt32(dr["Codigo_Barra"]),
                        TotalInventario = Convert.ToInt32(dr["TotalInventario"])
                    };
                    accesorios.Add(accesorioDTO);
                }
                return Pagination<AccesorioDTO>.GetPagination(accesorios, paginate.PageNumber, paginate.PageSize);
                //return accesorios.Skip((paginate.PageNumber - 1) * paginate.PageSize)
                //                 .Take(paginate.PageSize)
                //                 .ToList();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

    }
}
