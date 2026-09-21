using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pc.Servico.Interfaces;
using Pc.Servico.Modelos.Rag;

namespace Pc.Servico.Implementacoes.Rag
{
    /// <summary>
    /// Embeddings via Gemini API (gemini-embedding-001 @ 1536 dims + L2 normalize).
    /// </summary>
    public class GeminiEmbeddingService : IEmbeddingService
    {
        private readonly HttpClient _http;
        private readonly RagSettings _settings;
        private readonly ILogger<GeminiEmbeddingService> _logger;

        public GeminiEmbeddingService(
            HttpClient http,
            IOptions<RagSettings> settings,
            ILogger<GeminiEmbeddingService> logger)
        {
            _http = http;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<float[]> GerarEmbeddingAsync(string texto, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_settings.ApiKey))
                throw new InvalidOperationException("Rag:ApiKey não configurada (Gemini).");

            if (string.IsNullOrWhiteSpace(texto))
                throw new ArgumentException("Texto para embedding não pode ser vazio.", nameof(texto));

            var model = string.IsNullOrWhiteSpace(_settings.EmbeddingModel)
                ? "gemini-embedding-001"
                : _settings.EmbeddingModel.Trim();
            if (model.StartsWith("models/", StringComparison.OrdinalIgnoreCase))
                model = model["models/".Length..];

            var path = $"models/{model}:embedContent?key={Uri.EscapeDataString(_settings.ApiKey)}";
            var payload = new
            {
                content = new
                {
                    parts = new[] { new { text = texto.Length > 8000 ? texto[..8000] : texto } }
                },
                taskType = "RETRIEVAL_DOCUMENT",
                outputDimensionality = _settings.EmbeddingDimension
            };

            var maxAttempts = Math.Clamp(_settings.MaxRetries, 1, 8);
            HttpResponseMessage? lastResponse = null;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                lastResponse?.Dispose();
                lastResponse = await _http.PostAsJsonAsync(path, payload, cancellationToken);

                if (lastResponse.IsSuccessStatusCode)
                    break;

                var status = (int)lastResponse.StatusCode;
                var body = await lastResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Erro Gemini embeddings. Status={Status} Attempt={Attempt}/{Max} BodyLength={Len}",
                    status,
                    attempt,
                    maxAttempts,
                    body.Length);

                if (status is 401 or 403)
                {
                    lastResponse.Dispose();
                    throw new RagEmbeddingAuthException(
                        status,
                        "Rag:ApiKey rejeitada pelo Gemini (401/403). "
                        + "Confirme a key do Google AI Studio em user-secrets Rag:ApiKey.");
                }

                // Rate limit: espera e tenta de novo (causa típica dos milhares de erros no reindex).
                if (status == 429 && attempt < maxAttempts)
                {
                    var delay = ObterDelayRetry(lastResponse, attempt);
                    // Mínimo 45s em 429 — free tier Gemini satura fácil em reindex em massa.
                    if (delay < TimeSpan.FromSeconds(45))
                        delay = TimeSpan.FromSeconds(45 + attempt * 15);
                    _logger.LogWarning(
                        "Gemini 429 (rate limit). Aguardando {DelayMs}ms antes da tentativa {Next}/{Max}.",
                        (int)delay.TotalMilliseconds,
                        attempt + 1,
                        maxAttempts);
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }

                lastResponse.Dispose();
                throw new HttpRequestException($"Provider Gemini de embeddings retornou {status}.");
            }

            using (lastResponse!)
            {
                var parsed = await lastResponse.Content.ReadFromJsonAsync<GeminiEmbedResponse>(cancellationToken)
                    ?? throw new InvalidOperationException("Resposta Gemini de embedding inválida.");

                var vector = parsed.Embedding?.Values
                    ?? throw new InvalidOperationException("Embedding ausente na resposta Gemini.");

                if (vector.Length != _settings.EmbeddingDimension)
                {
                    throw new InvalidOperationException(
                        $"Dimensão Gemini inesperada: {vector.Length}, esperado {_settings.EmbeddingDimension}.");
                }

                var normalizado = NormalizarL2(vector);
                _logger.LogInformation(
                    "Embedding Gemini gerado. Modelo={Model} Dimensao={Dim}",
                    model,
                    normalizado.Length);
                return normalizado;
            }
        }

        private static TimeSpan ObterDelayRetry(HttpResponseMessage response, int attempt)
        {
            if (response.Headers.RetryAfter?.Delta is TimeSpan delta && delta > TimeSpan.Zero)
                return delta + TimeSpan.FromMilliseconds(200);

            if (response.Headers.RetryAfter?.Date is DateTimeOffset date)
            {
                var wait = date - DateTimeOffset.UtcNow;
                if (wait > TimeSpan.Zero)
                    return wait + TimeSpan.FromMilliseconds(200);
            }

            // Backoff exponencial: 2s, 4s, 8s... (cap 60s)
            var seconds = Math.Min(60, Math.Pow(2, attempt));
            return TimeSpan.FromSeconds(seconds);
        }

        private static float[] NormalizarL2(float[] v)
        {
            double sumSq = 0;
            for (var i = 0; i < v.Length; i++)
                sumSq += (double)v[i] * v[i];

            var norm = Math.Sqrt(sumSq);
            if (norm < 1e-12)
                return v;

            var outArr = new float[v.Length];
            for (var i = 0; i < v.Length; i++)
                outArr[i] = (float)(v[i] / norm);
            return outArr;
        }

        private sealed class GeminiEmbedResponse
        {
            [JsonPropertyName("embedding")]
            public GeminiEmbeddingValues? Embedding { get; set; }
        }

        private sealed class GeminiEmbeddingValues
        {
            [JsonPropertyName("values")]
            public float[] Values { get; set; } = Array.Empty<float>();
        }
    }
}
