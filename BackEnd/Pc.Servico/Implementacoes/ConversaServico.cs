using Pc.Dominio.Entities.Interacoes;
using Pc.Dominio.Enums;
using Pc.Repositorio.Interfaces;
using Pc.Servico.Interfaces;

namespace Pc.Servico.Implementacoes
{
    public class ConversaServico : IConversaServico
    {
        private readonly IConversaRepositorio _repo;
        private readonly ILojaRepositorio _lojaRepo;

        public ConversaServico(IConversaRepositorio repo, ILojaRepositorio lojaRepo)
        {
            _repo = repo;
            _lojaRepo = lojaRepo;
        }

        public async Task<List<Conversa>> ListarDoUsuarioAsync(Guid usuarioId, PapelUsuario papel, Guid? lojaId)
        {
            if (papel is PapelUsuario.Lojista or PapelUsuario.Vendedor)
            {
                if (!lojaId.HasValue)
                    return new List<Conversa>();
                return await _repo.ListarPorLojaAsync(lojaId.Value);
            }

            return await _repo.ListarPorClienteAsync(usuarioId);
        }

        public async Task<Conversa> AbrirComLojaAsync(Guid clienteId, Guid lojaId)
        {
            var loja = await _lojaRepo.ObterPorIdAsync(lojaId);
            if (loja == null)
                throw new Exception("Loja não encontrada.");

            var existente = await _repo.ObterPorClienteELojaAsync(clienteId, lojaId);
            if (existente != null)
                return existente;

            var conversa = new Conversa
            {
                ClienteId = clienteId,
                LojaId = lojaId,
                UltimaMensagemEm = DateTime.UtcNow
            };

            return await _repo.AdicionarAsync(conversa);
        }

        public Task<Conversa?> ObterPorIdAsync(Guid conversaId) =>
            _repo.ObterPorIdAsync(conversaId);

        public async Task<List<Mensagem>> ListarMensagensAsync(
            Guid conversaId, Guid usuarioId, PapelUsuario papel, Guid? lojaId, DateTime? apos, DateTime? antes = null, int pageSize = 50)
        {
            if (!await UsuarioPodeAcessarAsync(conversaId, usuarioId, papel, lojaId))
                throw new UnauthorizedAccessException("Acesso negado à conversa.");

            return await _repo.ListarMensagensAsync(conversaId, apos, antes, pageSize);
        }

        public Task<Dictionary<Guid, int>> ContarNaoLidasPorConversasAsync(
            Guid usuarioId, PapelUsuario papel, Guid? lojaId)
        {
            var ehLojista = papel is PapelUsuario.Lojista or PapelUsuario.Vendedor;
            return _repo.ContarNaoLidasPorConversasAsync(usuarioId, ehLojista, lojaId);
        }

        public async Task<Mensagem> EnviarMensagemAsync(
            Guid conversaId, Guid remetenteId, PapelUsuario remetentePapel, string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                throw new Exception("Mensagem não pode ser vazia.");

            var conversa = await _repo.ObterPorIdAsync(conversaId)
                ?? throw new Exception("Conversa não encontrada.");

            var lojaId = remetentePapel is PapelUsuario.Lojista or PapelUsuario.Vendedor
                ? conversa.LojaId
                : (Guid?)null;

            if (!await UsuarioPodeAcessarAsync(conversaId, remetenteId, remetentePapel, lojaId))
                throw new UnauthorizedAccessException("Acesso negado à conversa.");

            var mensagem = new Mensagem
            {
                ConversaId = conversaId,
                RemetenteId = remetenteId,
                RemetentePapel = remetentePapel,
                Texto = texto.Trim(),
                EnviadaEm = DateTime.UtcNow
            };

            var salva = await _repo.AdicionarMensagemAsync(mensagem);
            conversa.UltimaMensagemEm = salva.EnviadaEm;
            await _repo.AtualizarAsync(conversa);
            return salva;
        }

        public async Task MarcarComoLidasAsync(Guid conversaId, Guid leitorId)
        {
            await _repo.MarcarMensagensComoLidasAsync(conversaId, leitorId);
        }

        public Task<int> ContarNaoLidasAsync(Guid usuarioId, PapelUsuario papel, Guid? lojaId)
        {
            var ehLojista = papel is PapelUsuario.Lojista or PapelUsuario.Vendedor;
            return _repo.ContarNaoLidasAsync(usuarioId, ehLojista, lojaId);
        }

        public Task<bool> TemNaoLidasAsync(
            Guid usuarioId, PapelUsuario papel, Guid? lojaId, DateTime? desde = null)
        {
            var ehLojista = papel is PapelUsuario.Lojista or PapelUsuario.Vendedor;
            return _repo.TemNaoLidasAsync(usuarioId, ehLojista, lojaId, desde);
        }

        public async Task<bool> UsuarioPodeAcessarAsync(
            Guid conversaId, Guid usuarioId, PapelUsuario papel, Guid? lojaId)
        {
            var conversa = await _repo.ObterPorIdAsync(conversaId);
            if (conversa == null)
                return false;

            if (papel == PapelUsuario.Cliente)
                return conversa.ClienteId == usuarioId;

            if (papel is PapelUsuario.Lojista or PapelUsuario.Vendedor)
                return lojaId.HasValue && conversa.LojaId == lojaId.Value;

            return false;
        }
    }
}
