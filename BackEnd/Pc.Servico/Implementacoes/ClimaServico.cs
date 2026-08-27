using System.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pc.Servico.Excecoes;
using Pc.Servico.Interfaces;
using Pc.Servico.Modelos;

namespace Pc.Servico.Implementacoes
{
    public class ClimaServico : IClimaServico
    {
        private readonly IClimaProvedor _provedor;
        private readonly IMemoryCache _cache;
        private readonly ClimaSettings _settings;
        private readonly ILogger<ClimaServico> _logger;

        public ClimaServico(
            IClimaProvedor provedor,
            IMemoryCache cache,
            IOptions<ClimaSettings> settings,
            ILogger<ClimaServico> logger)
        {
            _provedor = provedor;
            _cache = cache;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<ClimaResposta> ObterPorCoordenadasAsync(
            decimal latitude,
            decimal longitude,
            CancellationToken cancellationToken = default)
        {
            if (!CoordenadasValidas(latitude, longitude))
                throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude ou longitude inválida.");

            var precisao = Math.Clamp(_settings.PrecisaoCoordenadas, 1, 4);
            var (lat, lng) = Normalizar(latitude, longitude, precisao);
            var chave = ChaveCache(lat, lng);

            if (_cache.TryGetValue(chave, out ClimaCacheEntry? entrada) && entrada is not null)
            {
                var fresco = DateTime.UtcNow <= entrada.FrescoAteUtc;
                if (fresco)
                {
                    _logger.LogInformation("Clima cache hit lat={Lat} lng={Lng}", lat, lng);
                    return Copiar(entrada.Dados, deCache: true, desatualizado: false);
                }
            }

            _logger.LogInformation("Clima cache miss lat={Lat} lng={Lng}", lat, lng);

            var sw = Stopwatch.StartNew();
            try
            {
                var clima = await _provedor.ConsultarAsync(lat, lng, cancellationToken);
                sw.Stop();
                _logger.LogInformation(
                    "Clima provider {Provedor} ok em {Ms}ms lat={Lat} lng={Lng}",
                    _provedor.Nome,
                    sw.ElapsedMilliseconds,
                    lat,
                    lng);

                clima.Localidade.Latitude = lat;
                clima.Localidade.Longitude = lng;
                clima.DeCache = false;
                clima.Desatualizado = false;
                clima.ObtidoEm = DateTime.UtcNow;

                Cachear(chave, clima);
                return clima;
            }
            catch (ClimaIndisponivelException ex)
            {
                sw.Stop();
                _logger.LogWarning(
                    ex,
                    "Clima provider falhou em {Ms}ms lat={Lat} lng={Lng}",
                    sw.ElapsedMilliseconds,
                    lat,
                    lng);

                if (entrada is not null)
                {
                    _logger.LogInformation("Clima fallback cache stale lat={Lat} lng={Lng}", lat, lng);
                    return Copiar(entrada.Dados, deCache: true, desatualizado: true);
                }

                throw;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                if (entrada is not null)
                    return Copiar(entrada.Dados, deCache: true, desatualizado: true);
                throw new ClimaIndisponivelException("Consulta de clima excedeu o tempo limite.");
            }
        }

        public static bool CoordenadasValidas(decimal latitude, decimal longitude) =>
            latitude is >= -90 and <= 90 && longitude is >= -180 and <= 180;

        public static (decimal Lat, decimal Lng) Normalizar(decimal latitude, decimal longitude, int precisao)
        {
            return (
                Math.Round(latitude, precisao, MidpointRounding.AwayFromZero),
                Math.Round(longitude, precisao, MidpointRounding.AwayFromZero));
        }

        public static string ChaveCache(decimal lat, decimal lng) =>
            $"clima:{lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}:{lng.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

        private void Cachear(string chave, ClimaResposta clima)
        {
            var minutos = Math.Clamp(_settings.CacheMinutos, 1, 120);
            var staleHoras = Math.Clamp(_settings.CacheStaleHoras, 1, 24);
            var agora = DateTime.UtcNow;
            var entrada = new ClimaCacheEntry
            {
                Dados = clima,
                FrescoAteUtc = agora.AddMinutes(minutos)
            };

            _cache.Set(chave, entrada, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(staleHoras)
            });
        }

        private static ClimaResposta Copiar(ClimaResposta origem, bool deCache, bool desatualizado) =>
            new()
            {
                Localidade = origem.Localidade,
                Atual = origem.Atual,
                Horaria = origem.Horaria,
                Diaria = origem.Diaria,
                ObtidoEm = origem.ObtidoEm,
                DeCache = deCache,
                Desatualizado = desatualizado,
                Provedor = origem.Provedor
            };
    }

    internal sealed class ClimaCacheEntry
    {
        public required ClimaResposta Dados { get; init; }
        public DateTime FrescoAteUtc { get; init; }
    }
}
