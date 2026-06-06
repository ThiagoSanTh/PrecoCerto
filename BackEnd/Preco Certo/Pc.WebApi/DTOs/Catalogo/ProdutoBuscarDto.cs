namespace Pc.WebApi.DTOs.Catalogo
{
    public class ProdutoBuscarDto
    {
        public string Nome { get; set; } = string.Empty;
        public Guid? LojaId { get; set; }
    }
}
