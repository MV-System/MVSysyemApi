using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MVSystemApi.Model;

namespace MVSystemApi.Model_Negocio
{
    public class PreFacturas_Negocio
    {
        private readonly AccesoDatos _accesoDatos;
        public PreFacturas_Negocio(AccesoDatos accesoDatos)
        {
            _accesoDatos = accesoDatos;
        }

        public List<PreFactura> ListarPreFacturas(PreFacturaFiltro filtro)
        {
            var dt = _accesoDatos.GetPreFacturasConsulta(filtro);
            return AccesoDatos.BindList<PreFactura>(dt);
        }

        public List<PreDetalleFactura> ObtenerDetallePreFactura(int numeroPreFactura)
        {
            var dt = _accesoDatos.GetPreDetalleFacturaConsulta(numeroPreFactura);
            return AccesoDatos.BindList<PreDetalleFactura>(dt);
        }

        public PreFactura ObtenerPreFacturaPorId(int numeroPreFactura)
        {
            var filtro = new PreFacturaFiltro { Estado = "P" };
            var dt = _accesoDatos.GetPreFacturasConsulta(filtro);
            var lista = AccesoDatos.BindList<PreFactura>(dt);
            return lista.FirstOrDefault(p => p.Numero_PreFactura == numeroPreFactura);
        }

        public PreDetalleFactura AgregarDetallePreFactura(PreDetalleFactura detalle)
        {
            var dt = _accesoDatos.InsertarPreDetalleFactura(detalle);
            var lista = AccesoDatos.BindList<PreDetalleFactura>(dt);
            return lista.Count > 0 ? lista[0] : null;
        }

        public object ProcesarPreFactura(int numeroPreFactura, string usuarioProcesamiento)
        {
            var resultado = _accesoDatos.ProcesarPreFactura(numeroPreFactura, usuarioProcesamiento);
            
            // Si el procesamiento fue exitoso, actualizar número de factura en detalles de pago
            if (resultado != null)
            {
                try
                {
                    var codFacturaProperty = resultado.GetType().GetProperty("Cod_Factura");
                    if (codFacturaProperty != null)
                    {
                        int nuevoNumeroFactura = Convert.ToInt32(codFacturaProperty.GetValue(resultado));
                        if (nuevoNumeroFactura > 0)
                        {
                            _accesoDatos.ActualizarNumeroFacturaEnDetallesPago(numeroPreFactura, nuevoNumeroFactura);
                        }
                    }
                }
                catch (Exception)
                {
                    // No lanzar excepción. La pre-factura ya se procesó correctamente.
                }
            }
            
            return resultado;
        }

        public object AnularPreFactura(int numeroPreFactura)
        {
            return _accesoDatos.AnularPreFactura(numeroPreFactura);
        }

        public PreFactura CrearPreFactura(PreFactura prefactura)
        {
            var dt = _accesoDatos.InsertarPreFactura(prefactura);
            var lista = AccesoDatos.BindList<PreFactura>(dt);
            return lista.Count > 0 ? lista[0] : null;
        }

        public object RebajarItemPreFactura(int numeroPreFactura, int idEquipo)
        {
            return _accesoDatos.RebajarItemPreFactura(numeroPreFactura, idEquipo);
        }
    }
}
