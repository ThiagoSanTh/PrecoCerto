namespace Pc.WebApi.DTOs.Interacoes
{
    public class AvaliacaoCriarDto
    {
        public Guid ClienteId { get; set; }
        public Guid LojaId { get; set; }
        public int Nota { get; set; }
        public string? Comentario { get; set; }
    }
}
