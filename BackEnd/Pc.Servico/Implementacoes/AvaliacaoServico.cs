using Pc.Dominio.Entities.Interacoes;
using Pc.Repositorio.Interfaces;
using Pc.Servico.Interfaces;

namespace Pc.Servico.Implementacoes
{
    /// <summary>
    /// Serviço de Avaliacao
    /// Contém lógica de negócio para avaliações de lojas
    /// Valida notas, evita duplicatas, calcula médias
    /// Padrão: Validação robusta antes de acesso a dados
    /// </summary>
    public class AvaliacaoServico : IAvaliacaoServico
    {
        private readonly IAvaliacaoRepositorio _avaliacaoRepositorio;

        /// <summary>
        /// Injeta o repositório de avaliação
        /// </summary>
        public AvaliacaoServico(IAvaliacaoRepositorio avaliacaoRepositorio)
        {
            _avaliacaoRepositorio = avaliacaoRepositorio;
        }

        /// <summary>
        /// Adiciona uma nova avaliação com validações
        /// Valida: cliente, loja, nota entre 1-5, evita duplicatas
        /// </summary>
        public async Task<Avaliacao> AdicionarAsync(Avaliacao avaliacao)
        {
            // Validação: cliente obrigatório
            if (avaliacao.ClienteId == Guid.Empty)
                throw new Exception("ClienteId é obrigatório.");

            // Validação: loja obrigatória
            if (avaliacao.LojaId == Guid.Empty)
                throw new Exception("LojaId é obrigatório.");

            // Validação: nota deve estar entre 1 e 5
            if (avaliacao.Nota < 1 || avaliacao.Nota > 5)
                throw new Exception("Nota deve estar entre 1 e 5.");

            // Validação: verifica se cliente já avaliou esta loja
            var avaliacaoExistente = await _avaliacaoRepositorio.VerificarAvaliacaoExistenteAsync(
                avaliacao.ClienteId, 
                avaliacao.LojaId
            );

            if (avaliacaoExistente != null)
                throw new Exception("Cliente já avaliou esta loja. Atualize a avaliação existente.");

            return await _avaliacaoRepositorio.AdicionarAsync(avaliacao);
        }

        /// <summary>
        /// Obtém uma avaliação por ID
        /// </summary>
        public async Task<Avaliacao?> ObterPorIdAsync(Guid id)
        {
            return await _avaliacaoRepositorio.ObterPorIdAsync(id);
        }

        /// <summary>
        /// Lista todas as avaliações de uma loja
        /// </summary>
        public async Task<List<Avaliacao>> ListarPorLojaAsync(Guid lojaId)
        {
            if (lojaId == Guid.Empty)
                throw new Exception("LojaId é obrigatório.");

            return await _avaliacaoRepositorio.ObterPorLojaAsync(lojaId);
        }

        /// <summary>
        /// Lista todas as avaliações feitas por um cliente
        /// </summary>
        public async Task<List<Avaliacao>> ListarPorClienteAsync(Guid clienteId)
        {
            if (clienteId == Guid.Empty)
                throw new Exception("ClienteId é obrigatório.");

            return await _avaliacaoRepositorio.ObterPorClienteAsync(clienteId);
        }

        /// <summary>
        /// Calcula a nota média de uma loja
        /// Retorna valor com uma casa decimal
        /// </summary>
        public async Task<double> ObterMediaAvaliacaoAsync(Guid lojaId)
        {
            if (lojaId == Guid.Empty)
                throw new Exception("LojaId é obrigatório.");

            return await _avaliacaoRepositorio.ObterMediaAvaliacaoAsync(lojaId);
        }

        /// <summary>
        /// Atualiza uma avaliação existente
        /// Valida nova nota e dados
        /// </summary>
        public async Task AtualizarAsync(Avaliacao avaliacao)
        {
            // Validação: ID obrigatório
            if (avaliacao.Id == Guid.Empty)
                throw new Exception("ID é obrigatório.");

            // Validação: nota deve estar entre 1 e 5
            if (avaliacao.Nota < 1 || avaliacao.Nota > 5)
                throw new Exception("Nota deve estar entre 1 e 5.");

            await _avaliacaoRepositorio.AtualizarAsync(avaliacao);
        }

        /// <summary>
        /// Remove uma avaliação por ID
        /// </summary>
        public async Task RemoverAsync(Guid id)
        {
            if (id == Guid.Empty)
                throw new Exception("ID é obrigatório.");

            await _avaliacaoRepositorio.RemoverAsync(id);
        }

        /// <summary>
        /// Obtém a quantidade de avaliações de uma loja
        /// </summary>
        public async Task<int> ObterQuantidadeAvaliacoesAsync(Guid lojaId)
        {
            if (lojaId == Guid.Empty)
                throw new Exception("LojaId é obrigatório.");

            var avaliacoes = await _avaliacaoRepositorio.ObterPorLojaAsync(lojaId);
            return avaliacoes.Count;
        }
    }
}
