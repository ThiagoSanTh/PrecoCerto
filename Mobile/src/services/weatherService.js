import api from './api';
import { obterCache, obterCacheStale, salvarCache } from './feedCache';

const NS = 'clima';
const TTL_MS = 15 * 60 * 1000;

function arredondar(valor) {
  const n = Number(valor);
  if (!Number.isFinite(n)) return null;
  return Math.round(n * 100) / 100;
}

export function normalizarCoordenadasClima(latitude, longitude) {
  const lat = arredondar(latitude);
  const lng = arredondar(longitude);
  if (lat == null || lng == null) return null;
  if (lat < -90 || lat > 90 || lng < -180 || lng > 180) return null;
  return { latitude: lat, longitude: lng };
}

function normalizarPrevisao(item) {
  if (!item) return null;
  return {
    dataHora: item.dataHora ?? item.DataHora ?? null,
    temperatura: item.temperatura ?? item.Temperatura ?? null,
    temperaturaMinima: item.temperaturaMinima ?? item.TemperaturaMinima ?? null,
    temperaturaMaxima: item.temperaturaMaxima ?? item.TemperaturaMaxima ?? null,
    sensacaoTermica: item.sensacaoTermica ?? item.SensacaoTermica ?? null,
    precipitacao: item.precipitacao ?? item.Precipitacao ?? null,
    probabilidadePrecipitacao:
      item.probabilidadePrecipitacao ?? item.ProbabilidadePrecipitacao ?? null,
    codigoClima: item.codigoClima ?? item.CodigoClima ?? null,
    descricao: item.descricao ?? item.Descricao ?? '',
    icone: item.icone ?? item.Icone ?? 'cloudy',
  };
}

function normalizarClima(data) {
  if (!data) return null;
  const localidade = data.localidade ?? data.Localidade ?? {};
  const atual = data.atual ?? data.Atual ?? {};
  return {
    localidade: {
      latitude: localidade.latitude ?? localidade.Latitude ?? null,
      longitude: localidade.longitude ?? localidade.Longitude ?? null,
      cidade: localidade.cidade ?? localidade.Cidade ?? null,
      estado: localidade.estado ?? localidade.Estado ?? null,
      pais: localidade.pais ?? localidade.Pais ?? null,
    },
    atual: {
      temperatura: atual.temperatura ?? atual.Temperatura ?? null,
      sensacaoTermica: atual.sensacaoTermica ?? atual.SensacaoTermica ?? null,
      umidade: atual.umidade ?? atual.Umidade ?? null,
      velocidadeVento: atual.velocidadeVento ?? atual.VelocidadeVento ?? null,
      precipitacao: atual.precipitacao ?? atual.Precipitacao ?? null,
      probabilidadePrecipitacao:
        atual.probabilidadePrecipitacao ?? atual.ProbabilidadePrecipitacao ?? null,
      codigoClima: atual.codigoClima ?? atual.CodigoClima ?? null,
      descricao: atual.descricao ?? atual.Descricao ?? '',
      icone: atual.icone ?? atual.Icone ?? 'cloudy',
      momento: atual.momento ?? atual.Momento ?? null,
    },
    horaria: Array.isArray(data.horaria ?? data.Horaria)
      ? (data.horaria ?? data.Horaria).map(normalizarPrevisao).filter(Boolean)
      : [],
    diaria: Array.isArray(data.diaria ?? data.Diaria)
      ? (data.diaria ?? data.Diaria).map(normalizarPrevisao).filter(Boolean)
      : [],
    obtidoEm: data.obtidoEm ?? data.ObtidoEm ?? null,
    deCache: Boolean(data.deCache ?? data.DeCache),
    desatualizado: Boolean(data.desatualizado ?? data.Desatualizado),
    provedor: data.provedor ?? data.Provedor ?? 'open-meteo',
  };
}

/**
 * Consome GET /api/Weather. Nunca chama o provedor meteorológico direto.
 */
export async function obterClima({ latitude, longitude, force = false } = {}) {
  const coords = normalizarCoordenadasClima(latitude, longitude);
  if (!coords) {
    return { status: 'sem-localizacao', data: null };
  }

  const cacheKey = coords;
  if (!force) {
    const cached = obterCache(NS, cacheKey, TTL_MS);
    if (cached) return { status: 'ok', data: cached, fromCache: true };
  }

  try {
    const { data } = await api.get('/Weather', {
      params: { latitude: coords.latitude, longitude: coords.longitude },
    });
    const normalizado = normalizarClima(data);
    if (!normalizado) return { status: 'erro', data: null };
    salvarCache(NS, cacheKey, normalizado);
    return { status: 'ok', data: normalizado, fromCache: false };
  } catch (error) {
    const stale = obterCacheStale(NS, cacheKey);
    if (stale) {
      return { status: 'ok', data: { ...stale, desatualizado: true }, fromCache: true, stale: true };
    }
    const offline = !error.response;
    return { status: offline ? 'offline' : 'erro', data: null };
  }
}
