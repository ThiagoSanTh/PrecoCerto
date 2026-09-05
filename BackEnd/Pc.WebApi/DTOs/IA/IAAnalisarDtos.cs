using System.ComponentModel.DataAnnotations;

namespace Pc.WebApi.DTOs.IA
{
    public class IAAnalisarRequestDto
    {
        [Required]
        [MaxLength(500)]
        public string Mensagem { get; set; } = string.Empty;

        [Range(-90, 90)]
        public double? Latitude { get; set; }

        [Range(-180, 180)]
        public double? Longitude { get; set; }

        public Guid? UsuarioId { get; set; }
    }

    public class IAAnalisarItemDto
    {
        public string Tipo { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public double Score { get; set; }
        public decimal? Preco { get; set; }
        public double? DistanciaKm { get; set; }
        public string? Loja { get; set; }
        public string? Produto { get; set; }
        public List<string> Motivos { get; set; } = new();
    }

    public class IAAnalisarResponseDto
    {
        public bool Sucesso { get; set; }
        public string Resposta { get; set; } = string.Empty;
        public string Intencao { get; set; } = string.Empty;
        public string? Objetivo { get; set; }
        public double Confianca { get; set; }
        public List<IAAnalisarItemDto> Resultados { get; set; } = new();
        public List<string> Motivos { get; set; } = new();
        public List<string> Fallbacks { get; set; } = new();
    }
}
