using Microsoft.EntityFrameworkCore;
using Pc.Dominio.Entities.Usuarios;
using Pc.Dominio.Enums;
using Pc.Infraestrutura;
using Pc.Servico.Interfaces;

namespace Pc.Servico.Implementacoes
{
    public class LojistaServico : ILojistaServico
    {
        private readonly AppDbContext _context;
        private readonly ISenhaServico _senhaServico;

        public LojistaServico(AppDbContext context, ISenhaServico senhaServico)
        {
            _context = context;
            _senhaServico = senhaServico;
        }

        public async Task<Lojista> RegistrarAsync(
            string nomeUsuario,
            string email,
            string senha,
            string? telefone = null,
            string? cargo = null)
        {
            var emailNormalizado = email.Trim().ToLowerInvariant();

            if (await _context.Usuarios.AnyAsync(u => u.Email.ToLower() == emailNormalizado))
                throw new InvalidOperationException("Email já registrado.");

            var usuario = new Usuario
            {
                NomeUsuario = nomeUsuario.Trim(),
                Email = email.Trim(),
                SenhaHash = _senhaServico.Hash(senha),
                Telefone = telefone,
                Tipo = TipoUsuario.Lojista,
                Ativo = true,
                DataCriacao = DateTime.UtcNow,
            };

            var lojista = new Lojista
            {
                Usuario = usuario,
                Cargo = cargo ?? "Gerente",
                LojaId = Guid.Empty,
                Ativo = true,
                DataCriacao = DateTime.UtcNow,
            };

            _context.Lojistas.Add(lojista);
            await _context.SaveChangesAsync();

            return await ObterPorIdAsync(lojista.Id) ?? lojista;
        }

        public async Task<Lojista?> ValidarLoginAsync(string email, string senha)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
                return null;

            var lojista = await _context.Lojistas
                .Include(l => l.Usuario)
                .Include(l => l.Loja)
                .FirstOrDefaultAsync(l =>
                    l.Usuario != null &&
                    l.Usuario.Email.ToLower() == email.Trim().ToLower() &&
                    l.Usuario.Tipo == TipoUsuario.Lojista &&
                    l.Usuario.Ativo);

            if (lojista?.Usuario == null)
                return null;

            if (!_senhaServico.Verificar(senha, lojista.Usuario.SenhaHash))
                return null;

            if (!lojista.Usuario.SenhaHash.StartsWith("$2"))
                lojista.Usuario.SenhaHash = _senhaServico.Hash(senha);

            lojista.Usuario.UltimoLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return lojista;
        }

        public async Task<Lojista?> ObterPorIdAsync(Guid id)
        {
            return await _context.Lojistas
                .Include(l => l.Usuario)
                .Include(l => l.Loja)
                .FirstOrDefaultAsync(l => l.Id == id);
        }
    }
}
