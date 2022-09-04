using MVSystemApi.Interfaz;
using System;
using System.Linq;
using System.Data;

namespace MVSystemApi.Model
{
    public class Catalogos_Negocio
    {
        private readonly IAccesoDatos Ac;

        public Catalogos_Negocio(IAccesoDatos db)
        {
            Ac = db;
        }
        internal object GetTipos_Pagos_Lista()
        {

            try
            {
                var Lista = (from dt in Ac.Tipos_Pagos_Cata_Consulta_Combo().AsEnumerable()
                             select new Tipos_Pagos
                             {
                                 ID_Tipo_Pagos = Convert.ToInt32(dt["ID_Tipo_Pagos"]),
                                 Descripcion_Tipo_Pagos = Convert.ToString(dt["Descripcion_Tipo_Pagos"]),

                             }).ToList();

                return Lista;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        internal object GetTipos_Factura_Lista()
        {

            try
            {
                var Lista = (from dt in Ac.GetTipos_Factura_Lista().AsEnumerable()
                             select new Tipos_Facturas
                             {
                                 ID_Tipo_Factura = Convert.ToInt32(dt["ID_Tipo_Factura"]),
                                 Descripcion_Factura_Tipo = Convert.ToString(dt["Descripcion_Factura_Tipo"]),

                             }).ToList();

                return Lista;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        internal object GetAccesorios_Facturacion_Lista(int? Id_accesorio, int? id_sucu)
        {

            try
            {
                var Lista = (from dt in Ac.Accesorios_Consulta_Facturacion(Id_accesorio, id_sucu).AsEnumerable()
                             select new Accesorios_Facturacion
                             {
                                 codigo = Convert.ToString(dt["CODIGO"]),
                                 Id = Convert.ToInt32(dt["ID"]),
                                 Accesorio_Descripcion = Convert.ToString(dt["ACCESORIO"]),
                                 Modelo = Convert.ToString(dt["MODELO"]),
                                 Codigo_Barra = Convert.ToInt64(dt["CODIGO BARRA"]),
                                 Precio_Por_Mayor = Convert.ToDecimal(dt["Precio_Por_Mayor"]),
                                 Precio_Detalle = Convert.ToDecimal(dt["Precio_Detalle"]),
                                 Cantidad = Convert.ToInt32(dt["Cantidad"]),


                             }).ToList();

                return Lista;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        internal List<Vendedores_Combo> GetVendedor_Lista()
        {

            try
            {
                var Lista = (from dt in Ac.Vendedor_Consulta_Combo().AsEnumerable()
                             select new Vendedores_Combo
                             {
                                 ID_Vendedor = Convert.ToInt32(dt["ID_Vendedor"]),
                                 Nombres = Convert.ToString(dt["Nombres"]),

                             }).ToList();

                return Lista;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        internal object GetTipo_Cliente_Lista()
        {

            try
            {
                var Lista = (from dt in Ac.Tipo_Cliente_Cata_Consulta_Combo().AsEnumerable()
                             select new TipoClientes
                             {
                                 ID_Tipo_Cliente = Convert.ToInt32(dt["ID_Tipo_Cliente"]),
                                 Tipo_Cliente_Descripcion = Convert.ToString(dt["Tipo_Cliente_Descripcion"]),

                             }).ToList();

                return Lista;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        internal object GetTipo_Suplidor_Lista()
        {
            try
            {
                var Lista = (from dt in Ac.Tipo_Suplidor_Cata_Consulta_Combo().AsEnumerable()
                             select new Tipo_Suplidor_Combo
                             {
                                 ID_Tipo_Suplidor = Convert.ToInt32(dt["ID_Tipo_Suplidor"]),
                                 Tipo_Suplidor_Descripcion = Convert.ToString(dt["Tipo_Suplidor_Descripcion"]),

                             }).ToList();

                return Lista;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        internal object GetMarcas_Combo()
        {

            try
            {
                var Lista = (from dt in Ac.Get_Marcas_Combo().AsEnumerable()
                             select new Marcas
                             {
                                 ID_Marca = Convert.ToInt32(dt["ID_Marca"]),
                                 descripcion = Convert.ToString(dt["Descripcion"]),

                             }).ToList();

                return Lista;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        internal object GetModelos_Combo(int? ID_Marca)
        {
            var result = (from dt in Ac.Get_Modelos_Combo(ID_Marca).AsEnumerable()
                          select new Modelos
                          {
                              ID_Modelo = Convert.ToInt32(dt["ID_Modelo"]),
                              Descripcion = Convert.ToString(dt["Descripcion"])
                          }).ToList();
            return result;
        }

        internal object GetTipo_Telefono_Lista()
        {
            //------------Cargar ComboBox de tipos De Telefonos-----------------------------

            try
            {
                var Lista = (from dt in Ac.GetTipo_Telefono_Lista().AsEnumerable()
                             select new Telefonos_Tipo
                             {
                                 ID_Tipo_Telefono = Convert.ToInt32(dt["ID_Tipo_Telefono"]),
                                 Tipo_Telefono_Descripcion = Convert.ToString(dt["Tipo_Telefono_Descripcion"]),

                             }).ToList();

                return Lista;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        internal object GetTecnologia_Combo()
        {
            var result = (from dt in Ac.Get_Tegnologia_Combo().AsEnumerable()
                          select new Tecnologias
                          {
                              ID_Tecnologia = Convert.ToInt32(dt["ID_Tecnologia"]),
                              Descripcion = Convert.ToString(dt["Tecnologia_Descripcion"])
                          }).ToList();
            return result;
        }

        internal object GetDesbloqueado_Combo()
        {
            var result = (from dt in Ac.Get_Desbloqueado_Equipo_Combo().AsEnumerable()
                          select new Desbloqueado
                          {
                              ID_Bloqueo = Convert.ToInt32(dt["ID_Estado_Bloqueo"]),
                              Descripcion = Convert.ToString(dt["Descripcion_Estado_Bloqueo"])
                          }).ToList();
            return result;
        }

        internal object GetCondicion_Equipo_Combo()
        {
            var result = (from dt in Ac.Get_Condicion_Equipo_Combo().AsEnumerable()
                          select new Condicion_Equipo
                          {
                              ID_Condicion = Convert.ToInt32(dt["ID_Condicion"]),
                              Descripcion = Convert.ToString(dt["Descripcion_Condicion"])
                          }).ToList();
            return result;
        }

        internal object Get_Garantias_Combo()
        {
            var result = (from dt in Ac.Get_Garantia_Equipo_Combo().AsEnumerable()
                          select new Garantias
                          {
                              ID_Garantia = Convert.ToInt32(dt["ID_Garantia"]),
                              Descripcion = Convert.ToString(dt["Descripcion_Garantia"])
                          }).ToList();
            return result;
        }



        internal object Get_Suplidor_Combo()
        {
            var result = (from dt in Ac.Get_Suplidor_Combo().AsEnumerable()
                          select new Suplidor_Combo
                          {
                              ID_Suplidor = Convert.ToInt32(dt["ID_Suplidor"]),
                              Descripcion = Convert.ToString(dt["Nombres"])
                          }).ToList();
            return result;
        }

        internal object GetAlmacen_Combo()
        {
            var result = (from dt in Ac.Get_Almacen_Combo().AsEnumerable()
                          select new Almacen
                          {
                              ID_Almacen = Convert.ToInt32(dt["ID_Almacen"]),
                              Descripcion = Convert.ToString(dt["Descripcion_Almacen"])
                          }).ToList();
            return result;
        }
        internal object GetComprobantes_Combo()
        {
            var result = (from dt in Ac.GetComprobantes_Combo().AsEnumerable()
                          select new Comprobantes
                          {
                              NFCTipoNumero = Convert.ToInt32(dt["NFCTipoNumero"]),
                              NFCTipoDescripcion = Convert.ToString(dt["NFCTipoDescripcion"])
                          }).ToList();
            return result;
        }
    }
}
