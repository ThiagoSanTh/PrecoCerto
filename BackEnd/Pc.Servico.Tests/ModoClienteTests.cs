using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Pc.Dominio.Enums;
using Pc.WebApi.Authorization;
using Pc.WebApi.Configuration;
using Pc.WebApi.Extensions;
using Pc.WebApi.Services;
using Xunit;

namespace Pc.Servico.Tests
{
    public class PapelClaimsTests
    {
        [Fact]
        public void Cliente_comum_so_tem_role_cliente()
        {
            Assert.Equal(new[] { "Cliente" }, PapelClaims.RolesPara(TipoUsuario.Cliente));
        }

        [Fact]
        public void Lojista_mantem_capacidade_de_cliente()
        {
            Assert.Equal(new[] { "Cliente", "Lojista" }, PapelClaims.RolesPara(TipoUsuario.Lojista));
        }

        [Fact]
        public void Vendedor_mantem_capacidade_de_cliente()
        {
            Assert.Equal(new[] { "Cliente", "Vendedor" }, PapelClaims.RolesPara(TipoUsuario.Vendedor));
        }

        [Fact]
        public void Admin_nao_recebe_role_cliente()
        {
            Assert.Equal(new[] { "Admin" }, PapelClaims.RolesPara(TipoUsuario.Admin));
        }
    }

    public class ContextoOperacionalTests
    {
        [Fact]
        public void Cenario1_cliente_sem_loja_em_modo_cliente()
        {
            var user = Principal("Cliente");
            Assert.Equal(PapelUsuario.Cliente, ContextoOperacional.ResolverPapel(user, "cliente"));
            Assert.True(user.IsCliente());
            Assert.False(user.IsLojista());
        }

        [Fact]
        public void Cenario2_lojista_em_modo_cliente_usa_persona_de_cliente()
        {
            var user = Principal("Cliente", "Lojista");
            Assert.Equal(PapelUsuario.Cliente, ContextoOperacional.ResolverPapel(user, "cliente"));
            Assert.True(user.IsCliente());
            Assert.True(user.IsLojista());
        }

        [Fact]
        public void Cenario3_lojista_em_modo_loja_usa_persona_de_lojista()
        {
            var user = Principal("Cliente", "Lojista");
            Assert.Equal(PapelUsuario.Lojista, ContextoOperacional.ResolverPapel(user, "loja"));
            Assert.True(user.IsLojista());
        }

        [Fact]
        public void Cenario4_troca_loja_para_cliente()
        {
            var user = Principal("Cliente", "Lojista");
            Assert.Equal(PapelUsuario.Lojista, ContextoOperacional.ResolverPapel(user, "loja"));
            Assert.Equal(PapelUsuario.Cliente, ContextoOperacional.ResolverPapel(user, "cliente"));
        }

        [Fact]
        public void Cenario5_troca_cliente_para_loja()
        {
            var user = Principal("Cliente", "Lojista");
            Assert.Equal(PapelUsuario.Cliente, ContextoOperacional.ResolverPapel(user, "cliente"));
            Assert.Equal(PapelUsuario.Lojista, ContextoOperacional.ResolverPapel(user, "loja"));
        }

        [Fact]
        public void Cliente_comum_nao_assume_loja_mesmo_pedindo_contexto_loja()
        {
            var user = Principal("Cliente");
            Assert.Equal(PapelUsuario.Cliente, ContextoOperacional.ResolverPapel(user, "loja"));
        }

        [Fact]
        public void Header_tem_prioridade_sobre_query()
        {
            var ctx = new DefaultHttpContext();
            ctx.Request.Headers[ContextoOperacional.HeaderName] = "cliente";
            ctx.Request.QueryString = new QueryString("?contexto=loja");
            Assert.Equal("cliente", ContextoOperacional.Ler(ctx.Request));
        }

        [Fact]
        public void Le_contexto_da_query_quando_nao_ha_header()
        {
            var ctx = new DefaultHttpContext();
            ctx.Request.QueryString = new QueryString("?contexto=loja");
            Assert.Equal("loja", ContextoOperacional.Ler(ctx.Request));
        }

        private static ClaimsPrincipal Principal(params string[] roles)
        {
            var claims = roles.Select(r => new Claim(ClaimTypes.Role, r));
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        }
    }

    public class JwtTokenServiceModoClienteTests
    {
        [Fact]
        public void Token_de_lojista_inclui_role_cliente()
        {
            var token = CriarSut().GenerateToken(Guid.NewGuid(), TipoUsuario.Lojista, Guid.NewGuid());
            var roles = RolesDoToken(token);
            Assert.Contains("Cliente", roles);
            Assert.Contains("Lojista", roles);
        }

        [Fact]
        public void Token_de_cliente_nao_inclui_role_lojista()
        {
            var token = CriarSut().GenerateToken(Guid.NewGuid(), TipoUsuario.Cliente);
            var roles = RolesDoToken(token);
            Assert.Contains("Cliente", roles);
            Assert.DoesNotContain("Lojista", roles);
        }

