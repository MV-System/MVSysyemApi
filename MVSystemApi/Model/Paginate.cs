using System.ComponentModel.DataAnnotations;

namespace MVSystemApi.Model
{
    public class Paginate
    {
        const int maxPageSize = 100;
        [Required]
        public int PageNumber { get; set; } = 1;
        private int _pageSize = 10;

        [Required]
        public int PageSize
        {
            get { return _pageSize; }
            set { _pageSize = (value > maxPageSize) ? maxPageSize : value; }
        }
    }
}
