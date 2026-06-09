using Moq;
using Pc.Dominio.Entities.Usuarios;
using Pc.Repositorio.Interfaces;
using Pc.Servico.Implementacoes;
using Pc.Servico.Interfaces;
using Xunit;

namespace Pc.Tests
{
    public class ClienteServicoTests
    {
        private readonly Mock<IClienteRepositorio> _repo = new();
        private readonly Mock<IPasswordHasher> _hasher = new();
        private readonly ClienteServico _servico;

        public ClienteServicoTests()
        {
            _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns<string>(s => $"hashed:{s}");
            _servico = new ClienteServico(_repo.Object, _hasher.Object);
        }

        private static Cliente NovoCliente(string email = "teste@exemplo.com", string senha = "senha123") => new()
        {
            NomeUsuario = "Fulano",
            Email = email,
            SenhaHash = senha
        };

        [Fact]
        public async Task RegistrarAsync_DeveLancar_QuandoEmailVazio()
        {
            var cliente = NovoCliente(email: "");
            await Assert.ThrowsAsync<Exception>(() => _servico.RegistrarAsync(cliente));
        }

        [Fact]
        public async Task RegistrarAsync_DeveLancar_QuandoSenhaCurta()
        {
            var cliente = NovoCliente(senha: "123");
            await Assert.ThrowsAsync<Exception>(() => _servico.RegistrarAsync(cliente));
        }

        [Fact]
        public async Task RegistrarAsync_DeveLancar_QuandoEmailJaExiste()
        {
            _repo.Setup(r => r.ListarAsync()).ReturnsAsync(new List<Cliente> { NovoCliente() });

            var novo = NovoCliente();
            var ex = await Assert.ThrowsAsync<Exception>(() => _servico.RegistrarAsync(novo));
            Assert.Contains("Email já registrado", ex.Message);
        }

        [Fact]
        public async Task RegistrarAsync_DeveHashearSenha_EGerarTokenConfirmacao()
        {
            _repo.Setup(r => r.ListarAsync()).ReturnsAsync(new List<Cliente>());
            _repo.Setup(r => r.AdicionarAsync(It.IsAny<Cliente>())).ReturnsAsync((Cliente c) => c);

            var resultado = await _servico.RegistrarAsync(NovoCliente());

            Assert.Equal("hashed:senha123", resultado.SenhaHash);
            Assert.False(resultado.EmailConfirmado);
            Assert.False(string.IsNullOrWhiteSpace(resultado.TokenConfirmacao));
            _repo.Verify(r => r.AdicionarAsync(It.IsAny<Cliente>()), Times.Once);
        }

        [Fact]
        public async Task AlterarSenhaAsync_DeveLancar_QuandoNovaSenhaCurta()
        {
            await Assert.ThrowsAsync<Exception>(
                () => _servico.AlterarSenhaAsync(Guid.NewGuid(), "atual", "123"));
        }

        [Fact]
        public async Task AlterarSenhaAsync_DeveLancar_QuandoSenhaAtualIncorreta()
        {
            var cliente = NovoCliente();
            cliente.SenhaHash = "hashed:correta";
            _repo.Setup(r => r.ObterPorIdAsync(It.IsAny<Guid>())).ReturnsAsync(cliente);
            _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
            _hasher.Setup(h => h.IsBcryptHash(It.IsAny<string>())).Returns(true);

            await Assert.ThrowsAsync<Exception>(
                () => _servico.AlterarSenhaAsync(cliente.Id, "errada", "novaSenha"));
        }

        [Fact]
        public async Task AlterarSenhaAsync_DeveAtualizar_QuandoSenhaAtualCorreta()
        {
            var cliente = NovoCliente();
            cliente.SenhaHash = "hashed:correta";
            _repo.Setup(r => r.ObterPorIdAsync(It.IsAny<Guid>())).ReturnsAsync(cliente);
            _hasher.Setup(h => h.Verify("correta", "hashed:correta")).Returns(true);

            await _servico.AlterarSenhaAsync(cliente.Id, "correta", "novaSenha");

            Assert.Equal("hashed:novaSenha", cliente.SenhaHash);
            _repo.Verify(r => r.AtualizarAsync(cliente), Times.Once);
        }

        [Fact]
        public async Task ConfirmarEmailAsync_DeveConfirmar_QuandoTokenValido()
        {
            var cliente = NovoCliente();
            cliente.TokenConfirmacao = "tok123";
            _repo.Setup(r => r.ListarAsync()).ReturnsAsync(new List<Cliente> { cliente });

            var ok = await _servico.ConfirmarEmailAsync("tok123");

            Assert.True(ok);
            Assert.True(cliente.EmailConfirmado);
            Assert.Null(cliente.TokenConfirmacao);
        }

        [Fact]
        public async Task ConfirmarEmailAsync_DeveFalhar_QuandoTokenInvalido()
        {
            _repo.Setup(r => r.ListarAsync()).ReturnsAsync(new List<Cliente>());
            var ok = await _servico.ConfirmarEmailAsync("inexistente");
            Assert.False(ok);
        }
    }
}
