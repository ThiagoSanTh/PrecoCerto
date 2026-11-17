using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Pc.Dominio.Entities.Catalogo;
using Pc.Dominio.Entities.Estabelecimentos;
using Pc.Dominio.Entities.Interacoes;
using Pc.Dominio.Entities.Usuarios;
using Pc.Repositorio.Interfaces;
using Pc.Servico.Excecoes;
using Pc.Servico.Implementacoes;
using Pc.Servico.Interfaces;
using Pc.WebApi.Controllers;
using Pc.WebApi.DTOs.Catalogo;
using Pc.WebApi.DTOs.Estabelecimentos;
using Pc.WebApi.DTOs.Interacoes;
using Xunit;

namespace Pc.Servico.Tests
{
    public class AutorizacaoPosseTests
    {
        [Fact]
        public async Task Lojista_nao_sequestra_oferta_de_outra_loja()
        {
            var loja1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var loja2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var ofertaId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var ofertas = new Mock<IOfertaServico>();
            ofertas.Setup(s => s.ObterPorIdAsync(ofertaId)).ReturnsAsync(new Oferta
            {
                Id = ofertaId,
                LojaId = loja1,
                ProdutoId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                Preco = 9.90m
            });

            var controller = Ofertas(ofertas);
            Autenticar(controller, Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), "Lojista", loja2);

            var resultado = await controller.Atualizar(ofertaId, new OfertaCriarDto
            {
                ProdutoId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                LojaId = loja2,
                Preco = 0.01m
            });

            Assert.IsType<ForbidResult>(resultado);
            ofertas.Verify(s => s.AtualizarAsync(It.IsAny<Oferta>()), Times.Never);
        }

        [Fact]
        public async Task Dono_nao_troca_lojaId_da_propria_oferta()
        {
            var loja1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var loja2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var ofertaId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var ofertas = new Mock<IOfertaServico>();
            ofertas.Setup(s => s.ObterPorIdAsync(ofertaId)).ReturnsAsync(new Oferta
            {
                Id = ofertaId,
                LojaId = loja1,
                Preco = 9.90m
            });

            var controller = Ofertas(ofertas);
            Autenticar(controller, Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), "Lojista", loja1);

            var resultado = await controller.Atualizar(ofertaId, new OfertaCriarDto
            {
                ProdutoId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                LojaId = loja2,
                Preco = 8m
            });

