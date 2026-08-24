using Pc.Dominio.Enums;

namespace Pc.Dominio.Comum
{
    /// <summary>
    /// Linha do feed já projetada (produto + melhor oferta disponível).
    /// Evita carregar o grafo Loja/Oferta só para montar o DTO.
    /// </summary>
    public class FeedProdutoLinha
    {
        public Guid ProdutoId { get; init; }
        public string Nome { get; init; } = string.Empty;
        public string? ImagemUrl { get; init; }
        public Guid? LojaId { get; init; }
        public string? LojaNome { get; init; }
        public decimal PrecoBase { get; init; }
        public decimal PrecoExibicao { get; init; }
        public decimal? PrecoAnterior { get; init; }
        public bool EmPromocao { get; init; }
        public CategoriaProduto Categoria { get; init; }
    }
}
