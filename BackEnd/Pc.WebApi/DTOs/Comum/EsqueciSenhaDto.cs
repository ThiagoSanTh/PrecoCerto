using System.ComponentModel.DataAnnotations;

namespace Pc.WebApi.DTOs.Comum
{
    public class EsqueciSenhaDto
    {
        [Required(ErrorMessage = "E-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        [MaxLength(150, ErrorMessage = "E-mail deve ter no máximo 150 caracteres.")]
        public string Email { get; set; } = string.Empty;
    }
}
