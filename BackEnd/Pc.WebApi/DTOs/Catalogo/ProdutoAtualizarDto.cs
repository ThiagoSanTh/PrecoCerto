using System.ComponentModel.DataAnnotations;
using Pc.Dominio.Enums;

namespace Pc.WebApi.DTOs.Catalogo
{
    /// <summary>Dados para atualização; LojaId identifica a loja autenticada (dono do produto).</summary>
    public class ProdutoAtualizarDto
    {
        [Required, MinLength(2), MaxLength(150)]
        public string NomeProduto { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Descricao { get; set; }

        [MaxLength(100)]
        public string Marca { get; set; } = string.Empty;

        [MaxLength(50)]
        public string CodigoBarras { get; set; } = string.Empty;

        [Range(0.01, 9999999, ErrorMessage = "Preço deve ser maior que zero.")]
        public decimal Preco { get; set; }

        [Required]
        public Guid LojaId { get; set; }

        [MaxLength(500), Url]
        public string? ImagemUrl { get; set; }

        public CategoriaProduto? Categoria { get; set; }
    }
}
