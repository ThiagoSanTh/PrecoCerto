using Pc.Dominio.Enums;

namespace Pc.Servico.Modelos
{
    public class FeedItem
    {
        public Guid ProdutoId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? ImagemUrl { get; set; }
        public Guid? LojaId { get; set; }
        public string? LojaNome { get; set; }
        public decimal PrecoBase { get; set; }
        public decimal PrecoExibicao { get; set; }
        public decimal? PrecoAnterior { get; set; }
        public bool EmPromocao { get; set; }
        public CategoriaProduto Categoria { get; set; }
    }
}
