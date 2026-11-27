using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Pc.Dominio.Entities.Usuarios;
using Pc.Repositorio.Interfaces;
using Pc.Servico.Implementacoes;
using Pc.Servico.Interfaces;
using Pc.Servico.Modelos;
using Pc.WebApi.Controllers;
using Pc.WebApi.DTOs.Comum;
using Pc.WebApi.Services;
using Xunit;

namespace Pc.Servico.Tests
{
    public class AuthLoginTests
    {
        [Fact]
        public async Task Login_email_inexistente_e_senha_incorreta_devolvem_o_mesmo_401()
        {
            var usuarios = new Mock<IClienteServico>();
            usuarios
                .Setup(s => s.ValidarLoginDetalhadoAsync("qa-inexistente@example.com", "senhaerrada123"))
                .ReturnsAsync(LoginValidacaoResult.EmailNaoEncontrado());
            usuarios
                .Setup(s => s.ValidarLoginDetalhadoAsync("existe@example.com", "senhaerrada123"))
                .ReturnsAsync(LoginValidacaoResult.SenhaIncorreta());

            var admins = new Mock<IAdminServico>();
            admins
                .Setup(s => s.ValidarLoginAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((Admin?)null);

            var controller = CriarAuth(usuarios.Object, admins.Object);

            var inexistente = await controller.Login(new AuthLoginDto
            {
                Email = "qa-inexistente@example.com",
                Senha = "senhaerrada123"
            });
            var senhaErrada = await controller.Login(new AuthLoginDto
            {
                Email = "existe@example.com",
                Senha = "senhaerrada123"
            });

            var corpoInexistente = CorpoUnauthorized(inexistente);
            var corpoSenhaErrada = CorpoUnauthorized(senhaErrada);

            Assert.Equal(JsonSerializer.Serialize(corpoInexistente), JsonSerializer.Serialize(corpoSenhaErrada));
            var json = JsonSerializer.Serialize(corpoInexistente);
            Assert.Contains("INVALID_CREDENTIALS", json);
            Assert.DoesNotContain("EMAIL_NOT_FOUND", json);
            Assert.DoesNotContain("WRONG_PASSWORD", json);
            Assert.DoesNotContain("não existe", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Senha incorreta", json, StringComparison.Ordinal);
        }

        [Fact]
        public async Task Clientes_login_email_inexistente_e_senha_incorreta_devolvem_o_mesmo_401()
        {
            var usuarios = new Mock<IClienteServico>();
            usuarios
                .Setup(s => s.ValidarLoginDetalhadoAsync("qa-inexistente@example.com", "senhaerrada123"))
                .ReturnsAsync(LoginValidacaoResult.EmailNaoEncontrado());
            usuarios
                .Setup(s => s.ValidarLoginDetalhadoAsync("existe@example.com", "senhaerrada123"))
                .ReturnsAsync(LoginValidacaoResult.SenhaIncorreta());

            var controller = new ClientesController(
                usuarios.Object,
                Mock.Of<IJwtTokenService>(),
                Mock.Of<IEmailService>(),
                Mock.Of<IIdCodificador>());

            var inexistente = await controller.Login(new LoginDto
            {
                Email = "qa-inexistente@example.com",
                Senha = "senhaerrada123"
            });
            var senhaErrada = await controller.Login(new LoginDto
            {
                Email = "existe@example.com",
                Senha = "senhaerrada123"
            });

            Assert.Equal(
                JsonSerializer.Serialize(CorpoUnauthorized(inexistente)),
                JsonSerializer.Serialize(CorpoUnauthorized(senhaErrada)));
            Assert.Contains("INVALID_CREDENTIALS", JsonSerializer.Serialize(CorpoUnauthorized(inexistente)));
        }

        [Fact]
        public void AuthLoginDto_email_invalido_tem_mensagem_em_portugues()
        {
            var erros = Validar(new AuthLoginDto { Email = "nao-e-um-email", Senha = "123456" });

            Assert.Contains(erros, e => e.MemberNames.Contains("Email") && e.ErrorMessage == "E-mail inválido.");
            Assert.DoesNotContain(
                erros,
                e => e.ErrorMessage != null && e.ErrorMessage.Contains("not a valid e-mail", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void LoginDto_email_invalido_tem_mensagem_em_portugues()
        {
            var erros = Validar(new LoginDto { Email = "nao-e-um-email", Senha = "123456" });

            Assert.Contains(erros, e => e.MemberNames.Contains("Email") && e.ErrorMessage == "E-mail inválido.");
        }

        [Fact]
        public void EsqueciSenhaDto_email_invalido_tem_mensagem_em_portugues()
        {
            var erros = Validar(new EsqueciSenhaDto { Email = "nao-e-um-email" });

            Assert.Contains(erros, e => e.MemberNames.Contains("Email") && e.ErrorMessage == "E-mail inválido.");
        }

        [Fact]
        public void AuthLoginDto_senha_curta_tem_mensagem_em_portugues()
        {
            var erros = Validar(new AuthLoginDto { Email = "qa@example.com", Senha = "123" });

            Assert.Contains(erros, e => e.MemberNames.Contains("Senha") && e.ErrorMessage == "Senha deve ter pelo menos 6 caracteres.");
        }

        [Fact]
        public async Task Usuario_inexistente_verifica_hash_ficticio()
        {
            var repo = new Mock<IClienteRepositorio>();
            repo.Setup(r => r.ObterPorEmailAsync(It.IsAny<string>())).ReturnsAsync((Usuario?)null);

            string? hashVerificado = null;
            var hasher = new Mock<IPasswordHasher>();
            hasher
                .Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((_, hash) => hashVerificado = hash)
                .Returns(false);

            var servico = new ClienteServico(repo.Object, hasher.Object, Mock.Of<IValidadorEmail>());
            var resultado = await servico.ValidarLoginDetalhadoAsync("qa-inexistente@example.com", "senhaerrada123");

            Assert.Equal(LoginErroCodigo.EmailNaoEncontrado, resultado.Erro);
            Assert.NotNull(hashVerificado);
            Assert.Equal("12", BCrypt.Net.BCrypt.InterrogateHash(hashVerificado).WorkFactor.ToString());
            hasher.Verify(h => h.Verify("senhaerrada123", hashVerificado), Times.Once);
        }

        [Fact]
        public async Task Senha_incorreta_continua_distinta_no_servico()
        {
            var usuario = new Usuario
            {
                Email = "existe@example.com",
                SenhaHash = "$2a$12$hash-do-usuario",
                Ativo = true
            };
            var repo = new Mock<IClienteRepositorio>();
            repo.Setup(r => r.ObterPorEmailAsync("existe@example.com")).ReturnsAsync(usuario);

            var hasher = new Mock<IPasswordHasher>();
            hasher.Setup(h => h.Verify("senhaerrada123", usuario.SenhaHash)).Returns(false);
            hasher.Setup(h => h.IsBcryptHash(usuario.SenhaHash)).Returns(true);

            var servico = new ClienteServico(repo.Object, hasher.Object, Mock.Of<IValidadorEmail>());
            var resultado = await servico.ValidarLoginDetalhadoAsync("existe@example.com", "senhaerrada123");

            Assert.Equal(LoginErroCodigo.SenhaIncorreta, resultado.Erro);
            hasher.Verify(h => h.Verify("senhaerrada123", usuario.SenhaHash), Times.Once);
        }

        private static AuthController CriarAuth(IClienteServico usuarios, IAdminServico admins) =>
            new(usuarios, admins, Mock.Of<IJwtTokenService>(), Mock.Of<IEmailService>(), Mock.Of<IIdCodificador>());

        private static object CorpoUnauthorized(IActionResult resultado)
        {
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(resultado);
            Assert.NotNull(unauthorized.Value);
            return unauthorized.Value;
        }

        private static List<ValidationResult> Validar(object dto)
        {
            var erros = new List<ValidationResult>();
            var ok = Validator.TryValidateObject(dto, new ValidationContext(dto), erros, validateAllProperties: true);
            Assert.False(ok);
            return erros;
        }
    }
}
