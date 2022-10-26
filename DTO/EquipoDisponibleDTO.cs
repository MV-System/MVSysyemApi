using MVSystemApi.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DTO
{
    public class EquipoDisponibleDTO : PagingDTO
    {
      
        public string Imei { get; set; }
        public string Descripcion { get; set; }
        public string Tecnologia { get; set; }
        public string Garantia { get; set; }
        public string EstadoBloqueo { get; set; }
        public decimal PrecioPorMayor { get; set; }
        public decimal PrecioDetalle { get; set; }
        public decimal CostoEquipo { get; set; }
        public decimal ComisionDetalle { get; set; }
        public decimal ComisionPorMayor { get; set; }
        public string IsDesbloqueado { get; set; }
        public string IsDisponible { get; set; }
        public string NotaAdicional { get; set; }
        public string DescripcionAlmacen { get; set; }

        public string Suplidor { get; set; }

        public int IdModelo { get; set; }
        public int IdSuplidor { get; set; }
        public int IdAlmacen { get; set; }
        public string Fecharegistro { get; set; }
        public decimal TotalInventario { get; set; }
        //public List<EquipoDisponibleDTO> EquipoDisponibleDTOs { get; set; }
    }
    public class EquipoDisponibleFilterDTO : RangeDatePaging
    {
        [Required]
        public int PageIndex { get; set; }
        [Required]

        public int PageSize { get; set; }
        public int? Almacen { get; set; }
        public string? Suplidor { get; set; }
        public string? Imei { get; set; }
        public string? Modelo { get; set; }

    }

}
