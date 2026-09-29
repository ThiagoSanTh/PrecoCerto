import { regiaoInicialSaquarema, travarRegiaoEmSaquarema } from './saquaremaLock.js';

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
    return regiaoInicialSaquarema();
  }

  if (pontos.length === 1) {
    return travarRegiaoEmSaquarema({
      latitude: pontos[0].latitude,
      longitude: pontos[0].longitude,
      latitudeDelta: DELTA_PADRAO,
      longitudeDelta: DELTA_PADRAO,
    });
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

  return travarRegiaoEmSaquarema({ latitude, longitude, latitudeDelta, longitudeDelta });
}

export function inicialPlaceholder(nome) {
  const letra = (nome || '?').trim().charAt(0).toUpperCase();
  return letra || '?';
}

/** Normaliza loja da API para o marcador do mapa. */
export function normalizarLojaParaMapa(loja) {
  const lat =
    loja?.latitude ??
    loja?.Latitude ??
    loja?.endereco?.latitude ??
    loja?.Endereco?.Latitude;
  const lng =
    loja?.longitude ??
    loja?.Longitude ??
    loja?.endereco?.longitude ??
    loja?.Endereco?.Longitude;
  const nome = loja?.nomeFantasia ?? loja?.NomeFantasia ?? 'Loja';
  const logradouro = loja?.endereco?.logradouro ?? loja?.Endereco?.Logradouro ?? '';
  const cidade = loja?.endereco?.cidade ?? loja?.Endereco?.Cidade ?? '';

  return {
    id: loja?.id ?? loja?.Id,
    lat: lat != null && lat !== '' ? Number(lat) : null,
    lng: lng != null && lng !== '' ? Number(lng) : null,
    nome,
    endereco: [logradouro, cidade].filter(Boolean).join(' — '),
    media: loja?.mediaAvaliacoes ?? loja?.MediaAvaliacoes ?? null,
    inicial: inicialPlaceholder(nome),
  };
}
