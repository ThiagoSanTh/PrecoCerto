using System.ComponentModel.DataAnnotations;

namespace Pc.WebApi.DTOs.Catalogo
{
    public class ProdutoCriarDto
    {
        [Required(ErrorMessage = "Nome do produto é obrigatório.")]
        [MaxLength(200)]
        public string NomeProduto { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Descricao { get; set; }

        [MaxLength(100)]
        public string Marca { get; set; } = string.Empty;

        [MaxLength(50)]
        public string CodigoBarras { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "Preço deve ser maior que zero.")]
        public decimal Preco { get; set; }

        public Guid? LojaId { get; set; }

        [MaxLength(500)]
        public string? ImagemUrl { get; set; }
    }
}
