using Pc.Dominio.Entities.Interacoes;
using Pc.Repositorio.Interfaces;
using Pc.Servico.Interfaces;

namespace Pc.Servico.Implementacoes
{
    /// <summary>
    /// Serviço de PreferenciaCliente
    /// Contém lógica de negócio para preferências/configurações
    /// Usa padrão chave-valor para máxima flexibilidade
    /// Padrão: Validação + acesso via repositório
    /// </summary>
    public class PreferenciaClienteServico : IPreferenciaClienteServico
    {
        private readonly IPreferenciaClienteRepositorio _preferenciaRepositorio;

        /// <summary>
        /// Injeta o repositório de preferência
        /// </summary>
        public PreferenciaClienteServico(IPreferenciaClienteRepositorio preferenciaRepositorio)
        {
            _preferenciaRepositorio = preferenciaRepositorio;
        }

        /// <summary>
        /// Salva uma nova preferência ou atualiza se existir
        /// Valida cliente, chave e valor não vazios
        /// </summary>
        public async Task<PreferenciaCliente> SalvarAsync(PreferenciaCliente preferencia)
        {
            // Validação: cliente obrigatório
            if (preferencia.ClienteId == Guid.Empty)
                throw new Exception("ClienteId é obrigatório.");

            // Validação: chave obrigatória e não vazia
            if (string.IsNullOrWhiteSpace(preferencia.Chave))
                throw new Exception("Chave não pode estar vazia.");

            // Validação: valor obrigatório e não vazio
            if (string.IsNullOrWhiteSpace(preferencia.Valor))
                throw new Exception("Valor não pode estar vazio.");

            // Normaliza chave (lowercase para padronizar)
            preferencia.Chave = preferencia.Chave.Trim().ToLower();
            preferencia.Valor = preferencia.Valor.Trim();

            // Verifica se preferência já existe
            var existente = await _preferenciaRepositorio.ObterPorChaveAsync(
                preferencia.ClienteId, 
                preferencia.Chave
            );

            if (existente != null)
            {
                // Se existe, atualiza
                existente.Valor = preferencia.Valor;
                await _preferenciaRepositorio.AtualizarAsync(existente);
                return existente;
            }

            // Se não existe, cria nova
            return await _preferenciaRepositorio.AdicionarAsync(preferencia);
        }

        /// <summary>
        /// Obtém uma preferência por ID
        /// </summary>
        public async Task<PreferenciaCliente?> ObterAsync(Guid id)
        {
            return await _preferenciaRepositorio.ObterPorIdAsync(id);
        }

        /// <summary>
        /// Lista todas as preferências de um cliente
        /// </summary>
        public async Task<List<PreferenciaCliente>> ListarPorClienteAsync(Guid clienteId)
        {
            if (clienteId == Guid.Empty)
                throw new Exception("ClienteId é obrigatório.");

            return await _preferenciaRepositorio.ObterPorClienteAsync(clienteId);
        }

        /// <summary>
        /// Obtém o valor de uma preferência específica
        /// Retorna null se não existir
        /// </summary>
        public async Task<string?> ObterValorAsync(Guid clienteId, string chave)
        {
            if (clienteId == Guid.Empty)
                throw new Exception("ClienteId é obrigatório.");

            if (string.IsNullOrWhiteSpace(chave))
                throw new Exception("Chave não pode estar vazia.");

            var preferencia = await _preferenciaRepositorio.ObterPorChaveAsync(
                clienteId, 
                chave.ToLower()
            );

            return preferencia?.Valor;
        }

        /// <summary>
        /// Atualiza o valor de uma preferência existente
        /// Se não existir, cria nova
        /// </summary>
        public async Task AtualizarAsync(Guid clienteId, string chave, string valor)
        {
            if (clienteId == Guid.Empty)
                throw new Exception("ClienteId é obrigatório.");

            if (string.IsNullOrWhiteSpace(chave) || string.IsNullOrWhiteSpace(valor))
                throw new Exception("Chave e Valor não podem estar vazios.");

            var preferencia = await _preferenciaRepositorio.ObterPorChaveAsync(
                clienteId, 
                chave.ToLower()
            );

            if (preferencia == null)
            {
                preferencia = new PreferenciaCliente
                {
                    ClienteId = clienteId,
                    Chave = chave.ToLower(),
                    Valor = valor.Trim()
                };
                await _preferenciaRepositorio.AdicionarAsync(preferencia);
            }
            else
            {
                preferencia.Valor = valor.Trim();
                await _preferenciaRepositorio.AtualizarAsync(preferencia);
            }
        }

        /// <summary>
        /// Remove uma preferência por ID
        /// </summary>
        public async Task RemoverAsync(Guid id)
        {
            if (id == Guid.Empty)
                throw new Exception("ID é obrigatório.");

            await _preferenciaRepositorio.RemoverAsync(id);
        }

        /// <summary>
        /// Remove uma preferência por cliente e chave
        /// </summary>
        public async Task RemoverPorChaveAsync(Guid clienteId, string chave)
        {
            if (clienteId == Guid.Empty)
                throw new Exception("ClienteId é obrigatório.");

            if (string.IsNullOrWhiteSpace(chave))
                throw new Exception("Chave não pode estar vazia.");

            var preferencia = await _preferenciaRepositorio.ObterPorChaveAsync(
                clienteId, 
                chave.ToLower()
            );

            if (preferencia != null)
                await _preferenciaRepositorio.RemoverAsync(preferencia.Id);
        }

        /// <summary>
        /// Limpa todas as preferências de um cliente
        /// Operação irreversível
        /// </summary>
        public async Task LimparTudasAsync(Guid clienteId)
        {
            if (clienteId == Guid.Empty)
                throw new Exception("ClienteId é obrigatório.");

            var preferencias = await _preferenciaRepositorio.ObterPorClienteAsync(clienteId);

            foreach (var preferencia in preferencias)
                await _preferenciaRepositorio.RemoverAsync(preferencia.Id);
        }
    }
}
