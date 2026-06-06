using Pc.Dominio.Entities.Usuarios;
using Pc.Repositorio.Interfaces;
using Pc.Servico.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Pc.Servico.Implementacoes
{
    /// <summary>
    /// Serviço de Admin: lógica de negócio para autenticação e gerenciamento de administradores
    /// Validações: email único, senha válida, dados obrigatórios
    /// </summary>
    public class AdminServico : IAdminServico
    {
        private readonly IAdminRepositorio _adminRepositorio;

        public AdminServico(IAdminRepositorio adminRepositorio)
        {
            _adminRepositorio = adminRepositorio;
        }

        /// <summary>
        /// Registra um novo administrador
        /// Validações: email único, NomeUsuario obrigatório, senha mínima 6 caracteres
        /// Apenas superadmins podem criar novos admins (verificar em Controller)
        /// </summary>
        public async Task<Admin> RegistrarAsync(Admin admin)
        {
            if (string.IsNullOrWhiteSpace(admin.Email))
                throw new Exception("Email é obrigatório.");

            if (string.IsNullOrWhiteSpace(admin.NomeUsuario))
                throw new Exception("Nome de usuário é obrigatório.");

            if (string.IsNullOrWhiteSpace(admin.SenhaHash) || admin.SenhaHash.Length < 6)
                throw new Exception("Senha deve ter pelo menos 6 caracteres.");

            var emailExiste = await _adminRepositorio.ListarAsync();
            if (emailExiste.Exists(a => a.Email.ToLower() == admin.Email.ToLower()))
                throw new Exception("Email já registrado.");

            admin.Ativo = true;
            admin.DataCriacao = DateTime.UtcNow;
            admin.Tipo = Pc.Dominio.Enums.TipoUsuario.Admin;

            return await _adminRepositorio.AdicionarAsync(admin);
        }

        /// <summary>
        /// Valida credenciais de login
        /// Retorna o admin se credenciais forem válidas
        /// TODO: Usar bcrypt para comparação de senha
        /// </summary>
        public async Task<Admin?> ValidarLoginAsync(string email, string senha)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
                return null;

            var admin = await _adminRepositorio.ObterPorEmailAsync(email);

            if (admin == null || admin.SenhaHash != senha) // TODO: bcrypt
                return null;

            admin.UltimoLogin = DateTime.UtcNow;
            await _adminRepositorio.AtualizarAsync(admin);

            return admin;
        }

        /// <summary>
        /// Obtém admin por ID
        /// </summary>
        public async Task<Admin?> ObterPorIdAsync(Guid id)
        {
            return await _adminRepositorio.ObterPorIdAsync(id);
        }

        /// <summary>
        /// Obtém admin por email
        /// </summary>
        public async Task<Admin?> ObterPorEmailAsync(string email)
        {
            return await _adminRepositorio.ObterPorEmailAsync(email);
        }

        /// <summary>
        /// Lista todos os admins ativos
        /// </summary>
        public async Task<List<Admin>> ListarAtivosAsync()
        {
            return await _adminRepositorio.ListarAtivosAsync();
        }

        /// <summary>
        /// Lista todos os admins
        /// </summary>
        public async Task<List<Admin>> ListarAsync()
        {
            return await _adminRepositorio.ListarAsync();
        }

        /// <summary>
        /// Atualiza dados do admin
        /// </summary>
        public async Task AtualizarAsync(Admin admin)
        {
            admin.DataAtualizacao = DateTime.UtcNow;
            await _adminRepositorio.AtualizarAsync(admin);
        }

        /// <summary>
        /// Altera a senha do admin
        /// TODO: Usar bcrypt para comparação
        /// </summary>
        public async Task AlterarSenhaAsync(Guid adminId, string senhaAtual, string novaSenha)
        {
            if (string.IsNullOrWhiteSpace(novaSenha) || novaSenha.Length < 6)
                throw new Exception("Senha deve ter pelo menos 6 caracteres.");

            var admin = await _adminRepositorio.ObterPorIdAsync(adminId);
            if (admin == null)
                throw new Exception("Admin não encontrado.");

            if (admin.SenhaHash != senhaAtual) // TODO: bcrypt
                throw new Exception("Senha atual incorreta.");

            admin.SenhaHash = novaSenha;
            await _adminRepositorio.AtualizarAsync(admin);
        }

        /// <summary>
        /// Remove/desativa admin (soft delete)
        /// </summary>
        public async Task RemoverAsync(Guid id)
        {
            var admin = await _adminRepositorio.ObterPorIdAsync(id);
            if (admin != null)
            {
                admin.Ativo = false;
                await _adminRepositorio.AtualizarAsync(admin);
            }
        }
    }
}
