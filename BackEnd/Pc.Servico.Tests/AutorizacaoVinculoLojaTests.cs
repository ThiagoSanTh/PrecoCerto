using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Pc.Dominio.Entities.Catalogo;
using Pc.Dominio.Entities.Estabelecimentos;
using Pc.Dominio.Entities.Interacoes;
using Pc.Dominio.Entities.Usuarios;
using Pc.Dominio.Enums;
using Pc.Repositorio.Interfaces;
using Pc.Servico.Implementacoes;
using Pc.Servico.Interfaces;
using Pc.WebApi.Authorization;
using Pc.WebApi.Controllers;
using Pc.WebApi.DTOs.Estabelecimentos;
using Pc.WebApi.DTOs.Interacoes;
using Pc.WebApi.DTOs.Usuarios;
using Pc.WebApi.Hubs;
using Xunit;

namespace Pc.Servico.Tests
{
    /// <summary>
    /// O que faria cada teste falhar: aceitar de novo o claim lojaId,
    /// promover/remover vendedor sem olhar LojaVinculadaId, ou devolver 400
    /// quando o vendedor é de outra loja.
    /// </summary>
    public class AutorizacaoVinculoLojaTests
    {
        private static readonly Guid LojaA = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid LojaB = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid DonoA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid ExDono = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        private static readonly Guid VendedorId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        private static readonly Guid OfertaId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        private static readonly Guid ProdutoId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
        private static readonly Guid AdminId = Guid.Parse("99999999-9999-9999-9999-999999999999");

        [Fact]
        public async Task Lojista_nao_promove_vendedor_de_outra_loja()
        {
            var vendedor = VendedorDe(LojaB);
            var repo = RepoCom(vendedor);
            var servico = Servico(repo);

            var erro = await Record.ExceptionAsync(() =>
                servico.PromoverParaVendedorAsync(VendedorId, LojaA, "Caixa"));

            Assert.NotNull(erro);
            Assert.Equal(LojaB, vendedor.LojaVinculadaId);
            Assert.Equal(PapelUsuario.Vendedor, vendedor.Papel);
            repo.Verify(r => r.AtualizarAsync(It.IsAny<Usuario>()), Times.Never);
        }

        [Fact]
        public async Task Lojista_nao_remove_vendedor_de_outra_loja()
        {
            var vendedor = VendedorDe(LojaB);
            var repo = RepoCom(vendedor);
            var controller = Lojistas(Servico(repo), Acesso(LojaA, DonoA));
            Autenticar(controller, DonoA, "Lojista", LojaA);

            var resultado = await controller.RemoverVendedor(LojaA, VendedorId);

            Assert.IsType<ForbidResult>(resultado);
            Assert.Equal(LojaB, vendedor.LojaVinculadaId);
            Assert.Equal(PapelUsuario.Vendedor, vendedor.Papel);
            repo.Verify(r => r.AtualizarAsync(It.IsAny<Usuario>()), Times.Never);
        }

        [Fact]
        public async Task Lojista_nao_promove_vendedor_de_outra_loja_pelo_controller()
        {
            var vendedor = VendedorDe(LojaB);
            var repo = RepoCom(vendedor);
            var controller = Lojistas(Servico(repo), Acesso(LojaA, DonoA));
            Autenticar(controller, DonoA, "Lojista", LojaA);

            var resultado = await controller.PromoverVendedor(
                LojaA, new PromoverVendedorDto { UsuarioId = VendedorId, Cargo = "Caixa" });

            Assert.IsType<ForbidResult>(resultado);
            Assert.Equal(LojaB, vendedor.LojaVinculadaId);
            Assert.Equal(PapelUsuario.Vendedor, vendedor.Papel);
            repo.Verify(r => r.AtualizarAsync(It.IsAny<Usuario>()), Times.Never);
        }

