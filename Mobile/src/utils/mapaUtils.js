const DELTA_PADRAO = 0.05;
const DELTA_MIN = 0.02;

export function temCoordenadasValidas(produto) {
  const lat = Number(produto?.latitude);
  const lng = Number(produto?.longitude);
  if (!Number.isFinite(lat) || !Number.isFinite(lng)) return false;
  if (lat === 0 && lng === 0) return false;
  return true;
}

export function filtrarProdutosComCoordenadas(produtos) {
  return (produtos || []).filter(temCoordenadasValidas);
}

export function contarProdutosSemCoordenadas(produtos) {
  return (produtos || []).filter((p) => !temCoordenadasValidas(p)).length;
}

export function formatarPrecoBrl(valor) {
  return Number(valor).toLocaleString('pt-BR', {
    style: 'currency',
    currency: 'BRL',
  });
}

/**
 * Calcula região do mapa incluindo cliente e pins dos produtos.
 */
export function calcularRegiaoMapa(localizacaoCliente, produtosComCoordenadas) {
  const pontos = [];

  if (
    localizacaoCliente?.latitude != null &&
    localizacaoCliente?.longitude != null
  ) {
    pontos.push({
      latitude: Number(localizacaoCliente.latitude),
      longitude: Number(localizacaoCliente.longitude),
    });
  }

  produtosComCoordenadas.forEach((p) => {
    pontos.push({
      latitude: Number(p.latitude),
      longitude: Number(p.longitude),
    });
  });

  if (pontos.length === 0) {
    return {
      latitude: -23.5505,
      longitude: -46.6333,
      latitudeDelta: 0.15,
      longitudeDelta: 0.15,
    };
  }

  if (pontos.length === 1) {
    return {
      latitude: pontos[0].latitude,
      longitude: pontos[0].longitude,
      latitudeDelta: DELTA_PADRAO,
      longitudeDelta: DELTA_PADRAO,
    };
  }

  let minLat = pontos[0].latitude;
  let maxLat = pontos[0].latitude;
  let minLng = pontos[0].longitude;
  let maxLng = pontos[0].longitude;

  pontos.forEach((p) => {
    minLat = Math.min(minLat, p.latitude);
    maxLat = Math.max(maxLat, p.latitude);
    minLng = Math.min(minLng, p.longitude);
    maxLng = Math.max(maxLng, p.longitude);
  });

  const latitude = (minLat + maxLat) / 2;
  const longitude = (minLng + maxLng) / 2;
  const latitudeDelta = Math.max(maxLat - minLat + DELTA_MIN, DELTA_PADRAO);
  const longitudeDelta = Math.max(maxLng - minLng + DELTA_MIN, DELTA_PADRAO);

  return { latitude, longitude, latitudeDelta, longitudeDelta };
}

export function inicialPlaceholder(nome) {
  const letra = (nome || '?').trim().charAt(0).toUpperCase();
  return letra || '?';
}
