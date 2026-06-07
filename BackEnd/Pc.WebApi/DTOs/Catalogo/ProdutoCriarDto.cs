namespace Pc.WebApi.DTOs.Catalogo
{
    public class ProdutoCriarDto
    {
        public string NomeProduto { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public string Marca { get; set; } = string.Empty;
        public string CodigoBarras { get; set; } = string.Empty;
        public decimal Preco { get; set; }
        public Guid? LojaId { get; set; }
        public string? ImagemUrl { get; set; }
    }
}
