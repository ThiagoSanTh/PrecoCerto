namespace Pc.WebApi.DTOs.Interacoes
{
    public class FavoritoCriarDto
    {
        public Guid ClienteId { get; set; }
        public Guid? ProdutoId { get; set; }
        public Guid? LojaId { get; set; }
    }
}
