using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Pc.Dominio.Entities.Estabelecimentos;
using Pc.Dominio.Entities.Interacoes;
using Pc.Dominio.Entities.Usuarios;
using Pc.Dominio.Enums;
using Pc.Repositorio.Interfaces;
using Pc.Servico.Implementacoes;
using Pc.Servico.Interfaces;
using Pc.WebApi.Controllers;
using Pc.WebApi.DTOs.Estabelecimentos;
using Pc.WebApi.Hubs;
using Xunit;

namespace Pc.Servico.Tests
{
    public class PresencaChatTests
    {
        private static readonly Guid LojaId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid OutraLoja = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid DonoId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid NovoDono = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        private static readonly Guid VendedorId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        private static readonly Guid ConversaId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

        [Fact]
        public async Task Revogar_tira_a_conexao_da_loja_e_da_conversa()
        {
            var removidos = new List<string>();
            var grupos = Grupos(removidos);
            var presenca = Presenca(grupos);
            var hub = Hub(presenca, grupos, DonoId, "Lojista");

            await hub.ConectarUsuario();
            await hub.EntrarConversa("conversa-publica");
            await presenca.RevogarAsync(DonoId, LojaId);

            Assert.Contains($"loja:{LojaId}", removidos);
            Assert.Contains("conversa-publica", removidos);
            Assert.DoesNotContain($"usuario:{DonoId}", removidos);
            Assert.DoesNotContain($"loja:{OutraLoja}", removidos);
        }

        [Fact]
        public async Task Remover_vendedor_tira_a_conexao_dos_grupos_da_loja()
        {
            var removidos = new List<string>();
            var presenca = Presenca(Grupos(removidos));
            presenca.Ligar("c1", VendedorId);
            presenca.Loja("c1", VendedorId, LojaId);
            presenca.Conversa("c1", VendedorId, LojaId, "conversa-publica");

            var vendedor = new Usuario
            {
                Id = VendedorId,
                NomeUsuario = "Vend",
                Email = "v@loja.test",
                Papel = PapelUsuario.Vendedor,
                Tipo = TipoUsuario.Vendedor,
                LojaVinculadaId = LojaId
            };
            var repo = new Mock<IClienteRepositorio>();
            repo.Setup(r => r.ObterPorIdAsync(VendedorId)).ReturnsAsync(vendedor);
            repo.Setup(r => r.AtualizarAsync(It.IsAny<Usuario>())).Returns(Task.CompletedTask);
            var controller = new LojistasController(
                new ClienteServico(repo.Object, Mock.Of<IPasswordHasher>(), Mock.Of<IValidadorEmail>()),
                Mock.Of<IIdCodificador>(),
                AcessoDono(),
                presenca);
            Autenticar(controller, DonoId, "Lojista", LojaId);

            var resultado = await controller.RemoverVendedor(LojaId, VendedorId);

            Assert.IsType<NoContentResult>(resultado);
            Assert.Contains($"loja:{LojaId}", removidos);
            Assert.Contains("conversa-publica", removidos);
        }

        [Fact]
        public async Task Transferir_loja_tira_o_ex_dono_dos_grupos()
        {
            var removidos = new List<string>();
            var presenca = Presenca(Grupos(removidos));
            presenca.Ligar("c1", DonoId);
            presenca.Loja("c1", DonoId, LojaId);
            presenca.Conversa("c1", DonoId, LojaId, "conversa-publica");
            presenca.Loja("c1", DonoId, OutraLoja);

            var lojas = new Mock<ILojaServico>();
            lojas.Setup(s => s.ObterPorIdAsync(LojaId)).ReturnsAsync(new Loja
            {
                Id = LojaId,
                UsuarioId = DonoId,
                NomeFantasia = "Loja"
            });
            lojas.Setup(s => s.AtualizarAsync(It.IsAny<Loja>())).Returns(Task.CompletedTask);
            lojas.Setup(s => s.ObterPorUsuarioIdAsync(NovoDono)).ReturnsAsync((Loja?)null);
            var usuarios = new Mock<IClienteServico>();
            usuarios.Setup(s => s.ObterPorIdAsync(NovoDono)).ReturnsAsync(new Usuario { Id = NovoDono, NomeUsuario = "Novo", Email = "n@loja.test" });

            var controller = new LojasController(
                lojas.Object,
                usuarios.Object,
                Mock.Of<IConsultaCnpjServico>(),
                Mock.Of<IIdCodificador>(),
                presenca);
            Autenticar(controller, Guid.Parse("99999999-9999-9999-9999-999999999999"), "Admin", null);

            var resultado = await controller.Atualizar(LojaId, new LojaCriarDto
            {
                NomeFantasia = "Loja",
                UsuarioId = NovoDono
            });

            Assert.IsType<NoContentResult>(resultado);
            Assert.Contains($"loja:{LojaId}", removidos);
            Assert.Contains("conversa-publica", removidos);
            Assert.DoesNotContain($"loja:{OutraLoja}", removidos);
        }

        [Fact]
        public async Task Conectar_entrar_e_conectar_de_novo_ainda_revoga_a_conversa()
        {
            await SequenciaRevogaConversa(async hub =>
            {
                await hub.ConectarUsuario();
                await hub.EntrarConversa("conversa-publica");
                await hub.ConectarUsuario();
            });
        }

        [Fact]
        public async Task So_entrar_na_conversa_ainda_revoga_a_conversa()
        {
            await SequenciaRevogaConversa(async hub =>
            {
                await hub.EntrarConversa("conversa-publica");
            });
        }

