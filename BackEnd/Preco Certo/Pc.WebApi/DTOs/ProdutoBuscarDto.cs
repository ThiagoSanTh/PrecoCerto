using System.ComponentModel.DataAnnotations;

namespace Pc.WebApi.DTOs
{
    public class ProdutoBuscarDto
    {
        [Required(ErrorMessage = "Nome é obrigatório.")]
        [MinLength(2, ErrorMessage = "Nome deve ter pelo menos 2 caracteres.")]
        [MaxLength(200)]
        public string Nome { get; set; } = string.Empty;
    }
}
