using DTO;
using Microsoft.Extensions.Caching.Memory;
using MVSystemApi.Interfaz;
using MVSystemApi.Model_Negocio.Seguridad;
using System.Data;
using System.Data.SqlClient;

namespace MVSystemApi.Model
{
    public class AccesoDatos : IAccesoDatos
    {
        private readonly SqlConnection cn;
        public static int Numero_Registro;
        private readonly string _connectionStrings;
        private readonly IMemoryCache _memoryCache;

        public AccesoDatos(IHttpContextAccessor httpContextAccessor, SeguridadService seguridad, IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
            var keyConnString = $"ConnString:{httpContextAccessor.HttpContext.User.Identity.Name}";

            if (_memoryCache.TryGetValue<string>(keyConnString, out var value))
                _connectionStrings = value;
            else
            {
                var conn = seguridad.GetConnStringByUser(httpContextAccessor.HttpContext.User.Identity.Name);
                _memoryCache.Set(keyConnString, conn, TimeSpan.FromMinutes(13));
                _connectionStrings = conn;
            }

            cn = new SqlConnection(_connectionStrings);
        }

        public static Int64 codigo;

        #region Accesorios
        /// <summary>
        /// Return a equipo by Imei    
        /// Author: Xavier Mejia
        /// Date: 6.Oct.2022
        /// </summary>
        /// <param name="id">The id accesorio</param>
        /// <returns>
        /// DataTable
        /// </returns>
        public DataTable GetAccesorioById(int id)
        {
            try
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("Proc_Accesorios_Consulta_Id", cn);
                DataTable dt = new DataTable();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@id", id);
              
                cmd.ExecuteNonQuery();
                da.Fill(dt);
                cn.Close();

                return dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        /// <summary>
        /// 
        /// Author: Xavier Mejia
        /// Date: 8.Oct.2022
        /// </summary>
        /// <param name="accesorio">The accesorio model</param>
        /// <returns>
        /// DataTable
        /// </returns>
        public DataTable UpdateAccesorio(AccesorioDTO accesorio)
        {
            try
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("Accesorios_Modifica", cn);
                DataTable dt = new DataTable();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ID_Accesorio", accesorio.ID);
                cmd.Parameters.AddWithValue("@Codigo", accesorio.Codigo);
                cmd.Parameters.AddWithValue("@Descripcion_Accesorio", accesorio.DescripcionAccesorio);
                cmd.Parameters.AddWithValue("@Precio_Por_Mayor", accesorio.PrecioPorMayor);
                cmd.Parameters.AddWithValue("@Precio_Detalle", accesorio.PrecioDetalle);
                cmd.Parameters.AddWithValue("@Estado", accesorio.Estado);
                cmd.Parameters.AddWithValue("@Cantidad", accesorio.Cantidad);
                cmd.Parameters.AddWithValue("@Usuario", "Xavier08");

                cmd.ExecuteNonQuery();
                da.Fill(dt);
                cn.Close();

                return dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public DataTable GetAccesorios(Paginate paginate,string accesorio, int almacen)
        {
            try
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("Accesorios_Consulta", cn);
                DataTable dt = new DataTable();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@PageIndex", paginate.PageIndex);
                cmd.Parameters.AddWithValue("@PageSize", paginate.PageSize);
                cmd.Parameters.AddWithValue("@Accesorio", accesorio);
                cmd.Parameters.AddWithValue("@Criterio", almacen);

                cmd.ExecuteNonQuery();
                da.Fill(dt);
                cn.Close();

                return dt;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public DataTable GetAccesoriosVendidos(Paginate paginate, string accesorio, int almacen)
        {
            try
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("Accesorios_Vendidos_Consulta", cn);
                DataTable dt = new DataTable();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@PageIndex", paginate.PageIndex);
                cmd.Parameters.AddWithValue("@PageSize", paginate.PageSize);
                cmd.Parameters.AddWithValue("@Accesorio", accesorio);
                cmd.Parameters.AddWithValue("@id_sucursal", almacen);
                cmd.Parameters.AddWithValue("@Fecha_Inicio", paginate.FechaInicio);
                cmd.Parameters.AddWithValue("@Fecha_Final", paginate.FechaFinal);

                cmd.ExecuteNonQuery();
                da.Fill(dt);
                cn.Close();

                return dt;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public DataTable Accesorio_Insert(Accesorio Accesorio)
        {
            try
            {
                cn.Open();

                SqlCommand cmd = cn.CreateCommand();
                DataTable dt = new DataTable();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "Accesorio_Insert";

                cmd.Parameters.AddWithValue("@Imei_Equipo", Accesorio.Imei_Equipo);
                cmd.Parameters.AddWithValue("@ID_Modelo", Accesorio.ID_Modelo);
                cmd.Parameters.AddWithValue("@Numero_de_Factura", Accesorio.codigo);
                cmd.Parameters.AddWithValue("@Descripcion_Accesorio", Accesorio.Descripcion_Accesorio);
                cmd.Parameters.AddWithValue("@ID_Almacen", Accesorio.ID_Almacen);
                cmd.Parameters.AddWithValue("@Cantidad", Accesorio.Cantidad);
                cmd.Parameters.AddWithValue("@Can_Max", Accesorio.Can_Max);
                cmd.Parameters.AddWithValue("@Can_Min", Accesorio.Can_Min);
                cmd.Parameters.AddWithValue("@Precio_Por_Mayor", Accesorio.Precio_Por_Mayor);
                cmd.Parameters.AddWithValue("@Precio_Detalle", Accesorio.Precio_Detalle);
                cmd.Parameters.AddWithValue("@Costo_Equipo", Accesorio.Costo_Equipo);
                cmd.Parameters.AddWithValue("@Codigo_Barra", Accesorio.Cod_Barra);
                cmd.Parameters.AddWithValue("@Disponible", Accesorio.Disponible);
                cmd.Parameters.AddWithValue("@Disponible_Detalle", Accesorio.Disponible_Detalle);
                cmd.Parameters.AddWithValue("@Usuario", Accesorio.Usuario);

                cmd.ExecuteNonQuery();
                da.Fill(dt);
                cn.Close();

                return dt;
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }
        public DataTable ConsultaAccesorio(int Codigo)
        {
            try
            {
                cn.Open();

                Accesorio Accesorio = new Accesorio();
                SqlCommand comando = cn.CreateCommand();
                comando.CommandText = "Accesorios_Consulta_pk";
                comando.Parameters.AddWithValue("Codigo", Codigo);
                comando.CommandType = CommandType.StoredProcedure;

                SqlDataAdapter da = new SqlDataAdapter();
                da.SelectCommand = comando;
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();

                return dt;
            }
            catch (Exception)
            {
                throw;
            }

        }

        #endregion
        public DataTable Suplidor_Insert(Suplidor Suplidor)
        {
            cn.Open();

            SqlCommand cmd = cn.CreateCommand();
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "Suplidores_Insert";

            cmd.Parameters.AddWithValue("@Nombres", Suplidor.Nombres);
            cmd.Parameters.AddWithValue("@Apellidos", Suplidor.Apellidos);
            cmd.Parameters.AddWithValue("@Numero_Telefono", Suplidor.Numero_Telefono);
            cmd.Parameters.AddWithValue("@Extencion", Suplidor.Extencion);
            cmd.Parameters.AddWithValue("@Email_Detalle", Suplidor.Email_Detalle);
            cmd.Parameters.AddWithValue("@Direccion_Detalle", Suplidor.Direccion_Detalle);
            cmd.Parameters.AddWithValue("@ID_Tipo_Telefono", Suplidor.ID_Tipo_Telefono);
            cmd.Parameters.AddWithValue("@ID_Tipo_Suplidor", Suplidor.ID_Tipo_Suplidor);
            cmd.Parameters.AddWithValue("@Usuario", Suplidor.Usuario);

            da.Fill(dt);
            cn.Close();
            return dt;
        }
        public DataTable Vendedor_Insert(Vendedor Vendedor)
        {
            try
            {
                cn.Open();
                SqlCommand cmd = cn.CreateCommand();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();

                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "Vendedor_Insert";

                cmd.Parameters.AddWithValue("@Nombres", Vendedor.Nombres);
                cmd.Parameters.AddWithValue("@Apellidos", Vendedor.Apellidos);
                cmd.Parameters.AddWithValue("@Numero_Telefono", Vendedor.Numero_Telefono);
                cmd.Parameters.AddWithValue("@Extencion", Vendedor.Extencion);
                cmd.Parameters.AddWithValue("@Email_Detalle", Vendedor.Email_Detalle);
                cmd.Parameters.AddWithValue("@Direccion_Detalle", Vendedor.Direccion_Detalle);
                cmd.Parameters.AddWithValue("@ID_Tipo_Telefono", Vendedor.ID_Tipo_Telefono);
                cmd.Parameters.AddWithValue("@Usuario", Vendedor.Usuario);

                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public DataTable Cliente_Insert(Clientes clientes)
        {
            try
            {
                cn.Open();
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "Cliente_Insert";

                cmd.Parameters.AddWithValue("@Nombres", clientes.Nombres);
                cmd.Parameters.AddWithValue("@Apellidos", clientes.Apellidos);
                cmd.Parameters.AddWithValue("@ID_Tipo_Cliente", clientes.ID_Tipo_Cliente);
                cmd.Parameters.AddWithValue("@Numero_Telefono", clientes.Numero_Telefono);
                cmd.Parameters.AddWithValue("@Limite_Credito", clientes.Limite_Credito);
                cmd.Parameters.AddWithValue("@Extencion", clientes.Extencion);
                cmd.Parameters.AddWithValue("@Email_Detalle", clientes.Email_Detalle);
                cmd.Parameters.AddWithValue("@Direccion_Detalle", clientes.Direccion_Detalle);
                cmd.Parameters.AddWithValue("@ID_Tipo_Telefono", clientes.ID_Tipo_Telefono);
                cmd.Parameters.AddWithValue("@Usuario", clientes.Usuario);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public DataTable Cliente_Consulta_Por_Id_Cliente(int id_Cliente)
        {

            ////SqlConnection cn = new SqlConnection(con);
            cn.Open();
            try
            {
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = "Cliente_Consulta_Por_Id_Cliente";
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IdCliente", id_Cliente);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public DataTable Cliente_Consulta(string criterio)
        {

            ////SqlConnection cn = new SqlConnection(con);
            cn.Open();
            try
            {
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = "Cliente_Consulta";
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Criterio", criterio);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public DataTable GetTipo_Telefono_Lista()
        {

            ////SqlConnection cn = new SqlConnection(con);
            cn.Open();
            try
            {
                DataTable dt = new DataTable();
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = "Tipo_Telefonos_Cata_Consulta_Combo";
                cmd.CommandType = CommandType.StoredProcedure;
                SqlDataAdapter da = new SqlDataAdapter(cmd);

                da.Fill(dt);
                return dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public DataTable Tipos_Pagos_Cata_Consulta_Combo()
        {

            ////SqlConnection cn = new SqlConnection(con);
            cn.Open();
            try
            {
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = "Tipos_Pagos_Cata_Consulta_Combo";
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public DataTable GetTipos_Factura_Lista()
        {

            ////SqlConnection cn = new SqlConnection(con);
            cn.Open();
            try
            {
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = "Tipos_Facturas_Consulta_Combo";
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public DataTable Accesorios_Consulta_Facturacion(int? Id_accesorio, int? id_sucu)
        {

            ////SqlConnection cn = new SqlConnection(con);
            cn.Open();
            try
            {
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = "Accesorios_Consulta_Facturacion";
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ID_Accesorio", Id_accesorio);
                cmd.Parameters.AddWithValue("@id_sucu", id_sucu);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public DataTable Vendedor_Consulta_Combo()
        {

            ////SqlConnection cn = new SqlConnection(con);
            cn.Open();
            try
            {
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = "Vendedor_Consulta_Combo";
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public DataTable Get_Marcas_Combo()
        {

            ////SqlConnection cn = new SqlConnection(con);
            cn.Open();
            try
            {
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = "Marcas_Consulta_Combo";
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public DataTable Tipo_Cliente_Cata_Consulta_Combo()
        {

            ////SqlConnection cn = new SqlConnection(con);
            cn.Open();
            try
            {
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = "Tipo_Cliente_Cata_Consulta_Combo";
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public DataTable Tipo_Suplidor_Cata_Consulta_Combo()
        {

            ////SqlConnection cn = new SqlConnection(con);
            cn.Open();
            try
            {
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = "Tipos_Suplidor_Cata_Consulta_Combo";
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public DataTable Get_Tegnologia_Combo()
        {
            cn.Open();

            SqlCommand cmd = cn.CreateCommand();
            DataTable dt = new DataTable();
            SqlDataAdapter da = new SqlDataAdapter(cmd);

            cmd.CommandText = "Tecnologias_Cata_Consulta_Combo";
            cmd.CommandType = CommandType.StoredProcedure;

            da.Fill(dt);
            cn.Close();
            return dt;
        }
        public DataTable Get_Garantia_Equipo_Combo()
        {
            cn.Open();
            SqlCommand cmd = cn.CreateCommand();
            DataTable dt = new DataTable();
            SqlDataAdapter da = new SqlDataAdapter(cmd);

            cmd.CommandText = "Garantias_Cata_Consulta_Combo";
            cmd.CommandType = CommandType.StoredProcedure;

            da.Fill(dt);
            cn.Close();
            return dt;
        }
        public DataTable Get_Desbloqueado_Equipo_Combo()
        {
            cn.Open();
            DataTable dt = new DataTable();
            SqlCommand cmd = cn.CreateCommand();
            SqlDataAdapter da = new SqlDataAdapter(cmd);

            cmd.CommandText = "Estados_Bloqueos_Cata_Consulta_Combo";
            cmd.CommandType = CommandType.StoredProcedure;
            da.Fill(dt);
            cn.Close();
            return dt;
        }
        public DataTable Get_Condicion_Equipo_Combo()
        {
            cn.Open();
            DataTable dt = new DataTable();
            SqlCommand cmd = cn.CreateCommand();
            SqlDataAdapter da = new SqlDataAdapter(cmd);

            cmd.CommandText = "Condicion_equipos_Cata_Consulta_Combo";
            cmd.CommandType = CommandType.StoredProcedure;

            da.Fill(dt);
            cn.Close();
            return dt;
        }
        public DataTable Get_Almacen_Combo()
        {
            cn.Open();
            DataTable dt = new DataTable();
            SqlCommand cmd = cn.CreateCommand();
            SqlDataAdapter da = new SqlDataAdapter(cmd);

            cmd.CommandText = "Almacen_Cata_Consulta_Combo";
            cmd.CommandType = CommandType.StoredProcedure;

            da.Fill(dt);
            cn.Close();
            return dt;
        }
        public DataTable Get_Sucursal_Combo()
        {
            cn.Open();
            DataTable dt = new DataTable();
            SqlCommand cmd = cn.CreateCommand();
            SqlDataAdapter da = new SqlDataAdapter(cmd);

            cmd.CommandText = "Sucursales_cata_Combo";
            cmd.CommandType = CommandType.StoredProcedure;

            da.Fill(dt);
            cn.Close();
            return dt;
        }
        public DataTable Get_Modelos_Combo(int? ID_Marca)
        {
            cn.Open();
            DataTable dt = new DataTable();
            SqlCommand cmd = cn.CreateCommand();
            SqlDataAdapter da = new SqlDataAdapter(cmd);

            cmd.CommandText = "Modelos_Consulta_Combo";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@criterio", ID_Marca);

            da.Fill(dt);
            cn.Close();
            return dt;
        }
        public DataTable Get_Suplidor_Combo()
        {
            cn.Open();
            DataTable dt = new DataTable();
            SqlCommand cmd = cn.CreateCommand();
            SqlDataAdapter da = new SqlDataAdapter(cmd);

            cmd.CommandText = "Suplidor_Consulta_Combo";
            cmd.CommandType = CommandType.StoredProcedure;

            da.Fill(dt);
            cn.Close();
            return dt;
        }

        #region Equipos

        public DataTable Equipo_Insert(Equipos Equipo)
        {
            try
            {
                cn.Open();

                SqlCommand cmd = cn.CreateCommand();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();


                cmd.CommandText = "Equipos_Insert";
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Imei", Equipo.Imei);
                cmd.Parameters.AddWithValue("@ID_Modelo", Equipo.ID_Modelo);
                cmd.Parameters.AddWithValue("@ID_Tecnologia", Equipo.ID_Tecnologia);
                cmd.Parameters.AddWithValue("@ID_Garantia", Equipo.ID_Garantia);
                cmd.Parameters.AddWithValue("@ID_Condicion", Equipo.ID_Condicion);
                cmd.Parameters.AddWithValue("@Numero_de_Factura", Equipo.Numero_de_Factura);
                cmd.Parameters.AddWithValue("@ID_Estado_Bloqueo", Equipo.ID_Estado_Bloqueo);
                cmd.Parameters.AddWithValue("@ID_Almacen", Equipo.ID_Almacen);
                cmd.Parameters.AddWithValue("@ID_Suplidor", Equipo.ID_Suplidor);
                cmd.Parameters.AddWithValue("@Precio_Por_Mayor", Equipo.Precio_Por_Mayor);
                cmd.Parameters.AddWithValue("@Precio_Detalle", Equipo.Precio_Detalle);
                cmd.Parameters.AddWithValue("@Costo_Equipo", Equipo.Costo_Equipo);
                cmd.Parameters.AddWithValue("@Desbloqueado", Equipo.Desbloqueado);
                cmd.Parameters.AddWithValue("@Disponible", Equipo.Disponible);
                cmd.Parameters.AddWithValue("@Nota_Adicional", Equipo.Nota_Adicional);
                cmd.Parameters.AddWithValue("@Usuario", Equipo.Usuario);
                cmd.Parameters.AddWithValue("@Fecha_registro", DateTime.Now);
                cmd.Parameters.AddWithValue("@Numero_Registro", Equipo.Numero_Registro);
                cmd.Parameters.AddWithValue("@comision_Detalle", Equipo.Comision_Detalle);
                cmd.Parameters.AddWithValue("@comision_Por_Mayor", Equipo.Comision_Por_Mayor);
                cmd.ExecuteNonQuery();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        public DataTable GetEquiposDisponible(EquipoDisponibleFilterDTO equipoDisponibleFilterDTO)
        {
            try
            {
                cn.Open();

                SqlCommand cmd = cn.CreateCommand();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();


                cmd.CommandText = "Proc_Equipo_Disponible_Consulta";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Almacen", equipoDisponibleFilterDTO.Almacen);
                cmd.Parameters.AddWithValue("@Suplidor", equipoDisponibleFilterDTO.Suplidor);
                cmd.Parameters.AddWithValue("@Imei", equipoDisponibleFilterDTO.Imei);
                cmd.Parameters.AddWithValue("@Modelo", equipoDisponibleFilterDTO.Modelo);
                cmd.Parameters.AddWithValue("@PageIndex", equipoDisponibleFilterDTO.PageIndex);
                cmd.Parameters.AddWithValue("@PageSize", equipoDisponibleFilterDTO.PageSize);
                cmd.Parameters.AddWithValue("@Fecha_Inicio", equipoDisponibleFilterDTO.FechaInicio);
                cmd.Parameters.AddWithValue("@Fecha_Final", equipoDisponibleFilterDTO.FechaFinal);

                //cmd.Parameters.AddWithValue("@criterio", criterio);
                cmd.ExecuteNonQuery();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }  
        public DataTable GetEquiposTranferidos()
        {
            try
            {
                cn.Open();

                SqlCommand cmd = cn.CreateCommand();
                SqlDataAdapter da = new(cmd);
                DataTable dt = new();

                cmd.CommandText = "Proc_Equipo_Ultimos_Transferidos_Consulta";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.ExecuteNonQuery();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        public DataTable GetEquiposVendidos(EquipoVendidoFilter equipoVendidoFilter)
        {
            try
            {
                cn.Open();

                SqlCommand cmd = cn.CreateCommand();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();


                cmd.CommandText = "Proc_Equipo_Consulta_Vendidos";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Almacen", equipoVendidoFilter.Almacen);
                cmd.Parameters.AddWithValue("@Imei", equipoVendidoFilter.Imei);
                cmd.Parameters.AddWithValue("@Suplidor", equipoVendidoFilter.Suplidor);
                cmd.Parameters.AddWithValue("@Vendedor", equipoVendidoFilter.Vendedor);
                cmd.Parameters.AddWithValue("@Telefono", equipoVendidoFilter.Telefono);
                cmd.Parameters.AddWithValue("@Modelo", equipoVendidoFilter.Modelo);
                cmd.Parameters.AddWithValue("@PageIndex", equipoVendidoFilter.PageIndex);
                cmd.Parameters.AddWithValue("@PageSize", equipoVendidoFilter.PageSize);
                //cmd.Parameters.AddWithValue("@Fecha_Registro", equipoVendidoFilter.FechaRegistro);
                cmd.Parameters.AddWithValue("@Fecha_Inicio", equipoVendidoFilter.FechaInicio);
                cmd.Parameters.AddWithValue("@Fecha_Final", equipoVendidoFilter.FechaFinal);
                cmd.Parameters.AddWithValue("@Fecha_factura_Inicial", equipoVendidoFilter.FechaInicioFactura);
                cmd.Parameters.AddWithValue("@Fecha_factura_Final", equipoVendidoFilter.FechaFinalFactura);

                //cmd.Parameters.AddWithValue("@criterio", criterio);
                cmd.ExecuteNonQuery();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }
        public DataTable Equipo_Busca_Disponible(string Equipo, int Id_Almacen)
        {
            try
            {
                cn.Open();

                SqlCommand cmd = cn.CreateCommand();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();


                cmd.CommandText = "Equipo_Busca_Disponible";
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Equipo", Equipo);
                cmd.Parameters.AddWithValue("@ID_Almacen", Id_Almacen);
                cmd.ExecuteNonQuery();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }


        public SqlDataReader GetEquipoByImei(string imei)
        {
            try
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("Proc_Equipo_Imei_Transfiere_Consulta", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@imei", imei);
                SqlDataReader dr = cmd.ExecuteReader();

                return dr;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public SqlDataReader GetEquipoUltimoIdTransferencia()
        {
            try
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("Proc_Equipo_Transferencia_Trans_Ultimo_Consulta", cn);

                SqlDataReader dr = cmd.ExecuteReader();

                cmd.CommandType = CommandType.StoredProcedure;
                return dr;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public void PostEquipoTransferencia(EquipoTransferencia tranferencia)
        {
            try
            {
                cn.Open();

                SqlCommand cmd = new SqlCommand("Proc_Equipo_Transferencia_Trans_Guarda", cn);

                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Id_Transferencia", tranferencia.Id);
                cmd.Parameters.AddWithValue("@Imei", tranferencia.Imei);
                cmd.Parameters.AddWithValue("@Almacen_Salida", tranferencia.AlmacenSalida);
                cmd.Parameters.AddWithValue("@Almacen_Destino", tranferencia.AlmacenDestino);
                cmd.Parameters.AddWithValue("@Modelo", tranferencia.Modelo);
                cmd.Parameters.AddWithValue("@Cantidad_Equipos", tranferencia.CantidadEquipos);
                cmd.Parameters.AddWithValue("@Usuario", "Xavier");
                //cmd.Parameters.AddWithValue("@Usuario", tranferencia.Usuario);

                cmd.ExecuteNonQuery();
                cn.Close();
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        #endregion
        public DataTable GetComprobantes_Combo()
        {
            cn.Open();
            DataTable dt = new DataTable();
            SqlCommand cmd = cn.CreateCommand();
            SqlDataAdapter da = new SqlDataAdapter(cmd);

            cmd.CommandText = "TIPO_RNC_Cata_Consulta_Combo";
            cmd.CommandType = CommandType.StoredProcedure;

            da.Fill(dt);
            cn.Close();
            return dt;
        }

        public DataTable AsumeITBIS()
        {
            cn.Open();
            DataTable dt = new DataTable();
            SqlCommand cmd = cn.CreateCommand();
            SqlDataAdapter da = new SqlDataAdapter(cmd);

            cmd.CommandText = "Asume_ITBIS_Consulta";
            cmd.CommandType = CommandType.StoredProcedure;

            da.Fill(dt);
            cn.Close();
            return dt;
        }

        public DataTable PostDetalleFactura(List<DetalleFactura> detalleFactura, int numeroFactura)
        {


            DataTable dt = new DataTable();
            using (cn)
            {
                SqlCommand cmd = new SqlCommand("Detalle_Factura_Insert", cn);
                cmd.CommandType = CommandType.StoredProcedure;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                cn.Open();

                foreach (var item in detalleFactura)
                {
                    cmd.Parameters.Clear();

                    cmd.Parameters.AddWithValue("@Numero_Factura", numeroFactura);
                    cmd.Parameters.AddWithValue("@ID_Equipo", item.IdEquipo);
                    cmd.Parameters.AddWithValue("@Descripcion", item.Descripcion);
                    cmd.Parameters.AddWithValue("@ID_Tipo", item.IdTipo);
                    cmd.Parameters.AddWithValue("@ID_Garantia", item.IdGarantia);
                    cmd.Parameters.AddWithValue("@ID_Tipo_Factura", item.IdTipoFactura);
                    cmd.Parameters.AddWithValue("@ID_Vendedor", item.IdVendedor);
                    cmd.Parameters.AddWithValue("@SubTotal", item.SubTotal);
                    cmd.Parameters.AddWithValue("@cantidad", item.Cantidad);
                    cmd.Parameters.AddWithValue("@precio", item.Precio);
                    cmd.Parameters.AddWithValue("@Descuento", item.Descuento);
                    cmd.Parameters.AddWithValue("@Itbis", item.Itbis);
                    cmd.Parameters.AddWithValue("@Total", item.Total);
                    //cmd.Parameters.AddWithValue("@Estado", "A");
                    //cmd.Parameters.AddWithValue("@Usuario", StaticClass.UsuarioLogin);
                    cmd.Parameters.AddWithValue("@Usuario", "Xavier08");
                    cmd.Parameters.AddWithValue("@fecha_registro", item.FechaRegistro);

                    //cmd.ExecuteNonQuery();
                    da.Fill(dt);
                }

                cn.Close();

            }
            return dt;

        }

        public static List<T> BindList<T>(DataTable dt) where T : new()
        {
            var props = typeof(T).GetProperties();
            var l = new List<T>();

            foreach (DataRow dr in dt.Rows)
            {
                var ob = new T();

                foreach (var p in props)
                {
                    foreach (DataColumn dc in dt.Columns)
                    {
                        // Matching the columns with fields
                        if (p.Name == dc.ColumnName)
                        {
                            // Get the value from the datatable cell
                            object value = dr[dc.ColumnName];

                            if (value is DBNull)
                                break;

                            // Set the value into the object
                            p.SetValue(ob, value);
                            break;
                        }
                    }
                }

                l.Add(ob);
            }

            return l;
        }

        public Facturas PostFactura(Facturas Factura)
        {
            try
            {
                using (cn)
                {
                    SqlCommand cmd = new SqlCommand("facturas_Insert", cn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    //if (tran != null)
                    //tran = cn.BeginTransaction();
                    //DataTable dt = new DataTable();
                    //SqlCommand cmd = cn.CreateCommand();
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    //cmd.CommandType = CommandType.StoredProcedure;
                    //cmd.CommandText = "facturas_Insert";

                    cmd.Parameters.AddWithValue("@ID_Cliente", Factura.IdCliente);
                    cmd.Parameters.AddWithValue("@ID_Vendedor", Factura.IdVendedor);
                    cmd.Parameters.AddWithValue("@ID_Tipo_Factura", Factura.IdTipoFactura);
                    cmd.Parameters.AddWithValue("@ID_Tipo_Pagos", Factura.IdTipoPago);
                    cmd.Parameters.AddWithValue("@ID_Almacen", Factura.IdAlmacen);

                    cmd.Parameters.AddWithValue("@Cantidad_Articulos", Factura.CantidadArticulos);
                    cmd.Parameters.AddWithValue("@Itbis", Factura.Itbis);
                    cmd.Parameters.AddWithValue("@NFCTipoNumero", Factura.NcfTipoNumero);
                    cmd.Parameters.AddWithValue("@RNC", Factura.RNC);
                    cmd.Parameters.AddWithValue("@SubTotal", Factura.SubTotal);
                    cmd.Parameters.AddWithValue("@Abono", Factura.Abono);
                    cmd.Parameters.AddWithValue("@Tiempo_Credito", Factura.TiempoCredito);
                    cmd.Parameters.AddWithValue("@Total", Factura.Total);
                    cmd.Parameters.AddWithValue("@Descuento", Factura.Descuento);
                    cmd.Parameters.AddWithValue("@Nota", Factura.Nota);
                    //cmd.Parameters.AddWithValue("@Estado", "A");
                    cmd.Parameters.AddWithValue("@Usuario", "Xavier08");
                    //cmd.Parameters.AddWithValue("@Usuario", StaticClass.UsuarioLogin);
                    cmd.Parameters.AddWithValue("@fecha_registro", Factura.FechaRegistro);
                    //cmd.Parameters.AddWithValue("@Cotizacion_Numero", Cotizacion_Numero);

                    // cmd.Parameters.AddWithValue("@Fecha_registro", Factura.Fecha_registro);
                    cn.Open();

                    SqlParameter numero_Factura = new SqlParameter("@Numero_factura", 0);
                    numero_Factura.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(numero_Factura);
                    cmd.ExecuteNonQuery();
                    Factura.NumeroFactura = Convert.ToInt32(cmd.Parameters["@Numero_factura"].Value.ToString());
                    //da.Fill(dt);
                    cn.Close();
                    PostDetalleFactura(Factura.DetalleFacturaList, Factura.NumeroFactura);

                    return Factura;
                }
            }
            catch (Exception ex)
            {
                //string lineNumber = ex.StackTrace.Substring(ex.StackTrace.Length - 7, 7);
                //DateTime fecha = DateTime.Now;
                //using (System.IO.StreamWriter file =
                //new System.IO.StreamWriter(@"./Errores Leyenda.txt", true))
                //{
                //   file.WriteLine("class: FacturacionDLL" + " li" + lineNumber + " ERROR: " + ex.Message.ToString() + " Fecha: " + fecha + " Sucursal: " + FrmFacturacion.sucu + " Usuario " + StaticClass.UsuarioNombre);
                //}
                throw ex;
            }
        }
        public DataTable Equipo_Consulta_Ultimo_Registro()
        {
            cn.Open();

            try
            {
                DataTable dt = new DataTable();
                SqlCommand cmd = cn.CreateCommand();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                cmd.CommandText = "Equipo_Consulta_Ultimo_Registro";
                cmd.CommandType = CommandType.StoredProcedure;

                da.Fill(dt);
                cn.Close();
                return dt;

            }
            catch (Exception ex)
            {
                throw ex;
            }


        }

        public DataTable GetEquipoPreciosEstatusByImei(string imei)
        {
            cn.Open();

            try
            {
                DataTable dt = new DataTable();
                SqlCommand cmd = cn.CreateCommand();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                cmd.CommandText = "Equipo_Consulta_Imei";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@criterio", imei);
                da.Fill(dt);
                cn.Close();
                return dt;

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public void ModificarEquipo(Equipo_return_Imei equipo)
        {
            cn.Open();

            try
            {
                SqlCommand cmd = cn.CreateCommand();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                cmd.CommandText = "Equipo_Actualiza_Estado_y_precio";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Imei", equipo.Imei);
                cmd.Parameters.AddWithValue("@Id_Condicion", equipo.CondicionId);
                cmd.Parameters.AddWithValue("@Costo_Equipo", equipo.CostoEquipo);
                cmd.Parameters.AddWithValue("@Precio_Por_Mayor", equipo.PrecioPorMayor);
                cmd.Parameters.AddWithValue("@Precio_Detalle", equipo.PrecioDetalle);
                cmd.Parameters.AddWithValue("@Usuario", "EmanuelTejada");
                cmd.Parameters.AddWithValue("@Comision_Detalle", equipo.ComisionDetalle);
                cmd.Parameters.AddWithValue("@Comision_Por_Mayor", equipo.ComisionMayor);
                cmd.Parameters.AddWithValue("@Id_estado_Bloqueo", equipo.EstadoBloqueoId);
                cmd.ExecuteNonQuery();
                cn.Close();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void InsertarEquipoRecibido(EquipoRecepcion equipo)
        {
            cn.Open();

            try
            {
                SqlCommand cmd = cn.CreateCommand();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                cmd.CommandText = "Recepcion_Equipos_Insert";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Nombres", equipo.Nombres);
                cmd.Parameters.AddWithValue("@Apellidos", equipo.Apellidos);
                cmd.Parameters.AddWithValue("@Cedula", equipo.Cedula);
                cmd.Parameters.AddWithValue("@Telefono", equipo.Telefono);
                cmd.Parameters.AddWithValue("@Imei_Entrada", equipo.ImeiEntrada);
                cmd.Parameters.AddWithValue("@Precio_Imei_Entra", equipo.PrecioImeiEntra);
                cmd.Parameters.AddWithValue("@ID_Modelo", equipo.IDModelo);
                cmd.Parameters.AddWithValue("@ID_Condicion", equipo.IDCondicion);
                cmd.Parameters.AddWithValue("@Imei_Sale", equipo.ImeiSale);
                cmd.Parameters.AddWithValue("@Nota", equipo.Nota);
                cmd.Parameters.AddWithValue("@Usuario", "EmanuelTejada");
                cmd.ExecuteNonQuery();
                cn.Close();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void EquipoEstadoActualiza(EquipoEstadoUpdate dataActualizar)
        {
            cn.Open();

            try
            {
                SqlCommand cmd = cn.CreateCommand();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                cmd.CommandText = "Equipos_Actualiza_Disponible";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Imei", dataActualizar.Imei);
                cmd.Parameters.AddWithValue("@Disponible", dataActualizar.Disponible);
                cmd.Parameters.AddWithValue("@Nota_Adicional", dataActualizar.NotaAdicional);
                cmd.Parameters.AddWithValue("@Usuario",dataActualizar.Usuario) ;
                cmd.ExecuteNonQuery();
                cn.Close();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public DataTable Marca_Insert(Marcas Marca)
        {
            cn.Open();
            try
            {
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = "Marcas_Insert";
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Descripcion", Marca.descripcion);
                cmd.Parameters.AddWithValue("@Usuario", Marca.Usuario);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public DataTable Modelo_Insert(Modelos Modelo)
        {
            cn.Open();
            try
            {
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = "Modelos_Insert";
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ID_Marca", Modelo.ID_Marca);
                cmd.Parameters.AddWithValue("@Descripcion", Modelo.Descripcion);
                cmd.Parameters.AddWithValue("@Estado", Modelo.Estado);
                cmd.Parameters.AddWithValue("@Usuario", Modelo.Usuario);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public List<FacturaReporte> ObtenerFacturaReporte(int codigoFactura, int sucursal)
        {
            using var cn = new SqlConnection(_connectionStrings);
            using var cmd = new SqlCommand("SP_Reporte_Factura", cn);
            cmd.CommandType = CommandType.StoredProcedure;

            SqlDataAdapter da = new SqlDataAdapter(cmd);
            //cmd.CommandType = CommandType.StoredProcedure;
            //cmd.CommandText = "facturas_Insert";

            cmd.Parameters.AddWithValue("@Cod_Factura", codigoFactura);
            cmd.Parameters.AddWithValue("@sucursal", sucursal);
            cn.Open();

            var dt = new DataTable();
            da.Fill(dt);
            cn.Close();

            return BindList<FacturaReporte>(dt);
        }
    }
}
