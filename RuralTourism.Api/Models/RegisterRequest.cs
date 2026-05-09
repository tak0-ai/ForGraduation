using System.ComponentModel.DataAnnotations;

namespace RuralTourism.Api.Models
{
    public class RegisterRequest
    {
        [Required]
        [MinLength(3)]
        public string UserName { get; set; } = default!;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = default!;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = default!;

    }
}
