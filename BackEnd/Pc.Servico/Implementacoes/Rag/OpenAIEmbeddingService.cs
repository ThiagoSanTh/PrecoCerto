using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pc.Servico.Interfaces;
using Pc.Servico.Modelos.Rag;

namespace Pc.Servico.Implementacoes.Rag
{
    public class OpenAIEmbeddingService : IEmbeddingService
    {
        private readonly HttpClient _http;
        private readonly RagSettings _settings;
        private readonly ILogger<OpenAIEmbeddingService> _logger;

        public OpenAIEmbeddingService(
            HttpClient http,
            IOptions<RagSettings> settings,
            ILogger<OpenAIEmbeddingService> logger)
        {
            _http = http;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<float[]> GerarEmbeddingAsync(string texto, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_settings.ApiKey))
                throw new InvalidOperationException("Rag:ApiKey não configurada.");

            if (string.IsNullOrWhiteSpace(texto))
                throw new ArgumentException("Texto para embedding não pode ser vazio.", nameof(texto));

            using var request = new HttpRequestMessage(HttpMethod.Post, "embeddings");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
            request.Content = JsonContent.Create(new
            {
                input = texto.Length > 8000 ? texto[..8000] : texto,
                model = _settings.EmbeddingModel
            });

            using var response = await _http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                var status = (int)response.StatusCode;
                _logger.LogError(
                    "Erro no provider de embeddings. Status={Status} BodyLength={Len}",
                    status,
                    body.Length);

                if (status is 401 or 403)
                {
                    throw new RagEmbeddingAuthException(
                        status,
                        "Rag:ApiKey rejeitada pelo provider OpenAI (401/403). "
                        + "Use uma key OpenAI válida em user-secrets — key Gemini não funciona neste endpoint.");
                }

                throw new HttpRequestException($"Provider de embeddings retornou {status}.");
            }

            var payload = await response.Content.ReadFromJsonAsync<OpenAiEmbeddingResponse>(cancellationToken)
                ?? throw new InvalidOperationException("Resposta de embedding inválida.");

            var vector = payload.Data?.FirstOrDefault()?.Embedding
                ?? throw new InvalidOperationException("Embedding ausente na resposta.");

            if (vector.Length != _settings.EmbeddingDimension)
            {
                throw new InvalidOperationException(
                    $"Dimensão inesperada: {vector.Length}, esperado {_settings.EmbeddingDimension}.");
            }

            _logger.LogInformation("Embedding gerado. Dimensao={Dim}", vector.Length);
            return vector;
        }

        private sealed class OpenAiEmbeddingResponse
        {
            [JsonPropertyName("data")]
            public List<OpenAiEmbeddingItem>? Data { get; set; }
        }

        private sealed class OpenAiEmbeddingItem
        {
            [JsonPropertyName("embedding")]
            public float[] Embedding { get; set; } = Array.Empty<float>();
        }
    }
}
