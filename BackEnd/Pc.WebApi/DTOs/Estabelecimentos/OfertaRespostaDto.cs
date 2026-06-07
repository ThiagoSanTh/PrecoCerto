namespace Pc.WebApi.DTOs.Estabelecimentos
{
    public class OfertaRespostaDto
    {
        public Guid Id { get; set; }
        public Guid ProdutoId { get; set; }
        public string NomeProduto { get; set; } = string.Empty;
        public string MarcaProduto { get; set; } = string.Empty;
        public Guid LojaId { get; set; }
        public string NomeLoja { get; set; } = string.Empty;
        public decimal Preco { get; set; }
        public decimal? PrecoAnterior { get; set; }
        public bool EmPromocao { get; set; }
        public DateTime? DataInicioPromocao { get; set; }
        public DateTime? DataFimPromocao { get; set; }
        public bool Disponivel { get; set; }
        public int QuantidadeEstoque { get; set; }
        public DateTime DataAtualizacaoPreco { get; set; }
    }
}
