using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Pc.Dominio.Enums.MotorIA;
using Pc.Repositorio.Comum;
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
            ILogger<MotorIA> logger)
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
        }

        public async Task<ResultadoAnaliseIA> AnalisarAsync(
            PedidoAnaliseIA pedido,
            CancellationToken cancellationToken = default)
        {
            var swTotal = Stopwatch.StartNew();
            var swInterp = Stopwatch.StartNew();
            var ctx = _interpretador.Interpretar(pedido);
            swInterp.Stop();

            if (ctx.Intencao is IntencaoIA.ForaDoDominio or IntencaoIA.NaoEntendida or IntencaoIA.Saudacao)
            {
                return Finalizar(ctx, swTotal, swInterp.ElapsedMilliseconds, 0, 0);
            }

            await TentarClimaAsync(ctx, cancellationToken);
            _regras.Aplicar(ctx);

            var swDados = Stopwatch.StartNew();
            await CarregarCandidatosAsync(ctx, cancellationToken);
            swDados.Stop();

            // #region agent log
            try
            {
                var payload = System.Text.Json.JsonSerializer.Serialize(new
                {
                    sessionId = "6c7c29",
                    runId = "pre-fix",
                    hypothesisId = "E",
                    location = "MotorIA.cs:apos-SQL",
                    message = "candidatos-apos-sql",
                    data = new
                    {
                        produto = ctx.ProdutoTermo,
                        termos = ctx.TermosBuscaProduto,
                        candidatos = ctx.Candidatos.Count,
                        intencao = ctx.Intencao.ToString()
                    },
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });
                System.IO.File.AppendAllText(@"d:\Dev\PrecoCerto\debug-6c7c29.log", payload + "\n");
            }
            catch { /* debug */ }
            // #endregion

            var swRag = Stopwatch.StartNew();
            await TentarRagAsync(ctx, cancellationToken);
            swRag.Stop();

            _recomendacao.Recomendar(ctx);

            _logger.LogInformation(
                "MotorIA: Intencao={Intencao} Objetivo={Objetivo} InterpMs={Interp} DadosMs={Dados} RagMs={Rag} Candidatos={Cand} Confianca={Conf} Fallbacks={Fb}",
                ctx.Intencao,
                ctx.Objetivo,
                swInterp.ElapsedMilliseconds,
                swDados.ElapsedMilliseconds,
                swRag.ElapsedMilliseconds,
                ctx.Candidatos.Count,
                ctx.NivelConfianca,
                string.Join(',', ctx.FallbacksUsados));

            return Finalizar(ctx, swTotal, swInterp.ElapsedMilliseconds, swDados.ElapsedMilliseconds, swRag.ElapsedMilliseconds);
        }

        private ResultadoAnaliseIA Finalizar(
            ContextoIA ctx, Stopwatch total, long interpMs, long dadosMs, long ragMs)
        {
            total.Stop();
            var resposta = _gerador.Gerar(ctx);
            var resultados = (ctx.Decisao?.Ranking ?? Array.Empty<CandidatoIA>())
                .Select(c => new ResultadoItemAnaliseIA
                {
                    Tipo = c.OfertaId.HasValue ? "Oferta" : (c.LojaId.HasValue ? "Loja" : "Produto"),
                    Titulo = c.Titulo,
                    Score = c.Score,
                    Preco = c.Preco,
                    DistanciaKm = c.DistanciaKm.HasValue ? Math.Round((double)c.DistanciaKm.Value, 1) : null,
                    Loja = c.NomeLoja,
                    Produto = c.NomeProduto,
                    Motivos = c.Motivos
                }).ToList();

            _logger.LogInformation(
                "MotorIA fim: TotalMs={Total} ScoreMelhor={Score}",
                total.ElapsedMilliseconds,
                ctx.Decisao?.Melhor?.Score);

            return new ResultadoAnaliseIA
            {
                Sucesso = true,
                Resposta = resposta,
                Intencao = ctx.Intencao,
                Objetivo = ctx.Objetivo,
                Confianca = ctx.NivelConfianca,
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
                if (produtos.Count == 0 && !string.IsNullOrWhiteSpace(termo))
                    produtos = await _produtos.BuscarPorNomeAsync(termo);

                if (produtos.Count > 0)
                {
                    if (ctx.Categoria.HasValue)
                        produtos = produtos.Where(p => p.Categoria == ctx.Categoria.Value).ToList();

                    var ids = produtos.Take(20).Select(p => p.Id).ToList();
                    var ofertas = await _ofertas.ListarDisponiveisPorProdutosAsync(ids);

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
                        var lojaIds = ofertas.Where(o => o.Loja != null).Select(o => o.LojaId).Distinct().ToList();
                        var medias = new Dictionary<Guid, (double Media, int Qtd)>();
                        foreach (var lojaId in lojaIds.Take(15))
                        {
                            try
                            {
                                medias[lojaId] = await _avaliacoes.ObterResumoAvaliacaoAsync(lojaId);
                            }
                            catch
                            {
                                /* ignore */
                            }
                        }

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

        private async Task CarregarLojasAsync(ContextoIA ctx, CancellationToken ct)
        {
            List<Dominio.Entities.Estabelecimentos.Loja> lojas;
            if (!string.IsNullOrWhiteSpace(ctx.LojaTermo))
                lojas = await _lojas.BuscarPorNomeAsync(ctx.LojaTermo);
            else if (ctx.Latitude is double lat && ctx.Longitude is double lng)
            {
                // Raio amplo para "mais próxima": ranqueamento por distância faz o corte fino.
                var raio = ctx.DistanciaMaxKm
                    ?? (ctx.Intencao is IntencaoIA.BuscarLojaMaisProxima or IntencaoIA.ConsultarEntrega ? 500m : 15m);
                lojas = await _lojas.ListarPorProximidadeAsync((decimal)lat, (decimal)lng, raio, 20);
                if (lojas.Count == 0)
                {
                    var page = await _lojas.ListarPaginadoAsync(Dominio.Comum.PaginacaoParametros.De(1, 20));
                    lojas = page.Items.ToList();
                    ctx.FallbacksUsados.Add("sem_lojas_no_raio");
                }
            }
            else
            {
                var page = await _lojas.ListarPaginadoAsync(Dominio.Comum.PaginacaoParametros.De(1, 20));
                lojas = page.Items.ToList();
            }

            foreach (var l in lojas.Take(20))
            {
                decimal? dist = null;
                if (ctx.Latitude is double lat && ctx.Longitude is double lng
                    && l.Endereco?.Latitude is decimal olat && l.Endereco.Longitude is decimal olng)
                {
                    dist = GeoHelper.CalcularDistanciaKm((decimal)lat, (decimal)lng, olat, olng);
                }

                (double Media, int Qtd) av = (0, 0);
                try { av = await _avaliacoes.ObterResumoAvaliacaoAsync(l.Id); } catch { /* */ }

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

            // #region agent log
            try
            {
                var payload = System.Text.Json.JsonSerializer.Serialize(new
                {
                    sessionId = "6c7c29",
                    runId = "pre-fix",
                    hypothesisId = "A,C,D",
                    location = "MotorIA.cs:TentarRagAsync",
                    message = "rag-resultado",
                    data = new
                    {
                        precisaRag,
                        consultas,
                        hits = hits.Count,
                        candidatosAntes = ctx.Candidatos.Count,
                        produto = ctx.ProdutoTermo
                    },
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });
                System.IO.File.AppendAllText(@"d:\Dev\PrecoCerto\debug-6c7c29.log", payload + "\n");
            }
            catch { /* debug */ }
            // #endregion

            if (hits.Count == 0)
            {
                ctx.FallbacksUsados.Add("sem_rag");
                // #region agent log
                try
                {
                    var payload = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        sessionId = "6c7c29",
                        runId = "post-fix",
                        hypothesisId = "C",
                        location = "MotorIA.cs:TentarRagAsync:sem_rag",
                        message = "sem_rag-apos-consultas",
                        data = new { consultas, produto = ctx.ProdutoTermo, candidatos = ctx.Candidatos.Count },
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    });
                    System.IO.File.AppendAllText(@"d:\Dev\PrecoCerto\debug-6c7c29.log", payload + "\n");
                }
                catch { /* debug */ }
                // #endregion
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
