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
                    SituacaoCadastral = json.SituacaoCadastral?.Trim() ?? string.Empty,
                    Endereco = MontarEndereco(
                        json.TipoLogradouro, json.Logradouro, json.Numero, json.Complemento, json.Bairro, json.Municipio, json.Uf, json.Cep),
                    Email = Texto(json.Email),
                    Telefone = TelefoneOpenCnpj(json.Telefones)
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
                    SituacaoCadastral = json.DescricaoSituacaoCadastral?.Trim() ?? string.Empty,
                    Endereco = MontarEndereco(
                        json.DescricaoTipoDeLogradouro, json.Logradouro, json.Numero, json.Complemento, json.Bairro, json.Municipio, json.Uf, json.Cep),
                    Email = Texto(json.Email),
                    Telefone = Digitos(json.DddTelefone1)
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

        private static CnpjEndereco? MontarEndereco(
            string? tipoLogradouro,
            string? logradouro,
            string? numero,
            string? complemento,
            string? bairro,
            string? cidade,
            string? uf,
            string? cep)
        {
            var endereco = new CnpjEndereco
            {
                Logradouro = LogradouroComTipo(tipoLogradouro, logradouro),
                Numero = Texto(numero),
                Complemento = Texto(complemento),
                Bairro = Texto(bairro),
                Cidade = Texto(cidade),
                Uf = Texto(uf),
                Cep = Digitos(cep)
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

        private static string? TelefoneOpenCnpj(List<OpenCnpjTelefone>? telefones)
        {
            if (telefones is null)
                return null;

            foreach (var telefone in telefones)
            {
                if (telefone.IsFax)
                    continue;

                var digitos = Digitos(string.Concat(telefone.Ddd, telefone.Numero));
                if (digitos is not null)
                    return digitos;
            }

            return null;
        }

        private static string? LogradouroComTipo(string? tipo, string? logradouro)
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

        private sealed class OpenCnpjResponse
        {
            [JsonPropertyName("razao_social")]
            public string? RazaoSocial { get; set; }

            [JsonPropertyName("nome_fantasia")]
            public string? NomeFantasia { get; set; }

            [JsonPropertyName("situacao_cadastral")]
            public string? SituacaoCadastral { get; set; }

            [JsonPropertyName("tipo_logradouro")]
            public string? TipoLogradouro { get; set; }

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

            [JsonPropertyName("telefones")]
            public List<OpenCnpjTelefone>? Telefones { get; set; }
        }

        private sealed class OpenCnpjTelefone
        {
            [JsonPropertyName("ddd")]
            public string? Ddd { get; set; }

            [JsonPropertyName("numero")]
            public string? Numero { get; set; }

            [JsonPropertyName("is_fax")]
            public bool IsFax { get; set; }
        }

        private sealed class BrasilApiCnpjResponse
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