        [Fact]
        public async Task Entrar_e_depois_conectar_ainda_revoga_a_conversa()
        {
            await SequenciaRevogaConversa(async hub =>
            {
                await hub.EntrarConversa("conversa-publica");
                await hub.ConectarUsuario();
            });
        }

        [Fact]
        public async Task Entrar_duas_vezes_revoga_a_conversa_uma_vez()
        {
            var removidos = new List<string>();
            var grupos = Grupos(removidos);
            var presenca = Presenca(grupos);
            var hub = Hub(presenca, grupos, DonoId, "Lojista");

            await hub.ConectarUsuario();
            await hub.EntrarConversa("conversa-publica");
            await hub.EntrarConversa("conversa-publica");
            await presenca.RevogarAsync(DonoId, LojaId);

            Assert.Equal(1, removidos.Count(g => g == "conversa-publica"));
        }

        [Fact]
        public async Task Ex_vendedor_como_cliente_mantem_a_propria_conversa()
        {
            var removidos = new List<string>();
            var grupos = Grupos(removidos);
            var presenca = Presenca(grupos);
            var hub = Hub(presenca, grupos, VendedorId, "Vendedor", "cliente");

            await hub.ConectarUsuario();
            await hub.EntrarConversa("conversa-publica");
            await presenca.RevogarAsync(VendedorId, LojaId);

            Assert.DoesNotContain("conversa-publica", removidos);
        }

        private static async Task SequenciaRevogaConversa(Func<ChatHub, Task> sequencia)
        {
            var removidos = new List<string>();
            var grupos = Grupos(removidos);
            var presenca = Presenca(grupos);
            var hub = Hub(presenca, grupos, DonoId, "Lojista");

            await sequencia(hub);
            await presenca.RevogarAsync(DonoId, LojaId);

            Assert.Contains("conversa-publica", removidos);
        }

        private static LojaAcesso AcessoDono()
        {
            var lojas = new Mock<ILojaRepositorio>();
            lojas.Setup(r => r.ObterPorIdAsync(LojaId)).ReturnsAsync(new Loja
            {
                Id = LojaId,
                UsuarioId = DonoId,
                NomeFantasia = "Loja"
            });
            var pessoas = new Mock<IClienteRepositorio>();
            pessoas.Setup(r => r.ObterPorIdAsync(It.IsAny<Guid>())).ReturnsAsync((Usuario?)null);
            return new LojaAcesso(lojas.Object, pessoas.Object);
        }

        private static Mock<IGroupManager> Grupos(List<string> removidos)
        {
            var grupos = new Mock<IGroupManager>();
            grupos.Setup(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            grupos.Setup(g => g.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((_, grupo, _) => removidos.Add(grupo))
                .Returns(Task.CompletedTask);
            return grupos;
        }

        private static PresencaChat Presenca(Mock<IGroupManager> grupos)
        {
            var hub = new Mock<IHubContext<ChatHub>>();
            hub.Setup(h => h.Groups).Returns(grupos.Object);
            return new PresencaChat(hub.Object);
        }

        private static ChatHub Hub(
            PresencaChat presenca,
            Mock<IGroupManager> grupos,
            Guid userId,
            string role,
            string? contexto = null)
        {
            var conversas = new Mock<IConversaServico>();
            conversas.Setup(s => s.UsuarioPodeAcessarAsync(ConversaId, userId, It.IsAny<PapelUsuario>()))
                .ReturnsAsync(true);
            conversas.Setup(s => s.MarcarComoRecebidasAsync(ConversaId, userId)).Returns(Task.CompletedTask);
            conversas.Setup(s => s.ObterPorIdAsync(ConversaId)).ReturnsAsync(new Conversa
            {
                Id = ConversaId,
                LojaId = LojaId,
                ClienteId = Guid.NewGuid()
            });
            var acesso = new Mock<ILojaAcesso>();
            acesso.Setup(a => a.PertenceAsync(userId, LojaId)).ReturnsAsync(true);

            var hub = new ChatHub(
                conversas.Object,
                new Ids(),
                new ChatNotificacaoHelper(Mock.Of<IHubContext<ChatHub>>(), new Ids()),
                acesso.Object,
                presenca)
            {
                Context = Contexto(userId, role, contexto),
                Groups = grupos.Object
            };
            return hub;
        }

        private static HubCallerContext Contexto(Guid userId, string role, string? contextoOperacional = null)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new(ClaimTypes.Role, role),
                new("lojaId", LojaId.ToString())
            };
            var features = new FeatureCollection();
            if (contextoOperacional != null)
            {
                var http = new DefaultHttpContext();
                http.Request.QueryString = new QueryString($"?contexto={contextoOperacional}");
                features.Set<IHttpContextFeature>(new HttpDoHub { HttpContext = http });
            }

            var contexto = new Mock<HubCallerContext>();
            contexto.Setup(c => c.ConnectionId).Returns("c1");
            contexto.Setup(c => c.User).Returns(new ClaimsPrincipal(new ClaimsIdentity(claims, "test")));
            contexto.Setup(c => c.Features).Returns(features);
            return contexto.Object;
        }

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

        private sealed class HttpDoHub : IHttpContextFeature
        {
            public HttpContext? HttpContext { get; set; }
        }

        private sealed class Ids : IIdCodificador
        {
            public string Codificar(Guid id) => "conversa-publica";
            public Guid? Decodificar(string codigo) => ConversaId;
            public bool TentarDecodificar(string codigo, out Guid id)
            {
                id = ConversaId;
                return true;
            }
        }
    }
}
