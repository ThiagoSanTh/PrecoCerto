using Microsoft.EntityFrameworkCore;
using Pc.Dominio.Entities.Usuarios;
using Pc.Infraestrutura;
using Pc.Repositorio.Interfaces;

namespace Pc.Repositorio.Implementacoes
{
    /// <summary>
    /// Persistência de Cliente com consultas de autenticação e proximidade.
    /// </summary>
    public class ClienteRepositorio : Repositorio<Cliente>, IClienteRepositorio
    {
        public ClienteRepositorio(AppDbContext context) : base(context)
        {
        }

        public override async Task<Cliente?> ObterPorIdAsync(Guid id)
        {
            return await _context.Clientes
                .FirstOrDefaultAsync(c => c.Id == id && c.Ativo);
        }

        public async Task<Cliente?> ObterPorEmailAsync(string email)
        {
            return await _context.Clientes
                .FirstOrDefaultAsync(c => c.Email.ToLower() == email.ToLower() && c.Ativo);
        }

        public async Task<List<Cliente>> ListarAtivosAsync()
        {
            return await _context.Clientes
                .Where(c => c.Ativo)
                .ToListAsync();
        }

        public async Task<List<Cliente>> ObterPorProximidadeAsync(decimal latitude, decimal longitude, decimal raioKm)
        {
            var clientes = await _context.Clientes
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
            await _context.Clientes
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
