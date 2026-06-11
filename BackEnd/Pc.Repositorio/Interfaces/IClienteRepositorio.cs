using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Pc.Dominio.Entities.Usuarios;

namespace Pc.Repositorio.Interfaces
{
    /// <summary>
    /// Repositório do usuário (entidade unificada Usuario).
    /// Mantém o nome IClienteRepositorio por compatibilidade com a camada de serviço.
    /// </summary>
    public interface IClienteRepositorio : IRepositorio<Usuario>
    {
        Task<Usuario?> ObterPorEmailAsync(string email);

        Task<List<Usuario>> ObterPorProximidadeAsync(decimal latitude, decimal longitude, decimal raioKm);

        Task AtualizarLocalizacaoAsync(Guid clienteId, decimal latitude, decimal longitude);

        Task<List<Usuario>> ListarAtivosAsync();

        Task AtualizarUltimoLoginAsync(Guid clienteId, DateTime ultimoLogin);

        /// <summary>Obtém o usuário proprietário de uma loja (ou vinculado como vendedor).</summary>
        Task<Usuario?> ObterPorIdComLojaAsync(Guid id);

        /// <summary>Obtém o usuário pelo token de confirmação de e-mail.</summary>
        Task<Usuario?> ObterPorTokenConfirmacaoAsync(string token);

        /// <summary>Obtém usuário por e-mail para verificação de duplicidade no cadastro (ignora Ativo).</summary>
        Task<Usuario?> ObterPorEmailCadastroAsync(string email);

        Task<Usuario?> ObterPorTokenRecuperacaoSenhaAsync(string token);
    }
}
