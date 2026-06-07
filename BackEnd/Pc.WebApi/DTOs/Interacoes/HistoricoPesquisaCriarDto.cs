namespace Pc.WebApi.DTOs.Interacoes
{
    public class HistoricoPesquisaCriarDto
    {
        public Guid ClienteId { get; set; }
        public string TermoPesquisa { get; set; } = string.Empty;
    }
}
