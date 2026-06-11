import api from './api';
import { obterCache, obterCacheStale, salvarCache } from './feedCache';

const NS = 'feed';
const TTL = 60_000;

function normalizarFeedItem(item) {
  if (!item) return null;
  return {
    id: item.id,
    nome: item.nome ?? item.Nome ?? '',
    imagemUrl: item.imagemUrl ?? item.ImagemUrl ?? null,
    lojaId: item.lojaId ?? item.LojaId ?? null,
    lojaNome: item.lojaNome ?? item.LojaNome ?? null,
    precoBase: item.precoBase ?? item.PrecoBase ?? 0,
    preco: item.precoExibicao ?? item.PrecoExibicao ?? item.preco ?? 0,
    precoAnterior: item.precoAnterior ?? item.PrecoAnterior ?? null,
    emPromocao: item.emPromocao ?? item.EmPromocao ?? false,
    categoria: item.categoria ?? item.Categoria,
    categoriaNome: item.categoriaNome ?? item.CategoriaNome ?? '',
  };
}

function normalizarPaginado(data) {
  const items = Array.isArray(data?.items) ? data.items.map(normalizarFeedItem).filter(Boolean) : [];
  return {
    items,
    page: data?.page ?? 1,
    pageSize: data?.pageSize ?? 20,
    total: data?.total ?? items.length,
    hasNext: data?.hasNext ?? false,
  };
}

export async function listarFeed({ page = 1, pageSize = 20, termo = '', categoria = null, lojaId = null, force = false } = {}) {
  const cacheKey = { page, pageSize, termo: termo.trim(), categoria, lojaId };
  if (!force) {
    const cached = obterCache(NS, cacheKey, TTL);
    if (cached) return cached;
  }

  const params = { page, pageSize };
  if (termo?.trim()) params.termo = termo.trim();
  if (categoria != null) params.categoria = categoria;
  if (lojaId) params.lojaId = lojaId;

  const { data } = await api.get('/Feed', { params });
  const resultado = normalizarPaginado(data);
  salvarCache(NS, cacheKey, resultado);
  return resultado;
}

export async function listarFeedComStale({ page = 1, pageSize = 20, termo = '', categoria = null, lojaId = null } = {}) {
  const cacheKey = { page, pageSize, termo: termo.trim(), categoria, lojaId };
  const stale = obterCacheStale(NS, cacheKey);
  const fresh = obterCache(NS, cacheKey, TTL);
  if (fresh) return { data: fresh, fromCache: true };
  if (stale) {
    listarFeed({ page, pageSize, termo, categoria, lojaId, force: true }).catch(() => {});
    return { data: stale, fromCache: true, stale: true };
  }
  const data = await listarFeed({ page, pageSize, termo, categoria, lojaId, force: true });
  return { data, fromCache: false };
}

export { invalidarCache as invalidarFeedCache } from './feedCache';
