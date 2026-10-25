using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pc.Servico.Interfaces.MotorIA;
using Pc.Servico.Modelos.MotorIA;

namespace Pc.Servico.Implementacoes.MotorIA
{
    public class LlmRouterService : ILlmRouterService
    {
        private static readonly ConcurrentDictionary<string, DateTimeOffset> CooldownAte = new(StringComparer.OrdinalIgnoreCase);
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        private readonly IHttpClientFactory _httpFactory;
        private readonly LlmSettings _settings;
        private readonly ILogger<LlmRouterService> _logger;

        public LlmRouterService(
            IHttpClientFactory httpFactory,
            IOptions<LlmSettings> settings,
            ILogger<LlmRouterService> logger)
        {
            _httpFactory = httpFactory;
            _settings = settings.Value;
            _logger = logger;
        }

        public void ResetarCircuitBreaker() => CooldownAte.Clear();

        public async Task<LlmInterpretacaoResultado?> InterpretarAsync(
            PedidoAnaliseIA pedido,
            CancellationToken cancellationToken = default)
        {
            var prompt = MontarPromptInterpretacao(pedido);

            var (json, provedor) = await ChamarProvedoresAsync(prompt, jsonMode: true, cancellationToken);
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                var limpo = ExtrairBlocoJson(json);
                var parsed = JsonSerializer.Deserialize<LlmInterpretacaoResultado>(limpo, JsonOpts);
                if (parsed is null)
                    return null;

                parsed.Provedor = provedor;
                return parsed;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OmniRouter: falha ao desserializar JSON de {Provedor}.", provedor);
                return null;
            }
        }

        public async Task<(string? Texto, string Provedor)> GerarRespostaAsync(
            ContextoIA contexto,
            CancellationToken cancellationToken = default)
        {
            if (contexto.Decisao?.Ranking is null || contexto.Decisao.Ranking.Count == 0)
                return (null, "Deterministico");

            var prompt = MontarPromptResposta(contexto);
            var (texto, provedor) = await ChamarProvedoresAsync(prompt, jsonMode: false, cancellationToken);
            if (string.IsNullOrWhiteSpace(texto))
                return (null, "Deterministico");

            return (texto.Trim(), provedor);
        }

        private async Task<(string? Conteudo, string Provedor)> ChamarProvedoresAsync(
            string prompt,
            bool jsonMode,
            CancellationToken ct)
        {
            if (ProvedorDisponivel("Gemini", _settings.GeminiApiKey))
            {
                var res = await TentarGeminiAsync(prompt, jsonMode, ct);
                if (!string.IsNullOrWhiteSpace(res))
                    return (res, "Gemini");
            }

            if ( !ct.IsCancellationRequested && ProvedorDisponivel("NvidiaNim", _settings.NvidiaApiKey))
            {
                var res = await TentarNvidiaAsync(prompt, jsonMode, ct);
                if (!string.IsNullOrWhiteSpace(res))
                    return (res, "NvidiaNim");
            }

            return (null, "Deterministico");
        }

        private static bool ProvedorDisponivel(string nome, string? apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                return false;

            if (CooldownAte.TryGetValue(nome, out var ate) && DateTimeOffset.UtcNow < ate)
                return false;

            return true;
        }

        private void MarcarCooldown(string nome, string motivo)
        {
            var segundos = Math.Clamp(_settings.CooldownSegundos, 30, 3600);
            CooldownAte[nome] = DateTimeOffset.UtcNow.AddSeconds(segundos);
            _logger.LogWarning(
                "OmniRouter CircuitBreaker: provedor {Provedor} em cooldown por {Seg}s ({Motivo}).",
                nome,
                segundos,
                motivo);
        }

        private async Task<string?> TentarGeminiAsync(string prompt, bool jsonMode, CancellationToken ct)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(Math.Clamp(_settings.TimeoutMs, 1000, 10000));

