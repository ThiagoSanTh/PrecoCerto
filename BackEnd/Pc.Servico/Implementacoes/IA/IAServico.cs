using System.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Pc.Dominio.Comum;
using Pc.Dominio.Entities.Catalogo;
using Pc.Dominio.Entities.Estabelecimentos;
using Pc.Dominio.Enums;
using Pc.Repositorio.Comum;
using Pc.Servico.Interfaces;
using Pc.Servico.Modelos.IA;

namespace Pc.Servico.Implementacoes.IA
{
    public class IAServico : IIAServico
    {
        private static readonly TimeSpan CacheCatalogoTtl = TimeSpan.FromMinutes(5);

        private readonly IProdutoServico _produtoServico;
        private readonly ILojaServico _lojaServico;
        private readonly IOfertaServico _ofertaServico;
        private readonly IRespostaIAServico _respostaIAServico;
        private readonly IMemoryCache _cache;
        private readonly ILogger<IAServico> _logger;

        public IAServico(
            IProdutoServico produtoServico,
            ILojaServico lojaServico,
            IOfertaServico ofertaServico,
            IRespostaIAServico respostaIAServico,
            IMemoryCache cache,
            ILogger<IAServico> logger)
        {
            _produtoServico = produtoServico;
            _lojaServico = lojaServico;
            _ofertaServico = ofertaServico;
            _respostaIAServico = respostaIAServico;
            _cache = cache;
            _logger = logger;
        }

        public async Task<IAChatResultado> ProcessarAsync(
            IAChatPedido pedido,
            CancellationToken cancellationToken = default)
        {
            var sw = Stopwatch.StartNew();
            var mensagem = pedido.Mensagem?.Trim() ?? string.Empty;
            IAIntencao intencaoLog = IAIntencao.NaoEntendida;
            var sucessoLog = false;

            try
            {
                if (string.IsNullOrWhiteSpace(mensagem))
                {
                    intencaoLog = IAIntencao.NaoEntendida;
                    sucessoLog = true;
                    return Resultado(IAIntencao.NaoEntendida, new IAContextoResposta
                    {
                        Intencao = IAIntencao.NaoEntendida,
                        MensagemFixa = RespostaDeterministicaServico.MensagemNaoEntendida
                    });
                }

                if (mensagem.Length > IAIntencaoClassificador.MaxMensagemChars)
                {
                    intencaoLog = IAIntencao.NaoEntendida;
                    sucessoLog = true;
                    return Resultado(IAIntencao.NaoEntendida, new IAContextoResposta
                    {
                        Intencao = IAIntencao.NaoEntendida,
                        MensagemFixa = RespostaDeterministicaServico.MensagemMuitoLonga
                    });
                }

                var intencao = IAIntencaoClassificador.Classificar(mensagem);
                intencaoLog = intencao;

                // Recusa fora do domínio ANTES de qualquer consulta
                if (intencao == IAIntencao.ForaDoDominio)
                {
                    sucessoLog = true;
                    return Resultado(intencao, new IAContextoResposta { Intencao = intencao });
                }

                if (intencao == IAIntencao.NaoEntendida)
                {
                    sucessoLog = true;
                    return Resultado(intencao, new IAContextoResposta { Intencao = intencao });
                }

                var entidades = IAEntidadeExtrator.Extrair(mensagem, intencao);
                var contexto = await MontarContextoAsync(intencao, entidades, pedido, cancellationToken);
                sucessoLog = true;
                return Resultado(intencao, contexto);
            }
            catch (Exception ex)
            {
                sucessoLog = false;
                _logger.LogError(ex, "IA Chat falhou. Usuario={UsuarioId}", pedido.UsuarioId);
                return new IAChatResultado
                {
                    Sucesso = false,
                    Intencao = IAIntencao.NaoEntendida,
                    Resposta = "Não foi possível processar sua pergunta no momento. Tente novamente."
                };
            }
            finally
            {
                sw.Stop();
                _logger.LogInformation(
                    "IA Chat: Usuario={UsuarioId} Intencao={Intencao} TempoMs={TempoMs} Sucesso={Sucesso}",
                    pedido.UsuarioId,
                    intencaoLog,
                    sw.ElapsedMilliseconds,
                    sucessoLog);
            }
        }

        private IAChatResultado Resultado(IAIntencao intencao, IAContextoResposta contexto)
        {
            contexto.Intencao = intencao;
            var texto = _respostaIAServico.Gerar(contexto);
            return new IAChatResultado
            {
                Sucesso = true,
                Intencao = intencao,
                Resposta = texto,
                Dados = MontarDados(contexto)
            };
        }

