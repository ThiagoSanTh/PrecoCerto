using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Pc.Dominio.Entities.Catalogo;
using Pc.Dominio.Entities.Estabelecimentos;
using Pc.Dominio.Enums.MotorIA;
using Pc.Repositorio.Comum;
using Pc.Servico.Implementacoes.IA;
using Pc.Servico.Interfaces;
using Pc.Servico.Interfaces.MotorIA;
using Pc.Servico.Modelos.MotorIA;

namespace Pc.Servico.Implementacoes.MotorIA
{
    public class MotorIA : IMotorIA
    {
        private readonly IInterpretadorIA _interpretador;
        private readonly IMotorRegrasIA _regras;
        private readonly IMotorRecomendacaoIA _recomendacao;
        private readonly IGeradorRespostaIA _gerador;
        private readonly IRagConhecimentoIA _rag;
        private readonly IProdutoServico _produtos;
        private readonly IOfertaServico _ofertas;
        private readonly ILojaServico _lojas;
        private readonly IAvaliacaoServico _avaliacoes;
        private readonly IClimaServico _clima;
        private readonly ILogger<MotorIA> _logger;
        private readonly ILlmRouterService? _llmRouter;

        public MotorIA(
            IInterpretadorIA interpretador,
            IMotorRegrasIA regras,
            IMotorRecomendacaoIA recomendacao,
            IGeradorRespostaIA gerador,
            IRagConhecimentoIA rag,
            IProdutoServico produtos,
            IOfertaServico ofertas,
            ILojaServico lojas,
            IAvaliacaoServico avaliacoes,
            IClimaServico clima,
            ILogger<MotorIA> logger,
            ILlmRouterService? llmRouter = null)
        {
            _interpretador = interpretador;
            _regras = regras;
            _recomendacao = recomendacao;
            _gerador = gerador;
            _rag = rag;
            _produtos = produtos;
            _ofertas = ofertas;
            _lojas = lojas;
            _avaliacoes = avaliacoes;
            _clima = clima;
            _logger = logger;
            _llmRouter = llmRouter;
        }

        public async Task<ResultadoAnaliseIA> AnalisarAsync(
            PedidoAnaliseIA pedido,
            CancellationToken cancellationToken = default)
        {
            var swTotal = Stopwatch.StartNew();
            var swInterp = Stopwatch.StartNew();
            var ctx = _interpretador.Interpretar(pedido);

            if (_llmRouter is not null && ctx.Intencao is not IntencaoIA.Saudacao)
            {
                var llmInterp = await _llmRouter.InterpretarAsync(pedido, cancellationToken);
                if (llmInterp is not null)
                {
                    MesclarInterpretacaoLlm(ctx, llmInterp);
                }
            }

            swInterp.Stop();

            if (ctx.Intencao is IntencaoIA.ForaDoDominio or IntencaoIA.NaoEntendida or IntencaoIA.Saudacao)
            {
                return await FinalizarAsync(ctx, swTotal, swInterp.ElapsedMilliseconds, 0, 0, cancellationToken);
            }

            if (PrecisaClima(ctx))
            {
                await TentarClimaAsync(ctx, cancellationToken);
            }

            _regras.Aplicar(ctx);

            var swDados = Stopwatch.StartNew();
            await CarregarCandidatosAsync(ctx, cancellationToken);
            swDados.Stop();

            var swRag = Stopwatch.StartNew();
            await TentarRagAsync(ctx, cancellationToken);
            swRag.Stop();

            _recomendacao.Recomendar(ctx);

            _logger.LogInformation(
                "MotorIA: Intencao={Intencao} Objetivo={Objetivo} InterpMs={Interp} DadosMs={Dados} RagMs={Rag} Candidatos={Cand} Confianca={Conf} Provedor={Prov} Fallbacks={Fb}",
                ctx.Intencao,
                ctx.Objetivo,
                swInterp.ElapsedMilliseconds,
                swDados.ElapsedMilliseconds,
                swRag.ElapsedMilliseconds,
                ctx.Candidatos.Count,
                ctx.NivelConfianca,
                ctx.ProvedorLlm,
                string.Join(',', ctx.FallbacksUsados));

            return await FinalizarAsync(
                ctx,
                swTotal,
                swInterp.ElapsedMilliseconds,
                swDados.ElapsedMilliseconds,
                swRag.ElapsedMilliseconds,
                cancellationToken);
        }

