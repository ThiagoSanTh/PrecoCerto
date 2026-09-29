import { travarCoordenadaEmSaquarema } from './saquaremaLock.js';

export function distanciaKm(origem, destino) {
  const lat1 = Number(origem?.latitude);
  const lng1 = Number(origem?.longitude);
  const lat2 = Number(destino?.latitude);
  const lng2 = Number(destino?.longitude);
  if (![lat1, lng1, lat2, lng2].every(Number.isFinite)) return Infinity;

  const R = 6371;
  const dLat = ((lat2 - lat1) * Math.PI) / 180;
  const dLng = ((lng2 - lng1) * Math.PI) / 180;
  const a =
    Math.sin(dLat / 2) ** 2 +
    Math.cos((lat1 * Math.PI) / 180) *
      Math.cos((lat2 * Math.PI) / 180) *
      Math.sin(dLng / 2) ** 2;
  return 2 * R * Math.asin(Math.min(1, Math.sqrt(a)));
}

export function formatarDistanciaKm(km) {
  if (!Number.isFinite(km)) return '';
  if (km < 1) return `${Math.round(km * 1000)} m`;
  return `${km.toFixed(1).replace('.', ',')} km`;
}

/**
 * Loja de destino da busca: a escolhida no pin, ou a mais próxima entre os resultados.
 */
export function escolherLojaDestino(lojas, lojaIdsDestaque, cliente, lojaDestinoId) {
  const lista = Array.isArray(lojas) ? lojas : [];
  if (lojaDestinoId != null && lojaDestinoId !== '') {
    const manual = lista.find((loja) => String(loja.id) === String(lojaDestinoId));
    if (manual) return manual;
  }
  if (!Array.isArray(lojaIdsDestaque) || !cliente) return null;

  const ids = new Set(lojaIdsDestaque.map(String));
  const candidatas = lista.filter((loja) => ids.has(String(loja.id)));
  if (candidatas.length === 0) return null;

  return candidatas.reduce((melhor, loja) => {
    const distancia = distanciaKm(cliente, { latitude: loja.lat, longitude: loja.lng });
    if (!melhor || distancia < melhor.distancia) return { loja, distancia };
    return melhor;
  }, null).loja;
}

export function montarUrlRotaOsrm(origem, destino) {
  return `https://router.project-osrm.org/route/v1/driving/${origem.longitude},${origem.latitude};${destino.longitude},${destino.latitude}?overview=full&geometries=geojson`;
}

export function coordenadasDaRotaOsrm(json) {
  const coords = json?.routes?.[0]?.geometry?.coordinates;
  if (!Array.isArray(coords) || coords.length < 2) return null;
  const pontos = coords
    .map(([lng, lat]) => ({ latitude: Number(lat), longitude: Number(lng) }))
    .filter((p) => Number.isFinite(p.latitude) && Number.isFinite(p.longitude));
  return pontos.length >= 2 ? pontos : null;
}

/**
 * Rota de carro entre o ponto do cliente e a loja.
 * Se o serviço de rota falhar, devolve a linha reta entre os dois pontos.
 */
export async function buscarRota(origem, destino, fetchImpl = globalThis.fetch) {
  const inicio = travarCoordenadaEmSaquarema(origem);
  const fim = travarCoordenadaEmSaquarema(destino);
  const reta = [inicio, fim];
  if (typeof fetchImpl !== 'function') return reta;

  const controller = typeof AbortController !== 'undefined' ? new AbortController() : null;
  const timer = controller ? setTimeout(() => controller.abort(), 6000) : null;
  try {
    const resposta = await fetchImpl(montarUrlRotaOsrm(inicio, fim), {
      signal: controller?.signal,
    });
    if (!resposta?.ok) return reta;
    const json = await resposta.json();
    return coordenadasDaRotaOsrm(json) || reta;
  } catch {
    return reta;
  } finally {
    if (timer) clearTimeout(timer);
  }
}
