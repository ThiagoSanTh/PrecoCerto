using Microsoft.EntityFrameworkCore;
using Pc.Dominio.Entities.MotorIA;
using Pc.Infraestrutura;
using Pc.Repositorio.Interfaces;

namespace Pc.Repositorio.Implementacoes
{
    public class SessaoChatRepositorio : ISessaoChatRepositorio
    {
        private readonly AppDbContext _context;

        public SessaoChatRepositorio(AppDbContext context)
        {
            _context = context;
        }

        public async Task<SessaoChat> ObterOuCriarAsync(Guid? sessaoId, Guid? usuarioId, CancellationToken ct = default)
        {
            if (sessaoId is { } id && id != Guid.Empty)
            {
                var existente = await _context.SessoesChat
                    .FirstOrDefaultAsync(s => s.Id == id && s.Ativo, ct);
                if (existente is not null)
                    return existente;
            }

            var nova = new SessaoChat
            {
                Id = sessaoId is { } sid && sid != Guid.Empty ? sid : Guid.NewGuid(),
                UsuarioId = usuarioId
            };
            _context.SessoesChat.Add(nova);
            await _context.SaveChangesAsync(ct);
            return nova;
        }

        public async Task<List<MensagemChat>> ListarUltimasMensagensAsync(Guid sessaoId, int limite = 10, CancellationToken ct = default)
        {
            var itens = await _context.MensagensChat
                .AsNoTracking()
                .Where(m => m.SessaoChatId == sessaoId && m.Ativo)
                .OrderByDescending(m => m.DataCriacao)
                .Take(Math.Clamp(limite, 1, 50))
                .ToListAsync(ct);

            itens.Reverse();
            return itens;
        }

        public async Task AdicionarTurnoAsync(Guid sessaoId, string textoUsuario, string textoAssistente, CancellationToken ct = default)
        {
            var agora = DateTime.UtcNow;

            _context.MensagensChat.AddRange(
                new MensagemChat
                {
                    SessaoChatId = sessaoId,
                    Papel = "user",
                    Texto = textoUsuario,
                    DataCriacao = agora
                },
                new MensagemChat
                {
                    SessaoChatId = sessaoId,
                    Papel = "assistant",
                    Texto = textoAssistente,
                    DataCriacao = agora.AddTicks(1)
                });

            var sessao = await _context.SessoesChat.FirstOrDefaultAsync(s => s.Id == sessaoId, ct);
            if (sessao is not null)
            {
                sessao.UltimaInteracaoEm = agora;
                sessao.DataAtualizacao = agora;
            }

            await _context.SaveChangesAsync(ct);
        }
    }
}