        private static bool PrecisaClima(ContextoIA ctx) =>
            ctx.NecessitaEntrega
            || ctx.Intencao == IntencaoIA.ConsultarEntrega
            || ctx.MensagemNormalizada.Contains("chuva", StringComparison.Ordinal)
            || ctx.MensagemNormalizada.Contains("clima", StringComparison.Ordinal);

        private static void MesclarInterpretacaoLlm(ContextoIA ctx, LlmInterpretacaoResultado llm)
        {
            ctx.ProvedorLlm = llm.Provedor;

            if (!string.IsNullOrWhiteSpace(llm.Intencao)
                && Enum.TryParse<IntencaoIA>(llm.Intencao, ignoreCase: true, out var intencaoParsed)
                && intencaoParsed != IntencaoIA.NaoEntendida)
            {
                // Preserva guardrails locais estritos quando já detectados
                if (ctx.Intencao is not IntencaoIA.AnalisarConcorrenciaLojista)
                    ctx.Intencao = intencaoParsed;
            }

            if (!string.IsNullOrWhiteSpace(llm.Objetivo)
                && Enum.TryParse<ObjetivoIA>(llm.Objetivo, ignoreCase: true, out var objParsed))
            {
                ctx.Objetivo = objParsed;
            }

            if (!string.IsNullOrWhiteSpace(llm.ProdutoTermo))
            {
                ctx.ProdutoTermo = TextoNormalizador.Normalizar(llm.ProdutoTermo);
                ctx.TermosBuscaProduto = Vocabulario.VocabularioIA.ExpandirTermosBusca(ctx.ProdutoTermo).ToList();
            }

            if (llm.ItensBusca is { Count: > 0 })
            {
                ctx.ItensBusca = llm.ItensBusca
                    .Where(i => !string.IsNullOrWhiteSpace(i))
                    .Select(TextoNormalizador.Normalizar)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(llm.LojaTermo))
                ctx.LojaTermo = TextoNormalizador.Normalizar(llm.LojaTermo);

            if (llm.OrcamentoMax is > 0)
                ctx.OrcamentoMax = llm.OrcamentoMax;

            if (llm.DistanciaMaxKm is > 0)
                ctx.DistanciaMaxKm = llm.DistanciaMaxKm;
        }

        private async Task<ResultadoAnaliseIA> FinalizarAsync(
            ContextoIA ctx,
            Stopwatch total,
            long interpMs,
            long dadosMs,
            long ragMs,
            CancellationToken ct)
        {
            string? resposta = null;
            if (_llmRouter is not null
                && ctx.Candidatos.Count > 0
                && ctx.Intencao is not (IntencaoIA.ForaDoDominio or IntencaoIA.NaoEntendida or IntencaoIA.Saudacao))
            {
                var (textoLlm, provedorGen) = await _llmRouter.GerarRespostaAsync(ctx, ct);
                resposta = textoLlm;
                if (!string.IsNullOrWhiteSpace(provedorGen) && provedorGen != "Deterministico")
                {
                    ctx.ProvedorLlm = provedorGen;
                }
            }

            if (string.IsNullOrWhiteSpace(resposta))
            {
                resposta = _gerador.Gerar(ctx);
            }

            total.Stop();

            var resultados = (ctx.Decisao?.Ranking ?? Array.Empty<CandidatoIA>())
                .Select(c => new ResultadoItemAnaliseIA
                {
                    Tipo = c.OfertaId.HasValue ? "Oferta" : (c.ProdutoId.HasValue ? "Produto" : "Loja"),
                    Titulo = c.Titulo,
                    ProdutoId = c.ProdutoId,
                    LojaId = c.LojaId,
                    Score = c.Score,
                    Preco = c.Preco,
                    EmPromocao = c.EmPromocao,
                    DistanciaKm = c.DistanciaKm.HasValue ? Math.Round((double)c.DistanciaKm.Value, 1) : null,
                    Loja = c.NomeLoja,
                    Produto = c.NomeProduto,
                    Motivos = c.Motivos
                }).ToList();

            _logger.LogInformation(
                "MotorIA fim: TotalMs={Total} ScoreMelhor={Score} Provedor={Provedor}",
                total.ElapsedMilliseconds,
                ctx.Decisao?.Melhor?.Score,
                ctx.ProvedorLlm);

            return new ResultadoAnaliseIA
            {
                Sucesso = true,
                Resposta = resposta,
                Intencao = ctx.Intencao,
                Objetivo = ctx.Objetivo,
                Confianca = ctx.NivelConfianca,
                ProvedorLlm = ctx.ProvedorLlm,
                Resultados = resultados,
                Motivos = ctx.Decisao?.Motivos ?? new List<string>(),
                Fallbacks = ctx.FallbacksUsados
            };
        }

