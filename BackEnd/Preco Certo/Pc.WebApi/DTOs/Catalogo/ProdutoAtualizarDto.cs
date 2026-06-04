namespace Pc.WebApi.DTOs.Catalogo
{
    /// <summary>Dados para atualização; LojaId identifica a loja autenticada (dono do produto).</summary>
    public class ProdutoAtualizarDto
    {
        public string NomeProduto { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public string Marca { get; set; } = string.Empty;
        public string CodigoBarras { get; set; } = string.Empty;
        public decimal Preco { get; set; }
        public Guid LojaId { get; set; }
        public string? ImagemUrl { get; set; }
    }
}
