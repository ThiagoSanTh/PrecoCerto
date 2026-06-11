import api from './api';
import { getToken } from './tokenStorage';
import { obterCache, obterCacheStale, salvarCache } from './feedCache';

const CONVERSAS_TTL_MS = 30_000;

function hubBaseUrl() {
  const base = api.defaults.baseURL || '';
  return base.replace(/\/api\/?$/, '');
}

export async function listarConversas({ force = false } = {}) {
  if (!force) {
    const fresh = obterCache('conversas', {}, CONVERSAS_TTL_MS);
    if (fresh) return fresh;
  }

  const { data } = await api.get('/Conversas');
  salvarCache('conversas', {}, data);
  return data;
}

export async function listarConversasComStale() {
  const stale = obterCacheStale('conversas', {});
  const fresh = obterCache('conversas', {}, CONVERSAS_TTL_MS);
  if (fresh) return { data: fresh, fromCache: true, stale: false };
  if (stale) {
    listarConversas({ force: true }).catch(() => {});
    return { data: stale, fromCache: true, stale: true };
  }
  const data = await listarConversas({ force: true });
  return { data, fromCache: false, stale: false };
}

export async function contarNaoLidas() {
  const { data } = await api.get('/Conversas/nao-lidas');
  return data?.total ?? 0;
}

export async function abrirConversa(lojaCodigo) {
  const { data } = await api.post('/Conversas/abrir', null, {
    params: { lojaCodigo },
  });
  return data;
}

export async function listarMensagens(conversaCodigo, { apos, antes, pageSize = 50 } = {}) {
  const params = {};
  if (apos) params.apos = apos;
  if (antes) params.antes = antes;
  if (pageSize) params.pageSize = pageSize;
  const { data } = await api.get(`/Conversas/${conversaCodigo}/mensagens`, { params });
  return data;
}

export async function enviarMensagem(conversaCodigo, texto) {
  const { data } = await api.post(`/Conversas/${conversaCodigo}/mensagens`, { texto });
  return data;
}

export async function conectarChatHub(conversaCodigo, onMensagem) {
  try {
    const token = await getToken();
    if (!token) return null;

    const signalR = await import('@microsoft/signalr');
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${hubBaseUrl()}/hubs/chat`, {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect()
      .build();

    connection.on('ReceberMensagem', onMensagem);
    await connection.start();
    await connection.invoke('EntrarConversa', conversaCodigo);
    return connection;
  } catch {
    return null;
  }
}
