using System.ComponentModel.DataAnnotations;

namespace Pc.WebApi.DTOs.IA
{
    public class IAMensagemHistoricoDto
    {
        public string Papel { get; set; } = "user";
        public string Texto { get; set; } = string.Empty;
    }

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

        public Guid? LojaIdUsuario { get; set; }

        public string? ModoOperacional { get; set; }

        public List<IAMensagemHistoricoDto>? Historico { get; set; }
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
        public Guid? ProdutoId { get; set; }
        public Guid? LojaId { get; set; }
        public bool EmPromocao { get; set; }
        public List<string> Motivos { get; set; } = new();
    }

    public class IAAnalisarResponseDto
    {
        public bool Sucesso { get; set; }
        public string Resposta { get; set; } = string.Empty;
        public string Intencao { get; set; } = string.Empty;
        public string? Objetivo { get; set; }
        public double Confianca { get; set; }
        public string ProvedorLlm { get; set; } = "Deterministico";
        public List<IAAnalisarItemDto> Resultados { get; set; } = new();
        public List<string> Motivos { get; set; } = new();
        public List<string> Fallbacks { get; set; } = new();
    }
}
