using Microsoft.EntityFrameworkCore;
using Pc.Dominio.Entities.Usuarios;
using Pc.Dominio.Enums;
using Pc.Infraestrutura;
using Pc.Servico.Interfaces;

namespace Pc.Servico.Implementacoes
{
    public class ClienteServico : IClienteServico
    {
        private readonly AppDbContext _context;
        private readonly ISenhaServico _senhaServico;

        public ClienteServico(AppDbContext context, ISenhaServico senhaServico)
        {
            _context = context;
            _senhaServico = senhaServico;
        }

        public async Task<Cliente> RegistrarAsync(
            string nomeUsuario,
            string email,
            string senha,
            string? telefone = null,
            decimal? latitudeAtual = null,
            decimal? longitudeAtual = null)
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
                Tipo = TipoUsuario.Cliente,
                Ativo = true,
                DataCriacao = DateTime.UtcNow,
            };

            var cliente = new Cliente
            {
                Usuario = usuario,
                LatitudeAtual = latitudeAtual,
                LongitudeAtual = longitudeAtual,
                Ativo = true,
                DataCriacao = DateTime.UtcNow,
            };

            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            return await ObterPorIdAsync(cliente.Id) ?? cliente;
        }

        public async Task<Cliente?> ValidarLoginAsync(string email, string senha)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
                return null;

            var cliente = await _context.Clientes
                .Include(c => c.Usuario)
                .FirstOrDefaultAsync(c =>
                    c.Usuario != null &&
                    c.Usuario.Email.ToLower() == email.Trim().ToLower() &&
                    c.Usuario.Tipo == TipoUsuario.Cliente &&
                    c.Usuario.Ativo);

            if (cliente?.Usuario == null)
                return null;

            if (!_senhaServico.Verificar(senha, cliente.Usuario.SenhaHash))
                return null;

            if (!cliente.Usuario.SenhaHash.StartsWith("$2"))
                cliente.Usuario.SenhaHash = _senhaServico.Hash(senha);

            cliente.Usuario.UltimoLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return cliente;
        }

        public async Task<Cliente?> ObterPorIdAsync(Guid id)
        {
            return await _context.Clientes
                .Include(c => c.Usuario)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task AlterarSenhaAsync(Guid id, string senhaAtual, string novaSenha)
        {
            var cliente = await ObterPorIdAsync(id)
                ?? throw new InvalidOperationException("Cliente não encontrado.");

            if (cliente.Usuario == null ||
                !_senhaServico.Verificar(senhaAtual, cliente.Usuario.SenhaHash))
                throw new InvalidOperationException("Senha atual incorreta.");

            cliente.Usuario.SenhaHash = _senhaServico.Hash(novaSenha);
            await _context.SaveChangesAsync();
        }
    }
}
