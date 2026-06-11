using System.ComponentModel.DataAnnotations;

namespace Pc.WebApi.DTOs.Interacoes
{
    public class FavoritoCriarDto
    {
        [Required]
        public Guid ClienteId { get; set; }

        public Guid? ProdutoId { get; set; }
        public Guid? LojaId { get; set; }
    }
}
