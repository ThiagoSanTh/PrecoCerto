using Pc.Dominio.Entities.Interacoes;
using Pc.Repositorio.Interfaces;
using Pc.Servico.Interfaces;

namespace Pc.Servico.Implementacoes
{
    /// <summary>
    /// Serviço de HistoricoPesquisa
    /// Contém lógica de negócio para histórico de pesquisas
    /// Valida termos e gerencia dados históricos
    /// Padrão: Validação no serviço, acesso via repositório
    /// </summary>
    public class HistoricoPesquisaServico : IHistoricoPesquisaServico
    {
        private readonly IHistoricoPesquisaRepositorio _historicoPesquisaRepositorio;

        /// <summary>
        /// Injeta o repositório de histórico
        /// </summary>
        public HistoricoPesquisaServico(IHistoricoPesquisaRepositorio historicoPesquisaRepositorio)
        {
            _historicoPesquisaRepositorio = historicoPesquisaRepositorio;
        }

        /// <summary>
        /// Registra uma nova pesquisa no histórico
        /// Valida que o termo não está vazio ou apenas espaços
        /// </summary>
        public async Task<HistoricoPesquisa> RegistrarPesquisaAsync(HistoricoPesquisa historico)
        {
            // Validação: cliente obrigatório
            if (historico.ClienteId == Guid.Empty)
                throw new Exception("ClienteId é obrigatório.");

            // Validação: termo não pode estar vazio
            if (string.IsNullOrWhiteSpace(historico.TermoPesquisa))
                throw new Exception("TermoPesquisa não pode estar vazio.");

            // Normaliza o termo (remove espaços extras)
            historico.TermoPesquisa = historico.TermoPesquisa.Trim();

            return await _historicoPesquisaRepositorio.AdicionarAsync(historico);
        }

        /// <summary>
        /// Obtém um registro por ID
        /// </summary>
        public async Task<HistoricoPesquisa?> ObterPorIdAsync(Guid id)
        {
            return await _historicoPesquisaRepositorio.ObterPorIdAsync(id);
        }

        /// <summary>
        /// Obtém todo o histórico de um cliente
        /// Ordenado por data descrescente (mais recentes primeiro)
        /// </summary>
        public async Task<List<HistoricoPesquisa>> ObterHistoricoClienteAsync(Guid clienteId)
        {
            if (clienteId == Guid.Empty)
                throw new Exception("ClienteId é obrigatório.");

            return await _historicoPesquisaRepositorio.ObterPorClienteAsync(clienteId);
        }

        /// <summary>
        /// Obtém os últimos N termos pesquisados
        /// Útil para mostrar histórico recente na UI
        /// </summary>
        public async Task<List<HistoricoPesquisa>> ObterUltimosTermosAsync(Guid clienteId, int quantidade)
        {
            if (clienteId == Guid.Empty)
                throw new Exception("ClienteId é obrigatório.");

            if (quantidade <= 0)
                throw new Exception("Quantidade deve ser maior que zero.");

            return await _historicoPesquisaRepositorio.ObterUltimosAsync(clienteId, quantidade);
        }

        /// <summary>
        /// Obtém sugestões de termos para autocomplete
        /// Realiza busca case-insensitive
        /// </summary>
        public async Task<List<HistoricoPesquisa>> ObterSugestoesAsync(string termoPartial)
        {
            if (string.IsNullOrWhiteSpace(termoPartial))
                throw new Exception("TermoPesquisa é obrigatório.");

            return await _historicoPesquisaRepositorio.BuscarPorTermoAsync(termoPartial.Trim());
        }

        /// <summary>
        /// Limpa todo o histórico de um cliente
        /// Operação irreversível - usar com cuidado
        /// </summary>
        public async Task LimparHistoricoAsync(Guid clienteId)
        {
            if (clienteId == Guid.Empty)
                throw new Exception("ClienteId é obrigatório.");

            var historicos = await _historicoPesquisaRepositorio.ObterPorClienteAsync(clienteId);
            
            foreach (var historico in historicos)
                await _historicoPesquisaRepositorio.RemoverAsync(historico.Id);
        }

        /// <summary>
        /// Remove um registro específico do histórico
        /// </summary>
        public async Task RemoverAsync(Guid id)
        {
            if (id == Guid.Empty)
                throw new Exception("ID é obrigatório.");

            await _historicoPesquisaRepositorio.RemoverAsync(id);
        }
    }
}
