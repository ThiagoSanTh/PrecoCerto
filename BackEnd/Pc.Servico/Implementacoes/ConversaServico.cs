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
        private readonly ILojaAcesso _acesso;

        public ConversaServico(
            IConversaRepositorio repo,
            ILojaRepositorio lojaRepo,
            IProdutoRepositorio produtoRepo,
            IOfertaRepositorio ofertaRepo,
            ILojaAcesso acesso)
        {
            _repo = repo;
            _lojaRepo = lojaRepo;
            _produtoRepo = produtoRepo;
            _ofertaRepo = ofertaRepo;
            _acesso = acesso;
        }

        public async Task<List<Conversa>> ListarDoUsuarioAsync(Guid usuarioId, PapelUsuario papel, Guid? lojaId)
        {
            if (papel is PapelUsuario.Lojista or PapelUsuario.Vendedor)
            {
                await ExigirStaffAsync(usuarioId, papel, lojaId);
                return await _repo.ListarPorLojaAsync(lojaId!.Value);
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
            Guid conversaId, Guid usuarioId, PapelUsuario papel, DateTime? apos, DateTime? antes = null, int pageSize = 50)
        {
            if (!await UsuarioPodeAcessarAsync(conversaId, usuarioId, papel))
                throw new UnauthorizedAccessException("Acesso negado à conversa.");

            return await _repo.ListarMensagensAsync(conversaId, apos, antes, pageSize);
        }

        public async Task<Dictionary<Guid, int>> ContarNaoLidasPorConversasAsync(
            Guid usuarioId, PapelUsuario papel, Guid? lojaId)
        {
            await ExigirStaffAsync(usuarioId, papel, lojaId);
            var ehLojista = papel is PapelUsuario.Lojista or PapelUsuario.Vendedor;
            return await _repo.ContarNaoLidasPorConversasAsync(usuarioId, ehLojista, lojaId);
        }

        public Task<Dictionary<Guid, string>> ObterUltimasMensagensTextoAsync(IEnumerable<Guid> conversaIds) =>
            _repo.ObterUltimasMensagensTextoAsync(conversaIds);

        public async Task<Mensagem> EnviarMensagemAsync(
            Guid conversaId, Guid remetenteId, PapelUsuario remetentePapel, string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                throw new Exception("Mensagem não pode ser vazia.");

            var conversa = await _repo.ObterPorIdAsync(conversaId)
                ?? throw new Exception("Conversa não encontrada.");

            if (conversa.ClienteId == remetenteId)
                remetentePapel = PapelUsuario.Cliente;

            if (!await UsuarioPodeAcessarAsync(conversaId, remetenteId, remetentePapel))
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

        public async Task<int> ContarNaoLidasAsync(Guid usuarioId, PapelUsuario papel, Guid? lojaId)
        {
            await ExigirStaffAsync(usuarioId, papel, lojaId);
            var ehLojista = papel is PapelUsuario.Lojista or PapelUsuario.Vendedor;
            return await _repo.ContarNaoLidasAsync(usuarioId, ehLojista, lojaId);
        }

        public async Task<bool> TemNaoLidasAsync(
            Guid usuarioId, PapelUsuario papel, Guid? lojaId, DateTime? desde = null)
        {
            await ExigirStaffAsync(usuarioId, papel, lojaId);
            var ehLojista = papel is PapelUsuario.Lojista or PapelUsuario.Vendedor;
            return await _repo.TemNaoLidasAsync(usuarioId, ehLojista, lojaId, desde);
        }

        public async Task<bool> UsuarioPodeAcessarAsync(
            Guid conversaId, Guid usuarioId, PapelUsuario papel)
        {
            var conversa = await _repo.ObterPorIdAsync(conversaId);
            if (conversa == null)
                return false;

            if (conversa.ClienteId == usuarioId)
                return true;

            if (papel is PapelUsuario.Lojista or PapelUsuario.Vendedor)
                return await _acesso.PertenceAsync(usuarioId, conversa.LojaId);

            return false;
        }

        private async Task ExigirStaffAsync(Guid usuarioId, PapelUsuario papel, Guid? lojaId)
        {
            if (papel is not (PapelUsuario.Lojista or PapelUsuario.Vendedor))
                return;

            if (!lojaId.HasValue || !await _acesso.PertenceAsync(usuarioId, lojaId.Value))
                throw new UnauthorizedAccessException("Acesso negado à loja.");
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