        [Fact]
        public void Token_de_admin_nao_inclui_role_cliente()
        {
            var token = CriarSut().GenerateToken(Guid.NewGuid(), TipoUsuario.Admin);
            var roles = RolesDoToken(token);
            Assert.Contains("Admin", roles);
            Assert.DoesNotContain("Cliente", roles);
        }

        private static JwtTokenService CriarSut()
        {
            var settings = Options.Create(new JwtSettings
            {
                Secret = "test-secret-key-at-least-32-chars!!",
                Issuer = "PrecoCerto",
                Audience = "PrecoCertoApp",
                ExpirationHours = 1
            });
            return new JwtTokenService(settings);
        }

        private static List<string> RolesDoToken(string token)
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            return jwt.Claims
                .Where(c => c.Type is ClaimTypes.Role or "role")
                .Select(c => c.Value)
                .ToList();
        }
    }

    public class LoginLojaResolverTests
    {
        [Fact]
        public void Vendedor_usa_loja_vinculada_nao_a_loja_do_dono()
        {
            var lojaDono = Guid.NewGuid();
            var lojaVinculada = Guid.NewGuid();
            var vendedorId = Guid.NewGuid();
            var usuario = new Pc.Dominio.Entities.Usuarios.Usuario
            {
                Id = vendedorId,
                Email = "thiagoalmeidasantanay@gmail.com",
                NomeUsuario = "Thiago",
                Papel = PapelUsuario.Vendedor,
                Tipo = TipoUsuario.Vendedor,
                LojaVinculadaId = lojaVinculada,
                LojaVinculada = new Pc.Dominio.Entities.Estabelecimentos.Loja
                {
                    Id = lojaVinculada,
                    NomeFantasia = "Brink",
                    UsuarioId = lojaDono
                }
            };

            Assert.True(LoginLojaResolver.EhStaffLoja(usuario));
            Assert.Equal(TipoUsuario.Vendedor, LoginLojaResolver.ResolverTipoJwt(usuario));
            Assert.Equal("vendedor", LoginLojaResolver.ResolverTipoResposta(usuario));
            Assert.Equal(lojaVinculada, LoginLojaResolver.ResolverLojaId(usuario));
            Assert.Equal("Brink", LoginLojaResolver.ResolverNomeLoja(usuario));
            Assert.NotEqual(lojaDono, usuario.Id);
        }

        [Fact]
        public void Vendedor_com_papel_desatualizado_ainda_e_staff_pela_loja_vinculada()
        {
            var lojaId = Guid.NewGuid();
            var usuario = new Pc.Dominio.Entities.Usuarios.Usuario
            {
                Id = Guid.NewGuid(),
                Email = "thiago@exemplo.com",
                Papel = PapelUsuario.Cliente,
                Tipo = TipoUsuario.Cliente,
                LojaVinculadaId = lojaId,
                LojaVinculada = new Pc.Dominio.Entities.Estabelecimentos.Loja
                {
                    Id = lojaId,
                    NomeFantasia = "Brink"
                }
            };

            Assert.True(LoginLojaResolver.EhStaffLoja(usuario));
            Assert.Equal(TipoUsuario.Vendedor, LoginLojaResolver.ResolverTipoJwt(usuario));
            Assert.Equal(lojaId, LoginLojaResolver.ResolverLojaId(usuario));
            Assert.Equal("Brink", LoginLojaResolver.ResolverNomeLoja(usuario));
        }

        [Fact]
        public void Lojista_usa_loja_propria()
        {
            var lojaId = Guid.NewGuid();
            var usuario = new Pc.Dominio.Entities.Usuarios.Usuario
            {
                Id = Guid.NewGuid(),
                Papel = PapelUsuario.Lojista,
                LojaPropria = new Pc.Dominio.Entities.Estabelecimentos.Loja
                {
                    Id = lojaId,
                    NomeFantasia = "Brink"
                }
            };

            Assert.Equal(TipoUsuario.Lojista, LoginLojaResolver.ResolverTipoJwt(usuario));
            Assert.Equal(lojaId, LoginLojaResolver.ResolverLojaId(usuario));
            Assert.Equal("Brink", LoginLojaResolver.ResolverNomeLoja(usuario));
        }

        [Fact]
        public void Cliente_sem_loja_nao_e_staff()
        {
            var usuario = new Pc.Dominio.Entities.Usuarios.Usuario
            {
                Id = Guid.NewGuid(),
                Papel = PapelUsuario.Cliente
            };

            Assert.False(LoginLojaResolver.EhStaffLoja(usuario));
            Assert.Equal(TipoUsuario.Cliente, LoginLojaResolver.ResolverTipoJwt(usuario));
            Assert.Null(LoginLojaResolver.ResolverLojaId(usuario));
        }
    }
}
