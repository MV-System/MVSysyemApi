using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MVSystemApi.Handlers
{
    /// <summary>
    /// Exige que el usuario tenga AL MENOS UNO de los permisos indicados.
    /// Lee los permisos del token (claims "role"), los mismos que usa el front para mostrar pantallas y botones.
    /// No usa [Authorize(Roles = ...)] porque ese camino pasa por RolesAuthorizationHandler / RoleService,
    /// que consulta otro modelo de datos y no las tablas seg.* actuales.
    /// Sin el permiso responde 403 { error = "No tiene permiso para esta acción." }.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class RequierePermisoAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _permisos;

        public RequierePermisoAttribute(params string[] permisos)
        {
            _permisos = permisos ?? Array.Empty<string>();
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var usuario = context.HttpContext.User;
            if (usuario?.Identity?.IsAuthenticated != true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var tiene = usuario.Claims.Any(c =>
                (c.Type == "role" || c.Type == ClaimTypes.Role) && _permisos.Contains(c.Value));

            if (!tiene)
            {
                context.Result = new ObjectResult(new { error = "No tiene permiso para esta acción." })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
        }
    }

    /// <summary>Nombres de permisos (seg.Permisos.Nombre) usados por el API. Deben coincidir con los del front.</summary>
    public static class PermisosApi
    {
        // Seguridad (menú del front)
        public const string VerUsuarios = "VerUsuarios";
        public const string VerRoles = "VerRoles";
        public const string VerPermisos = "VerPermisos";
        public const string VerDepartamentos = "VerDepartamentos";
        public const string VerModulos = "VerModulos";
        public const string VerSistemas = "VerSistemas";
        public const string AsignarPermisos = "AsignarPermisos";
        public const string ModoDesarrollador = "ModoDesarrollador";
        public const string UsoSistemaDesconectar = "USO_SISTEMA_DESCONECTAR";

        // Laptops (la pantalla de custodia lista los usuarios para elegir el técnico)
        public const string LaptopCambioPieza = "LAPTOP_CAMBIO_PIEZA";
        public const string LaptopCustodiaConsulta = "LAPTOP_CUSTODIA_CONSULTA";
        public const string LaptopCustodiaRecibir = "LAPTOP_CUSTODIA_RECIBIR";

        // Consulta de facturas (botones)
        public const string FacturaAnular = "VNT_DETALLE_FACTURA_BTN_ANULAR";
        public const string FacturaNotaCredito = "VNT_DETALLE_FACTURA_BTN_NOTA_CREDITO";
        public const string FacturaCambiaImei = "VNT_DETALLE_FACTURA_BTN_CAMBIA_IMEI";
        public const string FacturaRebaja = "VNT_DETALLE_FACTURA_BTN_REBAJA";

        // Utilidades (tambien habilita el dashboard completo)
        public const string VerUtilidades = "verUtilidadesToolStripMenuItem";

        // Equipos
        public const string MantEquipos = "MNU_MANT_EQUIPOS";
        public const string ConsEquipos = "MNU_CONS_EQUIPOS";
        public const string EquiposVendidos = "equiposVendidosMenu";
        public const string CambiarEstadoEquipo = "cambiarEstadoDeEquipoToolStripMenuItem";
        public const string TransfiereEquipos = "MNTransfiereEquipos";
        public const string RecepcionEquiposMenu = "MNU_RECEPCION_EQUIPOS";
        public const string RecepcionEquipos = "recepcionequipos";
        public const string ConsRecepcionEquipos = "MNU_CONS_RECEPCION_EQUIPOS";
        public const string MantReparacion = "MNU_MANT_EET_REPARACION";

        // Inventario
        public const string MantPasarInventario = "MNU_MANT_PASARINVEQ";
        public const string ConsultaInventario = "consultaInventarioToolStripMenuItem";

        // Facturacion, caja y cotizacion
        public const string MantFacturacion = "MNU_MANT_FACTURACION";
        public const string ConsFacturas = "MNU_CONS_FACTURAS";
        public const string CajaPreFacturas = "CajaPreFacturas";
        public const string CotizacionMenu = "CotizacionMenu";
        public const string ConsCotizaciones = "MNU_CONS_COTIZACIONES";

        // Clientes y catalogos de mantenimiento
        public const string MantClientes = "MNU_MANT_CLIENTES";
        public const string ConsClientes = "MNU_CONS_CLIENTES";
        public const string ClientesCuentasPendientes = "clienteConCuentesPendientesToolStripMenuItem";
        public const string MantVendedores = "MNU_MANT_VENDEDORES";
        public const string MantSuplidores = "MNU_MANT_SUPLIDORES";
        public const string MantMarcas = "MNU_MANT_MARCAS";
        public const string GestionarEquipo = "GESTIONEQUIPO";
    }
}