            Assert.IsType<ForbidResult>(resultado);
            ofertas.Verify(s => s.AtualizarAsync(It.IsAny<Oferta>()), Times.Never);
        }

        [Fact]
        public async Task Dono_atualiza_preco_da_propria_oferta_sem_mudar_loja()
        {
            var loja1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var ofertaId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var produtoId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            Oferta? salva = null;
            var ofertas = new Mock<IOfertaServico>();
            ofertas.Setup(s => s.ObterPorIdAsync(ofertaId)).ReturnsAsync(new Oferta
            {
                Id = ofertaId,
                LojaId = loja1,
                ProdutoId = produtoId,
                Preco = 9.90m
            });
            ofertas.Setup(s => s.AtualizarAsync(It.IsAny<Oferta>()))
                .Callback<Oferta>(o => salva = o)
                .Returns(Task.CompletedTask);

            var dono = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
            var controller = Ofertas(ofertas, LojaDe(loja1, dono), ProdutoDe(produtoId, loja1));
            Autenticar(controller, dono, "Lojista", loja1);

            var resultado = await controller.Atualizar(ofertaId, new OfertaCriarDto
            {
                ProdutoId = produtoId,
                LojaId = loja1,
                Preco = 8.50m
            });

            Assert.IsType<NoContentResult>(resultado);
            Assert.NotNull(salva);
            Assert.Equal(loja1, salva!.LojaId);
            Assert.Equal(8.50m, salva.Preco);
        }

        [Fact]
        public async Task Lojista_nao_apaga_oferta_de_outra_loja()
        {
            var loja1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var loja2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var ofertaId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var ofertas = new Mock<IOfertaServico>();
            ofertas.Setup(s => s.ObterPorIdAsync(ofertaId)).ReturnsAsync(new Oferta
            {
                Id = ofertaId,
                LojaId = loja1
            });

            var controller = Ofertas(ofertas);
            Autenticar(controller, Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), "Lojista", loja2);

            var resultado = await controller.Remover(ofertaId);

            Assert.IsType<ForbidResult>(resultado);
            ofertas.Verify(s => s.RemoverAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task Produto_de_outra_loja_nao_e_alterado_nem_removido()
        {
            var loja1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var loja2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var produtoId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            var repo = new Mock<IProdutoRepositorio>();
            repo.Setup(r => r.ObterPorIdAsync(produtoId)).ReturnsAsync(new Produto
            {
                Id = produtoId,
                LojaId = loja1,
                NomeProduto = "Arroz"
            });
            var servico = new ProdutoServico(
                repo.Object,
                Mock.Of<IRagIndexFila>(),
                Mock.Of<IRagCascadeIndexador>());

            var aoAtualizar = await Assert.ThrowsAsync<ProdutoOperacaoException>(() =>
                servico.AtualizarPorLojaAsync(produtoId, new Produto { NomeProduto = "Arroz", Preco = 0.01m }, loja2));
            var aoRemover = await Assert.ThrowsAsync<ProdutoOperacaoException>(() =>
                servico.RemoverPorLojaAsync(produtoId, loja2));

            Assert.True(aoAtualizar.AcessoNegado);
            Assert.True(aoRemover.AcessoNegado);
            repo.Verify(r => r.AtualizarCamposAsync(It.IsAny<Produto>()), Times.Never);
            repo.Verify(r => r.RemoverPorIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task Lojista_nao_atualiza_loja_alheia()
        {
            var loja1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var loja2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var dono1 = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
            var lojas = new Mock<ILojaServico>();
            lojas.Setup(s => s.ObterPorIdAsync(loja1)).ReturnsAsync(new Loja
            {
                Id = loja1,
                UsuarioId = dono1,
                NomeFantasia = "Loja 1"
            });

            var controller = new LojasController(
                lojas.Object,
                Mock.Of<IClienteServico>(),
                Mock.Of<IConsultaCnpjServico>(),
                Mock.Of<IIdCodificador>());
            Autenticar(controller, Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), "Lojista", loja2);

            var resultado = await controller.Atualizar(loja1, new LojaCriarDto
            {
                NomeFantasia = "Loja sequestrada",
                UsuarioId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc")
            });

            Assert.IsType<ForbidResult>(resultado);
            lojas.Verify(s => s.AtualizarAsync(It.IsAny<Loja>()), Times.Never);
        }

        [Fact]
        public async Task Lojista_nao_transfere_o_dono_da_propria_loja()
        {
            var loja1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var dono = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
            var outro = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
            var lojas = new Mock<ILojaServico>();
            lojas.Setup(s => s.ObterPorIdAsync(loja1)).ReturnsAsync(new Loja
            {
                Id = loja1,
                UsuarioId = dono,
                NomeFantasia = "Loja 1"
            });

            var controller = new LojasController(
                lojas.Object,
                Mock.Of<IClienteServico>(),
                Mock.Of<IConsultaCnpjServico>(),
                Mock.Of<IIdCodificador>());
            Autenticar(controller, dono, "Lojista", loja1);

            var resultado = await controller.Atualizar(loja1, new LojaCriarDto
            {
                NomeFantasia = "Loja 1",
                UsuarioId = outro
            });

            Assert.IsType<ForbidResult>(resultado);
            lojas.Verify(s => s.AtualizarAsync(It.IsAny<Loja>()), Times.Never);
        }

        [Fact]
        public async Task Lojista_atualiza_a_propria_loja_sem_trocar_o_dono()
        {
            var loja1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var dono = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
            Loja? salva = null;
            var lojas = new Mock<ILojaServico>();
            lojas.Setup(s => s.ObterPorIdAsync(loja1)).ReturnsAsync(new Loja
            {
                Id = loja1,
                UsuarioId = dono,
                NomeFantasia = "Loja 1"
            });
            lojas.Setup(s => s.AtualizarAsync(It.IsAny<Loja>()))
                .Callback<Loja>(l => salva = l)
                .Returns(Task.CompletedTask);

            var controller = new LojasController(
                lojas.Object,
                Mock.Of<IClienteServico>(),
                Mock.Of<IConsultaCnpjServico>(),
                Mock.Of<IIdCodificador>());
            Autenticar(controller, dono, "Lojista", loja1);

            var resultado = await controller.Atualizar(loja1, new LojaCriarDto
            {
                NomeFantasia = "Loja atualizada",
                UsuarioId = dono
            });

            Assert.IsType<NoContentResult>(resultado);
            Assert.Equal(dono, salva!.UsuarioId);
            Assert.Equal("Loja atualizada", salva.NomeFantasia);
        }

        [Fact]
        public async Task Admin_pode_transferir_o_dono_da_loja()
        {
            var loja1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var dono = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
            var novoDono = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
            Loja? salva = null;
            var lojas = new Mock<ILojaServico>();
            lojas.Setup(s => s.ObterPorIdAsync(loja1)).ReturnsAsync(new Loja
            {
                Id = loja1,
                UsuarioId = dono,
                NomeFantasia = "Loja 1"
            });
            lojas.Setup(s => s.AtualizarAsync(It.IsAny<Loja>()))
                .Callback<Loja>(l => salva = l)
                .Returns(Task.CompletedTask);

            var usuarios = new Mock<IClienteServico>();
            usuarios.Setup(s => s.ObterPorIdAsync(novoDono)).ReturnsAsync(new Usuario { Id = novoDono });
            var controller = new LojasController(
                lojas.Object,
                usuarios.Object,
                Mock.Of<IConsultaCnpjServico>(),
                Mock.Of<IIdCodificador>());
            Autenticar(controller, Guid.Parse("99999999-9999-9999-9999-999999999999"), "Admin", null);

            var resultado = await controller.Atualizar(loja1, new LojaCriarDto
            {
                NomeFantasia = "Loja 1",
                UsuarioId = novoDono
            });

            Assert.IsType<NoContentResult>(resultado);
            Assert.Equal(novoDono, salva!.UsuarioId);
        }

        [Fact]
        public async Task Ex_dono_nao_altera_oferta_com_token_antigo()
        {
            var loja1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var exDono = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
            var novoDono = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
            var ofertaId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var produtoId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            var ofertas = new Mock<IOfertaServico>();
            ofertas.Setup(s => s.ObterPorIdAsync(ofertaId)).ReturnsAsync(new Oferta
            {
                Id = ofertaId,
                LojaId = loja1,
                ProdutoId = produtoId,
                Preco = 9.90m
            });

            var controller = Ofertas(ofertas, LojaDe(loja1, novoDono), ProdutoDe(produtoId, loja1));
            Autenticar(controller, exDono, "Lojista", loja1);

            var resultado = await controller.Atualizar(ofertaId, new OfertaCriarDto
            {
                ProdutoId = produtoId,
                LojaId = loja1,
                Preco = 0.01m
            });

            Assert.IsType<ForbidResult>(resultado);
            ofertas.Verify(s => s.AtualizarAsync(It.IsAny<Oferta>()), Times.Never);
        }

        [Fact]
        public async Task Ex_dono_nao_cria_oferta_nem_apaga_com_token_antigo()
        {
            var loja1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var exDono = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
            var novoDono = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
            var ofertaId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var produtoId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            var ofertas = new Mock<IOfertaServico>();
            ofertas.Setup(s => s.ObterPorIdAsync(ofertaId)).ReturnsAsync(new Oferta
            {
                Id = ofertaId,
                LojaId = loja1,
                ProdutoId = produtoId
            });
            ofertas.Setup(s => s.AdicionarAsync(It.IsAny<Oferta>())).ReturnsAsync(new Oferta
            {
                Id = ofertaId,
                LojaId = loja1,
                ProdutoId = produtoId,
                Preco = 1m
            });

            var controller = Ofertas(ofertas, LojaDe(loja1, novoDono), ProdutoDe(produtoId, loja1));
            Autenticar(controller, exDono, "Lojista", loja1);

            var criada = await controller.Adicionar(new OfertaCriarDto
            {
                ProdutoId = produtoId,
                LojaId = loja1,
                Preco = 1m
            });
            var apagada = await controller.Remover(ofertaId);

            Assert.IsType<ForbidResult>(criada);
            Assert.IsType<ForbidResult>(apagada);
            ofertas.Verify(s => s.AdicionarAsync(It.IsAny<Oferta>()), Times.Never);
            ofertas.Verify(s => s.RemoverAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task Ex_dono_nao_altera_loja_com_token_antigo()
        {
            var loja1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var exDono = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
            var novoDono = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
            var lojas = new Mock<ILojaServico>();
            lojas.Setup(s => s.ObterPorIdAsync(loja1)).ReturnsAsync(new Loja
            {
                Id = loja1,
                UsuarioId = novoDono,
                NomeFantasia = "Loja 1"
            });

            var controller = new LojasController(
                lojas.Object,
                Mock.Of<IClienteServico>(),
                Mock.Of<IConsultaCnpjServico>(),
                Mock.Of<IIdCodificador>());
            Autenticar(controller, exDono, "Lojista", loja1);

            var resultado = await controller.Atualizar(loja1, new LojaCriarDto { NomeFantasia = "Invadida" });

            Assert.IsType<ForbidResult>(resultado);
            lojas.Verify(s => s.AtualizarAsync(It.IsAny<Loja>()), Times.Never);
        }

        [Fact]
        public async Task Ex_dono_nao_cria_produto_com_token_antigo()
        {
            var loja1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var exDono = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
            var novoDono = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
            var produtos = new Mock<IProdutoServico>();
            produtos.Setup(s => s.AdicionarAsync(It.IsAny<Produto>()))
                .ReturnsAsync(new Produto { Id = Guid.NewGuid(), NomeProduto = "Arroz", LojaId = loja1 });
            produtos.Setup(s => s.ObterPorIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new Produto { NomeProduto = "Arroz", LojaId = loja1 });

            var lojas = LojaDe(loja1, novoDono);
            var controller = new ProdutosController(
                produtos.Object,
                Mock.Of<IIdCodificador>(),
                AcessoDaLoja(lojas));
            Autenticar(controller, exDono, "Lojista", loja1);

            var resultado = await controller.Adicionar(new ProdutoCriarDto
            {
                NomeProduto = "Arroz",
                LojaId = loja1,
                Preco = 0.01m
            });

            Assert.IsType<ForbidResult>(resultado);
            produtos.Verify(s => s.AdicionarAsync(It.IsAny<Produto>()), Times.Never);
        }

        [Fact]
        public async Task Oferta_rejeita_produto_de_outra_loja()
        {
            var loja1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var loja2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var dono = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
            var produtoId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            var ofertaId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var ofertas = new Mock<IOfertaServico>();
            ofertas.Setup(s => s.AdicionarAsync(It.IsAny<Oferta>())).ReturnsAsync(new Oferta
            {
                Id = ofertaId,
                LojaId = loja1,
                ProdutoId = produtoId,
                Preco = 2m
            });
            ofertas.Setup(s => s.ObterPorIdAsync(ofertaId)).ReturnsAsync(new Oferta
            {
                Id = ofertaId,
                LojaId = loja1,
                ProdutoId = produtoId,
                Preco = 2m
            });

            var controller = Ofertas(ofertas, LojaDe(loja1, dono), ProdutoDe(produtoId, loja2));
            Autenticar(controller, dono, "Lojista", loja1);
            var dto = new OfertaCriarDto { ProdutoId = produtoId, LojaId = loja1, Preco = 2m };

            var criada = await controller.Adicionar(dto);
            var atualizada = await controller.Atualizar(ofertaId, dto);

            Assert.IsType<BadRequestObjectResult>(criada);
            Assert.IsType<BadRequestObjectResult>(atualizada);
            ofertas.Verify(s => s.AdicionarAsync(It.IsAny<Oferta>()), Times.Never);
            ofertas.Verify(s => s.AtualizarAsync(It.IsAny<Oferta>()), Times.Never);
        }

        [Fact]
        public async Task Oferta_rejeita_produto_inexistente()
        {
            var loja1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var dono = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
            var produtoId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            var ofertaId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var ofertas = new Mock<IOfertaServico>();
            ofertas.Setup(s => s.AdicionarAsync(It.IsAny<Oferta>())).ReturnsAsync(new Oferta
            {
                Id = ofertaId,
                LojaId = loja1,
                ProdutoId = produtoId,
                Preco = 2m
            });
            ofertas.Setup(s => s.ObterPorIdAsync(ofertaId)).ReturnsAsync(new Oferta
            {
                Id = ofertaId,
                LojaId = loja1,
                Preco = 2m
            });

            var controller = Ofertas(ofertas, LojaDe(loja1, dono));
            Autenticar(controller, dono, "Lojista", loja1);

            var resultado = await controller.Adicionar(new OfertaCriarDto
            {
                ProdutoId = produtoId,
                LojaId = loja1,
                Preco = 2m
            });

            Assert.IsType<NotFoundObjectResult>(resultado);
            ofertas.Verify(s => s.AdicionarAsync(It.IsAny<Oferta>()), Times.Never);
        }

        [Fact]
        public async Task Admin_nao_transfere_loja_para_usuario_inexistente()
        {
            var loja1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var dono = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
            var fantasma = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
            var lojas = new Mock<ILojaServico>();
            lojas.Setup(s => s.ObterPorIdAsync(loja1)).ReturnsAsync(new Loja
            {
                Id = loja1,
                UsuarioId = dono,
                NomeFantasia = "Loja 1"
            });
            var usuarios = new Mock<IClienteServico>();
            usuarios.Setup(s => s.ObterPorIdAsync(fantasma)).ReturnsAsync((Usuario?)null);

            var controller = new LojasController(
                lojas.Object,
                usuarios.Object,
                Mock.Of<IConsultaCnpjServico>(),
                Mock.Of<IIdCodificador>());
            Autenticar(controller, Guid.Parse("99999999-9999-9999-9999-999999999999"), "Admin", null);

            var resultado = await controller.Atualizar(loja1, new LojaCriarDto
            {
                NomeFantasia = "Loja 1",
                UsuarioId = fantasma
            });

            Assert.IsType<BadRequestObjectResult>(resultado);
            lojas.Verify(s => s.AtualizarAsync(It.IsAny<Loja>()), Times.Never);
        }

        [Fact]
        public async Task Admin_nao_transfere_loja_para_quem_ja_tem_loja()
        {
            var loja1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var loja2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var dono = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
            var ocupado = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
            var lojas = new Mock<ILojaServico>();
            lojas.Setup(s => s.ObterPorIdAsync(loja1)).ReturnsAsync(new Loja
            {
                Id = loja1,
                UsuarioId = dono,
                NomeFantasia = "Loja 1"
            });
            lojas.Setup(s => s.ObterPorUsuarioIdAsync(ocupado)).ReturnsAsync(new Loja
            {
                Id = loja2,
                UsuarioId = ocupado
            });
            var usuarios = new Mock<IClienteServico>();
            usuarios.Setup(s => s.ObterPorIdAsync(ocupado)).ReturnsAsync(new Usuario { Id = ocupado });

            var controller = new LojasController(
                lojas.Object,
                usuarios.Object,
                Mock.Of<IConsultaCnpjServico>(),
                Mock.Of<IIdCodificador>());
            Autenticar(controller, Guid.Parse("99999999-9999-9999-9999-999999999999"), "Admin", null);

            var resultado = await controller.Atualizar(loja1, new LojaCriarDto
            {
                NomeFantasia = "Loja 1",
                UsuarioId = ocupado
            });

            Assert.IsType<ConflictObjectResult>(resultado);
            lojas.Verify(s => s.AtualizarAsync(It.IsAny<Loja>()), Times.Never);
        }

        [Fact]
        public async Task Lista_avaliacoes_sem_token_retorna_401()
        {
            var controller = Avaliacoes(new Mock<IAvaliacaoServico>());
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            var resultado = await controller.ListarPorCliente(Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"));

            Assert.IsType<UnauthorizedResult>(resultado);
        }

        [Fact]
        public async Task Admin_lista_avaliacoes_de_cliente_vazio_retorna_400()
        {
            var avaliacoes = new Mock<IAvaliacaoServico>();
            avaliacoes.Setup(s => s.ListarPorClienteAsync(Guid.Empty))
                .ThrowsAsync(new Exception("ClienteId é obrigatório."));
            var controller = Avaliacoes(avaliacoes);
            Autenticar(controller, Guid.Parse("99999999-9999-9999-9999-999999999999"), "Admin", null);

            var resultado = await controller.ListarPorCliente(Guid.Empty);

            Assert.IsType<BadRequestObjectResult>(resultado);
            avaliacoes.Verify(s => s.ListarPorClienteAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task Cliente_nao_lista_avaliacoes_de_outro()
        {
            var outro = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
            var avaliacoes = new Mock<IAvaliacaoServico>();
            avaliacoes.Setup(s => s.ListarPorClienteAsync(outro)).ReturnsAsync(new List<Avaliacao>
            {
                new() { Id = Guid.NewGuid(), ClienteId = outro, Nota = 5 }
            });

            var controller = Avaliacoes(avaliacoes);
            Autenticar(controller, Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"), "Cliente", null);

            var resultado = await controller.ListarPorCliente(outro);

            Assert.IsType<ForbidResult>(resultado);
            avaliacoes.Verify(s => s.ListarPorClienteAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task Admin_lista_avaliacoes_de_um_cliente()
        {
            var cliente = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
            var avaliacoes = new Mock<IAvaliacaoServico>();
            avaliacoes.Setup(s => s.ListarPorClienteAsync(cliente)).ReturnsAsync(new List<Avaliacao>());

            var controller = Avaliacoes(avaliacoes);
            Autenticar(controller, Guid.Parse("99999999-9999-9999-9999-999999999999"), "Admin", null);

            var resultado = await controller.ListarPorCliente(cliente);

            Assert.IsType<OkObjectResult>(resultado);
        }

        [Fact]
        public async Task Cliente_lista_as_proprias_avaliacoes()
        {
            var eu = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
            var avaliacaoId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var avaliacoes = new Mock<IAvaliacaoServico>();
            avaliacoes.Setup(s => s.ListarPorClienteAsync(eu)).ReturnsAsync(new List<Avaliacao>
            {
                new()
                {
                    Id = avaliacaoId,
                    ClienteId = eu,
                    LojaId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Nota = 4
                }
            });

            var controller = Avaliacoes(avaliacoes);
            Autenticar(controller, eu, "Cliente", null);

            var resultado = await controller.ListarPorCliente(eu);

            var ok = Assert.IsType<OkObjectResult>(resultado);
            var lista = Assert.IsAssignableFrom<IEnumerable<AvaliacaoRespostaDto>>(ok.Value).ToList();
            Assert.Single(lista);
            Assert.Equal(eu, lista[0].ClienteId);
            Assert.Equal(avaliacaoId, lista[0].Id);
        }

        [Fact]
        public async Task Obter_avaliacao_nao_expoe_clienteId_de_terceiro()
        {
            var dono = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
            var avaliacaoId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var avaliacoes = new Mock<IAvaliacaoServico>();
            avaliacoes.Setup(s => s.ObterPorIdAsync(avaliacaoId)).ReturnsAsync(new Avaliacao
            {
                Id = avaliacaoId,
                ClienteId = dono,
                LojaId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Nota = 2,
                Comentario = "fila longa"
            });

            var controller = Avaliacoes(avaliacoes);
            Autenticar(controller, Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"), "Cliente", null);

            var resultado = await controller.ObterPorId(avaliacaoId);

            var ok = Assert.IsType<OkObjectResult>(resultado);
            var dto = Assert.IsType<AvaliacaoRespostaDto>(ok.Value);
            Assert.Equal(Guid.Empty, dto.ClienteId);
            Assert.Equal(2, dto.Nota);
            Assert.Equal("fila longa", dto.Comentario);
        }

        [Fact]
        public async Task Dono_e_admin_veem_clienteId_da_avaliacao()
        {
            var dono = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
            var avaliacaoId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var avaliacoes = new Mock<IAvaliacaoServico>();
            avaliacoes.Setup(s => s.ObterPorIdAsync(avaliacaoId)).ReturnsAsync(new Avaliacao
            {
                Id = avaliacaoId,
                ClienteId = dono,
                LojaId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Nota = 5
            });

            var comoDono = Avaliacoes(avaliacoes);
            Autenticar(comoDono, dono, "Cliente", null);
            var comoAdmin = Avaliacoes(avaliacoes);
            Autenticar(comoAdmin, Guid.Parse("99999999-9999-9999-9999-999999999999"), "Admin", null);

            var doDono = Assert.IsType<OkObjectResult>(await comoDono.ObterPorId(avaliacaoId));
            var doAdmin = Assert.IsType<OkObjectResult>(await comoAdmin.ObterPorId(avaliacaoId));

            Assert.Equal(dono, Assert.IsType<AvaliacaoRespostaDto>(doDono.Value).ClienteId);
            Assert.Equal(dono, Assert.IsType<AvaliacaoRespostaDto>(doAdmin.Value).ClienteId);
        }

        private static OfertasController Ofertas(
            Mock<IOfertaServico> ofertas,
            Mock<ILojaServico>? lojas = null,
            Mock<IProdutoServico>? produtos = null) =>
            new(
                ofertas.Object,
                Mock.Of<IIdCodificador>(),
                (produtos ?? new Mock<IProdutoServico>()).Object,
                AcessoDaLoja(lojas ?? new Mock<ILojaServico>()));

        private static LojaAcesso AcessoDaLoja(Mock<ILojaServico> lojas)
        {
            var repo = new Mock<ILojaRepositorio>();
            repo.Setup(r => r.ObterPorIdAsync(It.IsAny<Guid>()))
                .Returns<Guid>(id => lojas.Object.ObterPorIdAsync(id) ?? Task.FromResult<Loja?>(null));
            var usuarios = new Mock<IClienteRepositorio>();
            usuarios.Setup(r => r.ObterPorIdAsync(It.IsAny<Guid>())).ReturnsAsync((Usuario?)null);
            return new LojaAcesso(repo.Object, usuarios.Object);
        }

        private static Mock<ILojaServico> LojaDe(Guid lojaId, Guid donoId)
        {
            var lojas = new Mock<ILojaServico>();
            lojas.Setup(s => s.ObterPorIdAsync(lojaId)).ReturnsAsync(new Loja
            {
                Id = lojaId,
                UsuarioId = donoId,
                NomeFantasia = "Loja"
            });
            return lojas;
        }

        private static Mock<IProdutoServico> ProdutoDe(Guid produtoId, Guid lojaId)
        {
            var produtos = new Mock<IProdutoServico>();
            produtos.Setup(s => s.ObterPorIdAsync(produtoId)).ReturnsAsync(new Produto
            {
                Id = produtoId,
                LojaId = lojaId,
                NomeProduto = "Arroz"
            });
            return produtos;
        }

        private static AvaliacoesController Avaliacoes(Mock<IAvaliacaoServico> avaliacoes) =>
            new(avaliacoes.Object);

        private static void Autenticar(ControllerBase controller, Guid userId, string role, Guid? lojaId)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new(ClaimTypes.Role, role)
            };
            if (lojaId.HasValue)
                claims.Add(new Claim("lojaId", lojaId.Value.ToString()));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
                }
            };
        }
    }
}
