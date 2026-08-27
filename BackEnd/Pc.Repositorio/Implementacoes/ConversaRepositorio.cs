using Microsoft.EntityFrameworkCore;
using Pc.Dominio.Entities.Interacoes;
using Pc.Dominio.Enums;
using Pc.Infraestrutura;
using Pc.Repositorio.Interfaces;

namespace Pc.Repositorio.Implementacoes
{
    public class ConversaRepositorio : IConversaRepositorio
    {
        private readonly AppDbContext _context;

        public ConversaRepositorio(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Conversa?> ObterPorIdAsync(Guid id)
        {
            return await _context.Conversas
                .Include(c => c.Cliente)
                .Include(c => c.Loja)
                .FirstOrDefaultAsync(c => c.Id == id && c.Ativo);
        }

        public async Task<Conversa?> ObterPorClienteELojaAsync(Guid clienteId, Guid lojaId)
        {
            return await _context.Conversas
                .Include(c => c.Cliente)
                .Include(c => c.Loja)
                .FirstOrDefaultAsync(c => c.ClienteId == clienteId && c.LojaId == lojaId && c.Ativo);
        }

        public async Task<List<Conversa>> ListarPorClienteAsync(Guid clienteId)
        {
            return await _context.Conversas
                .Include(c => c.Loja)
                .Where(c => c.ClienteId == clienteId && c.Ativo)
                .OrderByDescending(c => c.UltimaMensagemEm ?? c.DataCriacao)
                .ToListAsync();
        }

        public async Task<List<Conversa>> ListarPorLojaAsync(Guid lojaId)
        {
            return await _context.Conversas
                .Include(c => c.Cliente)
                .Where(c => c.LojaId == lojaId && c.Ativo)
                .OrderByDescending(c => c.UltimaMensagemEm ?? c.DataCriacao)
                .ToListAsync();
        }

        public async Task<Conversa> AdicionarAsync(Conversa conversa)
        {
            _context.Conversas.Add(conversa);
            await _context.SaveChangesAsync();
            return conversa;
        }

        public async Task AtualizarAsync(Conversa conversa)
        {
            conversa.DataAtualizacao = DateTime.UtcNow;
            _context.Conversas.Update(conversa);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Mensagem>> ListarMensagensAsync(
            Guid conversaId, DateTime? apos, DateTime? antes = null, int pageSize = 50)
        {
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _context.Mensagens
                .AsNoTracking()
                .Where(m => m.ConversaId == conversaId);

            if (apos.HasValue)
            {
                return await query
                    .Where(m => m.EnviadaEm > apos.Value)
                    .OrderBy(m => m.EnviadaEm)
                    .Take(pageSize)
                    .ToListAsync();
            }

            if (antes.HasValue)
                query = query.Where(m => m.EnviadaEm < antes.Value);

            var items = await query
                .OrderByDescending(m => m.EnviadaEm)
                .Take(pageSize)
                .ToListAsync();

            items.Reverse();
            return items;
        }

        public async Task<Dictionary<Guid, int>> ContarNaoLidasPorConversasAsync(
            Guid usuarioId, bool ehLojista, Guid? lojaId)
        {
            IQueryable<Mensagem> query = _context.Mensagens
                .AsNoTracking()
                .Include(m => m.Conversa)
                .Where(m => m.Conversa != null && m.Conversa.Ativo && m.RemetenteId != usuarioId && !m.Lida);

            if (ehLojista && lojaId.HasValue)
            {
                query = query.Where(m => m.Conversa!.LojaId == lojaId.Value);
            }
            else
            {
                query = query.Where(m => m.Conversa!.ClienteId == usuarioId);
            }

            var rows = await query
                .GroupBy(m => m.ConversaId)
                .Select(g => new { ConversaId = g.Key, Total = g.Count() })
                .ToListAsync();

            return rows.ToDictionary(r => r.ConversaId, r => r.Total);
        }

        public async Task<Mensagem> AdicionarMensagemAsync(Mensagem mensagem)
        {
            _context.Mensagens.Add(mensagem);
            await _context.SaveChangesAsync();
            return mensagem;
        }

        public async Task MarcarMensagensComoLidasAsync(Guid conversaId, Guid leitorId)
        {
            var mensagens = await _context.Mensagens
                .Where(m => m.ConversaId == conversaId && m.RemetenteId != leitorId && !m.Lida)
                .ToListAsync();

            foreach (var msg in mensagens)
                msg.Lida = true;

            if (mensagens.Count > 0)
                await _context.SaveChangesAsync();
        }

        public async Task MarcarMensagensComoRecebidasAsync(Guid conversaId, Guid leitorId)
        {
            var agora = DateTime.UtcNow;
            var mensagens = await _context.Mensagens
                .Where(m => m.ConversaId == conversaId
                    && m.RemetenteId != leitorId
                    && m.RecebidaEm == null)
                .ToListAsync();

            foreach (var msg in mensagens)
                msg.RecebidaEm = agora;

            if (mensagens.Count > 0)
                await _context.SaveChangesAsync();
        }

        public async Task<int> ContarNaoLidasAsync(Guid usuarioId, bool ehLojista, Guid? lojaId)
        {
            if (ehLojista && lojaId.HasValue)
            {
                return await _context.Mensagens
                    .Include(m => m.Conversa)
                    .Where(m => m.Conversa!.LojaId == lojaId.Value
                        && m.Conversa.Ativo
                        && m.RemetenteId != usuarioId
                        && !m.Lida)
                    .CountAsync();
            }

            return await _context.Mensagens
                .Include(m => m.Conversa)
                .Where(m => m.Conversa!.ClienteId == usuarioId
                    && m.Conversa.Ativo
                    && m.RemetenteId != usuarioId
                    && !m.Lida)
                .CountAsync();
        }

        public async Task<bool> TemNaoLidasAsync(
            Guid usuarioId, bool ehLojista, Guid? lojaId, DateTime? desde = null)
        {
            IQueryable<Mensagem> query = _context.Mensagens
                .AsNoTracking()
                .Include(m => m.Conversa)
                .Where(m => m.Conversa != null
                    && m.Conversa.Ativo
                    && m.RemetenteId != usuarioId
                    && !m.Lida);

            if (desde.HasValue)
                query = query.Where(m => m.EnviadaEm > desde.Value);

            if (ehLojista && lojaId.HasValue)
            {
                return await query
                    .Where(m => m.Conversa!.LojaId == lojaId.Value)
                    .AnyAsync();
            }

            return await query
                .Where(m => m.Conversa!.ClienteId == usuarioId)
                .AnyAsync();
        }
    }
}
