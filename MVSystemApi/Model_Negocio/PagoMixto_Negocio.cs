using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MVSystemApi.Model;

namespace MVSystemApi.Model_Negocio
{
    public class PagoMixto_Negocio
    {
        /// <summary>
        /// Valida que un pago mixto sea correcto
        /// </summary>
        public bool ValidarPagoMixto(decimal totalFactura, List<DTO.DetallePagoDTO> pagos)
        {
            if (pagos == null || pagos.Count == 0)
                return false;

            // Si solo hay un pago, no es mixto
            if (pagos.Count < 2)
                return false;

            // La suma debe ser igual al total
            decimal sumaPagos = pagos.Sum(p => p.Monto_Pago);
            decimal diferencia = Math.Abs(sumaPagos - totalFactura);

            // Permitir margen de error de 0.01
            return diferencia < 0.01m;
        }

        /// <summary>
        /// Valida que no haya montos negativos
        /// </summary>
        public bool ValidarMontosPositivos(List<DTO.DetallePagoDTO> pagos)
        {
            if (pagos == null || pagos.Count == 0)
                return false;

            return pagos.All(p => p.Monto_Pago > 0);
        }

        /// <summary>
        /// Valida que no se repita el mismo tipo de pago
        /// </summary>
        public bool ValidarSinTiposDuplicados(List<DTO.DetallePagoDTO> pagos)
        {
            if (pagos == null || pagos.Count == 0)
                return false;

            return pagos.Select(p => p.ID_Tipo_Pago).Distinct().Count() == pagos.Count;
        }

        /// <summary>
        /// Valida que todos los tipos de pago existan (excluyendo MIXTO)
        /// </summary>
        public bool ValidarTiposPago(List<DTO.DetallePagoDTO> pagos, List<Tipos_Pagos> tiposDisponibles)
        {
            if (pagos == null || tiposDisponibles == null)
                return false;

            return pagos.All(p => tiposDisponibles.Any(t => t.ID_Tipo_Pagos == p.ID_Tipo_Pago));
        }
    }
}
