using System.ComponentModel.DataAnnotations;

namespace Pc.WebApi.DTOs.Comum
{
    public class RedefinirSenhaDto
    {
        [Required(ErrorMessage = "Token é obrigatório.")]
        [MinLength(32, ErrorMessage = "Token inválido.")]
        [MaxLength(64, ErrorMessage = "Token inválido.")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nova senha é obrigatória.")]
        [MinLength(6, ErrorMessage = "Senha deve ter pelo menos 6 caracteres.")]
        [MaxLength(100, ErrorMessage = "Senha deve ter no máximo 100 caracteres.")]
        public string NovaSenha { get; set; } = string.Empty;
    }
}
