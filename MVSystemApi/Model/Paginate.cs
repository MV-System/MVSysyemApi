using System.ComponentModel.DataAnnotations;

namespace MVSystemApi.Model
{
    public class Paginate
    {

        [Required]
        public int PageIndex { get; set; } 

        [Required]
        public int PageSize { get; set; } 
    }
}
