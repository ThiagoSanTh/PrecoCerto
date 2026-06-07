namespace Pc.WebApi.DTOs.Interacoes
{
    public class HistoricoPesquisaRespostaDto
    {
        public Guid Id { get; set; }
        public Guid ClienteId { get; set; }
        public string TermoPesquisa { get; set; } = string.Empty;
        public DateTime DataPesquisa { get; set; }
    }
}
