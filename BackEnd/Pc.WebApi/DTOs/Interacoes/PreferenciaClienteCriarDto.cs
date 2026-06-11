using System.ComponentModel.DataAnnotations;

namespace Pc.WebApi.DTOs.Interacoes
{
    public class PreferenciaClienteCriarDto
    {
        [Required]
        public Guid ClienteId { get; set; }

        [Required, MaxLength(100)]
        public string Chave { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string Valor { get; set; } = string.Empty;
    }
}
