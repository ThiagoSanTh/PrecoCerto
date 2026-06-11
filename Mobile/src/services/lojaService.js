import api from './api';
import { obterCache, salvarCache, invalidarCache } from './feedCache';

const NS = 'lojasMapa';
const TTL = 60_000;

function normalizarPaginado(data) {
  if (Array.isArray(data)) {
    return { items: data, page: 1, pageSize: data.length, total: data.length, hasNext: false };
  }
  return {
    items: Array.isArray(data?.items) ? data.items : [],
    page: data?.page ?? 1,
    pageSize: data?.pageSize ?? 20,
    total: data?.total ?? 0,
    hasNext: data?.hasNext ?? false,
  };
}

export async function listarLojas({ page = 1, pageSize = 100 } = {}) {
  const { data } = await api.get('/Lojas', { params: { page, pageSize } });
  return normalizarPaginado(data);
}

export async function listarLojasMapa(latitude, longitude, raioKm = 15) {
  const cacheKey = { latitude, longitude, raioKm };
  const cached = obterCache(NS, cacheKey, TTL);
  if (cached) return cached;

  const { data } = await api.get('/Lojas/mapa', {
    params: { latitude, longitude, raioKm },
  });
  const lista = Array.isArray(data) ? data : [];
  salvarCache(NS, cacheKey, lista);
  return lista;
}

export function invalidarLojasCache() {
  invalidarCache(NS);
}

export async function obterLoja(id) {
  const { data } = await api.get(`/Lojas/${id}`);
  return data;
}

export async function criarLoja(loja) {
  const { data } = await api.post('/Lojas', loja);
  invalidarLojasCache();
  return data;
}

export async function atualizarLoja(id, loja) {
  await api.put(`/Lojas/${id}`, loja);
  invalidarLojasCache();
}

export async function buscarLojasPorNome(nome, page = 1, pageSize = 20) {
  const { data } = await api.post('/Lojas/buscar', JSON.stringify(nome), {
    params: { page, pageSize },
  });
  return normalizarPaginado(data);
}
