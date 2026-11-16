namespace Pc.WebApi.DTOs.IA
{
    public class IAChatResponseDto
    {
        public bool Sucesso { get; set; }
        public Guid? SessaoId { get; set; }
        public string Intencao { get; set; } = string.Empty;
        public string Resposta { get; set; } = string.Empty;
        public string? ProvedorLlm { get; set; }
        public List<string> Fallbacks { get; set; } = new();
        public object? Dados { get; set; }
    }
}
