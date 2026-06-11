using System.ComponentModel.DataAnnotations;

namespace Pc.WebApi.DTOs.Interacoes
{
    public class HistoricoPesquisaCriarDto
    {
        [Required]
        public Guid ClienteId { get; set; }

        [Required, MaxLength(200)]
        public string TermoPesquisa { get; set; } = string.Empty;

        /// <summary>Produto aberto a partir da pesquisa (opcional, para BI).</summary>
        public Guid? ProdutoId { get; set; }

        /// <summary>Loja vinculada ao resultado da pesquisa (opcional, para BI).</summary>
        public Guid? LojaId { get; set; }
    }
}
