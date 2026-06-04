using System.ComponentModel.DataAnnotations;

namespace Pc.WebApi.DTOs.Catalogo
{
    public class ProdutoBuscarDto
    {
        [Required(ErrorMessage = "Termo de busca é obrigatório.")]
        [MaxLength(200)]
        public string Nome { get; set; } = string.Empty;

        public Guid? LojaId { get; set; }
    }
}