        private async Task TentarClimaAsync(ContextoIA ctx, CancellationToken ct)
        {
            if (ctx.Latitude is null || ctx.Longitude is null)
                return;

            try
            {
                var clima = await _clima.ObterPorCoordenadasAsync(
                    (decimal)ctx.Latitude.Value,
                    (decimal)ctx.Longitude.Value,
                    ct);
                ctx.ClimaDisponivel = true;
                ctx.ClimaDescricao = clima.Atual.Descricao;
                ctx.Chuva =
                    (clima.Atual.Precipitacao ?? 0) > 0
                    || (clima.Atual.ProbabilidadePrecipitacao ?? 0) >= 50
                    || string.Equals(clima.Atual.Icone, "rain", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(clima.Atual.Icone, "thunder", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Clima indisponível para MotorIA.");
                ctx.FallbacksUsados.Add("sem_clima");
            }
        }

        private async Task CarregarCandidatosAsync(ContextoIA ctx, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (ctx.Intencao == IntencaoIA.AnalisarConcorrenciaLojista)
            {
                await CarregarConcorrenciaLojistaAsync(ctx, ct);
                return;
            }

            if (ctx.Intencao == IntencaoIA.MontarCesta)
            {
                await CarregarCestaAsync(ctx, ct);
                return;
            }

            if (ctx.Intencao == IntencaoIA.ProdutosDaLoja)
            {
                await CarregarProdutosDaLojaAsync(ctx, ct);
                return;
            }

            if (ctx.Intencao == IntencaoIA.RecomendarPorOcasiao)
            {
                await CarregarPorOcasiaoAsync(ctx, ct);
                return;
            }

            if (ctx.Intencao is IntencaoIA.BuscarLoja or IntencaoIA.BuscarLojaMaisProxima or IntencaoIA.RecomendarLoja)
            {
                // "onde achar X" pode ter sido classificado como loja; se há produto, pesquisa o catálogo.
                if (!string.IsNullOrWhiteSpace(ctx.ProdutoTermo)
                    && ctx.Intencao is not IntencaoIA.BuscarLojaMaisProxima)
                {
                    // segue para carga por produto abaixo
                }
                else
                {
                    await CarregarLojasAsync(ctx, ct);
                    return;
                }
            }

            var termo = ctx.ProdutoTermo;
            if (string.IsNullOrWhiteSpace(termo) && ctx.Categoria.HasValue)
                termo = ctx.Categoria.Value.ToString();

            if (!string.IsNullOrWhiteSpace(termo) || ctx.TermosBuscaProduto.Count > 0)
            {
                var produtos = ctx.TermosBuscaProduto.Count > 0
                    ? await _produtos.BuscarPorTermosAsync(ctx.TermosBuscaProduto)
                    : await _produtos.BuscarPorNomeAsync(termo!);

                // Fallback: termo limpo único se sinônimos não acharam nada.
                if ((produtos is null || produtos.Count == 0) && !string.IsNullOrWhiteSpace(termo))
                    produtos = await _produtos.BuscarPorNomeAsync(termo);

                if (produtos is { Count: > 0 })
                {
                    if (ctx.Categoria.HasValue)
                        produtos = produtos.Where(p => p.Categoria == ctx.Categoria.Value).ToList();

                    var ids = produtos.Take(20).Select(p => p.Id).ToList();
                    var ofertas = await _ofertas.ListarDisponiveisPorProdutosAsync(ids) ?? new List<Oferta>();

                    if (ofertas.Count == 0)
                    {
                        foreach (var p in produtos.Take(10))
                        {
                            decimal? dist = null;
                            if (ctx.Latitude is double lat && ctx.Longitude is double lng
                                && p.Loja?.Endereco?.Latitude is decimal plat
                                && p.Loja.Endereco.Longitude is decimal plng)
                            {
                                dist = GeoHelper.CalcularDistanciaKm((decimal)lat, (decimal)lng, plat, plng);
                            }

                            ctx.Candidatos.Add(new CandidatoIA
                            {
                                ProdutoId = p.Id,
                                LojaId = p.LojaId,
                                Titulo = p.NomeProduto,
                                NomeProduto = p.NomeProduto,
                                NomeLoja = p.Loja?.NomeFantasia,
                                Preco = p.Preco > 0 ? p.Preco : null,
                                DistanciaKm = dist,
                                Disponivel = p.Ativo
                            });
                        }
                    }
                    else
                    {
                        var lojaIds = ofertas.Where(o => o.LojaId != Guid.Empty).Select(o => o.LojaId).Distinct().Take(15).ToList();
                        var medias = await ObterResumosBatchSeguroAsync(lojaIds);

                        foreach (var o in ofertas.Take(40))
                        {
                            decimal? dist = null;
                            if (ctx.Latitude is double lat && ctx.Longitude is double lng
                                && o.Loja?.Endereco?.Latitude is decimal olat
                                && o.Loja.Endereco.Longitude is decimal olng)
                            {
                                dist = GeoHelper.CalcularDistanciaKm((decimal)lat, (decimal)lng, olat, olng);
                            }

                            medias.TryGetValue(o.LojaId, out var av);
                            var nomeProduto = o.Produto?.NomeProduto ?? produtos.FirstOrDefault(p => p.Id == o.ProdutoId)?.NomeProduto;
                            ctx.Candidatos.Add(new CandidatoIA
                            {
                                ProdutoId = o.ProdutoId,
                                OfertaId = o.Id,
                                LojaId = o.LojaId,
                                Titulo = $"{nomeProduto} — {o.Loja?.NomeFantasia}",
                                NomeProduto = nomeProduto,
                                NomeLoja = o.Loja?.NomeFantasia,
                                Preco = o.Preco,
                                DistanciaKm = dist,
                                EmPromocao = o.EmPromocao,
                                Disponivel = o.Disponivel,
                                MediaAvaliacao = av.Qtd > 0 ? av.Media : null,
                                QuantidadeAvaliacoes = av.Qtd
                            });
                        }
                    }

                    if (ctx.Intencao == IntencaoIA.BuscarPromocao)
                        ctx.Candidatos = ctx.Candidatos.Where(c => c.EmPromocao).ToList();
                }
            }

            // Entrega: sem inventar delivery; se não houver oferta/produto, ainda sugere lojas próximas.
            if (ctx.Intencao == IntencaoIA.ConsultarEntrega && ctx.Candidatos.Count == 0)
                await CarregarLojasAsync(ctx, ct);
        }

        private async Task<Dictionary<Guid, (double Media, int Qtd)>> ObterResumosBatchSeguroAsync(IEnumerable<Guid> lojaIds)
        {
            try
            {
                return await _avaliacoes.ObterResumosPorLojasAsync(lojaIds)
                       ?? new Dictionary<Guid, (double Media, int Qtd)>();
            }
            catch
            {
                return new Dictionary<Guid, (double Media, int Qtd)>();
            }
        }

        private sealed class CestaLojaAcumulador
        {
            public Loja? Loja { get; set; }
            public decimal Total { get; set; }
            public int Encontrados { get; set; }
            public List<string> NomesItens { get; } = new();
            public bool TemPromo { get; set; }
        }

        private async Task CarregarCestaAsync(ContextoIA ctx, CancellationToken ct)
        {
            var itens = ctx.ItensBusca.Count > 0
                ? ctx.ItensBusca
                : (!string.IsNullOrWhiteSpace(ctx.ProdutoTermo)
                    ? new List<string> { ctx.ProdutoTermo }
                    : new List<string> { "arroz", "feijao", "oleo", "acucar", "cafe" });

            var porLoja = new Dictionary<Guid, CestaLojaAcumulador>();

            foreach (var item in itens.Take(8))
            {
                ct.ThrowIfCancellationRequested();
                var produtos = await _produtos.BuscarPorNomeAsync(item) ?? new List<Produto>();
                if (produtos.Count == 0) continue;

                var prodIds = produtos.Take(15).Select(p => p.Id).ToList();
                var ofertas = await _ofertas.ListarDisponiveisPorProdutosAsync(prodIds) ?? new List<Oferta>();

                // Agrupa a opção mais barata deste item por loja
                var opcoesPorLoja = new Dictionary<Guid, (Loja? Loja, string NomeProduto, decimal Preco, bool Promo)>();

                foreach (var o in ofertas.Where(o => o.Disponivel && o.Preco > 0 && o.LojaId != Guid.Empty))
                {
                    var nomeProd = o.Produto?.NomeProduto ?? produtos.FirstOrDefault(p => p.Id == o.ProdutoId)?.NomeProduto ?? item;
                    if (!opcoesPorLoja.TryGetValue(o.LojaId, out var atual) || o.Preco < atual.Preco)
                    {
                        opcoesPorLoja[o.LojaId] = (o.Loja, nomeProd, o.Preco, o.EmPromocao);
                    }
                }

                foreach (var p in produtos.Where(p => p.Ativo && p.Preco > 0 && p.LojaId.HasValue && p.LojaId.Value != Guid.Empty))
                {
                    var pLojaId = p.LojaId!.Value;
                    if (!opcoesPorLoja.TryGetValue(pLojaId, out var atual) || p.Preco < atual.Preco)
                    {
                        opcoesPorLoja[pLojaId] = (p.Loja, p.NomeProduto, p.Preco, false);
                    }
                }

                foreach (var kvp in opcoesPorLoja)
                {
                    var lojaId = kvp.Key;
                    var op = kvp.Value;
                    if (!porLoja.TryGetValue(lojaId, out var existente))
                    {
                        existente = new CestaLojaAcumulador { Loja = op.Loja };
                        porLoja[lojaId] = existente;
                    }

                    existente.Loja ??= op.Loja;
                    existente.Total += op.Preco;
                    existente.Encontrados += 1;
                    existente.NomesItens.Add($"{op.NomeProduto} ({op.Preco.ToString("C", CultureInfo.GetCultureInfo("pt-BR"))})");
                    existente.TemPromo = existente.TemPromo || op.Promo;
                }
            }

            if (porLoja.Count == 0) return;

            var medias = await ObterResumosBatchSeguroAsync(porLoja.Keys);

            foreach (var kvp in porLoja.OrderByDescending(x => x.Value.Encontrados).ThenBy(x => x.Value.Total))
            {
                var lojaId = kvp.Key;
                var info = kvp.Value;
                var nomeLoja = info.Loja?.NomeFantasia ?? "Loja";
                decimal? dist = null;
                if (ctx.Latitude is double lat && ctx.Longitude is double lng
                    && info.Loja?.Endereco?.Latitude is decimal olat
                    && info.Loja.Endereco.Longitude is decimal olng)
                {
                    dist = GeoHelper.CalcularDistanciaKm((decimal)lat, (decimal)lng, olat, olng);
                }

                medias.TryGetValue(lojaId, out var av);
                var resumoItens = $"{info.Encontrados}/{itens.Count} itens encontrados: {string.Join(", ", info.NomesItens)}";

                ctx.Candidatos.Add(new CandidatoIA
                {
                    LojaId = lojaId,
                    Titulo = $"Cesta ({info.Encontrados}/{itens.Count} itens) — {nomeLoja}",
                    NomeLoja = nomeLoja,
                    NomeProduto = $"Cesta ({info.Encontrados}/{itens.Count} itens)",
                    Preco = info.Total,
                    DistanciaKm = dist,
                    EmPromocao = info.TemPromo,
                    Disponivel = true,
                    MediaAvaliacao = av.Qtd > 0 ? av.Media : null,
                    QuantidadeAvaliacoes = av.Qtd,
                    Motivos = new List<string> { resumoItens }
                });
            }
        }

        private async Task CarregarProdutosDaLojaAsync(ContextoIA ctx, CancellationToken ct)
        {
            Loja? loja = null;
            if (!string.IsNullOrWhiteSpace(ctx.LojaTermo))
            {
                var lojas = await _lojas.BuscarPorNomeAsync(ctx.LojaTermo) ?? new List<Loja>();
                loja = lojas.FirstOrDefault();
            }
            else if (ctx.LojaIdUsuario.HasValue && ctx.LojaIdUsuario.Value != Guid.Empty)
            {
                loja = await _lojas.ObterPorIdAsync(ctx.LojaIdUsuario.Value);
            }
            else
            {
                await CarregarLojasAsync(ctx, ct);
                return;
            }

            if (loja is null) return;

            var produtos = await _produtos.ListarProdutosAsync(loja.Id) ?? new List<Produto>();
            if (produtos.Count == 0) return;

            var prodIds = produtos.Take(30).Select(p => p.Id).ToList();
            var ofertas = await _ofertas.ListarDisponiveisPorProdutosAsync(prodIds) ?? new List<Oferta>();
            var ofertaPorProd = ofertas
                .Where(o => o.Disponivel && o.LojaId == loja.Id)
                .GroupBy(o => o.ProdutoId)
                .ToDictionary(g => g.Key, g => g.OrderBy(o => o.Preco).First());

            decimal? dist = null;
            if (ctx.Latitude is double lat && ctx.Longitude is double lng
                && loja.Endereco?.Latitude is decimal olat && loja.Endereco.Longitude is decimal olng)
            {
                dist = GeoHelper.CalcularDistanciaKm((decimal)lat, (decimal)lng, olat, olng);
            }

            var medias = await ObterResumosBatchSeguroAsync(new[] { loja.Id });
            medias.TryGetValue(loja.Id, out var av);

            foreach (var p in produtos.Where(p => p.Ativo).Take(15))
            {
                ofertaPorProd.TryGetValue(p.Id, out var oferta);
                var preco = oferta?.Preco ?? (p.Preco > 0 ? p.Preco : (decimal?)null);

                ctx.Candidatos.Add(new CandidatoIA
                {
                    ProdutoId = p.Id,
                    OfertaId = oferta?.Id,
                    LojaId = loja.Id,
                    Titulo = $"{p.NomeProduto} — {loja.NomeFantasia}",
                    NomeProduto = p.NomeProduto,
                    NomeLoja = loja.NomeFantasia,
                    Preco = preco,
                    EmPromocao = oferta?.EmPromocao ?? false,
                    DistanciaKm = dist,
                    Disponivel = true,
                    MediaAvaliacao = av.Qtd > 0 ? av.Media : null,
                    QuantidadeAvaliacoes = av.Qtd
                });
            }
        }

        private async Task CarregarPorOcasiaoAsync(ContextoIA ctx, CancellationToken ct)
        {
            var itens = ctx.ItensBusca.Count > 0
                ? ctx.ItensBusca
                : Vocabulario.VocabularioIA.ItensPorOcasiao
                    .Where(kv => ctx.MensagemNormalizada.Contains(kv.Key, StringComparison.Ordinal))
                    .SelectMany(kv => kv.Value)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

            foreach (var item in itens.Take(4))
            {
                ct.ThrowIfCancellationRequested();
                var produtos = await _produtos.BuscarPorNomeAsync(item) ?? new List<Produto>();
                if (produtos.Count == 0) continue;

                var ids = produtos.Take(8).Select(p => p.Id).ToList();
                var ofertas = await _ofertas.ListarDisponiveisPorProdutosAsync(ids) ?? new List<Oferta>();

                if (ofertas.Count > 0)
                {
                    foreach (var o in ofertas.OrderBy(o => o.Preco).Take(3))
                    {
                        var nomeProd = o.Produto?.NomeProduto ?? produtos.FirstOrDefault(p => p.Id == o.ProdutoId)?.NomeProduto ?? item;
                        ctx.Candidatos.Add(new CandidatoIA
                        {
                            ProdutoId = o.ProdutoId,
                            OfertaId = o.Id,
                            LojaId = o.LojaId,
                            Titulo = $"{nomeProd} — {o.Loja?.NomeFantasia}",
                            NomeProduto = nomeProd,
                            NomeLoja = o.Loja?.NomeFantasia,
                            Preco = o.Preco,
                            EmPromocao = o.EmPromocao,
                            Disponivel = o.Disponivel
                        });
                    }
                }
                else
                {
                    foreach (var p in produtos.Where(p => p.Ativo).Take(2))
                    {
                        ctx.Candidatos.Add(new CandidatoIA
                        {
                            ProdutoId = p.Id,
                            LojaId = p.LojaId,
                            Titulo = $"{p.NomeProduto} — {p.Loja?.NomeFantasia}",
                            NomeProduto = p.NomeProduto,
                            NomeLoja = p.Loja?.NomeFantasia,
                            Preco = p.Preco > 0 ? p.Preco : null,
                            Disponivel = p.Ativo
                        });
                    }
                }
            }
        }

        private async Task CarregarConcorrenciaLojistaAsync(ContextoIA ctx, CancellationToken ct)
        {
            if (!ctx.LojaIdUsuario.HasValue || ctx.LojaIdUsuario.Value == Guid.Empty)
                return;

            var minhaLojaId = ctx.LojaIdUsuario.Value;
            var meusProdutos = await _produtos.ListarProdutosAsync(minhaLojaId) ?? new List<Produto>();
            if (meusProdutos.Count == 0) return;

            var cultura = CultureInfo.GetCultureInfo("pt-BR");

            foreach (var meuProd in meusProdutos.Where(p => p.Ativo && p.Preco > 0).Take(15))
            {
                ct.ThrowIfCancellationRequested();
                var similares = await _produtos.BuscarPorNomeAsync(meuProd.NomeProduto) ?? new List<Produto>();
                var concorrentes = similares.Where(p => p.LojaId != minhaLojaId && p.Ativo && p.Preco > 0).ToList();

                var simIds = similares.Select(p => p.Id).ToList();
                var ofertas = await _ofertas.ListarDisponiveisPorProdutosAsync(simIds) ?? new List<Oferta>();
                var ofertasConc = ofertas.Where(o => o.LojaId != minhaLojaId && o.Disponivel && o.Preco > 0).ToList();

                var precosConcorrentes = concorrentes.Select(c => (Preco: c.Preco, Loja: c.Loja?.NomeFantasia ?? "Concorrente"))
                    .Concat(ofertasConc.Select(o => (Preco: o.Preco, Loja: o.Loja?.NomeFantasia ?? "Concorrente")))
                    .OrderBy(x => x.Preco)
                    .ToList();

                var motivos = new List<string>();
                if (precosConcorrentes.Count > 0)
                {
                    var menor = precosConcorrentes[0];
                    var diff = meuProd.Preco - menor.Preco;
                    if (diff > 0)
                    {
                        motivos.Add($"Mais caro que a concorrência: {diff.ToString("C", cultura)} acima de {menor.Loja} ({menor.Preco.ToString("C", cultura)})");
                    }
                    else
                    {
                        motivos.Add($"Preço mais competitivo da plataforma (menor concorrente: {menor.Preco.ToString("C", cultura)} em {menor.Loja})");
                    }
                }
                else
                {
                    motivos.Add("Sem concorrentes diretos encontrados para este item");
                }

                ctx.Candidatos.Add(new CandidatoIA
                {
                    ProdutoId = meuProd.Id,
                    LojaId = minhaLojaId,
                    Titulo = $"{meuProd.NomeProduto} (Seu preço: {meuProd.Preco.ToString("C", cultura)})",
                    NomeProduto = meuProd.NomeProduto,
                    NomeLoja = meuProd.Loja?.NomeFantasia ?? "Sua Loja",
                    Preco = meuProd.Preco,
                    Disponivel = true,
                    Motivos = motivos
                });
            }
        }

        private async Task CarregarLojasAsync(ContextoIA ctx, CancellationToken ct)
        {
            List<Dominio.Entities.Estabelecimentos.Loja> lojas;
            if (!string.IsNullOrWhiteSpace(ctx.LojaTermo))
                lojas = await _lojas.BuscarPorNomeAsync(ctx.LojaTermo) ?? new List<Loja>();
            else if (ctx.Latitude is double lat && ctx.Longitude is double lng)
            {
                // Raio amplo para "mais próxima": ranqueamento por distância faz o corte fino.
                var raio = ctx.DistanciaMaxKm
                    ?? (ctx.Intencao is IntencaoIA.BuscarLojaMaisProxima or IntencaoIA.ConsultarEntrega ? 500m : 15m);
                lojas = await _lojas.ListarPorProximidadeAsync((decimal)lat, (decimal)lng, raio, 20) ?? new List<Loja>();
                if (lojas.Count == 0)
                {
                    var page = await _lojas.ListarPaginadoAsync(Dominio.Comum.PaginacaoParametros.De(1, 20));
                    lojas = page?.Items?.ToList() ?? new List<Loja>();
                    ctx.FallbacksUsados.Add("sem_lojas_no_raio");
                }
            }
            else
            {
                var page = await _lojas.ListarPaginadoAsync(Dominio.Comum.PaginacaoParametros.De(1, 20));
                lojas = page?.Items?.ToList() ?? new List<Loja>();
            }

            var topLojas = lojas.Take(20).ToList();
            var medias = await ObterResumosBatchSeguroAsync(topLojas.Select(l => l.Id));

            foreach (var l in topLojas)
            {
                decimal? dist = null;
                if (ctx.Latitude is double lat && ctx.Longitude is double lng
                    && l.Endereco?.Latitude is decimal olat && l.Endereco.Longitude is decimal olng)
                {
                    dist = GeoHelper.CalcularDistanciaKm((decimal)lat, (decimal)lng, olat, olng);
                }

                medias.TryGetValue(l.Id, out var av);

                ctx.Candidatos.Add(new CandidatoIA
                {
                    LojaId = l.Id,
                    Titulo = l.NomeFantasia,
                    NomeLoja = l.NomeFantasia,
                    DistanciaKm = dist,
                    Disponivel = l.Ativo,
                    MediaAvaliacao = av.Qtd > 0 ? av.Media : null,
                    QuantidadeAvaliacoes = av.Qtd
                });
            }
        }

        private async Task TentarRagAsync(ContextoIA ctx, CancellationToken ct)
        {
            var precisaRag =
                ctx.Intencao == IntencaoIA.BuscarProdutosRelacionados
                || (ctx.Candidatos.Count == 0 && (
                    !string.IsNullOrWhiteSpace(ctx.ProdutoTermo)
                    || ctx.TermosBuscaProduto.Count > 0
                    || !string.IsNullOrWhiteSpace(ctx.MensagemOriginal)))
                || ctx.Intencao == IntencaoIA.RecomendarProduto;

            if (!precisaRag)
                return;

            // Híbrido: tenta termo limpo → sinônimos → frase original.
            var consultas = Vocabulario.VocabularioIA.MontarConsultasRag(ctx.ProdutoTermo, ctx.MensagemOriginal);
            if (consultas.Count == 0 && ctx.TermosBuscaProduto.Count > 0)
                consultas = ctx.TermosBuscaProduto;

            IReadOnlyList<RagHitIA> hits = Array.Empty<RagHitIA>();
            foreach (var consulta in consultas)
            {
                hits = await _rag.BuscarAuxiliarAsync(consulta, 5, ct);
                if (hits.Count > 0)
                    break;
            }

            if (hits.Count == 0)
            {
                ctx.FallbacksUsados.Add("sem_rag");
                return;
            }

            ctx.UsouRag = true;
            ctx.ResultadosRag = hits.Count;
            ctx.FallbacksUsados.Add("rag_hibrido");

            // Refinamento: se o termo ainda estiver vazio/sujo, aproveita título do hit.
            if (string.IsNullOrWhiteSpace(ctx.ProdutoTermo)
                || ctx.ProdutoTermo.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length > 2)
            {
                var titulo = hits.FirstOrDefault(h => !string.IsNullOrWhiteSpace(h.Titulo))?.Titulo;
                if (!string.IsNullOrWhiteSpace(titulo))
                {
                    var primeiro = titulo.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(primeiro) && primeiro.Length >= 3)
                    {
                        ctx.ProdutoTermo = primeiro;
                        ctx.TermosBuscaProduto = Vocabulario.VocabularioIA.ExpandirTermosBusca(primeiro).ToList();
                    }
                }
            }

            // Enriquecer: se ainda sem candidatos, buscar produtos pelos títulos do RAG
            if (ctx.Candidatos.Count == 0)
            {
                foreach (var hit in hits.Where(h => h.Tipo == "Produto").Take(5))
                {
                    var p = await _produtos.ObterPorIdAsync(hit.EntidadeId);
                    if (p is null) continue;
                    ctx.Candidatos.Add(new CandidatoIA
                    {
                        ProdutoId = p.Id,
                        LojaId = p.LojaId,
                        Titulo = p.NomeProduto,
                        NomeProduto = p.NomeProduto,
                        NomeLoja = p.Loja?.NomeFantasia,
                        Preco = p.Preco > 0 ? p.Preco : null,
                        Disponivel = p.Ativo
                    });
                }

                // Segunda chance SQL com termos refinados pós-RAG.
                if (ctx.Candidatos.Count == 0 && ctx.TermosBuscaProduto.Count > 0)
                {
                    var produtosRag = await _produtos.BuscarPorTermosAsync(ctx.TermosBuscaProduto);
                    foreach (var p in produtosRag.Take(10))
                    {
                        ctx.Candidatos.Add(new CandidatoIA
                        {
                            ProdutoId = p.Id,
                            LojaId = p.LojaId,
                            Titulo = p.NomeProduto,
                            NomeProduto = p.NomeProduto,
                            NomeLoja = p.Loja?.NomeFantasia,
                            Preco = p.Preco > 0 ? p.Preco : null,
                            Disponivel = p.Ativo
                        });
                    }
                }

                if (ctx.Candidatos.Count > 0)
                {
                    var ids = ctx.Candidatos.Where(c => c.ProdutoId.HasValue).Select(c => c.ProdutoId!.Value).ToList();
                    var ofertas = await _ofertas.ListarDisponiveisPorProdutosAsync(ids);
                    if (ofertas.Count > 0)
                    {
                        ctx.Candidatos.Clear();
                        foreach (var o in ofertas.Take(20))
                        {
                            ctx.Candidatos.Add(new CandidatoIA
                            {
                                ProdutoId = o.ProdutoId,
                                OfertaId = o.Id,
                                LojaId = o.LojaId,
                                Titulo = $"{o.Produto?.NomeProduto} — {o.Loja?.NomeFantasia}",
                                NomeProduto = o.Produto?.NomeProduto,
                                NomeLoja = o.Loja?.NomeFantasia,
                                Preco = o.Preco,
                                EmPromocao = o.EmPromocao,
                                Disponivel = o.Disponivel
                            });
                        }
                    }
                }
            }
        }
    }
}
