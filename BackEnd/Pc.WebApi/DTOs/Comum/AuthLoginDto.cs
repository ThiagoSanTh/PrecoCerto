using System.ComponentModel.DataAnnotations;

namespace Pc.WebApi.DTOs.Comum
{
    public class AuthLoginDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string Senha { get; set; } = string.Empty;

        /// <summary>cliente | lojista | admin — opcional; default cliente / fallback admin no controller.</summary>
        public string? Tipo { get; set; }
    }
}