            try
            {
                var client = _httpFactory.CreateClient("GeminiLlm");
                var model = string.IsNullOrWhiteSpace(_settings.GeminiModel) ? "gemini-2.5-flash" : _settings.GeminiModel.Trim();
                if (model.StartsWith("models/", StringComparison.OrdinalIgnoreCase))
                    model = model["models/".Length..];

                var path = $"models/{model}:generateContent?key={Uri.EscapeDataString(_settings.GeminiApiKey)}";
                var payload = new
                {
                    contents = new[]
                    {
                        new { parts = new[] { new { text = prompt } } }
                    },
                    generationConfig = new
                    {
                        temperature = 0.1,
                        maxOutputTokens = 350,
                        responseMimeType = jsonMode ? "application/json" : "text/plain"
                    }
                };

                using var response = await client.PostAsJsonAsync(path, payload, cts.Token);
                if (!response.IsSuccessStatusCode)
                {
                    var code = (int)response.StatusCode;
                    if (code is 429 or 401 or 403 or >= 500)
                        MarcarCooldown("Gemini", $"HTTP {code}");
                    return null;
                }

                var parsed = await response.Content.ReadFromJsonAsync<GeminiGenerateResponse>(cts.Token);
                return parsed?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
            }
            catch (Exception ex)
            {
                if (!ct.IsCancellationRequested)
                    MarcarCooldown("Gemini", ex.GetType().Name);
                return null;
            }
        }

        private async Task<string?> TentarNvidiaAsync(string prompt, bool jsonMode, CancellationToken ct)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(Math.Clamp(_settings.TimeoutMs, 1000, 10000));

            try
            {
                var client = _httpFactory.CreateClient("NvidiaLlm");
                using var req = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.NvidiaApiKey);

                var model = string.IsNullOrWhiteSpace(_settings.NvidiaModel)
                    ? "meta/llama-3.3-70b-instruct"
                    : _settings.NvidiaModel.Trim();

                req.Content = JsonContent.Create(new
                {
                    model,
                    messages = new[]
                    {
                        new
                        {
                            role = "system",
                            content = jsonMode
                                ? "Você é o motor semântico do Preço Certo em Saquarema/RJ. Responda APENAS com um objeto JSON válido sem markdown."
                                : "Você é o assistente do Preço Certo em Saquarema/RJ. Responda em até 2 frases curtas usando apenas os preços e lojas fornecidos."
                        },
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.1,
                    max_tokens = 350
                });

                using var response = await client.SendAsync(req, cts.Token);
                if (!response.IsSuccessStatusCode)
                {
                    var code = (int)response.StatusCode;
                    if (code is 429 or 401 or 403 or >= 500)
                        MarcarCooldown("NvidiaNim", $"HTTP {code}");
                    return null;
                }

                var parsed = await response.Content.ReadFromJsonAsync<OpenAiChatResponse>(cts.Token);
                return parsed?.Choices?.FirstOrDefault()?.Message?.Content;
            }
            catch (Exception ex)
            {
                if (!ct.IsCancellationRequested)
                    MarcarCooldown("NvidiaNim", ex.GetType().Name);
                return null;
            }
        }

        private static string MontarPromptInterpretacao(PedidoAnaliseIA pedido)
        {
            var hist = pedido.Historico is { Count: > 0 }
                ? string.Join(" | ", pedido.Historico.TakeLast(4).Select(h => $"{h.Papel}: {h.Texto}"))
                : "vazio";

            return $$"""
            Analise a mensagem do usuário no app Preço Certo (comparador de preços em Saquarema/RJ) e retorne APENAS JSON com este formato exato:
            {
              "intencao": "BuscarProduto | BuscarProdutoMaisBarato | CompararPrecos | BuscarPromocao | BuscarLoja | BuscarLojaMaisProxima | ProdutosDaLoja | MontarCesta | RecomendarPorOcasiao | AnalisarConcorrenciaLojista | ConsultarEntrega | Saudacao | ForaDoDominio",
              "objetivo": "Economizar | Proximidade | Rapidez | Promocao | Qualidade | Conveniencia | null",
              "produtoTermo": "substantivo principal do produto em minúsculo sem acento ou null",
              "itensBusca": ["lista", "de", "produtos", "sem", "acento"],
              "lojaTermo": "nome da loja se citada (sem verbos como vende/tem) ou null",
              "orcamentoMax": numero_ou_null,
              "distanciaMaxKm": numero_ou_null,
              "urgenciaAlta": false,
              "necessitaEntrega": false
            }
            Regras:
            - Se pedir receita, churrasco, café da manhã ou sintoma leve (dor de cabeça), use intencao="RecomendarPorOcasiao" e liste os ingredientes/itens em "itensBusca".
            - Se pedir futebol, política, piada ou tema fora de compras, use intencao="ForaDoDominio".
            - Se modoOperacional="loja" e perguntar sobre seus produtos/concorrência, use intencao="AnalisarConcorrenciaLojista".
            ModoOperacional: {{pedido.ModoOperacional ?? "cliente"}}
            HistoricoRecente: {{hist}}
            MensagemAtual: {{pedido.Mensagem}}
            """;
        }

        private static string MontarPromptResposta(ContextoIA ctx)
        {
            var top = ctx.Decisao!.Ranking.Take(4).Select(c => new
            {
                titulo = c.Titulo,
                loja = c.NomeLoja,
                produto = c.NomeProduto,
                preco = c.Preco,
                distanciaKm = c.DistanciaKm,
                emPromocao = c.EmPromocao,
                motivos = c.Motivos
            });

            var dadosJson = JsonSerializer.Serialize(top);
            return $"""
            Pergunta do usuário: "{ctx.MensagemOriginal}"
            Intenção: {ctx.Intencao}
            Opções reais do banco de dados (NUNCA invente preços ou lojas fora desta lista):
            {dadosJson}
            Redija uma resposta curta, natural e prestativa em português (máximo 2 frases) destacando a melhor opção e o valor exato em R$.
            """;
        }

        private static string ExtrairBlocoJson(string raw)
        {
            var s = raw.Trim();
            var idxIni = s.IndexOf('{');
            var idxFim = s.LastIndexOf('}');
            if (idxIni >= 0 && idxFim > idxIni)
                return s[idxIni..(idxFim + 1)];
            return s;
        }

        private sealed class GeminiGenerateResponse
        {
            [JsonPropertyName("candidates")]
            public List<GeminiCandidate>? Candidates { get; set; }
        }

        private sealed class GeminiCandidate
        {
            [JsonPropertyName("content")]
            public GeminiContent? Content { get; set; }
        }

        private sealed class GeminiContent
        {
            [JsonPropertyName("parts")]
            public List<GeminiPart>? Parts { get; set; }
        }

        private sealed class GeminiPart
        {
            [JsonPropertyName("text")]
            public string? Text { get; set; }
        }

        private sealed class OpenAiChatResponse
        {
            [JsonPropertyName("choices")]
            public List<OpenAiChoice>? Choices { get; set; }
        }

        private sealed class OpenAiChoice
        {
            [JsonPropertyName("message")]
            public OpenAiMessage? Message { get; set; }
        }

        private sealed class OpenAiMessage
        {
            [JsonPropertyName("content")]
            public string? Content { get; set; }
        }
    }
}
