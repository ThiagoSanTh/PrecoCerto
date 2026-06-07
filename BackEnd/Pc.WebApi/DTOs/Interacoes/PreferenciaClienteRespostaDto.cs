namespace Pc.WebApi.DTOs.Interacoes
{
    public class PreferenciaClienteRespostaDto
    {
        public Guid Id { get; set; }
        public Guid ClienteId { get; set; }
        public string Chave { get; set; } = string.Empty;
        public string Valor { get; set; } = string.Empty;
        public DateTime DataCriacao { get; set; }
    }
}