        private static object? MontarDados(IAContextoResposta ctx)
        {
            if (ctx.Intencao is IAIntencao.ForaDoDominio or IAIntencao.NaoEntendida)
                return null;

            return new
            {
                produto = ctx.ProdutoEncontrado ?? ctx.ProdutoMencionado,
                loja = ctx.LojaEncontrada ?? ctx.LojaMencionada,
                disponivel = ctx.Disponivel,
                quantidadeLojas = ctx.QuantidadeLojas,
                quantidadeProdutos = ctx.QuantidadeProdutos,
                preco = ctx.Preco,
                distanciaKm = ctx.DistanciaKm is decimal d ? Math.Round((double)d, 1) : (double?)null,
                emPromocao = ctx.EmPromocao,
                lojas = ctx.NomesLojas,
                produtos = ctx.NomesProdutos,
                categoria = ctx.Categoria
            };
        }

        private async Task<IAContextoResposta> MontarContextoAsync(
            IAIntencao intencao,
            IAEntidadesExtraidas entidades,
            IAChatPedido pedido,
            CancellationToken ct)
        {
            return intencao switch
            {
                IAIntencao.DisponibilidadeProduto => await DisponibilidadeAsync(entidades, ct),
                IAIntencao.LojasPorProduto => await LojasPorProdutoAsync(entidades, pedido, ct),
                IAIntencao.PrecoProduto => await PrecoAsync(entidades, ct),
                IAIntencao.DistanciaLoja => await DistanciaAsync(entidades, pedido, ct),
                IAIntencao.OfertaProduto => await OfertaAsync(entidades, ct),
                IAIntencao.ProdutosDaLoja => await ProdutosDaLojaAsync(entidades, ct),
                IAIntencao.InformacaoLoja => await InformacaoLojaAsync(entidades, pedido, ct),
                IAIntencao.CategoriaProduto => await CategoriaAsync(entidades, ct),
                _ => new IAContextoResposta { Intencao = intencao }
            };
        }

        private async Task<IAContextoResposta> DisponibilidadeAsync(IAEntidadesExtraidas e, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var ctx = new IAContextoResposta
            {
                Intencao = IAIntencao.DisponibilidadeProduto,
                ProdutoMencionado = e.Produto
            };

            if (string.IsNullOrWhiteSpace(e.Produto))
            {
                ctx.EntidadeNaoEncontrada = true;
                return ctx;
            }

            var produtos = await BuscarProdutosAsync(e.Produto);
            if (produtos.Count == 0)
            {
                ctx.EntidadeNaoEncontrada = true;
                return ctx;
            }

            ctx.ProdutoEncontrado = produtos[0].NomeProduto;
            var ofertas = await _ofertaServico.ListarDisponiveisPorProdutosAsync(produtos.Select(p => p.Id));
            var lojas = ofertas
                .Where(o => o.Loja != null)
                .Select(o => o.Loja!.NomeFantasia)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Produto cadastrado sem oferta: ainda "existe", mas disponibilidade via oferta
            ctx.Disponivel = lojas.Count > 0 || produtos.Any(p => p.Ativo);
            ctx.QuantidadeLojas = lojas.Count > 0
                ? lojas.Count
                : produtos.Where(p => p.Loja != null).Select(p => p.LojaId).Distinct().Count();
            ctx.NomesLojas = lojas.Count > 0
                ? lojas
                : produtos.Where(p => p.Loja != null).Select(p => p.Loja!.NomeFantasia).Distinct().ToList();

            if (ctx.QuantidadeLojas == 0 && produtos.Count > 0)
            {
                // Existe no catálogo mas sem loja/oferta
                ctx.Disponivel = true;
                ctx.QuantidadeLojas = 0;
            }

            return ctx;
        }

