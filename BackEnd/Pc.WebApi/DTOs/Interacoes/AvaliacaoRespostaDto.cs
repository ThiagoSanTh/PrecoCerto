namespace Pc.WebApi.DTOs.Interacoes
{
    public class AvaliacaoRespostaDto
    {
        public Guid Id { get; set; }
        public Guid ClienteId { get; set; }
        public Guid LojaId { get; set; }
        public string NomeLoja { get; set; } = string.Empty;
        public int Nota { get; set; }
        public string? Comentario { get; set; }
        public DateTime DataAvaliacao { get; set; }
    }
}
