using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Pc.Dominio.Entities.Estabelecimentos;
using Pc.Dominio.Entities.Rag;
using Pc.Dominio.Entities.Usuarios;
using Pc.Dominio.Enums;
using Pc.Infraestrutura;
using Pc.Repositorio.Implementacoes;
using Pc.Servico.Implementacoes;
using Xunit;

namespace Pc.Servico.Tests
{
    public class LojaAcessoSqliteTests
    {
        private sealed class ContextoSqlite(DbContextOptions<AppDbContext> options) : AppDbContext(options)
        {
            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);
                modelBuilder.Ignore<DocumentoRag>();
            }
        }

        [Fact]
        public async Task Dono_e_vendedor_pertencem_no_mesmo_dbcontext()
        {
            await using var conexao = new SqliteConnection("Data Source=:memory:");
            await conexao.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(conexao)
                .AddInterceptors(new SeguraLeitura())
                .Options;
            await using var ctx = new ContextoSqlite(options);
            await ctx.Database.EnsureCreatedAsync();

            var dono = new Usuario
            {
                NomeUsuario = "Dono",
                Email = "dono@loja.test",
                SenhaHash = "x",
                Papel = PapelUsuario.Lojista,
                Tipo = TipoUsuario.Lojista
            };
            var vendedor = new Usuario
            {
                NomeUsuario = "Vend",
                Email = "vend@loja.test",
                SenhaHash = "x",
                Papel = PapelUsuario.Vendedor,
                Tipo = TipoUsuario.Vendedor
            };
            var loja = new Loja
            {
                NomeFantasia = "Loja",
                UsuarioId = dono.Id
            };
            vendedor.LojaVinculadaId = loja.Id;
            ctx.Usuarios.AddRange(dono, vendedor);
            ctx.Lojas.Add(loja);
            await ctx.SaveChangesAsync();

            var acesso = new LojaAcesso(new LojaRepositorio(ctx), new ClienteRepositorio(ctx));

            Assert.True(await acesso.PertenceAsync(dono.Id, loja.Id));
            Assert.True(await acesso.PertenceAsync(vendedor.Id, loja.Id));
        }

        [Fact]
        public async Task Dono_inativo_nao_pertence_a_loja()
        {
            await using var conexao = new SqliteConnection("Data Source=:memory:");
            await conexao.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(conexao)
                .Options;
            await using var ctx = new ContextoSqlite(options);
            await ctx.Database.EnsureCreatedAsync();

            var dono = new Usuario
            {
                NomeUsuario = "Dono",
                Email = "dono-inativo@loja.test",
                SenhaHash = "x",
                Papel = PapelUsuario.Lojista,
                Tipo = TipoUsuario.Lojista,
                Ativo = false
            };
            var loja = new Loja
            {
                NomeFantasia = "Loja",
                UsuarioId = dono.Id
            };
            ctx.Usuarios.Add(dono);
            ctx.Lojas.Add(loja);
            await ctx.SaveChangesAsync();

            var acesso = new LojaAcesso(new LojaRepositorio(ctx), new ClienteRepositorio(ctx));

            Assert.False(await acesso.PertenceAsync(dono.Id, loja.Id));
        }

        /// <summary>
        /// Segura a primeira leitura para as duas consultas do WhenAll
        /// se sobreporem no mesmo DbContext.
        /// </summary>
        private sealed class SeguraLeitura : DbCommandInterceptor
        {
            public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
                DbCommand command,
                CommandEventData eventData,
                InterceptionResult<DbDataReader> result,
                CancellationToken cancellationToken = default)
            {
                await Task.Delay(50, cancellationToken);
                return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
            }
        }
    }
}
