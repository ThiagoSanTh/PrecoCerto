using Pc.Dominio.Entities.Estabelecimentos;
using Pc.Repositorio.Interfaces;
using Pc.Servico.Interfaces;

namespace Pc.Servico.Implementacoes
{
    public class OfertaServico : IOfertaServico
    {
        private readonly IOfertaRepositorio _ofertaRepositorio;

        public OfertaServico(IOfertaRepositorio ofertaRepositorio)
        {
            _ofertaRepositorio = ofertaRepositorio;
        }

        public async Task<Oferta> AdicionarAsync(Oferta oferta)
        {
            if (oferta.Preco <= 0)
                throw new Exception("O preço da oferta deve ser maior que zero.");

            return await _ofertaRepositorio.AdicionarAsync(oferta);
        }

        public async Task<Oferta?> ObterPorIdAsync(Guid id)
        {
            return await _ofertaRepositorio.ObterPorIdAsync(id);
        }

        public async Task<List<Oferta>> ListarAsync()
        {
            return await _ofertaRepositorio.ListarAsync();
        }

        public async Task<List<Oferta>> ObterPorProdutoAsync(Guid produtoId)
        {
            return await _ofertaRepositorio.ObterPorProdutoAsync(produtoId);
        }

        public async Task AtualizarAsync(Oferta oferta)
        {
            if (oferta.Preco <= 0)
                throw new Exception("O preço da oferta deve ser maior que zero.");

            await _ofertaRepositorio.AtualizarAsync(oferta);
        }

        public async Task RemoverAsync(Guid id)
        {
            await _ofertaRepositorio.RemoverAsync(id);
        }
    }
}
