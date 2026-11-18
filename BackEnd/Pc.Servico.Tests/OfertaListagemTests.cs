using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Pc.Dominio.Comum;
using Pc.Dominio.Entities.Catalogo;
using Pc.Dominio.Entities.Estabelecimentos;
using Pc.Dominio.Entities.Rag;
using Pc.Infraestrutura;
using Pc.Repositorio.Implementacoes;
using Pc.Servico.Implementacoes;
using Pc.Servico.Interfaces;
using Pc.WebApi.Controllers;
using Pc.WebApi.DTOs.Comum;
using Pc.WebApi.DTOs.Estabelecimentos;
using Xunit;

namespace Pc.Servico.Tests
{
    public class OfertaListagemTests
    {
        private sealed class TestAppDbContext(DbContextOptions<AppDbContext> options) : AppDbContext(options)
        {
            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);
                modelBuilder.Entity<DocumentoRag>().Ignore(d => d.Embedding);
            }
        }

        [Fact]
        public async Task Listar_ComLojaId_FiltraNoBancoEMantemEnvelope()
        {
            var lojaA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var lojaB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            var agora = new DateTime(2026, 11, 18, 12, 0, 0, DateTimeKind.Utc);
            await using var ctx = CriarContexto();
            ctx.Ofertas.AddRange(
                Oferta(ctx, lojaB, 5m, 1, agora),
                Oferta(ctx, lojaA, 10m, 7, agora.AddMinutes(-1)),
                Oferta(ctx, lojaA, 8m, 3, agora.AddMinutes(-2)));
            await ctx.SaveChangesAsync();

            var ok = Assert.IsType<OkObjectResult>(await Listar(ctx, 1, 1, lojaA));
            var corpo = Assert.IsType<PaginacaoRespostaDto<OfertaRespostaDto>>(ok.Value);

            Assert.Equal(1, corpo.Page);
            Assert.Equal(1, corpo.PageSize);
            Assert.Equal(2, corpo.Total);
            Assert.True(corpo.HasNext);
            var item = Assert.Single(corpo.Items);
            Assert.Equal(lojaA, item.LojaId);
            Assert.Equal(10m, item.Preco);
            Assert.Equal(0, item.QuantidadeEstoque);
        }

        [Fact]
        public async Task Listar_SemLojaId_DevolveOfertasDeTodasAsLojas()
        {
            var lojaA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var lojaB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            var agora = new DateTime(2026, 11, 18, 12, 0, 0, DateTimeKind.Utc);
            await using var ctx = CriarContexto();
            ctx.Ofertas.AddRange(
                Oferta(ctx, lojaA, 10m, 7, agora),
                Oferta(ctx, lojaB, 5m, 1, agora.AddMinutes(-1)));
            await ctx.SaveChangesAsync();

            var ok = Assert.IsType<OkObjectResult>(await Listar(ctx, 1, 20, null));
            var corpo = Assert.IsType<PaginacaoRespostaDto<OfertaRespostaDto>>(ok.Value);

            Assert.Equal(2, corpo.Total);
            Assert.False(corpo.HasNext);
            Assert.Equal(2, corpo.Items.Count);
            Assert.Contains(corpo.Items, i => i.LojaId == lojaA);
            Assert.Contains(corpo.Items, i => i.LojaId == lojaB);
        }

        [Fact]
        public async Task Listar_LojaIdVazio_Retorna400SemConsultar()
        {
            var servico = new Mock<IOfertaServico>();
            var controller = new OfertasController(
                servico.Object,
                Mock.Of<IIdCodificador>(),
                Mock.Of<IProdutoServico>(),
                Mock.Of<ILojaAcesso>());

            var resultado = await controller.Listar(1, 20, Guid.Empty);

            Assert.IsType<BadRequestObjectResult>(resultado);
            servico.Verify(
                s => s.ListarPaginadoAsync(It.IsAny<PaginacaoParametros>(), It.IsAny<Guid?>()),
                Times.Never);
        }

        private static async Task<IActionResult> Listar(AppDbContext ctx, int page, int pageSize, Guid? lojaId)
        {
            var controller = new OfertasController(
                new OfertaServico(new OfertaRepositorio(ctx), Mock.Of<IRagIndexFila>()),
                Mock.Of<IIdCodificador>(),
                Mock.Of<IProdutoServico>(),
                Mock.Of<ILojaAcesso>());
            return await controller.Listar(page, pageSize, lojaId);
        }

        private static Oferta Oferta(AppDbContext ctx, Guid lojaId, decimal preco, int estoque, DateTime atualizado)
        {
            var loja = ctx.Lojas.Local.FirstOrDefault(l => l.Id == lojaId);
            if (loja is null)
            {
                var endereco = new Endereco();
                loja = new Loja
                {
                    Id = lojaId,
                    NomeFantasia = "Loja",
                    Endereco = endereco,
                    EnderecoId = endereco.Id
                };
                ctx.Add(endereco);
                ctx.Add(loja);
            }

            var produto = new Produto { NomeProduto = "Item", Loja = loja, LojaId = lojaId };
            ctx.Add(produto);
            return new Oferta
            {
                Produto = produto,
                ProdutoId = produto.Id,
                Loja = loja,
                LojaId = lojaId,
                Preco = preco,
                QuantidadeEstoque = estoque,
                Disponivel = true,
                DataAtualizacaoPreco = atualizado
            };
        }

        private static AppDbContext CriarContexto()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new TestAppDbContext(options);
        }
    }
}
