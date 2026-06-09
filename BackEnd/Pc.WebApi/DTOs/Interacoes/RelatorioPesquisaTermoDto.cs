namespace Pc.WebApi.DTOs.Interacoes
{
    /// <summary>Linha do relatório de BI: termo, quantidade e última pesquisa.</summary>
    public class RelatorioPesquisaTermoDto
    {
        public string Termo { get; set; } = string.Empty;
        public int Quantidade { get; set; }
        public DateTime UltimaPesquisa { get; set; }
    }
}
