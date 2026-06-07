namespace Pc.WebApi.DTOs.Interacoes
{
    public class PreferenciaClienteCriarDto
    {
        public Guid ClienteId { get; set; }
        public string Chave { get; set; } = string.Empty;
        public string Valor { get; set; } = string.Empty;
    }
}
