using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pc.Dominio.Entities.Interacoes;
using Pc.Infraestrutura;
using Pc.Repositorio.Interfaces;

namespace Pc.Repositorio.Implementacoes
{
    /// <summary>
    /// Implementação do repositório para PreferenciaCliente
    /// Herda do Repositorio genérico e implementa métodos específicos
    /// Gerencia preferências do cliente usando padrão chave-valor
    /// </summary>
    public class PreferenciaClienteRepositorio : Repositorio<PreferenciaCliente>, IPreferenciaClienteRepositorio
    {
        /// <summary>
        /// Construtor que injeta o contexto do banco de dados
        /// </summary>
        public PreferenciaClienteRepositorio(AppDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Obtém todas as preferências de um cliente
        /// Retorna lista com todas as configurações do usuário
        /// </summary>
        public async Task<List<PreferenciaCliente>> ObterPorClienteAsync(Guid clienteId)
        {
            return await _context.PreferenciasClientes
                .Where(p => p.ClienteId == clienteId)
                .ToListAsync();
        }

        /// <summary>
        /// Obtém uma preferência específica por chave
        /// Retorna null se não existir
        /// Útil para buscas por configuração específica
        /// </summary>
        public async Task<PreferenciaCliente?> ObterPorChaveAsync(Guid clienteId, string chave)
        {
            return await _context.PreferenciasClientes
                .FirstOrDefaultAsync(p => p.ClienteId == clienteId && p.Chave == chave);
        }

        /// <summary>
        /// Busca todas as preferências com uma chave específica
        /// Útil para encontrar um tipo de preferência entre vários clientes
        /// Exemplo: encontrar todos que habilitaram notificações
        /// </summary>
        public async Task<List<PreferenciaCliente>> BuscarPorChaveAsync(string chave)
        {
            return await _context.PreferenciasClientes
                .Where(p => p.Chave == chave)
                .ToListAsync();
        }
    }
}
