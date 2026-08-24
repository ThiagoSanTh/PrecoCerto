import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE = __ENV.BASE_URL || 'http://127.0.0.1:5132';
const LOJA_ID = __ENV.LOJA_ID || '';

export const params = { timeout: '10s', tags: {} };

export function url(path) {
  return `${BASE}${path}`;
}

export function get(path, tags) {
  return http.get(url(path), { timeout: '10s', tags });
}

export function feed() {
  return get('/api/Feed?page=1&pageSize=20', { endpoint: 'feed' });
}

export function feedSearch() {
  return get('/api/Feed?page=1&pageSize=20&termo=arroz', { endpoint: 'feed_search' });
}

export function lojas() {
  return get('/api/Lojas?page=1&pageSize=20', { endpoint: 'lojas' });
}

export function mapa() {
  return get('/api/Lojas/mapa?latitude=-23.5505&longitude=-46.6333&raioKm=15', { endpoint: 'mapa' });
}

export function sugestoes() {
  return get('/api/HistoricoPesquisa/sugestoes?termo=ar', { endpoint: 'sugestoes' });
}

export function health() {
  return get('/api/health', { endpoint: 'health' });
}

export function ready() {
  return get('/api/health/ready', { endpoint: 'ready' });
}

export function avaliacoes() {
  if (!LOJA_ID) return health();
  return get(`/api/Avaliacoes/loja/${LOJA_ID}`, { endpoint: 'avaliacoes' });
}

export function media() {
  if (!LOJA_ID) return health();
  return get(`/api/Avaliacoes/loja/${LOJA_ID}/media`, { endpoint: 'media' });
}

export function lojaDetalhe() {
  if (!LOJA_ID) return lojas();
  return get(`/api/Lojas/${LOJA_ID}`, { endpoint: 'loja_id' });
}

export function ok2xx(res) {
  check(res, { 'status 2xx': (r) => r.status >= 200 && r.status < 300 });
}

export function think() {
  sleep(0.05);
}
