using System.ComponentModel.DataAnnotations;

namespace Pc.WebApi.DTOs.Estabelecimentos
{
    public class EnderecoDto
    {
        [Required, RegularExpression(@"^\d{5}-?\d{3}$", ErrorMessage = "CEP inválido. Use o formato 00000-000.")]
        public string Cep { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Logradouro { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Numero { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Complemento { get; set; }

        [MaxLength(100)]
        public string? Bairro { get; set; }

        [Required, MaxLength(100)]
        public string Cidade { get; set; } = string.Empty;

        [Required, StringLength(2, MinimumLength = 2, ErrorMessage = "Estado deve ser a sigla com 2 letras (ex.: SP).")]
        public string Estado { get; set; } = string.Empty;

        [Range(-90, 90)]
        public decimal? Latitude { get; set; }

        [Range(-180, 180)]
        public decimal? Longitude { get; set; }
    }
}
