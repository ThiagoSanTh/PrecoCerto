using Moq;
using Pc.Dominio.Entities.Interacoes;
using Pc.Dominio.Enums;
using Pc.Repositorio.Interfaces;
using Pc.Servico.Implementacoes;
using Xunit;

namespace Pc.Servico.Tests
{
    public class ConversaModoClienteTests
    {
        private static ConversaServico CriarServico(IConversaRepositorio repo) =>
            new(repo, Mock.Of<ILojaRepositorio>(), Mock.Of<IProdutoRepositorio>(), Mock.Of<IOfertaRepositorio>());

        [Fact]
        public async Task Staff_acessa_thread_em_que_e_o_cliente_mesmo_com_papel_de_loja()
        {
            var usuarioId = Guid.NewGuid();
            var conversa = new Conversa
            {
                Id = Guid.NewGuid(),
                ClienteId = usuarioId,
                LojaId = Guid.NewGuid()
            };
            var repo = new Mock<IConversaRepositorio>();
            repo.Setup(r => r.ObterPorIdAsync(conversa.Id)).ReturnsAsync(conversa);

            var pode = await CriarServico(repo.Object).UsuarioPodeAcessarAsync(
                conversa.Id, usuarioId, PapelUsuario.Vendedor, Guid.NewGuid());

            Assert.True(pode);
        }

        [Fact]
        public async Task Staff_nao_acessa_thread_de_outro_cliente_em_outra_loja()
        {
            var conversa = new Conversa
            {
                Id = Guid.NewGuid(),
                ClienteId = Guid.NewGuid(),
                LojaId = Guid.NewGuid()
            };
            var repo = new Mock<IConversaRepositorio>();
            repo.Setup(r => r.ObterPorIdAsync(conversa.Id)).ReturnsAsync(conversa);

            var pode = await CriarServico(repo.Object).UsuarioPodeAcessarAsync(
                conversa.Id, Guid.NewGuid(), PapelUsuario.Lojista, Guid.NewGuid());

            Assert.False(pode);
        }

        [Fact]
        public async Task Envio_na_thread_do_cliente_grava_papel_cliente_mesmo_com_role_vendedor()
        {
            var usuarioId = Guid.NewGuid();
            var conversa = new Conversa
            {
                Id = Guid.NewGuid(),
                ClienteId = usuarioId,
                LojaId = Guid.NewGuid()
            };
            var repo = new Mock<IConversaRepositorio>();
            repo.Setup(r => r.ObterPorIdAsync(conversa.Id)).ReturnsAsync(conversa);
            repo.Setup(r => r.AdicionarMensagemAsync(It.IsAny<Mensagem>()))
                .ReturnsAsync((Mensagem m) => m);
            repo.Setup(r => r.AtualizarAsync(conversa)).Returns(Task.CompletedTask);

            var msg = await CriarServico(repo.Object).EnviarMensagemAsync(
                conversa.Id, usuarioId, PapelUsuario.Vendedor, "oi");

            Assert.Equal(PapelUsuario.Cliente, msg.RemetentePapel);
            Assert.Equal("oi", msg.Texto);
        }
    }
}
