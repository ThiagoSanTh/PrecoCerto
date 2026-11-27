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
    /// Consulta CNPJ na BrasilAPI (https://brasilapi.com.br/api/cnpj/v1/{cnpj}).
    /// </summary>
    public class ConsultaCnpjServico : IConsultaCnpjServico
    {
        private const string Indisponivel =
            "Consulta de CNPJ temporariamente indisponível. Tente novamente em instantes.";

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

            var json = await BuscarAsync(digitos, cancellationToken);
            if (json is null || string.IsNullOrWhiteSpace(json.RazaoSocial))
                return null;

            var resultado = new CnpjConsultaResultado
            {
                Cnpj = digitos,
                RazaoSocial = json.RazaoSocial.Trim(),
                NomeFantasia = Texto(json.NomeFantasia),
                SituacaoCadastral = json.DescricaoSituacaoCadastral?.Trim() ?? string.Empty,
                Endereco = Endereco(json),
                Email = Texto(json.Email),
                Telefone = Digitos(json.DddTelefone1)
            };
            _cache.Set(cacheKey, resultado, TimeSpan.FromMinutes(_settings.CacheMinutos));
            return resultado;
        }

        private async Task<BrasilApiCnpj?> BuscarAsync(string digitos, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _http.GetAsync(
                    $"{_settings.BrasilApiBaseUrl.TrimEnd('/')}/cnpj/v1/{digitos}",
                    cancellationToken);

                if (response.StatusCode == HttpStatusCode.NotFound)
                    return null;

                if (!response.IsSuccessStatusCode)
                    throw new CnpjConsultaIndisponivelException(Indisponivel);

                return await response.Content.ReadFromJsonAsync<BrasilApiCnpj>(JsonOptions, cancellationToken);
            }
            catch (CnpjConsultaIndisponivelException)
            {
                throw;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                throw new CnpjConsultaIndisponivelException(Indisponivel);
            }
        }

        private static CnpjEndereco? Endereco(BrasilApiCnpj json)
        {
            var endereco = new CnpjEndereco
            {
                Logradouro = Logradouro(json.DescricaoTipoDeLogradouro, json.Logradouro),
                Numero = Texto(json.Numero),
                Complemento = Texto(json.Complemento),
                Bairro = Texto(json.Bairro),
                Cidade = Texto(json.Municipio),
                Uf = Texto(json.Uf),
                Cep = Digitos(json.Cep)
            };

            if (endereco.Logradouro is null
                && endereco.Numero is null
                && endereco.Complemento is null
                && endereco.Bairro is null
                && endereco.Cidade is null
                && endereco.Uf is null
                && endereco.Cep is null)
            {
                return null;
            }

            return endereco;
        }

        private static string? Logradouro(string? tipo, string? logradouro)
        {
            var nome = Texto(logradouro);
            var prefixo = Texto(tipo);
            if (prefixo is null || nome is null)
                return nome;
            if (nome.StartsWith(prefixo, StringComparison.OrdinalIgnoreCase))
                return nome;
            return $"{prefixo} {nome}";
        }

        private static string? Texto(string? valor) =>
            string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

        private static string? Digitos(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return null;

            var digitos = new string(valor.Where(char.IsDigit).ToArray());
            return digitos.Length == 0 ? null : digitos;
        }

        private sealed class BrasilApiCnpj
        {
            [JsonPropertyName("razao_social")]
            public string? RazaoSocial { get; set; }

            [JsonPropertyName("nome_fantasia")]
            public string? NomeFantasia { get; set; }

            [JsonPropertyName("descricao_situacao_cadastral")]
            public string? DescricaoSituacaoCadastral { get; set; }

            [JsonPropertyName("descricao_tipo_de_logradouro")]
            public string? DescricaoTipoDeLogradouro { get; set; }

            [JsonPropertyName("logradouro")]
            public string? Logradouro { get; set; }

            [JsonPropertyName("numero")]
            public string? Numero { get; set; }

            [JsonPropertyName("complemento")]
            public string? Complemento { get; set; }

            [JsonPropertyName("bairro")]
            public string? Bairro { get; set; }

            [JsonPropertyName("municipio")]
            public string? Municipio { get; set; }

            [JsonPropertyName("uf")]
            public string? Uf { get; set; }

            [JsonPropertyName("cep")]
            public string? Cep { get; set; }

            [JsonPropertyName("email")]
            public string? Email { get; set; }

            [JsonPropertyName("ddd_telefone_1")]
            public string? DddTelefone1 { get; set; }
        }
    }
}
