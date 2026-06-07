using Pc.Dominio.Entities.Interacoes;
using Pc.Repositorio.Interfaces;
using Pc.Servico.Interfaces;

namespace Pc.Servico.Implementacoes
{
    /// <summary>
    /// Serviço de Favorito
    /// Contém lógica de negócio para operações com favoritos
    /// Valida regras antes de delegação ao repositório
    /// Escolheu-se este padrão para manter separação entre validação e acesso a dados
    /// </summary>
    public class FavoritoServico : IFavoritoServico
    {
        private readonly IFavoritoRepositorio _favoritoRepositorio;

        /// <summary>
        /// Injeta o repositório de favorito
        /// </summary>
        public FavoritoServico(IFavoritoRepositorio favoritoRepositorio)
        {
            _favoritoRepositorio = favoritoRepositorio;
        }

        /// <summary>
        /// Adiciona um novo favorito com validação
        /// Garante que pelo menos Produto ou Loja está preenchido
        /// </summary>
        public async Task<Favorito> AdicionarAsync(Favorito favorito)
        {
            // Validação: favorito deve ter cliente
            if (favorito.ClienteId == Guid.Empty)
                throw new Exception("ClienteId é obrigatório.");

            // Validação: pelo menos um de Produto ou Loja deve estar preenchido
            if (!favorito.ProdutoId.HasValue && !favorito.LojaId.HasValue)
                throw new Exception("Pelo menos um de Produto ou Loja é obrigatório.");

            return await _favoritoRepositorio.AdicionarAsync(favorito);
        }

        /// <summary>
        /// Obtém um favorito por ID
        /// </summary>
        public async Task<Favorito?> ObterPorIdAsync(Guid id)
        {
            return await _favoritoRepositorio.ObterPorIdAsync(id);
        }

        /// <summary>
        /// Lista todos os favoritos de um cliente
        /// </summary>
        public async Task<List<Favorito>> ListarPorClienteAsync(Guid clienteId)
        {
            if (clienteId == Guid.Empty)
                throw new Exception("ClienteId é obrigatório.");

            return await _favoritoRepositorio.ObterPorClienteAsync(clienteId);
        }

        /// <summary>
        /// Remove um favorito por ID
        /// </summary>
        public async Task RemoverAsync(Guid id)
        {
            if (id == Guid.Empty)
                throw new Exception("ID é obrigatório.");

            await _favoritoRepositorio.RemoverAsync(id);
        }

        /// <summary>
        /// Remove um favorito de produto específico
        /// Encontra e remove o favorito correspondente
        /// </summary>
        public async Task RemoverFavoritoProdutoAsync(Guid clienteId, Guid produtoId)
        {
            if (clienteId == Guid.Empty || produtoId == Guid.Empty)
                throw new Exception("ClienteId e ProdutoId são obrigatórios.");

            var favorito = await _favoritoRepositorio.ObterFavoritoProdutoAsync(clienteId, produtoId);
            if (favorito != null)
                await _favoritoRepositorio.RemoverAsync(favorito.Id);
        }

        /// <summary>
        /// Remove um favorito de loja específica
        /// Encontra e remove o favorito correspondente
        /// </summary>
        public async Task RemoverFavoritoLojaAsync(Guid clienteId, Guid lojaId)
        {
            if (clienteId == Guid.Empty || lojaId == Guid.Empty)
                throw new Exception("ClienteId e LojaId são obrigatórios.");

            var favorito = await _favoritoRepositorio.ObterFavoritoLojaAsync(clienteId, lojaId);
            if (favorito != null)
                await _favoritoRepositorio.RemoverAsync(favorito.Id);
        }

        /// <summary>
        /// Verifica se um item é favorito do cliente
        /// </summary>
        public async Task<bool> EhFavoritoAsync(Guid clienteId, Guid? produtoId, Guid? lojaId)
        {
            if (clienteId == Guid.Empty)
                throw new Exception("ClienteId é obrigatório.");

            if (produtoId.HasValue)
                return await _favoritoRepositorio.ObterFavoritoProdutoAsync(clienteId, produtoId.Value) != null;

            if (lojaId.HasValue)
                return await _favoritoRepositorio.ObterFavoritoLojaAsync(clienteId, lojaId.Value) != null;

            return false;
        }
    }
}