        private async Task<IAContextoResposta> LojasPorProdutoAsync(
            IAEntidadesExtraidas e, IAChatPedido pedido, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var ctx = new IAContextoResposta
            {
                Intencao = IAIntencao.LojasPorProduto,
                ProdutoMencionado = e.Produto
            };

            if (string.IsNullOrWhiteSpace(e.Produto))
            {
                ctx.EntidadeNaoEncontrada = true;
                return ctx;
            }

            var produtos = await BuscarProdutosAsync(e.Produto);
            if (produtos.Count == 0)
            {
                ctx.EntidadeNaoEncontrada = true;
                return ctx;
            }

            ctx.ProdutoEncontrado = produtos[0].NomeProduto;
            var ofertas = await _ofertaServico.ListarDisponiveisPorProdutosAsync(produtos.Select(p => p.Id));

            IEnumerable<Oferta> ordenadas = ofertas;
            if (pedido.Latitude is double lat && pedido.Longitude is double lng)
            {
                var latD = (decimal)lat;
                var lngD = (decimal)lng;
                ordenadas = ofertas
                    .Where(o => o.Loja?.Endereco?.Latitude != null && o.Loja.Endereco.Longitude != null)
                    .OrderBy(o => GeoHelper.CalcularDistanciaKm(
                        latD, lngD,
                        o.Loja!.Endereco.Latitude!.Value,
                        o.Loja.Endereco.Longitude!.Value))
                    .Concat(ofertas.Where(o => o.Loja?.Endereco?.Latitude == null));
            }

            var lojas = ordenadas
                .Where(o => o.Loja != null)
                .Select(o => o.Loja!.NomeFantasia)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(10)
                .ToList();

            if (lojas.Count == 0)
            {
                lojas = produtos
                    .Where(p => p.Loja != null)
                    .Select(p => p.Loja!.NomeFantasia)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(10)
                    .ToList();
            }

            ctx.NomesLojas = lojas;
            ctx.QuantidadeLojas = lojas.Count;
            return ctx;
        }

        private async Task<IAContextoResposta> PrecoAsync(IAEntidadesExtraidas e, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var ctx = new IAContextoResposta
            {
                Intencao = IAIntencao.PrecoProduto,
                ProdutoMencionado = e.Produto
            };

            if (string.IsNullOrWhiteSpace(e.Produto))
            {
                ctx.EntidadeNaoEncontrada = true;
                return ctx;
            }

            var produtos = await BuscarProdutosAsync(e.Produto);
            if (produtos.Count == 0)
            {
                ctx.EntidadeNaoEncontrada = true;
                return ctx;
            }

            ctx.ProdutoEncontrado = produtos[0].NomeProduto;
            var ofertas = await _ofertaServico.ListarDisponiveisPorProdutosAsync(produtos.Select(p => p.Id));
            var melhor = ofertas.OrderBy(o => o.Preco).FirstOrDefault();

            if (melhor != null)
            {
                ctx.Preco = melhor.Preco;
                ctx.LojaEncontrada = melhor.Loja?.NomeFantasia;
                ctx.EmPromocao = melhor.EmPromocao;
            }
            else
            {
                // Fallback: preço base do produto
                ctx.Preco = produtos.Min(p => p.Preco);
                ctx.LojaEncontrada = produtos.FirstOrDefault(p => p.Loja != null)?.Loja?.NomeFantasia;
            }

            return ctx;
        }

        private async Task<IAContextoResposta> DistanciaAsync(
            IAEntidadesExtraidas e, IAChatPedido pedido, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var ctx = new IAContextoResposta
            {
                Intencao = IAIntencao.DistanciaLoja,
                LojaMencionada = e.Loja
            };

            if (string.IsNullOrWhiteSpace(e.Loja))
            {
                ctx.EntidadeNaoEncontrada = true;
                return ctx;
            }

            if (pedido.Latitude is null || pedido.Longitude is null)
            {
                // Ainda confirma se a loja existe
                var lojasCheck = await BuscarLojasAsync(e.Loja);
                if (lojasCheck.Count == 0)
                {
                    ctx.EntidadeNaoEncontrada = true;
                    return ctx;
                }

                ctx.LojaEncontrada = lojasCheck[0].NomeFantasia;
                ctx.PrecisaLocalizacao = true;
                return ctx;
            }

            var lojas = await BuscarLojasAsync(e.Loja);
            if (lojas.Count == 0)
            {
                ctx.EntidadeNaoEncontrada = true;
                return ctx;
            }

            var loja = lojas[0];
            ctx.LojaEncontrada = loja.NomeFantasia;

            if (loja.Endereco?.Latitude is null || loja.Endereco.Longitude is null)
                return ctx;

            ctx.DistanciaKm = GeoHelper.CalcularDistanciaKm(
                (decimal)pedido.Latitude.Value,
                (decimal)pedido.Longitude.Value,
                loja.Endereco.Latitude.Value,
                loja.Endereco.Longitude.Value);

            return ctx;
        }

