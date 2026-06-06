using Pc.Dominio.Entities.Usuarios;
using Pc.Repositorio.Interfaces;
using Pc.Servico.Interfaces;

namespace Pc.Servico.Implementacoes
{
    /// <summary>
    /// Serviço de Lojista: lógica de negócio para autenticação e gerenciamento de lojistas
    /// Validações: email único, senha válida, dados obrigatórios
    /// Consolidado - sem intermediário Usuario
    /// </summary>
    public class LojistaServico : ILojistaServico
    {
        private readonly ILojistaRepositorio _lojistaRepositorio;

        public LojistaServico(ILojistaRepositorio lojistaRepositorio)
        {
            _lojistaRepositorio = lojistaRepositorio;
        }

        /// <summary>
        /// Registra um novo lojista
        /// Validações: email único, NomeUsuario obrigatório e senha mínima de 6 caracteres
        /// </summary>
        public async Task<Lojista> RegistrarAsync(Lojista lojista)
        {
            if (string.IsNullOrWhiteSpace(lojista.Email))
                throw new Exception("Email é obrigatório.");

            if (string.IsNullOrWhiteSpace(lojista.NomeUsuario))
                throw new Exception("Nome de usuário é obrigatório.");

            if (string.IsNullOrWhiteSpace(lojista.SenhaHash) || lojista.SenhaHash.Length < 6)
                throw new Exception("Senha deve ter pelo menos 6 caracteres.");

            var lojistas = await _lojistaRepositorio.ListarAsync();
            if (lojistas.Exists(l => l.Email.ToLower() == lojista.Email.ToLower()))
                throw new Exception("Email já registrado.");

            lojista.Ativo = true;
            lojista.DataCriacao = DateTime.UtcNow;
            lojista.Tipo = Pc.Dominio.Enums.TipoUsuario.Lojista;

            return await _lojistaRepositorio.AdicionarAsync(lojista);
        }

        /// <summary>
        /// Valida credenciais de login
        /// Retorna o lojista se credenciais forem válidas
        /// TODO: Usar bcrypt para comparação de senha
        /// </summary>
        public async Task<Lojista?> ValidarLoginAsync(string email, string senha)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
                return null;

            var lojista = await _lojistaRepositorio.ObterPorEmailAsync(email);

            if (lojista == null || lojista.SenhaHash != senha) // TODO: bcrypt
                return null;

            lojista.UltimoLogin = DateTime.UtcNow;
            await _lojistaRepositorio.AtualizarAsync(lojista);

            return lojista;
        }

        /// <summary>
        /// Obtém lojista por ID
        /// </summary>
        public async Task<Lojista?> ObterPorIdAsync(Guid id)
        {
            return await _lojistaRepositorio.ObterPorIdAsync(id);
        }

        /// <summary>
        /// Obtém lojista por email
        /// </summary>
        public async Task<Lojista?> ObterPorEmailAsync(string email)
        {
            return await _lojistaRepositorio.ObterPorEmailAsync(email);
        }

        /// <summary>
        /// Lista todos os lojistas ativos
        /// </summary>
        public async Task<List<Lojista>> ListarAtivosAsync()
        {
            return await _lojistaRepositorio.ListarAtivosAsync();
        }

        /// <summary>
        /// Lista todos os lojistas
        /// </summary>
        public async Task<List<Lojista>> ListarAsync()
        {
            return await _lojistaRepositorio.ListarAsync();
        }

        /// <summary>
        /// Lista lojistas de uma loja específica
        /// </summary>
        public async Task<List<Lojista>> ListarPorLojaAsync(Guid lojaId)
        {
            return await _lojistaRepositorio.ListarPorLojaAsync(lojaId);
        }

        /// <summary>
        /// Atualiza dados do lojista
        /// </summary>
        public async Task AtualizarAsync(Lojista lojista)
        {
            lojista.DataAtualizacao = DateTime.UtcNow;
            await _lojistaRepositorio.AtualizarAsync(lojista);
        }

        /// <summary>
        /// Altera a senha do lojista
        /// TODO: Usar bcrypt para comparação
        /// </summary>
        public async Task AlterarSenhaAsync(Guid lojistaId, string senhaAtual, string novaSenha)
        {
            if (string.IsNullOrWhiteSpace(novaSenha) || novaSenha.Length < 6)
                throw new Exception("Senha deve ter pelo menos 6 caracteres.");

            var lojista = await _lojistaRepositorio.ObterPorIdAsync(lojistaId);
            if (lojista == null)
                throw new Exception("Lojista não encontrado.");

            if (lojista.SenhaHash != senhaAtual) // TODO: bcrypt
                throw new Exception("Senha atual incorreta.");

            lojista.SenhaHash = novaSenha;
            await _lojistaRepositorio.AtualizarAsync(lojista);
        }

        /// <summary>
        /// Remove/desativa lojista (soft delete)
        /// </summary>
        public async Task RemoverAsync(Guid id)
        {
            var lojista = await _lojistaRepositorio.ObterPorIdAsync(id);
            if (lojista != null)
            {
                lojista.Ativo = false;
                await _lojistaRepositorio.AtualizarAsync(lojista);
            }
        }

    }
}
