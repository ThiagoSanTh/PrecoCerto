namespace Pc.WebApi.DTOs.Interacoes
{
    public class FavoritoRespostaDto
    {
        public Guid Id { get; set; }
        public Guid ClienteId { get; set; }
        public Guid? ProdutoId { get; set; }
        public string? NomeProduto { get; set; }
        public Guid? LojaId { get; set; }
        public string? NomeLoja { get; set; }
        public DateTime DataCriacao { get; set; }
        public string? ImagemUrl { get; set; }
        public decimal? PrecoBase { get; set; }
        public decimal? PrecoExibicao { get; set; }
        public decimal? PrecoAnterior { get; set; }
        public bool EmPromocao { get; set; }
    }
}
