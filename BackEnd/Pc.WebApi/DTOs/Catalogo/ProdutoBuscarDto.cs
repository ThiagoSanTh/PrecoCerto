using System.ComponentModel.DataAnnotations;

namespace Pc.WebApi.DTOs.Catalogo
{
    public class ProdutoBuscarDto
    {
        [Required, MaxLength(150)]
        public string Nome { get; set; } = string.Empty;

        public Guid? LojaId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
