namespace Pc.WebApi.DTOs.Interacoes
{
    public class AdicionarItemCarrinhoDto
    {
        public Guid ProdutoId { get; set; }
        public int Quantidade { get; set; } = 1;
        public decimal PrecoUnitario { get; set; }
        public Guid? OfertaId { get; set; }
    }

    public class AtualizarItemCarrinhoDto
    {
        public int Quantidade { get; set; }
    }

    public class ItemCarrinhoRespostaDto
    {
        public Guid Id { get; set; }
        public Guid ProdutoId { get; set; }
        public string NomeProduto { get; set; } = string.Empty;
        public string? ImagemUrl { get; set; }
        public Guid? OfertaId { get; set; }
        public int Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class CarrinhoRespostaDto
    {
        public Guid Id { get; set; }
        public Guid ClienteId { get; set; }
        public List<ItemCarrinhoRespostaDto> Itens { get; set; } = new();
        public decimal Total { get; set; }
        public int QuantidadeItens { get; set; }
    }
}
