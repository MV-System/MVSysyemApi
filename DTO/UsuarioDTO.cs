using System.ComponentModel.DataAnnotations;

namespace DTO
{
    public class UsuarioDTO
    {
        [Required]
        public string UserName { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
