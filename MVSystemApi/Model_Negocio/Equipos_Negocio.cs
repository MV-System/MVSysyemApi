using DTO;
using MVSystemApi.Interfaz;
using System.Data;

namespace MVSystemApi.Model
{
    public class Equipos_Negocio
    {
        private readonly IAccesoDatos Ac;

        public Equipos_Negocio(IAccesoDatos Ad)
        {
            Ac = Ad;
        }
        internal object Equipo_Insert(Equipos Equipo)
        {
            var result = (from dt in Ac.Equipo_Insert(Equipo).AsEnumerable()
                          select new Equipo_return
                          {
                              Imei = Convert.ToString(dt["Imei"]),
                              Modelo = Convert.ToString(dt["Modelo"]),
                              Marca = Convert.ToString(dt["Marca"]),
                              Mensaje = Convert.ToString(dt["Mensaje"])

                          }).ToList();
                 

            return result;
        }
        internal object Equipo_Busca_Disponible(string Equipo, int Id_Almacen)
        {
            var result = (from dt in Ac.Equipo_Busca_Disponible(Equipo,Id_Almacen).AsEnumerable()
                          select new Equipo_return
                          {
                              Imei = Convert.ToString(dt["Equipo"]),
                              Marca = Convert.ToString(dt["Marca"]),
                              Modelo = Convert.ToString(dt["Modelo"]),
                              precioPorMayor = Convert.ToDecimal(dt["precioPorMayor"]),
                              PrecioDetalle = Convert.ToDecimal(dt["PrecioDetalle"]),
                              Disponible_Detalle = Convert.ToString(dt["Nota_Adicional"]),
                              Mensaje = Convert.ToString(dt["Mensaje"])

                          }).ToList();


            return result;
        }
     
        internal object GetEquiposDisponible(EquipoDisponibleFilterDTO equipoDisponibleFilterDTO)
        {


            var result = (from dt in Ac.GetEquiposDisponible(equipoDisponibleFilterDTO).AsEnumerable()
                          select new EquipoDisponibleDTO
                          {
                              Imei = Convert.ToString(dt["Imei"]),
                              Descripcion = Convert.ToString(dt["Descripcion"]),
                              Tecnologia = Convert.ToString(dt["Tecnologia"]),
                              Garantia = Convert.ToString(dt["Garantia"]),
                              EstadoBloqueo = Convert.ToString(dt["Estado Bloqueo"]),
                              PrecioPorMayor = Convert.ToDecimal(dt["Por Mayor"]),
                              PrecioDetalle = Convert.ToDecimal(dt["Al Detalle"]),
                              ComisionDetalle = Convert.ToDecimal(dt["Comision Detalle"]),
                              ComisionPorMayor = Convert.ToDecimal(dt["Comision Por Mayor"]),
                              DescripcionAlmacen = Convert.ToString(dt["Descripcion_Almacen"]),
                              Suplidor = Convert.ToString(dt["Suplidor"]),
                              CostoEquipo = Convert.ToDecimal(dt["Costo"]),
                              IsDesbloqueado = Convert.ToString(dt["Desbloqueado"]),
                              IsDisponible = Convert.ToString(dt["Disponible"]),
                              NotaAdicional = Convert.ToString(dt["Nota"]),
                              IdAlmacen = Convert.ToInt32(dt["ID_Almacen"]),
                              IdModelo = Convert.ToInt32(dt["ID_Modelo"]),
                              IdSuplidor = Convert.ToInt32(dt["ID_Suplidor"]),
                              Fecharegistro = Convert.ToString(dt["Fecha_registro"]),
                              LastLine = Convert.ToInt32(dt["Ultima_Linea"]),
                              Line = Convert.ToInt32(dt["Linea"]),
                              TotalRecord = Convert.ToInt32(dt["Cantidad_Registros"]),
                              TotalInventario = Convert.ToDecimal(dt["TotalInventario"])
                          }).ToList();




            return result;
            //return Pagination<EquipoDisponibleDTO>.GetPagination(result, paginate.PageNumber, paginate.PageSize);
        }
        internal object GetEquiposVendidos(EquipoVendidoFilter equipoVendidoFilter)
        {


            var result = (from dt in Ac.GetEquiposVendidos(equipoVendidoFilter).AsEnumerable()
                          select new EquipoVendido
                          {
                              Imei = Convert.ToString(dt["Imei"]),
                              Descripcion = Convert.ToString(dt["Descripcion"]),
                              PrecioVendido = Convert.ToDecimal(dt["Precio Vendido"]),
                              TipoFactura = Convert.ToString(dt["Tipo Factura"]),
                              Factura = Convert.ToString(dt["Factura"]),
                              PrecioPorMayor = Convert.ToDecimal(dt["Por Mayor"]),
                              PrecioDetalle = Convert.ToDecimal(dt["Al Detalle"]),
                              ComisionDetalle = Convert.ToDecimal(dt["Comision Detalle"]),
                              ComisionPorMayor = Convert.ToDecimal(dt["Comision Por Mayor"]),
                              DescripcionAlmacen = Convert.ToString(dt["Descripcion_Almacen"]),
                              Suplidor = Convert.ToString(dt["Suplidor"]),
                              Vendedor = Convert.ToString(dt["Vendedor"]),
                              Cliente = Convert.ToString(dt["Cliente"]),
                              CostoEquipo = Convert.ToDecimal(dt["Costo"]),
                              Itbis = Convert.ToDecimal(dt["Itbis"]),
                              IsDesbloqueado = Convert.ToString(dt["Desb"]),
                              NotaAdicional = Convert.ToString(dt["Nota"]),
                              //IdAlmacen = Convert.ToInt32(dt["ID_Almacen"]),
                              //IdModelo = Convert.ToInt32(dt["ID_Modelo"]),
                              //IdSuplidor = Convert.ToInt32(dt["ID_Suplidor"]),
                              FechaRegistro = Convert.ToString(dt["Fecha_registro"]),
                              FechaFacturado = Convert.ToString(dt["Fecha facturado"]),
                              LastLine = Convert.ToInt32(dt["Ultima_Linea"]),
                              Line = Convert.ToInt32(dt["Linea"]),
                              TotalRecord = Convert.ToInt32(dt["Cantidad_Registros"]),
                              TotalInventario = Convert.ToDecimal(dt["TotalInventario"]),
                              TotalFacturado = Convert.ToDecimal(dt["TotalFacturado"]),
                              TotalGanancia = Convert.ToDecimal(dt["TotalGanancia"]),
                              TotalComisionDetalle = Convert.ToDecimal(dt["TotalComisionDetalle"]),
                              TotalComisionXMayor = Convert.ToDecimal(dt["TotalComisionXMayor"])

                          }).ToList();

            return result;
        }
        internal object Numero_Registro()
        {
            var result = (from dt in Ac.Equipo_Consulta_Ultimo_Registro().AsEnumerable()
                          select new Numero_registro
                          {
                              Numero_Registro = Convert.ToInt32(dt["ID"])+1,

                          }).ToList();

            return result;
        }
    }
}
