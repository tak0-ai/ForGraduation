using System.ComponentModel.DataAnnotations;

namespace RuralTourism.Api.Models
{
    public class LoginRequest
    {
        [Required]
        public string UserNameOrEmail { get; set; }= default!;

        [Required]
        public string Password { get; set; }= default!;
    }
}
