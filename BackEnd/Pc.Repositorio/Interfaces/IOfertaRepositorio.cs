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
    }
}