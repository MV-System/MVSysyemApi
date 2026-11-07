namespace MVSystemApi.Model
{
    /// <summary>
    /// Regla única del descuento en los comprobantes electrónicos (proveedor 1 EasyConexión y proveedor 2 MicroStar).
    ///
    /// Los montos que guarda el sistema ya traen el descuento aplicado (SubTotal / Total son netos; el descuento se conserva
    /// aparte en la factura). Al proveedor se le envía el monto YA descontado y el descuento NO se envía: mandarlo además
    /// hacía que la DGII lo restara dos veces (proveedor 1: observaciones [2394] y [1934]; proveedor 2: [11014] y [11105],
    /// aceptado condicional). Con esta regla la DGII acepta limpio ([0]) en los dos proveedores, con ITBIS asumido o sumado.
    /// Sin descuento no cambia nada.
    /// </summary>
    public static class DescuentoComprobante
    {
        /// <summary>Descuento que se envía al proveedor: siempre 0 (ya va aplicado en el monto).</summary>
        public const decimal DescuentoEnviado = 0m;

        /// <summary>
        /// Monto y precio unitario de una línea con el descuento ya aplicado. Sin descuento devuelve el monto y el precio originales.
        /// </summary>
        public static (decimal Monto, decimal PrecioUnitario) AplicarEnLinea(decimal montoBruto, decimal precioOriginal, decimal cantidad, decimal descuento)
        {
            if (descuento == 0)
                return (montoBruto, precioOriginal);

            var neto = montoBruto - descuento;
            return (neto, cantidad > 0 ? System.Math.Round(neto / cantidad, 6) : precioOriginal);
        }
    }
}