        [Fact]
        public async Task Dono_promove_cliente_e_remove_vendedor_da_propria_loja()
        {
            var cliente = new Usuario
            {
                Id = VendedorId,
                NomeUsuario = "Cliente",
                Email = "cliente@loja.test",
                Papel = PapelUsuario.Cliente,
                Tipo = TipoUsuario.Cliente
            };
            var repo = RepoCom(cliente);
            var servico = Servico(repo);

            await servico.PromoverParaVendedorAsync(VendedorId, LojaA, "Caixa");
            Assert.Equal(PapelUsuario.Vendedor, cliente.Papel);
            Assert.Equal(LojaA, cliente.LojaVinculadaId);

            var controller = Lojistas(servico, Acesso(LojaA, DonoA));
            Autenticar(controller, DonoA, "Lojista", LojaA);
            var resultado = await controller.RemoverVendedor(LojaA, VendedorId);

            Assert.IsType<NoContentResult>(resultado);
            Assert.Equal(PapelUsuario.Cliente, cliente.Papel);
            Assert.Null(cliente.LojaVinculadaId);
        }

        [Fact]
        public async Task Ex_dono_com_token_antigo_nao_lista_nem_altera_vendedores()
        {
            var usuarios = new Mock<IClienteServico>();
            usuarios.Setup(s => s.ListarVendedoresPorLojaAsync(LojaA))
                .ReturnsAsync(new List<Usuario> { VendedorDe(LojaA) });
            var controller = Lojistas(usuarios.Object, Acesso(LojaA, DonoA, ExDonoUsuario()));
            Autenticar(controller, ExDono, "Lojista", LojaA);

            var lista = await controller.ListarVendedores(LojaA);
            var promover = await controller.PromoverVendedor(LojaA, new PromoverVendedorDto { UsuarioId = VendedorId });
            var remover = await controller.RemoverVendedor(LojaA, VendedorId);

            Assert.IsType<ForbidResult>(lista);
            Assert.IsType<ForbidResult>(promover);
            Assert.IsType<ForbidResult>(remover);
            usuarios.Verify(s => s.ListarVendedoresPorLojaAsync(It.IsAny<Guid>()), Times.Never);
            usuarios.Verify(
                s => s.PromoverParaVendedorAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string?>()),
                Times.Never);
            usuarios.Verify(s => s.RemoverVendedorAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task Dono_atual_lista_vendedores_e_ve_relatorio()
        {
            var usuarios = new Mock<IClienteServico>();
            usuarios.Setup(s => s.ListarVendedoresPorLojaAsync(LojaA)).ReturnsAsync(new List<Usuario>());
            var lojistas = Lojistas(usuarios.Object, Acesso(LojaA, DonoA));
            Autenticar(lojistas, DonoA, "Lojista", LojaA);

            var lista = await lojistas.ListarVendedores(LojaA);

            Assert.IsType<OkObjectResult>(lista);
            usuarios.Verify(s => s.ListarVendedoresPorLojaAsync(LojaA), Times.Once);

            var historico = new Mock<IHistoricoPesquisaServico>();
            historico.Setup(s => s.ObterRelatorioLojaAsync(LojaA))
                .ReturnsAsync(new List<RelatorioPesquisaTermo> { new("arroz", 4, DateTime.UtcNow) });
            var relatorio = new HistoricoPesquisaController(historico.Object, Acesso(LojaA, DonoA));
            Autenticar(relatorio, DonoA, "Lojista", LojaA);

            var resultado = await relatorio.ObterRelatorioLoja(LojaA);

            Assert.IsType<OkObjectResult>(resultado);
            historico.Verify(s => s.ObterRelatorioLojaAsync(LojaA), Times.Once);
        }

        [Fact]
        public async Task Ex_dono_com_token_antigo_nao_ve_relatorio_da_loja()
        {
            var historico = new Mock<IHistoricoPesquisaServico>();
            historico.Setup(s => s.ObterRelatorioLojaAsync(LojaA))
                .ReturnsAsync(new List<RelatorioPesquisaTermo> { new("arroz", 4, DateTime.UtcNow) });
            var controller = new HistoricoPesquisaController(
                historico.Object,
                Acesso(LojaA, DonoA, ExDonoUsuario()));
            Autenticar(controller, ExDono, "Lojista", LojaA);

            var resultado = await controller.ObterRelatorioLoja(LojaA);

            Assert.IsType<ForbidResult>(resultado);
            historico.Verify(s => s.ObterRelatorioLojaAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task Ex_dono_com_token_antigo_nao_lista_nem_envia_conversas()
        {
            var conversas = ConversasVazias();
            var controller = new ConversasController(
                conversas.Object,
                new IdsFixos(),
                Notificacao(),
                Acesso(LojaA, DonoA, ExDonoUsuario()));
            Autenticar(controller, ExDono, "Lojista", LojaA);

            var lista = await controller.Listar();
            var envio = await controller.Enviar("conversa", new MensagemCriarDto { Texto = "ainda estou aqui" });

            Assert.IsType<ForbidResult>(lista);
            Assert.IsType<ForbidResult>(envio);
            conversas.Verify(
                s => s.ListarDoUsuarioAsync(It.IsAny<Guid>(), It.IsAny<PapelUsuario>(), It.IsAny<Guid?>()),
                Times.Never);
            conversas.Verify(
                s => s.EnviarMensagemAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<PapelUsuario>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task Ex_dono_no_modo_cliente_lista_a_propria_caixa()
        {
            var conversas = ConversasVazias();
            var controller = new ConversasController(
                conversas.Object,
                new IdsFixos(),
                Notificacao(),
                Acesso(LojaA, DonoA, ExDonoUsuario()));
            Autenticar(controller, ExDono, "Lojista", LojaA);
            controller.HttpContext.Request.Headers[ContextoOperacional.HeaderName] = ContextoOperacional.Cliente;

            var resultado = await controller.Listar();

            Assert.IsType<OkObjectResult>(resultado);
            conversas.Verify(
                s => s.ListarDoUsuarioAsync(ExDono, PapelUsuario.Cliente, LojaA),
                Times.Once);
        }

        [Fact]
        public async Task Dono_e_vendedor_atuais_listam_conversas_da_loja()
        {
            var doDono = ConversasVazias();
            var dono = new ConversasController(doDono.Object, new IdsFixos(), Notificacao(), Acesso(LojaA, DonoA));
            Autenticar(dono, DonoA, "Lojista", LojaA);

            var vendedor = VendedorDe(LojaA);
            var doVendedor = ConversasVazias();
            var caixa = new ConversasController(
                doVendedor.Object,
                new IdsFixos(),
                Notificacao(),
                Acesso(LojaA, DonoA, vendedor));
            Autenticar(caixa, VendedorId, "Vendedor", LojaA);

            Assert.IsType<OkObjectResult>(await dono.Listar());
            Assert.IsType<OkObjectResult>(await caixa.Listar());
            doDono.Verify(
                s => s.ListarDoUsuarioAsync(DonoA, PapelUsuario.Lojista, LojaA),
                Times.Once);
            doVendedor.Verify(
                s => s.ListarDoUsuarioAsync(VendedorId, PapelUsuario.Vendedor, LojaA),
                Times.Once);
        }

        [Fact]
        public async Task Ex_dono_com_token_antigo_nao_envia_mensagem_da_loja()
        {
            var conversaId = Guid.Parse("abababab-abab-abab-abab-abababababab");
            var repo = RepoConversa(conversaId, LojaA);
            var servico = Conversa(repo, Acesso(LojaA, DonoA, ExDonoUsuario()));

            var erro = await Record.ExceptionAsync(() =>
                servico.EnviarMensagemAsync(conversaId, ExDono, PapelUsuario.Lojista, "ainda estou aqui"));

            Assert.IsType<UnauthorizedAccessException>(erro);
            repo.Verify(r => r.AdicionarMensagemAsync(It.IsAny<Mensagem>()), Times.Never);
        }

        [Fact]
        public async Task Dono_atual_envia_mensagem_da_loja()
        {
            var conversaId = Guid.Parse("abababab-abab-abab-abab-abababababab");
            var repo = RepoConversa(conversaId, LojaA);
            var servico = Conversa(repo, Acesso(LojaA, DonoA));

            var mensagem = await servico.EnviarMensagemAsync(conversaId, DonoA, PapelUsuario.Lojista, "pode retirar");

            Assert.Equal("pode retirar", mensagem.Texto);
            Assert.Equal(PapelUsuario.Lojista, mensagem.RemetentePapel);
            repo.Verify(r => r.AdicionarMensagemAsync(It.IsAny<Mensagem>()), Times.Once);
        }

        [Fact]
        public async Task Ex_dono_com_token_antigo_nao_entra_no_grupo_da_loja()
        {
            var grupos = Grupos();
            var hub = Hub(Acesso(LojaA, DonoA, ExDonoUsuario()), grupos, ExDono, "Lojista");

            var erro = await Record.ExceptionAsync(() => hub.ConectarUsuario());

            Assert.IsType<HubException>(erro);
            grupos.Verify(
                g => g.AddToGroupAsync(It.IsAny<string>(), $"loja:{LojaA}", It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Vendedor_atual_entra_no_grupo_da_loja()
        {
            var grupos = Grupos();
            var hub = Hub(Acesso(LojaA, DonoA, VendedorDe(LojaA)), grupos, VendedorId, "Vendedor");

            await hub.ConectarUsuario();

            grupos.Verify(
                g => g.AddToGroupAsync(It.IsAny<string>(), $"loja:{LojaA}", It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Vendedor_removido_nao_atualiza_oferta_com_token_antigo()
        {
            var ofertas = OfertaPronta();
            var removido = VendedorDe(LojaA);
            removido.Papel = PapelUsuario.Cliente;
            removido.Tipo = TipoUsuario.Cliente;
            removido.LojaVinculadaId = null;
            var controller = Ofertas(ofertas, Acesso(LojaA, DonoA, removido));
            Autenticar(controller, VendedorId, "Vendedor", LojaA);

            var resultado = await controller.Atualizar(OfertaId, OfertaDto());

            Assert.IsType<ForbidResult>(resultado);
            ofertas.Verify(s => s.AtualizarAsync(It.IsAny<Oferta>()), Times.Never);
        }

        [Fact]
        public async Task Dono_vendedor_e_admin_atuais_atualizam_oferta()
        {
            var doDono = OfertaPronta();
            var dono = Ofertas(doDono, Acesso(LojaA, DonoA));
            Autenticar(dono, DonoA, "Lojista", LojaA);

            var doVendedor = OfertaPronta();
            var vendedor = Ofertas(doVendedor, Acesso(LojaA, DonoA, VendedorDe(LojaA)));
            Autenticar(vendedor, VendedorId, "Vendedor", LojaA);

            var doAdmin = OfertaPronta();
            var admin = Ofertas(doAdmin, Montar(LojaA, DonoA, new Admin
            {
                Id = AdminId,
                NomeUsuario = "Admin",
                Email = "admin@loja.test",
                Ativo = true
            }));
            Autenticar(admin, AdminId, "Admin", null);

            Assert.IsType<NoContentResult>(await dono.Atualizar(OfertaId, OfertaDto()));
            Assert.IsType<NoContentResult>(await vendedor.Atualizar(OfertaId, OfertaDto()));
            Assert.IsType<NoContentResult>(await admin.Atualizar(OfertaId, OfertaDto()));
            doDono.Verify(s => s.AtualizarAsync(It.IsAny<Oferta>()), Times.Once);
            doVendedor.Verify(s => s.AtualizarAsync(It.IsAny<Oferta>()), Times.Once);
            doAdmin.Verify(s => s.AtualizarAsync(It.IsAny<Oferta>()), Times.Once);
        }

        [Fact]
        public async Task Admin_rebaixado_nao_pertence_a_loja()
        {
            var admins = new Mock<IAdminRepositorio>();
            admins.Setup(r => r.ObterPorIdAsync(AdminId)).ReturnsAsync(new Admin
            {
                Id = AdminId,
                NomeUsuario = "Admin",
                Email = "admin@loja.test",
                Ativo = false
            });
            var acesso = new LojaAcesso(
                Mock.Of<ILojaRepositorio>(),
                Mock.Of<IClienteRepositorio>(),
                admins.Object);

            var pertence = await Authz.PertenceAsync(Principal(AdminId, "Admin", null), LojaA, acesso);

            Assert.False(pertence);
        }

        [Fact]
        public async Task Admin_ativo_no_banco_pertence_a_loja()
        {
            var admins = new Mock<IAdminRepositorio>();
            admins.Setup(r => r.ObterPorIdAsync(AdminId)).ReturnsAsync(new Admin
            {
                Id = AdminId,
                NomeUsuario = "Admin",
                Email = "admin@loja.test",
                Ativo = true
            });
            var acesso = new LojaAcesso(
                Mock.Of<ILojaRepositorio>(),
                Mock.Of<IClienteRepositorio>(),
                admins.Object);

            var pertence = await Authz.PertenceAsync(Principal(AdminId, "Admin", null), LojaA, acesso);

            Assert.True(pertence);
        }

        private static Usuario VendedorDe(Guid lojaId) => new()
        {
            Id = VendedorId,
            NomeUsuario = "Vendedor",
            Email = "vendedor@loja.test",
            Papel = PapelUsuario.Vendedor,
            Tipo = TipoUsuario.Vendedor,
            LojaVinculadaId = lojaId
        };

        private static Usuario ExDonoUsuario() => new()
        {
            Id = ExDono,
            NomeUsuario = "Ex",
            Email = "ex@loja.test",
            Papel = PapelUsuario.Lojista,
            Tipo = TipoUsuario.Lojista
        };

        private static Mock<IClienteRepositorio> RepoCom(Usuario usuario)
        {
            var repo = new Mock<IClienteRepositorio>();
            repo.Setup(r => r.ObterPorIdAsync(usuario.Id)).ReturnsAsync(usuario);
            repo.Setup(r => r.AtualizarAsync(It.IsAny<Usuario>())).Returns(Task.CompletedTask);
            return repo;
        }

        private static ClienteServico Servico(Mock<IClienteRepositorio> repo) =>
            new(repo.Object, Mock.Of<IPasswordHasher>(), Mock.Of<IValidadorEmail>());

        private static LojaAcesso Acesso(Guid lojaId, Guid donoId, params Usuario[] usuarios) =>
            Montar(lojaId, donoId, null, usuarios);

        private static LojaAcesso Montar(Guid lojaId, Guid donoId, Admin? admin, params Usuario[] usuarios)
        {
            var lojas = new Mock<ILojaRepositorio>();
            lojas.Setup(r => r.ObterPorIdAsync(lojaId)).ReturnsAsync(new Loja
            {
                Id = lojaId,
                UsuarioId = donoId,
                NomeFantasia = "Loja"
            });
            var pessoas = new Mock<IClienteRepositorio>();
            pessoas.Setup(r => r.ObterPorIdAsync(It.IsAny<Guid>())).ReturnsAsync((Usuario?)null);
            pessoas.Setup(r => r.ObterPorIdAsync(donoId)).ReturnsAsync(new Usuario
            {
                Id = donoId,
                NomeUsuario = "Dono",
                Email = "dono@loja.test",
                Papel = PapelUsuario.Lojista,
                Tipo = TipoUsuario.Lojista,
                Ativo = true
            });
            foreach (var usuario in usuarios)
                pessoas.Setup(r => r.ObterPorIdAsync(usuario.Id)).ReturnsAsync(usuario);

            IAdminRepositorio? admins = null;
            if (admin is not null)
            {
                var repo = new Mock<IAdminRepositorio>();
                repo.Setup(r => r.ObterPorIdAsync(admin.Id)).ReturnsAsync(admin);
                admins = repo.Object;
            }

            return new LojaAcesso(lojas.Object, pessoas.Object, admins);
        }

        private static LojistasController Lojistas(IClienteServico servico, ILojaAcesso acesso) =>
            new(servico, Mock.Of<IIdCodificador>(), acesso);

        private static Mock<IOfertaServico> OfertaPronta()
        {
            var ofertas = new Mock<IOfertaServico>();
            ofertas.Setup(s => s.ObterPorIdAsync(OfertaId)).ReturnsAsync(new Oferta
            {
                Id = OfertaId,
                LojaId = LojaA,
                ProdutoId = ProdutoId,
                Preco = 10m
            });
            ofertas.Setup(s => s.AtualizarAsync(It.IsAny<Oferta>())).Returns(Task.CompletedTask);
            return ofertas;
        }

        private static OfertaCriarDto OfertaDto() => new()
        {
            ProdutoId = ProdutoId,
            LojaId = LojaA,
            Preco = 1m
        };

        private static OfertasController Ofertas(Mock<IOfertaServico> ofertas, ILojaAcesso acesso)
        {
            var produtos = new Mock<IProdutoServico>();
            produtos.Setup(s => s.ObterPorIdAsync(ProdutoId)).ReturnsAsync(new Produto
            {
                Id = ProdutoId,
                LojaId = LojaA,
                NomeProduto = "Arroz"
            });
            return new(ofertas.Object, Mock.Of<IIdCodificador>(), produtos.Object, acesso);
        }

        private static Mock<IConversaServico> ConversasVazias()
        {
            var conversas = new Mock<IConversaServico>();
            conversas.Setup(s => s.ListarDoUsuarioAsync(It.IsAny<Guid>(), It.IsAny<PapelUsuario>(), It.IsAny<Guid?>()))
                .ReturnsAsync(new List<Conversa>());
            conversas.Setup(s => s.ContarNaoLidasPorConversasAsync(It.IsAny<Guid>(), It.IsAny<PapelUsuario>(), It.IsAny<Guid?>()))
                .ReturnsAsync(new Dictionary<Guid, int>());
            conversas.Setup(s => s.ObterUltimasMensagensTextoAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(new Dictionary<Guid, string>());
            return conversas;
        }

        private static Mock<IConversaRepositorio> RepoConversa(Guid conversaId, Guid lojaId)
        {
            var repo = new Mock<IConversaRepositorio>();
            repo.Setup(r => r.ObterPorIdAsync(conversaId)).ReturnsAsync(new Conversa
            {
                Id = conversaId,
                ClienteId = Guid.NewGuid(),
                LojaId = lojaId
            });
            repo.Setup(r => r.AdicionarMensagemAsync(It.IsAny<Mensagem>()))
                .ReturnsAsync((Mensagem m) => m);
            repo.Setup(r => r.AtualizarAsync(It.IsAny<Conversa>())).Returns(Task.CompletedTask);
            return repo;
        }

        private static ConversaServico Conversa(Mock<IConversaRepositorio> repo, ILojaAcesso acesso) =>
            new(
                repo.Object,
                Mock.Of<IClienteRepositorio>(),
                Mock.Of<ILojaRepositorio>(),
                Mock.Of<IProdutoRepositorio>(),
                Mock.Of<IOfertaRepositorio>(),
                acesso,
                TimeProvider.System);

        private static Mock<IGroupManager> Grupos()
        {
            var grupos = new Mock<IGroupManager>();
            grupos.Setup(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            return grupos;
        }

        private static ChatHub Hub(ILojaAcesso acesso, Mock<IGroupManager> grupos, Guid userId, string role)
        {
            return new ChatHub(Mock.Of<IConversaServico>(), new IdsFixos(), Notificacao(), acesso)
            {
                Context = Contexto(userId, role, LojaA),
                Groups = grupos.Object
            };
        }

        private static ChatNotificacaoHelper Notificacao() =>
            new(Mock.Of<IHubContext<ChatHub>>(), new IdsFixos());

        private static HubCallerContext Contexto(Guid userId, string role, Guid? lojaId)
        {
            var contexto = new Mock<HubCallerContext>();
            contexto.Setup(c => c.ConnectionId).Returns("conexao-1");
            contexto.Setup(c => c.User).Returns(Principal(userId, role, lojaId));
            contexto.Setup(c => c.Features).Returns(new FeatureCollection());
            return contexto.Object;
        }

        private static void Autenticar(ControllerBase controller, Guid userId, string role, Guid? lojaId)
        {
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = Principal(userId, role, lojaId) }
            };
        }

        private static ClaimsPrincipal Principal(Guid userId, string role, Guid? lojaId)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new(ClaimTypes.Role, role)
            };
            if (lojaId.HasValue)
                claims.Add(new Claim("lojaId", lojaId.Value.ToString()));
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        }

        private sealed class IdsFixos : IIdCodificador
        {
            public string Codificar(Guid id) => id.ToString("N");
            public Guid? Decodificar(string codigo) => Guid.Empty;
            public bool TentarDecodificar(string codigo, out Guid id)
            {
                id = Guid.Parse("abababab-abab-abab-abab-abababababab");
                return true;
            }
        }
    }
}
