using Microsoft.EntityFrameworkCore;
using Pc.Dominio.Entities.MotorIA;
using Pc.Dominio.Entities.Rag;
using Pc.Infraestrutura;
using Pc.Repositorio.Implementacoes;
using Xunit;

namespace Pc.Servico.Tests.MotorIA
{
    public class SessaoChatRepositorioTests
    {
        private sealed class TestAppDbContext(DbContextOptions<AppDbContext> options) : AppDbContext(options)
        {
            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);
                modelBuilder.Entity<DocumentoRag>().Ignore(d => d.Embedding);
            }
        }

        private static AppDbContext CriarContexto()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new TestAppDbContext(options);
        }

        [Fact]
        public async Task ObterOuCriarAsync_SessaoIdNull_CriaNovaSessao()
        {
            await using var ctx = CriarContexto();
            var repo = new SessaoChatRepositorio(ctx);
            var usuarioId = Guid.NewGuid();

            var sessao = await repo.ObterOuCriarAsync(null, usuarioId);

            Assert.NotEqual(Guid.Empty, sessao.Id);
            Assert.Equal(usuarioId, sessao.UsuarioId);
            Assert.Equal(1, await ctx.SessoesChat.CountAsync());
        }

        [Fact]
        public async Task ObterOuCriarAsync_SessaoExistente_ReutilizaSemDuplicar()
        {
            await using var ctx = CriarContexto();
            var repo = new SessaoChatRepositorio(ctx);
            var usuarioId = Guid.NewGuid();

            var criada = await repo.ObterOuCriarAsync(null, usuarioId);
            var reutilizada = await repo.ObterOuCriarAsync(criada.Id, usuarioId);

            Assert.Equal(criada.Id, reutilizada.Id);
            Assert.Equal(1, await ctx.SessoesChat.CountAsync());
        }

        [Fact]
        public async Task AdicionarTurnoAsync_PersisteMensagensEAtualizaUltimaInteracao()
        {
            await using var ctx = CriarContexto();
            var repo = new SessaoChatRepositorio(ctx);
            var sessao = await repo.ObterOuCriarAsync(null, null);
            var antes = DateTime.UtcNow.AddMinutes(-10);
            sessao.UltimaInteracaoEm = antes;
            await ctx.SaveChangesAsync();

            await repo.AdicionarTurnoAsync(sessao.Id, "oi", "ola");

            var mensagens = await ctx.MensagensChat
                .Where(m => m.SessaoChatId == sessao.Id)
                .OrderBy(m => m.DataCriacao)
                .ToListAsync();

            Assert.Equal(2, mensagens.Count);
            Assert.Equal("user", mensagens[0].Papel);
            Assert.Equal("oi", mensagens[0].Texto);
            Assert.Equal("assistant", mensagens[1].Papel);
            Assert.Equal("ola", mensagens[1].Texto);

            var sessaoAtualizada = await ctx.SessoesChat.FirstAsync(s => s.Id == sessao.Id);
            Assert.True(sessaoAtualizada.UltimaInteracaoEm > antes);
        }

        [Fact]
        public async Task ListarUltimasMensagensAsync_RespeitaLimiteEOrdemCronologicaCrescente()
        {
            await using var ctx = CriarContexto();
            var repo = new SessaoChatRepositorio(ctx);
            var sessao = await repo.ObterOuCriarAsync(null, null);
            var baseTime = DateTime.UtcNow.AddMinutes(-5);

            for (var i = 1; i <= 5; i++)
            {
                ctx.MensagensChat.Add(new MensagemChat
                {
                    SessaoChatId = sessao.Id,
                    Papel = i % 2 == 1 ? "user" : "assistant",
                    Texto = $"m{i}",
                    DataCriacao = baseTime.AddSeconds(i)
                });
            }
            await ctx.SaveChangesAsync();

            var ultimas = await repo.ListarUltimasMensagensAsync(sessao.Id, limite: 3);

            Assert.Equal(new[] { "m3", "m4", "m5" }, ultimas.Select(m => m.Texto));
        }
    }
}
