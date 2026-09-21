using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Pc.Dominio.Enums;
using Pc.Dominio.Enums.MotorIA;
using Pc.Repositorio.Interfaces;
using Pc.Servico.Implementacoes.Rag;
using Pc.Servico.Interfaces;
using Pc.Servico.Interfaces.MotorIA;
using Pc.Servico.Modelos.IA;
using Pc.Servico.Modelos.MotorIA;
using Pc.Servico.Modelos.Rag;
using Pc.WebApi.Controllers;
using Pc.WebApi.DTOs.IA;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Pc.Servico.Tests.Rag
{
    public class RagCascadeAndDlqTests
    {
        [Fact]
        public async Task Cascata_produto_atualizado_enfileira_produto_e_ofertas()
        {
            var produtoId = Guid.NewGuid();
            var ofertaId = Guid.NewGuid();

            var produtos = new Mock<IProdutoRepositorio>();
            var ofertas = new Mock<IOfertaRepositorio>();
            ofertas.Setup(o => o.ListarIdsPorProdutoAsync(produtoId, It.IsAny<int>()))
                .ReturnsAsync(new List<Guid> { ofertaId });

            var enfileirados = new List<(RagDocumentoTipo tipo, Guid id, RagIndexAcao acao)>();
            var fila = new Mock<IRagIndexFila>();
            fila.Setup(f => f.Enfileirar(It.IsAny<RagDocumentoTipo>(), It.IsAny<Guid>(), It.IsAny<RagIndexAcao>()))
                .Callback<RagDocumentoTipo, Guid, RagIndexAcao>((t, id, a) => enfileirados.Add((t, id, a)));

            var cascade = new RagCascadeIndexador(
                produtos.Object, ofertas.Object, fila.Object, NullLogger<RagCascadeIndexador>.Instance);

            await cascade.EnfileirarProdutoAtualizadoAsync(produtoId);

            Assert.Contains(enfileirados, e => e.tipo == RagDocumentoTipo.Produto && e.id == produtoId && e.acao == RagIndexAcao.Indexar);
            Assert.Contains(enfileirados, e => e.tipo == RagDocumentoTipo.Oferta && e.id == ofertaId && e.acao == RagIndexAcao.Indexar);
        }

        [Fact]
        public async Task Cascata_loja_atualizada_enfileira_loja_produtos_ofertas()
        {
            var lojaId = Guid.NewGuid();
            var produtoId = Guid.NewGuid();
            var ofertaId = Guid.NewGuid();

            var produtos = new Mock<IProdutoRepositorio>();
            produtos.Setup(p => p.ListarIdsPorLojaAsync(lojaId, It.IsAny<int>()))
                .ReturnsAsync(new List<Guid> { produtoId });

            var ofertas = new Mock<IOfertaRepositorio>();
            ofertas.Setup(o => o.ListarIdsPorLojaAsync(lojaId, It.IsAny<int>()))
                .ReturnsAsync(new List<Guid> { ofertaId });

            var enfileirados = new List<(RagDocumentoTipo tipo, Guid id)>();
            var fila = new Mock<IRagIndexFila>();
            fila.Setup(f => f.Enfileirar(It.IsAny<RagDocumentoTipo>(), It.IsAny<Guid>(), It.IsAny<RagIndexAcao>()))
                .Callback<RagDocumentoTipo, Guid, RagIndexAcao>((t, id, _) => enfileirados.Add((t, id)));

            var cascade = new RagCascadeIndexador(
                produtos.Object, ofertas.Object, fila.Object, NullLogger<RagCascadeIndexador>.Instance);

            await cascade.EnfileirarLojaAtualizadaAsync(lojaId);

            Assert.Contains(enfileirados, e => e.tipo == RagDocumentoTipo.Loja && e.id == lojaId);
            Assert.Contains(enfileirados, e => e.tipo == RagDocumentoTipo.Produto && e.id == produtoId);
            Assert.Contains(enfileirados, e => e.tipo == RagDocumentoTipo.Oferta && e.id == ofertaId);
        }

        [Fact]
        public async Task Cascata_produto_removido_remove_ofertas_antes()
        {
            var produtoId = Guid.NewGuid();
            var ofertaId = Guid.NewGuid();
            var ordem = new List<string>();

            var produtos = new Mock<IProdutoRepositorio>();
            var ofertas = new Mock<IOfertaRepositorio>();
            ofertas.Setup(o => o.ListarIdsPorProdutoAsync(produtoId, It.IsAny<int>()))
                .ReturnsAsync(new List<Guid> { ofertaId });

            var fila = new Mock<IRagIndexFila>();
            fila.Setup(f => f.Enfileirar(It.IsAny<RagDocumentoTipo>(), It.IsAny<Guid>(), It.IsAny<RagIndexAcao>()))
                .Callback<RagDocumentoTipo, Guid, RagIndexAcao>((t, id, a) =>
                    ordem.Add($"{t}:{a}:{id}"));

            var cascade = new RagCascadeIndexador(
                produtos.Object, ofertas.Object, fila.Object, NullLogger<RagCascadeIndexador>.Instance);

            await cascade.EnfileirarProdutoRemovidoAsync(produtoId);

            Assert.Equal($"Oferta:Remover:{ofertaId}", ordem[0]);
            Assert.Equal($"Produto:Remover:{produtoId}", ordem[1]);
        }
    }

    public class IaChatLegadoFacadeTests
    {
        [Fact]
        public async Task Chat_legado_usa_IAServico()
        {
            var ia = new Mock<IIAServico>();
            ia.Setup(s => s.ProcessarAsync(It.IsAny<IAChatPedido>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new IAChatResultado
                {
                    Sucesso = true,
                    Intencao = Pc.Dominio.Enums.IAIntencao.PrecoProduto,
                    Resposta = "Encontrei opções."
                });

            var controller = new IAController(
                ia.Object,
                Mock.Of<IMotorIA>(),
                NullLogger<IAController>.Instance);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
            };

            var result = await controller.Chat(
                new IAChatRequestDto { Mensagem = "quero arroz" },
                CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<IAChatResponseDto>(ok.Value);
            Assert.True(dto.Sucesso);
            Assert.Equal("PrecoProduto", dto.Intencao);
            Assert.Equal("Encontrei opções.", dto.Resposta);
            ia.Verify(s => s.ProcessarAsync(
                It.Is<IAChatPedido>(p => p.Mensagem == "quero arroz"),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
