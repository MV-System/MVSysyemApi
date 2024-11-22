using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MVSystemApi.Model
{
    public class Clientes
    {
        public int IDContacto { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public int ID_Tipo_Cliente { get; set; }
        public string Tipo_Cliente_Descripcion { get; set; }
        public string Limite_Credito { get; set; }
        public Int64 Numero_Telefono { get; set; }
        public int ID_Tipo_Telefono { get; set; }
        public int? Extencion { get; set; }
        public string Email_Detalle { get; set; }
        public string Direccion_Detalle { get; set; }
        public string criterio { get; set; }
        public string Usuario { get; set; }
        public string Mensaje { get; internal set; }
        public int ID_Cliente { get; set; }
    }
    public class Clientes_consulta
    {
        public int Id_Cliente { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public Int64 Numero_Telefono { get; set; }
        public string Mensaje { get; internal set; }
    }
    public class Clientes_ConFacturasPendientes_Filtros
    {
        public string FechaInicio { get; set; }
        public string FechaFin { get; set; }
        public string NombreCliente { get; set; }
        public string NumeroTelefono { get; set; }
    }
    public class ObtenerClientesConFacturasPendientes
    {
        public int Id_Cliente { get; set; }
        public string Cliente { get; set; }
        public decimal MontoPendiente { get; set; }
    }
    public class ObtenerFacturasPendientesPorCliente
    {
        public int NumeroFactura { get; set; }
        public decimal TotalFactura { get; set; }
        public decimal MontoPendiente { get; set; }
        public string FechaFactura { get; set; }
    }
    public class AbonoFacturaParam
    {
        public int Id_Cliente { get; set; }
        public int NumeroFactura { get; set; }
        public decimal MontoAbono { get; set; }
        public int IdSucursal { get; set; }
        public string Usuario { get; set; }
    }
    public class AbonoFacturaParamRetorn
    {
        public string Mensaje { get; internal set; }
    }
}
