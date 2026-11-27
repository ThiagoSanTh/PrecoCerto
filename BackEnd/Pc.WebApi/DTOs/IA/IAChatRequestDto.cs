using System.ComponentModel.DataAnnotations;

namespace Pc.WebApi.DTOs.IA
{
    public class IAChatRequestDto
    {
        [Required(ErrorMessage = "Informe a mensagem.")]
        [MaxLength(500)]
        public string Mensagem { get; set; } = string.Empty;

        [Range(-90, 90)]
        public double? Latitude { get; set; }

        [Range(-180, 180)]
        public double? Longitude { get; set; }

        public Guid? SessaoId { get; set; }
    }
}
