/**
 * Trava o mapa na cidade de Saquarema/RJ.
 * A câmera não sai do município e um ponto fora da cidade é puxado para a borda.
 */

export const SAQUAREMA = {
  nome: 'Saquarema',
  uf: 'RJ',
  center: { latitude: -22.9329, longitude: -42.5096 },
  bounds: {
    north: -22.835,
    south: -23.005,
    west: -42.655,
    east: -42.405,
  },
};

export const MIN_ZOOM_SAQUAREMA = 10;
export const MAX_ZOOM_SAQUAREMA = 18;

export const LIMITES_NORDESTE_SAQUAREMA = {
  latitude: SAQUAREMA.bounds.north,
  longitude: SAQUAREMA.bounds.east,
};

export const LIMITES_SUDOESTE_SAQUAREMA = {
  latitude: SAQUAREMA.bounds.south,
  longitude: SAQUAREMA.bounds.west,
};

function clamp(valor, min, max) {
  return Math.min(max, Math.max(min, valor));
}

export function regiaoInicialSaquarema() {
  const { north, south, west, east } = SAQUAREMA.bounds;
  return {
    latitude: (north + south) / 2,
    longitude: (west + east) / 2,
    latitudeDelta: (north - south) * 0.92,
    longitudeDelta: (east - west) * 0.92,
  };
}

export function coordenadaDentroDeSaquarema(coordenada) {
  const lat = Number(coordenada?.latitude);
  const lng = Number(coordenada?.longitude);
  if (!Number.isFinite(lat) || !Number.isFinite(lng)) return false;
  const { north, south, west, east } = SAQUAREMA.bounds;
  return lat <= north && lat >= south && lng <= east && lng >= west;
}

/**
 * Garante que o ponto usado no mapa fique dentro de Saquarema.
 * Sem GPS válido, usa o centro da cidade.
 */
export function travarCoordenadaEmSaquarema(coordenada) {
  const lat = Number(coordenada?.latitude);
  const lng = Number(coordenada?.longitude);
  if (!Number.isFinite(lat) || !Number.isFinite(lng) || (lat === 0 && lng === 0)) {
    return { ...SAQUAREMA.center };
  }
  const { north, south, west, east } = SAQUAREMA.bounds;
  return {
    latitude: clamp(lat, south, north),
    longitude: clamp(lng, west, east),
  };
}

/**
 * Impede pan e zoom de mostrarem fora do município.
 * Não amplia o delta: um zoom mais próximo, ainda dentro da cidade, permanece.
 */
export function travarRegiaoEmSaquarema(region) {
  const base = regiaoInicialSaquarema();
  if (!region || !Number.isFinite(Number(region.latitude)) || !Number.isFinite(Number(region.longitude))) {
    return base;
  }

  const { north, south, west, east } = SAQUAREMA.bounds;
  const maxLatDelta = (north - south) * 0.98;
  const maxLngDelta = (east - west) * 0.98;
  const latitudeDelta = clamp(Number(region.latitudeDelta) || base.latitudeDelta, 0.004, maxLatDelta);
  const longitudeDelta = clamp(Number(region.longitudeDelta) || base.longitudeDelta, 0.004, maxLngDelta);

  const halfLat = latitudeDelta / 2;
  const halfLng = longitudeDelta / 2;
  const minLat = south + halfLat;
  const maxLat = north - halfLat;
  const minLng = west + halfLng;
  const maxLng = east - halfLng;

  const latitude = minLat <= maxLat
    ? clamp(Number(region.latitude), minLat, maxLat)
    : (north + south) / 2;
  const longitude = minLng <= maxLng
    ? clamp(Number(region.longitude), minLng, maxLng)
    : (east + west) / 2;

  return { latitude, longitude, latitudeDelta, longitudeDelta };
}

/**
 * True quando o centro saiu de Saquarema ou o zoom afastou além do município.
 * O delta de latitude do viewport alto (tela em pé) não conta: o zoom do mapa
 * segue a longitude, e tratar a altura da tela como fuga faz a câmera oscilar.
 */
export function regiaoEscapou(atual, travada) {
  if (!atual || !travada) return true;
  const centro =
    Math.abs(Number(atual.latitude) - travada.latitude) > 0.0015 ||
    Math.abs(Number(atual.longitude) - travada.longitude) > 0.0015;
  const afastou = Number(atual.longitudeDelta) > Number(travada.longitudeDelta) + 0.01;
  return centro || afastou;
}

export function regiaoParaPontos(pontos) {
  const validos = (pontos || []).filter(
    (p) => Number.isFinite(Number(p?.latitude)) && Number.isFinite(Number(p?.longitude))
  );
  if (validos.length === 0) return regiaoInicialSaquarema();

  let minLat = Number(validos[0].latitude);
  let maxLat = minLat;
  let minLng = Number(validos[0].longitude);
  let maxLng = minLng;
  validos.forEach((p) => {
    const lat = Number(p.latitude);
    const lng = Number(p.longitude);
    minLat = Math.min(minLat, lat);
    maxLat = Math.max(maxLat, lat);
    minLng = Math.min(minLng, lng);
    maxLng = Math.max(maxLng, lng);
  });

  return travarRegiaoEmSaquarema({
    latitude: (minLat + maxLat) / 2,
    longitude: (minLng + maxLng) / 2,
    latitudeDelta: Math.max((maxLat - minLat) * 1.8, 0.02),
    longitudeDelta: Math.max((maxLng - minLng) * 1.8, 0.02),
  });
}

/** Zoom equivalente ao longitudeDelta, já limitado à cidade. */
export function zoomDeRegiao(region) {
  const delta = Math.max(Number(region?.longitudeDelta) || 0.2, 0.0001);
  const zoom = Math.log2(360 / delta);
  return Math.max(MIN_ZOOM_SAQUAREMA, Math.min(MAX_ZOOM_SAQUAREMA, zoom));
}
