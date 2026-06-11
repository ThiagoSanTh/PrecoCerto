using Pc.Dominio.Comum;
using Pc.Dominio.Enums;
using Pc.Servico.Modelos;

namespace Pc.Servico.Interfaces
{
    public interface IFeedServico
    {
        Task<PaginacaoResultado<FeedItem>> ListarAsync(
            PaginacaoParametros paginacao,
            string? termo = null,
            CategoriaProduto? categoria = null,
            Guid? lojaId = null);
    }
}
