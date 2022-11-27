using MVSystemApi.Interfaz;
using MVSystemApi.Model;
using System.Data;

namespace MVSystemApi.Model_Negocio
{
    public class Inventario_Negocio
    {
        private readonly IAccesoDatos _Ac;

        public Inventario_Negocio(IAccesoDatos Ad)
        {
            _Ac = Ad;
        }

        internal object ObtenerEquiposInventarioPorAlmacenId(int almacenId) 
        { 
        
            var result = (from dt in _Ac.ObtenerEquiposInventarioPorAlmacenId(almacenId).AsEnumerable()
                          select new ArticuloInventario
                          {
                              IdInventario = Convert.ToInt32(dt["IdInventario"]),
                              IdEquipo = Convert.ToInt32(dt["IdEquipo"]),
                              Imei = Convert.ToString(dt["Imei"]),
                              Escaneado = Convert.ToChar(dt["Escaneado"]),
                              Descripcion = Convert.ToString(dt["Descripcion"]),
                              Usuario = Convert.ToString(dt["Usuario"]),
                              Fecha = Convert.ToString(dt["Fecha"])

                          }).ToList();


            return result;
        }

        internal void IniciarInventario(int almacenId)
        {
            _Ac.IniciarInventario(almacenId);
        }
        internal void CerrarInventario(int inventarioId)
        {
            _Ac.CerrarInventario(inventarioId);
        }
        internal void EscanearArticuloInventario(ArticuloInventarioUpdate articuloData)
        {
            _Ac.EscanearArticulo(articuloData);
        }
    }
}
