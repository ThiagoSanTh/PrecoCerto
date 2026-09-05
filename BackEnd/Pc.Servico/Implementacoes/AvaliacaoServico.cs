using Pc.Dominio.Entities.Interacoes;
using Pc.Dominio.Enums;
using Pc.Repositorio.Interfaces;
using Pc.Servico.Interfaces;
using Pc.Servico.Modelos.Rag;

namespace Pc.Servico.Implementacoes
{
    public class AvaliacaoServico : IAvaliacaoServico
    {
        private readonly IAvaliacaoRepositorio _avaliacaoRepositorio;
        private readonly IRagIndexFila _ragFila;

        public AvaliacaoServico(IAvaliacaoRepositorio avaliacaoRepositorio, IRagIndexFila ragFila)
        {
            _avaliacaoRepositorio = avaliacaoRepositorio;
            _ragFila = ragFila;
        }

        public async Task<Avaliacao> AdicionarAsync(Avaliacao avaliacao)
        {
            if (avaliacao.ClienteId == Guid.Empty)
                throw new Exception("ClienteId é obrigatório.");

            if (avaliacao.LojaId == Guid.Empty)
                throw new Exception("LojaId é obrigatório.");

            if (avaliacao.Nota < 1 || avaliacao.Nota > 5)
                throw new Exception("Nota deve estar entre 1 e 5.");

            var avaliacaoExistente = await _avaliacaoRepositorio.VerificarAvaliacaoExistenteAsync(
                avaliacao.ClienteId,
                avaliacao.LojaId
            );

            if (avaliacaoExistente != null)
                throw new Exception("Cliente já avaliou esta loja. Atualize a avaliação existente.");

            var criada = await _avaliacaoRepositorio.AdicionarAsync(avaliacao);
            EnfileirarAvaliacao(criada);
            return criada;
        }

        public async Task<Avaliacao?> ObterPorIdAsync(Guid id)
        {
            return await _avaliacaoRepositorio.ObterPorIdAsync(id);
        }

        public async Task<List<Avaliacao>> ListarPorLojaAsync(Guid lojaId)
        {
            if (lojaId == Guid.Empty)
                throw new Exception("LojaId é obrigatório.");

            return await _avaliacaoRepositorio.ObterPorLojaAsync(lojaId);
        }

        public async Task<List<Avaliacao>> ListarPorClienteAsync(Guid clienteId)
        {
            if (clienteId == Guid.Empty)
                throw new Exception("ClienteId é obrigatório.");

            return await _avaliacaoRepositorio.ObterPorClienteAsync(clienteId);
        }

        public async Task<double> ObterMediaAvaliacaoAsync(Guid lojaId)
        {
            if (lojaId == Guid.Empty)
                throw new Exception("LojaId é obrigatório.");

            return await _avaliacaoRepositorio.ObterMediaAvaliacaoAsync(lojaId);
        }

        public async Task<(double Media, int Quantidade)> ObterResumoAvaliacaoAsync(Guid lojaId)
        {
            if (lojaId == Guid.Empty)
                throw new Exception("LojaId é obrigatório.");

            return await _avaliacaoRepositorio.ObterResumoPorLojaAsync(lojaId);
        }

        public async Task AtualizarAsync(Avaliacao avaliacao)
        {
            if (avaliacao.Id == Guid.Empty)
                throw new Exception("ID é obrigatório.");

            if (avaliacao.Nota < 1 || avaliacao.Nota > 5)
                throw new Exception("Nota deve estar entre 1 e 5.");

            await _avaliacaoRepositorio.AtualizarAsync(avaliacao);
            EnfileirarAvaliacao(avaliacao);
        }

        public async Task RemoverAsync(Guid id)
        {
            if (id == Guid.Empty)
                throw new Exception("ID é obrigatório.");

            await _avaliacaoRepositorio.RemoverAsync(id);
            _ragFila.Enfileirar(RagDocumentoTipo.Avaliacao, id, RagIndexAcao.Remover);
        }

        public async Task<int> ObterQuantidadeAvaliacoesAsync(Guid lojaId)
        {
            if (lojaId == Guid.Empty)
                throw new Exception("LojaId é obrigatório.");

            var (_, quantidade) = await _avaliacaoRepositorio.ObterResumoPorLojaAsync(lojaId);
            return quantidade;
        }

        private void EnfileirarAvaliacao(Avaliacao avaliacao)
        {
            if (string.IsNullOrWhiteSpace(avaliacao.Comentario))
            {
                _ragFila.Enfileirar(RagDocumentoTipo.Avaliacao, avaliacao.Id, RagIndexAcao.Remover);
                return;
            }

            _ragFila.Enfileirar(RagDocumentoTipo.Avaliacao, avaliacao.Id, RagIndexAcao.Indexar);
        }
    }
}
