namespace Pc.WebApi.DTOs.Interacoes
{
    public class HistoricoPesquisaCriarDto
    {
        public Guid ClienteId { get; set; }
        public string TermoPesquisa { get; set; } = string.Empty;

        /// <summary>Produto aberto a partir da pesquisa (opcional, para BI).</summary>
        public Guid? ProdutoId { get; set; }

        /// <summary>Loja vinculada ao resultado da pesquisa (opcional, para BI).</summary>
        public Guid? LojaId { get; set; }
    }
}
