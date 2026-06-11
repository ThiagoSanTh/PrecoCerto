using System.ComponentModel.DataAnnotations;

namespace Pc.WebApi.DTOs.Estabelecimentos
{
    public class OfertaCriarDto
    {
        [Required]
        public Guid ProdutoId { get; set; }

        [Required]
        public Guid LojaId { get; set; }

        [Range(0.01, 9999999, ErrorMessage = "Preço deve ser maior que zero.")]
        public decimal Preco { get; set; }

        [Range(0.01, 9999999, ErrorMessage = "Preço anterior deve ser maior que zero.")]
        public decimal? PrecoAnterior { get; set; }

        public bool EmPromocao { get; set; }
        public DateTime? DataInicioPromocao { get; set; }
        public DateTime? DataFimPromocao { get; set; }
        public bool Disponivel { get; set; } = true;

        [Range(0, int.MaxValue, ErrorMessage = "Quantidade em estoque não pode ser negativa.")]
        public int? QuantidadeEstoque { get; set; }
    }
}
