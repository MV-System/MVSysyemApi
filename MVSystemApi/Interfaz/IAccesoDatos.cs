using DTO;
using MVSystemApi.Model;
using MVSystemApi.Model.Seguridad;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace MVSystemApi.Interfaz
{
    public interface IAccesoDatos
    {
        // Propiedad para acceder a la cadena de conexión
        string ConnectionString { get; }

        List<ReparacionReporteDTO> ObtenerReparaciones(ReparacionReporteFiltro filtro);
       DataTable GetReparacionesLista(ReparacionConsultaFiltro filtro);

        // --------------- Utilidades ------------------------//
        DataTable Vendedor_Consulta_Combo();

        #region  // ------------- equipos ---------------------//
        DataTable Equipo_Busca_Disponible(string Equipo, int Id_Almacen);
        public DataTable Reparaciones_Insert(ReparacionesInsert Reparaciones);
        DataTable Equipo_Insert(Equipos Equipo);
        DataTable Get_Tegnologia_Combo();
        DataTable Get_Garantia_Equipo_Combo();
        DataTable Get_Desbloqueado_Equipo_Combo();
        DataTable Get_Condicion_Equipo_Combo();
        DataTable Get_Almacen_Combo();
        DataTable Get_Sucursal_Combo();
        DataTable Get_StatusTaller_Combo();
        DataTable Get_Marcas_Combo();
        DataTable Get_Modelos_Combo(int? ID_Marca);
        DataTable Get_Suplidor_Combo();
        public DataTable GetEquiposDisponible(EquipoDisponibleFilterDTO equipoDisponibleFilterDTO);
        public DataTable GetEquiposVendidos(EquipoVendidoFilter equipoVendidoFilter);
        public SqlDataReader GetEquipoByImei(string imei);
        public DataTable GetEquiposTranferidos(int? idTransferencia);
        public DataTable GetEquiposRecibidos(CriterioFilters filterData);
        public void ModificarEquipo(Equipo_return_Imei imei);
        public void InsertarEquipoRecibido(EquipoRecepcion equipo);
        public void EquipoEstadoActualiza(EquipoEstadoUpdate equipo);

        internal DataTable GetEquipoPreciosEstatusByImei(string imei);

        public SqlDataReader GetEquipoUltimoIdTransferencia();
        public DataTable GetTransferenciaEquipos(int? Sucursal_Salida, int? Sucursal_Destino, string FechaInicio, string FechaFinal);

        public DataTable GetDetalleTransferenciaEquipos(int IdTransferencia);

        public DataTable PostEquipoTransferencia(EquipoTransferencia tranferencia);

        public ReparacionReporteDTO ObtenerReparacionReporte(int idReparacion);

        public DataTable GetEquiposRegistrados(int numeroRegistro);

        #endregion // ------------- fin de combos para el registro de equipo ----------------------

        #region // ------------- Clientes --------------------------
        DataTable GetTipo_Telefono_Lista();
        DataTable Tipo_Cliente_Cata_Consulta_Combo();
        DataTable Cliente_Insert(Clientes Cliente);
        DataTable Cliente_Update(Clientes Cliente);
        DataTable Cliente_Consulta(string criterio);
        DataTable Cliente_FacturasPendientes(string FechaInicio, string FechaFin, string Criterio);
        DataTable Cliente_FacturasPendientesDetalle(int cliente);
        DataTable ObtenerHistorialAbonos(int idCliente, string fechaInicio = null, string fechaFinal = null);
        
        #endregion

        #region // ------------- Vendedores ------------------------- 
        DataTable Vendedor_Insert(Vendedor Vendedor);
        #endregion

        #region // ------------- Suplidor ---------------------------
        DataTable Suplidor_Insert(Suplidor Suplidor);

        DataTable Tipo_Suplidor_Cata_Consulta_Combo();
        #endregion

        #region // ------------ Facturacion -------------------------
        DataTable GetUtilidades(int? sucursal, DateTime FI, DateTime FF); // trae la lista de vendedores
        DataTable Accesorios_Consulta_Facturacion(int? Id_accesorio, int? id_sucu);
        DataTable GetCanalesLista(int Id_Canal);
        DataTable Tipos_Pagos_Cata_Consulta_Combo();
        DataTable GetTipos_Factura_Lista();
        DataTable GetComprobantes_Combo(); // trae la lista de comprobantes
                                           //DataTable Post_Factura(Facturas Factura, int Cotizacion_Numero, SqlTransaction tran = null); // trae la lista de comprobantes

        string EditaDetalleFactura(EditaDetalleFactura EditaDetalleFactura);
        string RebajaEquipo(RebajaEquipoModel RebajaEquipo);

        public Facturas PostFactura(Facturas Factura);
        public Facturas PostFacturaConPagos(Facturas factura, List<DetallePagoDTO> detallesPagos);
        public FilterAnulaFactura PostAnulaFactura(FilterAnulaFactura Factura);
        //public EditaDetalleFactura EditaDetalleFactura(EditaDetalleFactura EditaDetalleFactura);
        public List<FacturaReporte> ObtenerFacturaReporte(int codigoFactura, int sucursal);
        public DataTable GetFacturas(FacturaFilter consulta);
        public DataTable GetFacturasReporte(FacturaFilter consulta);
        public DataTable GetFacturaNumeroUltimo();
        public DataTable GetCierreCajaReport(FacturaFilter filtro);
        
        public DataTable GetFacturasMixtoEnPeriodo(FacturaFilter filtro);

        public DataTable GetDetalleFacturaConsulta(int numeroFactura, int? sucursal);
        
        // Métodos para Pago Mixto
        public void PostDetallesPagoFactura(int numeroFactura, List<DetallePagoDTO> detallesPagos, string tipoDocumento);
        public List<DetallePagoDTO> GetDetallesPagoFactura(int numeroFactura, string tipoDocumento);

        // Métodos para Facturación Electrónica (LogFacturaElectronica)
        public Task<bool> InsertLogFacturaElectronica(LogFacturaElectronica log);
        public Task<bool> UpdateLogFacturaElectronica(LogFacturaElectronica log);
        public Task<LogFacturaElectronica> GetLogFacturaElectronica(int numeroFactura, string literal);
        public Task<List<LogFacturaElectronica>> GetLogFacturasElectronicasPendientes();
        public Task<(bool success, string errorMessage)> InsertFacturaProveedor2Dual(Facturas factura, List<DetalleFactura> detalles, string usuario, string rncEmisor, NotaCreditoInfo notaCreditoInfo = null);
        public Task<Facturas> ObtenerFacturaParaNotaCredito(int numeroFactura, int sucursal);
        public Task<string> ObtenerNcfNotaCredito(int tipoNcf);
        public Task<string> ObtenerEstatusRemotoProveedor2(string encf, string rncEmisor);
        public Task<NotaCreditoReporteViewModel> ObtenerNotaCreditoReporte(int numeroFacturaOrigen, int sucursal);

        #endregion

        #region // ------------ Cotizacion -------------------------
        public DataTable GetCotizaciones(CotizacionConsultaFiltro filtro);
        public DataTable GetCotizacionPorCodigo(int codigo);
        public DataTable GetDetalleCotizacionPorCodigo(int codigo);
        public DataTable GetCotizacionFacturaConsulta(string telefono);
        public DataTable GetDetalleCotizacionConsulta(int numeroFactura, int sucursal);
        public int GuardarCotizacion(CotizacionModel model);

        #endregion

        #region --------------Accesorios -------------------
        DataTable Accesorio_Insert(Accesorio Accesorio);
        DataTable ConsultaAccesorio(int Codigo);
        DataTable Equipo_Consulta_Ultimo_Registro();
        public DataTable GetAccesorios(Paginate paginate,string accesorio, int almacen);

        public DataTable GetAccesorioById(int id);
        public DataTable GetAccesoriosVendidos(Paginate paginate, string accesorio, int almacen);
        public DataTable UpdateAccesorio(AccesorioDTO accesorio);


        #endregion

        #region // ------------- Marcas --------------------------
        DataTable Marca_Insert(Marcas Marca);
        DataTable Modelo_Insert(Modelos Modelo);
        DataTable Grupo_Insert(Grupos Grupo);
        DataTable GetGrupos_Combo();
        DataTable Cliente_Consulta_Por_Id_Cliente(int id_Cliente);
        DataTable AsumeITBIS();
        int UpdateAsumeITBIS(string asumeITBIS, string usuario);
        DataSet ValidarEliminacionEquipo(string imei);
        ConsultaSqlResultado EjecutarConsultaSql(string consulta, int maxFilas);
        string Equipo_Delete(string imei, string Usuario);
        string EliminarRegistrosEquipoDeTabla(string imei, string tabla, string usuario);
        string Actualizar_CostoPrecio_Reparacion(ReparacionPendienteUpdateDTO data);
        string AbonoAfactura(AbonoFacturaParam param, out Guid idLoteAbono);
        string AbonoGeneral(AbonoFacturaParam param, out Guid idLoteAbono);
        ReciboAbonoDTO GetReciboAbono(int idCliente, int numeroFactura, int idSucursal);
        ReciboAbonoGeneralDTO GetReciboAbonoGeneral(Guid idLoteAbono);
        #endregion

        #region // ------------- Métodos Asíncronos para ADO.NET --------------------------
        /// <summary>
        /// Ejecuta un query y retorna un valor escalar de forma asíncrona
        /// </summary>
        Task<T> ExecuteScalarAsync<T>(string query, params SqlParameter[] parameters);

        /// <summary>
        /// Ejecuta un query y retorna un DataTable de forma asíncrona
        /// </summary>
        Task<DataTable> ExecuteDataTableAsync(string query, params SqlParameter[] parameters);

        /// <summary>
        /// Ejecuta un comando que no retorna resultados (INSERT, UPDATE, DELETE) de forma asíncrona
        /// </summary>
        Task<int> ExecuteNonQueryAsync(string query, params SqlParameter[] parameters);
        #endregion

        #region --------------Tipos de Pagos -------------------
        /// <summary>
        /// Obtiene un tipo de pago por su ID, incluyendo el código DGI
        /// </summary>
        Tipos_Pagos Tipo_Pagos_GetById(int idTipoPago);
        #endregion
    }
}
