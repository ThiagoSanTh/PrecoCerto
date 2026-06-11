using Pc.Dominio.Comum;
using Pc.Dominio.Entities.Estabelecimentos;
using Pc.Repositorio.Interfaces;
using Pc.Servico.Interfaces;

namespace Pc.Servico.Implementacoes
{
    public class LojaServico : ILojaServico
    {
        private readonly ILojaRepositorio _lojaRepositorio;

        public LojaServico(ILojaRepositorio lojaRepositorio)
        {
            _lojaRepositorio = lojaRepositorio;
        }

        public async Task<Loja> AdicionarAsync(Loja loja)
        {
            if (string.IsNullOrWhiteSpace(loja.NomeFantasia))
                throw new Exception("O nome fantasia da loja é obrigatório.");

            return await _lojaRepositorio.AdicionarAsync(loja);
        }

        public async Task<Loja?> ObterPorIdAsync(Guid id)
        {
            return await _lojaRepositorio.ObterPorIdAsync(id);
        }

        public async Task<Loja?> ObterPorUsuarioIdAsync(Guid usuarioId)
        {
            return await _lojaRepositorio.ObterPorUsuarioIdAsync(usuarioId);
        }

        public async Task<List<Loja>> ListarAsync()
        {
            return await _lojaRepositorio.ListarAsync();
        }

        public Task<PaginacaoResultado<Loja>> ListarPaginadoAsync(PaginacaoParametros paginacao) =>
            _lojaRepositorio.ListarPaginadoAsync(paginacao);

        public async Task<List<Loja>> BuscarPorNomeAsync(string nome)
        {
            return await _lojaRepositorio.BuscarPorNomeAsync(nome);
        }

        public Task<PaginacaoResultado<Loja>> BuscarPorNomePaginadoAsync(string nome, PaginacaoParametros paginacao) =>
            _lojaRepositorio.BuscarPorNomePaginadoAsync(nome, paginacao);

        public Task<List<Loja>> ListarPorProximidadeAsync(
            decimal latitude, decimal longitude, decimal raioKm, int limite = 500) =>
            _lojaRepositorio.ListarPorProximidadeAsync(latitude, longitude, raioKm, limite);

        public async Task AtualizarAsync(Loja loja)
        {
            if (string.IsNullOrWhiteSpace(loja.NomeFantasia))
                throw new Exception("O nome fantasia da loja é obrigatório.");

            await _lojaRepositorio.AtualizarAsync(loja);
        }

        public async Task RemoverAsync(Guid id)
        {
            await _lojaRepositorio.RemoverAsync(id);
        }
    }
}
