using System.ComponentModel.DataAnnotations;

namespace MVSystemApi.Model
{

    public class Equipos
    {
        public string Imei { get; set; }
        public int ID_Modelo { get; set; }
        public int ID_Suplidor { get; set; }
        public int ID_Tecnologia { get; set; }
        public int ID_Garantia { get; set; }
        public int ID_Condicion { get; set; }
        public int ID_Estado_Bloqueo { get; set; }
        public int ID_Almacen { get; set; }
        public string Numero_de_Factura { get; set; }
        public int Cantidad { get; set; }
        public int Can_Max { get; set; }
        public int Can_Min { get; set; }
        public decimal Precio_Por_Mayor { get; set; }
        public decimal Precio_Detalle { get; set; }
        public decimal Costo_Equipo { get; set; }
        public decimal Comision_Detalle { get; set; }
        public decimal Comision_Por_Mayor { get; set; }
        public string Desbloqueado { get; set; }
        public string Disponible { get; set; }
        public string Nota_Adicional { get; set; }
        
        public int? Numero_Registro { get; set; }
        public string Usuario { get; set; }

    }
    public class EquipoImei
    {
        public int Id { get; set; }
        public string Imei { get; set; }
        public string Modelo { get; set; }
        public string Mensaje { get; set; }
        public string AlmacenSalida { get; set; }
        public int AlmacenSalidaId { get; set; }
            
    }  
    public class EquipoTransferencia
    {
        public int Id { get; set; }
        public string Imei { get; set; }
        public string? Modelo { get; set; }

        public string AlmacenSalida { get; set; }
        public string AlmacenDestino { get; set; }
        public int CantidadEquipos { get; set; }
        public string Usuario { get; set; }
            
    }   

    public class Equipo_return
    {
        public string Imei { get; internal set; }
        public string Modelo { get; internal set; }
        public string Marca { get; internal set; }
        public decimal precioPorMayor { get; set; }
        public decimal PrecioDetalle { get; set; }
        public string Disponible { get; set; }
        public string Disponible_Detalle { get; set; }
        public string Mensaje { get;  set; }
    }


    public class Equipo_return_Imei
    {
        public string Imei { get; set; }
        public int CondicionId { get; set; }
        public string CondicionDescripcion { get; set; }
        public decimal CostoEquipo { get; set; }
        public decimal ComisionDetalle { get; set; }
        public decimal ComisionMayor { get; set; }
        public decimal PrecioPorMayor { get; set; }
        public decimal PrecioDetalle { get; set; }
        public int EstadoBloqueoId { get; set; }
        public string EstadoBloqueoDescripcion { get; set; }
    }

    public class EquipoRecepcion
    {
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string Cedula { get; set; }
        public string Telefono { get; set; }
        public string ImeiEntrada { get; set; }
        public decimal PrecioImeiEntra { get; set; }
        public int IDModelo { get; set; }
        public int IDCondicion { get; set; }
        public string ImeiSale { get; set; }
        public string Nota { get; set; }
    }

    public class EquipoRecepcionGet : PagingDTO
    {
        public int RecepcionNumero { get; set; }
        public string Cliente { get; set; }
        public string Cedula { get; set; }
        public string Telefono { get; set; }
        public string ImeiEntrada { get; set; }
        public decimal PrecioImeiEntra { get; set; }
        public string Modelo { get; set; }
        public string ImeiSale { get; set; }
        public string Nota { get; set; }
        public string Usuario { get; set; }
        public string FechaRegistro { get; set; }
        
    }
    
    public class CriterioFilters: RangeDatePaging
    {
        public string criterio { get; set; } = "";
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class ReporteData
    {
        public List<EquipoRecepcionGet> Registros { get; set; }
        public string FechaImpresion { get; set; }
    }

    public class EquipoEstadoUpdate
    {
        public string Imei { get; set; }
        public string Disponible { get; set; }
        public string NotaAdicional { get; set; }
        public string Usuario { get; set; }
    }
    public class Numero_registro
    {
        public int Numero_Registro { get; internal set; }

    }
    public class EquipoTranferenciaReporte
    {
        public int IdTransferencia { get; set; }
        public List<EquipoTransferencia> EquipoTransferencias { get; set; }
        public string FechaTransferencia { get; set; }

        public string Usuario { get; set; }
        public int Total { get; set; }
    }

    public class EquipoVendidoReporte
    {
        public List<EquipoVendido> EquipoVendidos { get; set; }
        public string FechaImpresion { get; set; }
        public EquipoVendidoFilter EquipoFilter { get; set; }
    }

    public class EquipoVendido : PagingDTO
    { 

        public string Imei { get; set; }
        public string Descripcion { get; set; }
        public decimal PrecioPorMayor { get; set; }
        public decimal PrecioVendido { get; set; }
        public decimal PrecioDetalle { get; set; }
        public decimal CostoEquipo { get; set; }
        public decimal Itbis { get; set; }
        public decimal ComisionDetalle { get; set; }
        public decimal ComisionPorMayor { get; set; }
        public string TipoFactura { get; set; }
        public string Factura { get; set; }
        public string IsDesbloqueado { get; set; }
        public string NotaAdicional { get; set; }
        public string DescripcionAlmacen { get; set; }
        public string Suplidor { get; set; }
        public string Cliente { get; set; }
        public string Vendedor { get; set; }
        //public int IdSuplidor { get; set; }
        //public int IdAlmacen { get; set; }
        public string FechaRegistro { get; set; }
        public string FechaFacturado { get; set; }
        public decimal TotalInventario { get; set; }
        public decimal TotalFacturado{ get; set; }
        public decimal TotalGanancia { get; set; }
        public decimal TotalComisionDetalle { get; set; }
        public decimal TotalComisionXMayor { get; set; }
        public decimal TotalItbis { get; set; }


    }
    public class EquipoVendidoFilter
    {
        [Required]
        public int PageIndex { get; set; }
        [Required]
        public int PageSize { get; set; }
        public int? Almacen { get; set; }
        public Int64? Telefono { get; set; }
        public string Suplidor { get; set; }
        public string Vendedor { get; set; }
        public string Imei { get; set; }
        public string FechaRegistro { get; set; }
        public string Modelo { get; set; }
        public string FechaInicio { get; set; }
        public string FechaFinal { get; set; }
        public string FechaInicioFactura { get; set; }
        public string FechaFinalFactura { get; set; }
    }
}
