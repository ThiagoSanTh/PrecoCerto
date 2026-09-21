namespace Pc.WebApi.DTOs.IA
{
    public class IAChatResponseDto
    {
        public bool Sucesso { get; set; }
        public string Intencao { get; set; } = string.Empty;
        public string Resposta { get; set; } = string.Empty;
        public object? Dados { get; set; }
    }
}
