using System.ComponentModel.DataAnnotations;

namespace Pc.WebApi.DTOs.Interacoes
{
    public class AvaliacaoCriarDto
    {
        [Required]
        public Guid ClienteId { get; set; }

        [Required]
        public Guid LojaId { get; set; }

        [Range(1, 5, ErrorMessage = "Nota deve ser entre 1 e 5.")]
        public int Nota { get; set; }

        [MaxLength(1000)]
        public string? Comentario { get; set; }
    }
}
