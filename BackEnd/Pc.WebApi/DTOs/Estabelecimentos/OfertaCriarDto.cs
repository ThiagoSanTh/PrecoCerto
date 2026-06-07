namespace Pc.WebApi.DTOs.Estabelecimentos
{
    public class OfertaCriarDto
    {
        public Guid ProdutoId { get; set; }
        public Guid LojaId { get; set; }
        public decimal Preco { get; set; }
        public decimal? PrecoAnterior { get; set; }
        public bool EmPromocao { get; set; }
        public DateTime? DataInicioPromocao { get; set; }
        public DateTime? DataFimPromocao { get; set; }
        public bool Disponivel { get; set; } = true;
        public int? QuantidadeEstoque { get; set; }
    }
}
