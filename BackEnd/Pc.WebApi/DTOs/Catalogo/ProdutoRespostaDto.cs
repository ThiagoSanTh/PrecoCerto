using Pc.Dominio.Enums;

namespace Pc.WebApi.DTOs.Catalogo
{
    public class ProdutoRespostaDto
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public string Marca { get; set; } = string.Empty;
        public string CodigoBarras { get; set; } = string.Empty;
        public decimal Preco { get; set; }
        public Guid? LojaId { get; set; }
        public string? ImagemUrl { get; set; }
        public CategoriaProduto Categoria { get; set; }
        public string CategoriaNome { get; set; } = string.Empty;
        public string? LojaNomeFantasia { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? Logradouro { get; set; }
        public string? Cidade { get; set; }
    }
}
