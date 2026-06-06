using System.ComponentModel.DataAnnotations;

namespace Pc.WebApi.DTOs.Comum
{
    public class AuthLoginDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string Senha { get; set; } = string.Empty;

        /// <summary>cliente | lojista | admin</summary>
        [Required]
        public string Tipo { get; set; } = "cliente";
    }
}