        private async Task<IAContextoResposta> OfertaAsync(IAEntidadesExtraidas e, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var ctx = new IAContextoResposta
            {
                Intencao = IAIntencao.OfertaProduto,
                ProdutoMencionado = e.Produto
            };

            if (string.IsNullOrWhiteSpace(e.Produto))
            {
                ctx.EntidadeNaoEncontrada = true;
                return ctx;
            }

            var produtos = await BuscarProdutosAsync(e.Produto);
            if (produtos.Count == 0)
            {
                ctx.EntidadeNaoEncontrada = true;
                return ctx;
            }

            ctx.ProdutoEncontrado = produtos[0].NomeProduto;
            var ofertas = await _ofertaServico.ListarDisponiveisPorProdutosAsync(produtos.Select(p => p.Id));
            var promos = ofertas.Where(o => o.EmPromocao).ToList();
            ctx.EmPromocao = promos.Count > 0;
            ctx.NomesLojas = promos
                .Where(o => o.Loja != null)
                .Select(o => o.Loja!.NomeFantasia)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(5)
                .ToList();
            ctx.QuantidadeLojas = ctx.NomesLojas.Count;
            if (promos.Count > 0)
                ctx.Preco = promos.Min(o => o.Preco);

            return ctx;
        }

        private async Task<IAContextoResposta> ProdutosDaLojaAsync(IAEntidadesExtraidas e, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var ctx = new IAContextoResposta
            {
                Intencao = IAIntencao.ProdutosDaLoja,
                LojaMencionada = e.Loja
            };

            if (string.IsNullOrWhiteSpace(e.Loja))
            {
                ctx.EntidadeNaoEncontrada = true;
                return ctx;
            }

            var lojas = await BuscarLojasAsync(e.Loja);
            if (lojas.Count == 0)
            {
                ctx.EntidadeNaoEncontrada = true;
                return ctx;
            }

            var loja = lojas[0];
            ctx.LojaEncontrada = loja.NomeFantasia;

            var page = await _produtoServico.ListarProdutosPaginadoAsync(
                PaginacaoParametros.De(1, 20),
                loja.Id);

            ctx.NomesProdutos = page.Items.Select(p => p.NomeProduto).Take(10).ToList();
            ctx.QuantidadeProdutos = page.Total;
            return ctx;
        }

        private async Task<IAContextoResposta> InformacaoLojaAsync(
            IAEntidadesExtraidas e, IAChatPedido pedido, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var ctx = new IAContextoResposta
            {
                Intencao = IAIntencao.InformacaoLoja,
                LojaMencionada = e.Loja
            };

            if (string.IsNullOrWhiteSpace(e.Loja))
            {
                ctx.EntidadeNaoEncontrada = true;
                return ctx;
            }

            var lojas = await BuscarLojasAsync(e.Loja);
            if (lojas.Count == 0)
            {
                ctx.EntidadeNaoEncontrada = true;
                return ctx;
            }

            var loja = lojas[0];
            ctx.LojaEncontrada = loja.NomeFantasia;

            if (pedido.Latitude is double lat && pedido.Longitude is double lng
                && loja.Endereco?.Latitude != null && loja.Endereco.Longitude != null)
            {
                ctx.DistanciaKm = GeoHelper.CalcularDistanciaKm(
                    (decimal)lat, (decimal)lng,
                    loja.Endereco.Latitude.Value, loja.Endereco.Longitude.Value);
            }

            return ctx;
        }

        private async Task<IAContextoResposta> CategoriaAsync(IAEntidadesExtraidas e, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var ctx = new IAContextoResposta
            {
                Intencao = IAIntencao.CategoriaProduto,
                ProdutoMencionado = e.Produto
            };

            if (string.IsNullOrWhiteSpace(e.Produto))
            {
                ctx.EntidadeNaoEncontrada = true;
                return ctx;
            }

            var produtos = await BuscarProdutosAsync(e.Produto);
            if (produtos.Count == 0)
            {
                ctx.EntidadeNaoEncontrada = true;
                return ctx;
            }

            ctx.ProdutoEncontrado = produtos[0].NomeProduto;
            ctx.Categoria = produtos[0].Categoria.ToString();
            return ctx;
        }

        private async Task<List<Produto>> BuscarProdutosAsync(string termo)
        {
            var chave = "ia:produtos:" + TextoNormalizador.Normalizar(termo);
            if (_cache.TryGetValue(chave, out List<Produto>? cached) && cached != null)
                return cached;

            var lista = await _produtoServico.BuscarPorNomeAsync(termo);
            _cache.Set(chave, lista, CacheCatalogoTtl);
            return lista;
        }

        private async Task<List<Loja>> BuscarLojasAsync(string termo)
        {
            var chave = "ia:lojas:" + TextoNormalizador.Normalizar(termo);
            if (_cache.TryGetValue(chave, out List<Loja>? cached) && cached != null)
                return cached;

            var lista = await _lojaServico.BuscarPorNomeAsync(termo);
            _cache.Set(chave, lista, CacheCatalogoTtl);
            return lista;
        }
    }
}
