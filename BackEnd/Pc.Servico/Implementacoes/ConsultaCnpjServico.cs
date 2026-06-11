using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Pc.Dominio.Validacoes;
using Pc.Servico.Excecoes;
using Pc.Servico.Interfaces;

namespace Pc.Servico.Implementacoes
{
    /// <summary>
    /// Consulta CNPJ na OpenCNPJ (primária, CDN estável) com fallback na BrasilAPI.
    /// </summary>
    public class ConsultaCnpjServico : IConsultaCnpjServico
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        private readonly HttpClient _http;
        private readonly IMemoryCache _cache;
        private readonly ConsultaCnpjSettings _settings;

        public ConsultaCnpjServico(HttpClient http, IMemoryCache cache, IOptions<ConsultaCnpjSettings> settings)
        {
            _http = http;
            _cache = cache;
            _settings = settings.Value;
        }

        public async Task<CnpjConsultaResultado?> ConsultarAsync(string cnpj, CancellationToken cancellationToken = default)
        {
            var digitos = CnpjValidator.ApenasDigitos(cnpj);
            if (!CnpjValidator.IsValido(digitos))
                return null;

            var cacheKey = $"cnpj:{digitos}";
            if (_cache.TryGetValue(cacheKey, out CnpjConsultaResultado? cached))
                return cached;

            var open = await ConsultarOpenCnpjAsync(digitos, cancellationToken);
            if (open.Resultado is not null)
            {
                Cachear(cacheKey, open.Resultado);
                return open.Resultado;
            }

            var brasil = await ConsultarBrasilApiAsync(digitos, cancellationToken);
            if (brasil.Resultado is not null)
            {
                Cachear(cacheKey, brasil.Resultado);
                return brasil.Resultado;
            }

            if (open.IsNaoEncontrado && (brasil.IsNaoEncontrado || brasil.IsIndisponivel))
                return null;

            if (brasil.IsNaoEncontrado && (open.IsNaoEncontrado || open.IsIndisponivel))
                return null;

            if (open.IsIndisponivel || brasil.IsIndisponivel)
            {
                throw new CnpjConsultaIndisponivelException(
                    "Consulta de CNPJ temporariamente indisponível. Tente novamente em instantes.");
            }

            return null;
        }

        private void Cachear(string cacheKey, CnpjConsultaResultado resultado)
        {
            _cache.Set(cacheKey, resultado, TimeSpan.FromMinutes(_settings.CacheMinutos));
        }

        private async Task<ConsultaProvedorResultado> ConsultarOpenCnpjAsync(string digitos, CancellationToken cancellationToken)
        {
            var url = $"{_settings.OpenCnpjBaseUrl.TrimEnd('/')}/{digitos}";

            try
            {
                var response = await _http.GetAsync(url, cancellationToken);

                if (response.StatusCode == HttpStatusCode.NotFound)
                    return ConsultaProvedorResultado.NaoEncontrado();

                if (response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable)
                    return ConsultaProvedorResultado.Indisponivel();

                if (!response.IsSuccessStatusCode)
                    return ConsultaProvedorResultado.Indisponivel();

                var json = await response.Content.ReadFromJsonAsync<OpenCnpjResponse>(JsonOptions, cancellationToken);
                if (json is null || string.IsNullOrWhiteSpace(json.RazaoSocial))
                    return ConsultaProvedorResultado.NaoEncontrado();

                return ConsultaProvedorResultado.Encontrado(new CnpjConsultaResultado
                {
                    Cnpj = digitos,
                    RazaoSocial = json.RazaoSocial.Trim(),
                    NomeFantasia = string.IsNullOrWhiteSpace(json.NomeFantasia) ? null : json.NomeFantasia.Trim(),
                    SituacaoCadastral = json.SituacaoCadastral?.Trim() ?? string.Empty
                });
            }
            catch
            {
                return ConsultaProvedorResultado.Indisponivel();
            }
        }

        private async Task<ConsultaProvedorResultado> ConsultarBrasilApiAsync(string digitos, CancellationToken cancellationToken)
        {
            var url = $"{_settings.BrasilApiBaseUrl.TrimEnd('/')}/cnpj/v1/{digitos}";

            try
            {
                var response = await _http.GetAsync(url, cancellationToken);

                if (response.StatusCode == HttpStatusCode.NotFound)
                    return ConsultaProvedorResultado.NaoEncontrado();

                if (response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable)
                    return ConsultaProvedorResultado.Indisponivel();

                if (!response.IsSuccessStatusCode)
                    return ConsultaProvedorResultado.Indisponivel();

                var json = await response.Content.ReadFromJsonAsync<BrasilApiCnpjResponse>(JsonOptions, cancellationToken);
                if (json is null || string.IsNullOrWhiteSpace(json.RazaoSocial))
                    return ConsultaProvedorResultado.NaoEncontrado();

                return ConsultaProvedorResultado.Encontrado(new CnpjConsultaResultado
                {
                    Cnpj = digitos,
                    RazaoSocial = json.RazaoSocial.Trim(),
                    NomeFantasia = string.IsNullOrWhiteSpace(json.NomeFantasia) ? null : json.NomeFantasia.Trim(),
                    SituacaoCadastral = json.DescricaoSituacaoCadastral?.Trim() ?? string.Empty
                });
            }
            catch
            {
                return ConsultaProvedorResultado.Indisponivel();
            }
        }

        private sealed class ConsultaProvedorResultado
        {
            public CnpjConsultaResultado? Resultado { get; init; }
            public bool IsNaoEncontrado { get; init; }
            public bool IsIndisponivel { get; init; }

            public static ConsultaProvedorResultado Encontrado(CnpjConsultaResultado resultado) =>
                new() { Resultado = resultado };

            public static ConsultaProvedorResultado NaoEncontrado() =>
                new() { IsNaoEncontrado = true };

            public static ConsultaProvedorResultado Indisponivel() =>
                new() { IsIndisponivel = true };
        }

        private sealed class OpenCnpjResponse
        {
            [JsonPropertyName("razao_social")]
            public string? RazaoSocial { get; set; }

            [JsonPropertyName("nome_fantasia")]
            public string? NomeFantasia { get; set; }

            [JsonPropertyName("situacao_cadastral")]
            public string? SituacaoCadastral { get; set; }
        }

        private sealed class BrasilApiCnpjResponse
        {
            [JsonPropertyName("razao_social")]
            public string? RazaoSocial { get; set; }

            [JsonPropertyName("nome_fantasia")]
            public string? NomeFantasia { get; set; }

            [JsonPropertyName("descricao_situacao_cadastral")]
            public string? DescricaoSituacaoCadastral { get; set; }
        }
    }
}
