﻿using DTO;
using MVSystemApi.Model;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace MVSystemApi.Interfaz
{
    public interface IAccesoDatos
    {
        #region  // ------------- equipos ---------------------//
        DataTable Equipo_Busca_Disponible(string Equipo, int Id_Almacen);

        DataTable Equipo_Insert(Equipos Equipo);
        DataTable Get_Tegnologia_Combo();
        DataTable Get_Garantia_Equipo_Combo();
        DataTable Get_Desbloqueado_Equipo_Combo();
        DataTable Get_Condicion_Equipo_Combo();
        DataTable Get_Almacen_Combo();
        DataTable Get_Sucursal_Combo();

        DataTable Get_Marcas_Combo();
        DataTable Get_Modelos_Combo(int? ID_Marca);
        DataTable Get_Suplidor_Combo();
        public DataTable GetEquiposDisponible(EquipoDisponibleFilterDTO equipoDisponibleFilterDTO);
        public DataTable GetEquiposVendidos(EquipoVendidoFilter equipoVendidoFilter);
        public SqlDataReader GetEquipoByImei(string imei);
        public DataTable GetEquiposTranferidos();
        public DataTable GetEquiposRecibidos(CriterioFilters filterData);
        public void ModificarEquipo(Equipo_return_Imei imei);
        public void InsertarEquipoRecibido(EquipoRecepcion equipo);
        public void EquipoEstadoActualiza(EquipoEstadoUpdate equipo);

        internal DataTable GetEquipoPreciosEstatusByImei(string imei);

        public SqlDataReader GetEquipoUltimoIdTransferencia();

        public void PostEquipoTransferencia(EquipoTransferencia tranferencia);



        #endregion // ------------- fin de combos para el registro de equipo ----------------------

        #region  // ------------- Inventarios ---------------------//
        public  DataTable ObtenerEquiposInventarioPorAlmacenId(int almacenId);
        public void IniciarInventario(int almacenId);
        public void CerrarInventario(int inventarioId);
        public void EscanearArticulo(ArticuloInventarioUpdate articuloData);
        //DataTable ObtenerEquiposPorInventarioId(int? inventarioId);
        //DataTable ObtenerInventariosPorAlmacenId(int? almacenPorId);

        #endregion // ------------- Fin region funcionalidad Inventario ----------------------

        #region // ------------- Clientes --------------------------
        DataTable GetTipo_Telefono_Lista();
        DataTable Tipo_Cliente_Cata_Consulta_Combo();
        DataTable Cliente_Insert(Clientes Cliente);
        DataTable Cliente_Consulta(string criterio);
        
        #endregion

        #region // ------------- Vendedores ------------------------- 
        DataTable Vendedor_Insert(Vendedor Vendedor);
        #endregion

        #region // ------------- Suplidor ---------------------------
        DataTable Suplidor_Insert(Suplidor Suplidor);

        DataTable Tipo_Suplidor_Cata_Consulta_Combo();
        #endregion

        #region // ------------ Facturacion -------------------------
        DataTable Vendedor_Consulta_Combo(); // trae la lista de vendedores
        DataTable Accesorios_Consulta_Facturacion(int? Id_accesorio, int? id_sucu);
        DataTable Tipos_Pagos_Cata_Consulta_Combo();
        DataTable GetTipos_Factura_Lista();
        DataTable GetComprobantes_Combo(); // trae la lista de comprobantes
        //DataTable Post_Factura(Facturas Factura, int Cotizacion_Numero, SqlTransaction tran = null); // trae la lista de comprobantes

        public Facturas PostFactura(Facturas Factura);
        public List<FacturaReporte> ObtenerFacturaReporte(int codigoFactura, int sucursal);


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
        DataTable Cliente_Consulta_Por_Id_Cliente(int id_Cliente);
        DataTable AsumeITBIS();

        #endregion


    }
}
