using System.ComponentModel.DataAnnotations;

namespace Pc.WebApi.DTOs.Comum
{
    public class PreferenciaValorDto
    {
        [Required, MaxLength(500)]
        public string Valor { get; set; } = string.Empty;
    }
}
