using System.ComponentModel.DataAnnotations;

namespace Pc.WebApi.DTOs.Comum
{
    public class AlterarSenhaDto
    {
        [Required(ErrorMessage = "Senha atual é obrigatória.")]
        public string SenhaAtual { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nova senha é obrigatória.")]
        [MinLength(6, ErrorMessage = "Nova senha deve ter pelo menos 6 caracteres.")]
        public string NovaSenha { get; set; } = string.Empty;
    }
}
