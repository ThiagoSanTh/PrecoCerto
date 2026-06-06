using System.ComponentModel.DataAnnotations;

namespace Pc.WebApi.DTOs.Comum
{
    public class LoginDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string Senha { get; set; } = string.Empty;
    }
}
