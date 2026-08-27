using System.Globalization;
using Pc.Dominio.Entities.Interacoes;
using Pc.Dominio.Enums;
using Pc.Repositorio.Interfaces;
using Pc.Servico.Interfaces;

namespace Pc.Servico.Implementacoes
{
    public class ConversaServico : IConversaServico
    {
        private static readonly CultureInfo CulturaPtBr = CultureInfo.GetCultureInfo("pt-BR");
        private static readonly TimeSpan JanelaMensagemInteresse = TimeSpan.FromMinutes(10);

        private readonly IConversaRepositorio _repo;
        private readonly ILojaRepositorio _lojaRepo;
        private readonly IProdutoRepositorio _produtoRepo;
        private readonly IOfertaRepositorio _ofertaRepo;

        public ConversaServico(
            IConversaRepositorio repo,
            ILojaRepositorio lojaRepo,
            IProdutoRepositorio produtoRepo,
            IOfertaRepositorio ofertaRepo)
        {
            _repo = repo;
            _lojaRepo = lojaRepo;
            _produtoRepo = produtoRepo;
            _ofertaRepo = ofertaRepo;
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

        public async Task<Mensagem> EnviarInteresseProdutoAsync(Guid conversaId, Guid clienteId, Guid produtoId)
        {
            var conversa = await _repo.ObterPorIdAsync(conversaId)
                ?? throw new Exception("Conversa não encontrada.");

            if (conversa.ClienteId != clienteId)
                throw new UnauthorizedAccessException("Acesso negado à conversa.");

            var produto = await _produtoRepo.ObterPorIdAsync(produtoId)
                ?? throw new Exception("Produto não encontrado.");

            var ofertas = await _ofertaRepo.ObterPorProdutoAsync(produtoId);
            var oferta = ofertas.FirstOrDefault(o => o.LojaId == conversa.LojaId);
            var loja = conversa.Loja ?? await _lojaRepo.ObterPorIdAsync(conversa.LojaId);

            var texto = MontarMensagemInteresse(produto.NomeProduto, oferta?.Preco ?? produto.Preco, loja?.NomeFantasia);

            var recentes = await _repo.ListarMensagensAsync(conversaId, null, null, 20);
            var duplicada = recentes.LastOrDefault(m =>
                m.RemetenteId == clienteId
                && m.Texto == texto
                && DateTime.UtcNow - m.EnviadaEm < JanelaMensagemInteresse);
            if (duplicada != null)
                return duplicada;

            return await EnviarMensagemAsync(conversaId, clienteId, PapelUsuario.Cliente, texto);
        }

        public async Task MarcarComoLidasAsync(Guid conversaId, Guid leitorId)
        {
            await _repo.MarcarMensagensComoLidasAsync(conversaId, leitorId);
        }

        public Task MarcarComoRecebidasAsync(Guid conversaId, Guid leitorId) =>
            _repo.MarcarMensagensComoRecebidasAsync(conversaId, leitorId);

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

        internal static string MontarMensagemInteresse(string nomeProduto, decimal preco, string? nomeLoja)
        {
            var precoFmt = preco.ToString("C", CulturaPtBr);
            var loja = string.IsNullOrWhiteSpace(nomeLoja) ? "a loja" : nomeLoja.Trim();
            return
                "Olá! Tenho interesse neste produto.\n\n" +
                $"Produto: {nomeProduto}\n" +
                $"Preço: {precoFmt}\n" +
                $"Loja: {loja}\n\n" +
                "Gostaria de saber se o produto está disponível.";
        }
    }
}
