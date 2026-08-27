using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pc.Servico.Excecoes;
using Pc.Servico.Interfaces;
using Pc.Servico.Modelos;

namespace Pc.Servico.Implementacoes
{
    /// <summary>
    /// Clima: Open-Meteo Forecast API.
    /// Cidade: Nominatim (OSM) — Open-Meteo ainda não oferece reverse geocode estável.
    /// </summary>
    public class OpenMeteoClimaProvedor : IClimaProvedor
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        private readonly HttpClient _http;
        private readonly ClimaSettings _settings;
        private readonly ILogger<OpenMeteoClimaProvedor> _logger;

        public OpenMeteoClimaProvedor(
            HttpClient http,
            IOptions<ClimaSettings> settings,
            ILogger<OpenMeteoClimaProvedor> logger)
        {
            _http = http;
            _settings = settings.Value;
            _logger = logger;
        }

        public string Nome => "open-meteo";

        public async Task<ClimaResposta> ConsultarAsync(
            decimal latitude,
            decimal longitude,
            CancellationToken cancellationToken = default)
        {
            var latStr = latitude.ToString(CultureInfo.InvariantCulture);
            var lngStr = longitude.ToString(CultureInfo.InvariantCulture);

            var climaTask = ConsultarForecastAsync(latStr, lngStr, cancellationToken);
            var geoTask = ConsultarNominatimAsync(latStr, lngStr, cancellationToken);

            ClimaResposta clima;
            try
            {
                clima = await climaTask;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Timeout ao consultar Open-Meteo lat={Lat} lng={Lng}", latStr, lngStr);
                try { await geoTask; } catch { /* observa a task paralela */ }
                throw new ClimaIndisponivelException("Consulta de clima excedeu o tempo limite.");
            }
            catch
            {
                try { await geoTask; } catch { /* observa a task paralela */ }
                throw;
            }

            clima.Localidade.Latitude = latitude;
            clima.Localidade.Longitude = longitude;

            try
            {
                var geo = await geoTask;
                if (geo is not null)
                {
                    clima.Localidade.Cidade = geo.Cidade ?? clima.Localidade.Cidade;
                    clima.Localidade.Estado = geo.Estado ?? clima.Localidade.Estado;
                    clima.Localidade.Pais = geo.Pais ?? clima.Localidade.Pais;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Reverse geocode falhou; clima segue sem cidade.");
            }

            return clima;
        }

        private async Task<ClimaResposta> ConsultarForecastAsync(
            string latStr,
            string lngStr,
            CancellationToken cancellationToken)
        {
            var url =
                $"{_settings.ForecastBaseUrl.TrimEnd('/')}?" +
                $"latitude={latStr}&longitude={lngStr}" +
                "&current=temperature_2m,relative_humidity_2m,apparent_temperature,precipitation,weather_code,wind_speed_10m" +
                "&hourly=temperature_2m,apparent_temperature,precipitation,precipitation_probability,weather_code" +
                "&daily=weather_code,temperature_2m_max,temperature_2m_min,apparent_temperature_max,precipitation_sum,precipitation_probability_max" +
                "&timezone=auto" +
                $"&forecast_days={Math.Clamp(_settings.DiasPrevisao, 1, 16)}";

            var sw = Stopwatch.StartNew();
            HttpResponseMessage resposta;
            try
            {
                resposta = await _http.GetAsync(url, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Falha de rede no Open-Meteo.");
                throw new ClimaIndisponivelException("Provedor de clima indisponível.");
            }

            sw.Stop();
            _logger.LogInformation(
                "Open-Meteo HTTP {Status} em {Ms}ms lat={Lat} lng={Lng}",
                (int)resposta.StatusCode,
                sw.ElapsedMilliseconds,
                latStr,
                lngStr);

            if (resposta.StatusCode == HttpStatusCode.TooManyRequests)
                throw new ClimaIndisponivelException("Limite do provedor de clima atingido. Tente mais tarde.");

            if (!resposta.IsSuccessStatusCode)
                throw new ClimaIndisponivelException("Provedor de clima retornou erro.");

            OpenMeteoForecastDto? dto;
            try
            {
                dto = await resposta.Content.ReadFromJsonAsync<OpenMeteoForecastDto>(JsonOptions, cancellationToken);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Falha ao interpretar JSON do Open-Meteo.");
                throw new ClimaIndisponivelException("Resposta de clima inválida.");
            }

            if (dto is null || dto.Error || dto.Current is null)
            {
                _logger.LogWarning("Open-Meteo sem current. Reason={Reason}", dto?.Reason);
                throw new ClimaIndisponivelException("Resposta de clima incompleta.");
            }

            return Normalizar(dto);
        }

        private async Task<ClimaLocalidade?> ConsultarNominatimAsync(
            string latStr,
            string lngStr,
            CancellationToken cancellationToken)
        {
            var url =
                $"{_settings.GeocodingBaseUrl.TrimEnd('/')}?" +
                $"lat={latStr}&lon={lngStr}&format=json&addressdetails=1&accept-language=pt-BR&zoom=10";

            try
            {
                using var resposta = await _http.GetAsync(url, cancellationToken);
                if (!resposta.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Nominatim HTTP {Status}", (int)resposta.StatusCode);
                    return null;
                }

                var dto = await resposta.Content.ReadFromJsonAsync<NominatimReverseDto>(JsonOptions, cancellationToken);
                var address = dto?.Address;
                if (address is null)
                    return null;

                var cidade = PrimeiroNaoVazio(address.City, address.Town, address.Village, address.Municipality, address.Hamlet);
                var estado = ExtrairUf(address) ?? ClimaWmo.UfBrasil(address.State);
                var pais = string.IsNullOrWhiteSpace(address.CountryCode)
                    ? address.Country
                    : address.CountryCode.ToUpperInvariant();

                return new ClimaLocalidade
                {
                    Cidade = cidade,
                    Estado = estado,
                    Pais = pais
                };
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Timeout no Nominatim.");
                return null;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Rede no Nominatim.");
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "JSON inválido no Nominatim.");
                return null;
            }
        }

        private ClimaResposta Normalizar(OpenMeteoForecastDto dto)
        {
            var current = dto.Current!;
            var codigo = current.WeatherCode;
            var (descricao, icone) = ClimaWmo.Interpretar(codigo);
            var probabilidade = ProbabilidadeAtual(dto, current.Time);

            var horas = Math.Clamp(_settings.HorasPrevisao, 1, 48);
            var dias = Math.Clamp(_settings.DiasPrevisao, 1, 16);

            return new ClimaResposta
            {
                Provedor = Nome,
                ObtidoEm = DateTime.UtcNow,
                DeCache = false,
                Localidade = new ClimaLocalidade
                {
                    Latitude = ToDec(dto.Latitude) ?? 0,
                    Longitude = ToDec(dto.Longitude) ?? 0
                },
                Atual = new ClimaAtual
                {
                    Temperatura = ToDec(current.Temperature2m),
                    SensacaoTermica = ToDec(current.ApparentTemperature),
                    Umidade = current.RelativeHumidity2m,
                    VelocidadeVento = ToDec(current.WindSpeed10m),
                    Precipitacao = ToDec(current.Precipitation),
                    ProbabilidadePrecipitacao = probabilidade,
                    CodigoClima = codigo,
                    Descricao = descricao,
                    Icone = icone,
                    Momento = ParseTime(current.Time)
                },
                Horaria = MapHoraria(dto.Hourly, current.Time, horas),
                Diaria = MapDiaria(dto.Daily, dias)
            };
        }

        private static List<ClimaPrevisao> MapHoraria(OpenMeteoHourlyDto? hourly, string? currentTime, int max)
        {
            if (hourly?.Time is null || hourly.Time.Count == 0)
                return new List<ClimaPrevisao>();

            var inicio = 0;
            if (!string.IsNullOrWhiteSpace(currentTime))
            {
                var idx = hourly.Time.FindIndex(t =>
                    string.Compare(t, currentTime, StringComparison.Ordinal) >= 0);
                if (idx >= 0)
                    inicio = idx;
            }

            var n = Math.Min(max, hourly.Time.Count - inicio);
            var lista = new List<ClimaPrevisao>(n);
            for (var i = 0; i < n; i++)
            {
                var pos = inicio + i;
                var codigo = GetAt(hourly.WeatherCode, pos);
                var (descricao, icone) = ClimaWmo.Interpretar(codigo);
                lista.Add(new ClimaPrevisao
                {
                    DataHora = ParseTime(hourly.Time[pos]) ?? DateTime.UtcNow,
                    Temperatura = ToDec(GetAt(hourly.Temperature2m, pos)),
                    SensacaoTermica = ToDec(GetAt(hourly.ApparentTemperature, pos)),
                    Precipitacao = ToDec(GetAt(hourly.Precipitation, pos)),
                    ProbabilidadePrecipitacao = GetAt(hourly.PrecipitationProbability, pos),
                    CodigoClima = codigo,
                    Descricao = descricao,
                    Icone = icone
                });
            }

            return lista;
        }

        private static List<ClimaPrevisao> MapDiaria(OpenMeteoDailyDto? daily, int max)
        {
            if (daily?.Time is null || daily.Time.Count == 0)
                return new List<ClimaPrevisao>();

            var n = Math.Min(max, daily.Time.Count);
            var lista = new List<ClimaPrevisao>(n);
            for (var i = 0; i < n; i++)
            {
                var codigo = GetAt(daily.WeatherCode, i);
                var (descricao, icone) = ClimaWmo.Interpretar(codigo);
                var maxTemp = ToDec(GetAt(daily.Temperature2mMax, i));
                lista.Add(new ClimaPrevisao
                {
                    DataHora = ParseTime(daily.Time[i]) ?? DateTime.UtcNow,
                    Temperatura = maxTemp,
                    TemperaturaMinima = ToDec(GetAt(daily.Temperature2mMin, i)),
                    TemperaturaMaxima = maxTemp,
                    SensacaoTermica = ToDec(GetAt(daily.ApparentTemperatureMax, i)),
                    Precipitacao = ToDec(GetAt(daily.PrecipitationSum, i)),
                    ProbabilidadePrecipitacao = GetAt(daily.PrecipitationProbabilityMax, i),
                    CodigoClima = codigo,
                    Descricao = descricao,
                    Icone = icone
                });
            }

            return lista;
        }

        private static int? ProbabilidadeAtual(OpenMeteoForecastDto dto, string? currentTime)
        {
            var hourly = dto.Hourly;
            if (hourly?.Time is null || hourly.PrecipitationProbability is null)
                return null;

            if (!string.IsNullOrWhiteSpace(currentTime))
            {
                var idx = hourly.Time.FindIndex(t =>
                    string.Compare(t, currentTime, StringComparison.Ordinal) >= 0);
                if (idx >= 0)
                    return GetAt(hourly.PrecipitationProbability, idx);
            }

            return GetAt(hourly.PrecipitationProbability, 0);
        }

        private static string? ExtrairUf(NominatimAddressDto address)
        {
            var iso = address.Iso3166;
            if (!string.IsNullOrWhiteSpace(iso))
            {
                var partes = iso.Split('-', 2);
                if (partes.Length == 2 && partes[1].Length == 2)
                    return partes[1].ToUpperInvariant();
            }

            return null;
        }

        private static string? PrimeiroNaoVazio(params string?[] valores)
        {
            foreach (var v in valores)
            {
                if (!string.IsNullOrWhiteSpace(v))
                    return v.Trim();
            }

            return null;
        }

        private static T? GetAt<T>(List<T>? lista, int i)
        {
            if (lista is null || i < 0 || i >= lista.Count)
                return default;
            return lista[i];
        }

        private static decimal? ToDec(double? valor) =>
            valor.HasValue ? Math.Round((decimal)valor.Value, 1, MidpointRounding.AwayFromZero) : null;

        private static DateTime? ParseTime(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return null;
            if (DateTime.TryParse(valor, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
                return dt;
            return null;
        }

        private sealed class OpenMeteoForecastDto
        {
            public double? Latitude { get; set; }
            public double? Longitude { get; set; }
            public bool Error { get; set; }
            public string? Reason { get; set; }
            public OpenMeteoCurrentDto? Current { get; set; }
            public OpenMeteoHourlyDto? Hourly { get; set; }
            public OpenMeteoDailyDto? Daily { get; set; }
        }

        private sealed class OpenMeteoCurrentDto
        {
            public string? Time { get; set; }

            [JsonPropertyName("temperature_2m")]
            public double? Temperature2m { get; set; }

            [JsonPropertyName("apparent_temperature")]
            public double? ApparentTemperature { get; set; }

            [JsonPropertyName("relative_humidity_2m")]
            public int? RelativeHumidity2m { get; set; }

            public double? Precipitation { get; set; }

            [JsonPropertyName("weather_code")]
            public int? WeatherCode { get; set; }

            [JsonPropertyName("wind_speed_10m")]
            public double? WindSpeed10m { get; set; }
        }

        private sealed class OpenMeteoHourlyDto
        {
            public List<string>? Time { get; set; }

            [JsonPropertyName("temperature_2m")]
            public List<double>? Temperature2m { get; set; }

            [JsonPropertyName("apparent_temperature")]
            public List<double>? ApparentTemperature { get; set; }

            public List<double>? Precipitation { get; set; }

            [JsonPropertyName("precipitation_probability")]
            public List<int>? PrecipitationProbability { get; set; }

            [JsonPropertyName("weather_code")]
            public List<int>? WeatherCode { get; set; }
        }

        private sealed class OpenMeteoDailyDto
        {
            public List<string>? Time { get; set; }

            [JsonPropertyName("weather_code")]
            public List<int>? WeatherCode { get; set; }

            [JsonPropertyName("temperature_2m_max")]
            public List<double>? Temperature2mMax { get; set; }

            [JsonPropertyName("temperature_2m_min")]
            public List<double>? Temperature2mMin { get; set; }

            [JsonPropertyName("apparent_temperature_max")]
            public List<double>? ApparentTemperatureMax { get; set; }

            [JsonPropertyName("precipitation_sum")]
            public List<double>? PrecipitationSum { get; set; }

            [JsonPropertyName("precipitation_probability_max")]
            public List<int>? PrecipitationProbabilityMax { get; set; }
        }

        private sealed class NominatimReverseDto
        {
            public NominatimAddressDto? Address { get; set; }
        }

        private sealed class NominatimAddressDto
        {
            public string? City { get; set; }
            public string? Town { get; set; }
            public string? Village { get; set; }
            public string? Municipality { get; set; }
            public string? Hamlet { get; set; }
            public string? State { get; set; }
            public string? Country { get; set; }

            [JsonPropertyName("country_code")]
            public string? CountryCode { get; set; }

            [JsonPropertyName("ISO3166-2-lvl4")]
            public string? Iso3166 { get; set; }
        }
    }
}
