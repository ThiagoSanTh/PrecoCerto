using System.ComponentModel.DataAnnotations;

namespace Pc.WebApi.DTOs
{
    public class ProdutoCriarDto
    {
        [Required(ErrorMessage = "Nome do produto é obrigatório.")]
        [MaxLength(200)]
        public string NomeProduto { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Descricao { get; set; }

        [Required(ErrorMessage = "Marca é obrigatória.")]
        [MaxLength(100)]
        public string Marca { get; set; } = string.Empty;

        [MaxLength(50)]
        public string CodigoBarras { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "Preço deve ser maior que zero.")]
        public decimal Preco { get; set; }
    }
}
