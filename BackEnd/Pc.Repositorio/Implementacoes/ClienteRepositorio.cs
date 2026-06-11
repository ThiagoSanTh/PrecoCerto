using Microsoft.EntityFrameworkCore;
using Pc.Dominio.Entities.Usuarios;
using Pc.Infraestrutura;
using Pc.Repositorio.Interfaces;

namespace Pc.Repositorio.Implementacoes
{
    /// <summary>
    /// Persistência do usuário (entidade unificada Usuario) com consultas de
    /// autenticação e proximidade.
    /// </summary>
    public class ClienteRepositorio : Repositorio<Usuario>, IClienteRepositorio
    {
        public ClienteRepositorio(AppDbContext context) : base(context)
        {
        }

        public override async Task<Usuario?> ObterPorIdAsync(Guid id)
        {
            return await _context.Usuarios
                .FirstOrDefaultAsync(c => c.Id == id && c.Ativo);
        }

        public async Task<Usuario?> ObterPorIdComLojaAsync(Guid id)
        {
            return await _context.Usuarios
                .Include(u => u.LojaPropria)
                .FirstOrDefaultAsync(c => c.Id == id && c.Ativo);
        }

        public async Task<Usuario?> ObterPorEmailAsync(string email)
        {
            return await _context.Usuarios
                .Include(u => u.LojaPropria)
                .FirstOrDefaultAsync(c => c.Email.ToLower() == email.ToLower() && c.Ativo);
        }

        public async Task<Usuario?> ObterPorEmailCadastroAsync(string email)
        {
            return await _context.Usuarios
                .FirstOrDefaultAsync(c => c.Email.ToLower() == email.ToLower());
        }

        public async Task<Usuario?> ObterPorTokenConfirmacaoAsync(string token)
        {
            return await _context.Usuarios
                .FirstOrDefaultAsync(c => c.TokenConfirmacao == token);
        }

        public async Task<Usuario?> ObterPorTokenRecuperacaoSenhaAsync(string token)
        {
            return await _context.Usuarios
                .FirstOrDefaultAsync(c =>
                    c.TokenRecuperacaoSenha == token
                    && c.TokenRecuperacaoExpira.HasValue
                    && c.TokenRecuperacaoExpira > DateTime.UtcNow);
        }

        public async Task<List<Usuario>> ListarAtivosAsync()
        {
            return await _context.Usuarios
                .Where(c => c.Ativo)
                .ToListAsync();
        }

        public async Task<List<Usuario>> ObterPorProximidadeAsync(decimal latitude, decimal longitude, decimal raioKm)
        {
            var clientes = await _context.Usuarios
                .Where(c => c.Ativo)
                .ToListAsync();

            return clientes
                .Where(c => c.LatitudeAtual.HasValue && c.LongitudeAtual.HasValue)
                .Where(c => CalcularDistancia(
                    latitude, longitude,
                    c.LatitudeAtual!.Value, c.LongitudeAtual!.Value) <= raioKm)
                .ToList();
        }

        public async Task AtualizarLocalizacaoAsync(Guid clienteId, decimal latitude, decimal longitude)
        {
            var cliente = await ObterPorIdAsync(clienteId);
            if (cliente != null)
            {
                cliente.LatitudeAtual = latitude;
                cliente.LongitudeAtual = longitude;
                await AtualizarAsync(cliente);
            }
        }

        public async Task AtualizarUltimoLoginAsync(Guid clienteId, DateTime ultimoLogin)
        {
            await _context.Usuarios
                .Where(c => c.Id == clienteId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.UltimoLogin, ultimoLogin)
                    .SetProperty(c => c.DataAtualizacao, ultimoLogin));
        }

        private static decimal CalcularDistancia(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
        {
            const decimal raioTerraKm = 6371m;

            var dLat = (lat2 - lat1) * (decimal)Math.PI / 180m;
            var dLon = (lon2 - lon1) * (decimal)Math.PI / 180m;
            var a = (decimal)Math.Sin((double)dLat / 2) * (decimal)Math.Sin((double)dLat / 2) +
                    (decimal)Math.Cos((double)lat1 * Math.PI / 180) * (decimal)Math.Cos((double)lat2 * Math.PI / 180) *
                    (decimal)Math.Sin((double)dLon / 2) * (decimal)Math.Sin((double)dLon / 2);
            var c = 2m * (decimal)Math.Atan2(Math.Sqrt((double)a), Math.Sqrt((double)(1 - a)));

            return raioTerraKm * c;
        }
    }
}
