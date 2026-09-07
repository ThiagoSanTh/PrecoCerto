using Pc.Dominio.Comum;
using Pc.Dominio.Entities.Catalogo;
using Pc.Dominio.Entities.Estabelecimentos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pc.Repositorio.Interfaces
{
    public interface IOfertaRepositorio : IRepositorio<Oferta>
    {
        Task<List<Oferta>> ObterPorProdutoAsync(Guid produtoId);
        Task<List<Guid>> ListarIdsPorProdutoAsync(Guid produtoId, int limite = 200);
        Task<List<Guid>> ListarIdsPorLojaAsync(Guid lojaId, int limite = 500);
        Task<List<Oferta>> ListarDisponiveisPorProdutosAsync(IEnumerable<Guid> produtoIds);
        Task<Dictionary<Guid, Oferta>> ObterMelhorOfertaPorProdutosAsync(IEnumerable<Guid> produtoIds);
        Task<PaginacaoResultado<Oferta>> ListarPaginadoAsync(PaginacaoParametros paginacao);
    }
}