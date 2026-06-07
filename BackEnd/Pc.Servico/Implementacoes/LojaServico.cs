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

        public async Task<List<Loja>> ListarAsync()
        {
            return await _lojaRepositorio.ListarAsync();
        }

        public async Task<List<Loja>> BuscarPorNomeAsync(string nome)
        {
            return await _lojaRepositorio.BuscarPorNomeAsync(nome);
        }

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
