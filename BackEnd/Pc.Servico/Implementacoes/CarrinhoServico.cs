using Pc.Dominio.Entities.Interacoes;
using Pc.Repositorio.Interfaces;
using Pc.Servico.Interfaces;

namespace Pc.Servico.Implementacoes
{
    public class CarrinhoServico : ICarrinhoServico
    {
        private readonly ICarrinhoRepositorio _carrinhoRepositorio;

        public CarrinhoServico(ICarrinhoRepositorio carrinhoRepositorio)
        {
            _carrinhoRepositorio = carrinhoRepositorio;
        }

        public async Task<Carrinho> ObterOuCriarAsync(Guid clienteId)
        {
            if (clienteId == Guid.Empty)
                throw new Exception("ClienteId é obrigatório.");

            var carrinho = await _carrinhoRepositorio.ObterPorClienteAsync(clienteId);
            if (carrinho != null)
                return carrinho;

            var novo = new Carrinho { ClienteId = clienteId };
            await _carrinhoRepositorio.AdicionarAsync(novo);
            return await _carrinhoRepositorio.ObterPorClienteAsync(clienteId) ?? novo;
        }

        public async Task<Carrinho> AdicionarItemAsync(Guid clienteId, Guid produtoId, int quantidade, decimal precoUnitario, Guid? ofertaId)
        {
            if (produtoId == Guid.Empty)
                throw new Exception("ProdutoId é obrigatório.");
            if (quantidade <= 0)
                throw new Exception("Quantidade deve ser maior que zero.");

            var carrinho = await ObterOuCriarAsync(clienteId);

            var itemExistente = carrinho.Itens.FirstOrDefault(i => i.ProdutoId == produtoId);
            if (itemExistente != null)
            {
                itemExistente.Quantidade += quantidade;
                itemExistente.PrecoUnitario = precoUnitario;
                itemExistente.OfertaId = ofertaId;
                await _carrinhoRepositorio.AtualizarItemAsync(itemExistente);
            }
            else
            {
                await _carrinhoRepositorio.AdicionarItemAsync(new ItemCarrinho
                {
                    CarrinhoId = carrinho.Id,
                    ProdutoId = produtoId,
                    OfertaId = ofertaId,
                    Quantidade = quantidade,
                    PrecoUnitario = precoUnitario
                });
            }

            return await _carrinhoRepositorio.ObterPorClienteAsync(clienteId) ?? carrinho;
        }

        public async Task<Carrinho> AtualizarQuantidadeAsync(Guid clienteId, Guid itemId, int quantidade)
        {
            var carrinho = await ObterOuCriarAsync(clienteId);
            var item = await _carrinhoRepositorio.ObterItemAsync(itemId);

            if (item == null || item.CarrinhoId != carrinho.Id)
                throw new Exception("Item não encontrado no carrinho.");

            if (quantidade <= 0)
                await _carrinhoRepositorio.RemoverItemAsync(item);
            else
            {
                item.Quantidade = quantidade;
                await _carrinhoRepositorio.AtualizarItemAsync(item);
            }

            return await _carrinhoRepositorio.ObterPorClienteAsync(clienteId) ?? carrinho;
        }

        public async Task<Carrinho> RemoverItemAsync(Guid clienteId, Guid itemId)
        {
            var carrinho = await ObterOuCriarAsync(clienteId);
            var item = await _carrinhoRepositorio.ObterItemAsync(itemId);

            if (item == null || item.CarrinhoId != carrinho.Id)
                throw new Exception("Item não encontrado no carrinho.");

            await _carrinhoRepositorio.RemoverItemAsync(item);
            return await _carrinhoRepositorio.ObterPorClienteAsync(clienteId) ?? carrinho;
        }

        public async Task LimparAsync(Guid clienteId)
        {
            var carrinho = await _carrinhoRepositorio.ObterPorClienteAsync(clienteId);
            if (carrinho == null)
                return;

            foreach (var item in carrinho.Itens.ToList())
                await _carrinhoRepositorio.RemoverItemAsync(item);
        }
    }
}
