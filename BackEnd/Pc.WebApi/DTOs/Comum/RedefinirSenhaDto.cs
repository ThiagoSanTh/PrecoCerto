using System.ComponentModel.DataAnnotations;

namespace Pc.WebApi.DTOs.Comum
{
    public class RedefinirSenhaDto
    {
        [Required, MinLength(32), MaxLength(64)]
        public string Token { get; set; } = string.Empty;

        [Required, MinLength(6), MaxLength(100)]
        public string NovaSenha { get; set; } = string.Empty;
    }
}
