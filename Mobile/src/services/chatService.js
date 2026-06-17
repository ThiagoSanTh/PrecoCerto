import api from './api';
import { getToken } from './tokenStorage';
import { obterCache, obterCacheStale, salvarCache } from './feedCache';

const CONVERSAS_TTL_MS = 30_000;
const HUB_RETRY_BACKOFF_MS = 30_000;
const CHAT_POLL_FALLBACK_MS = 15_000;

let revalidandoConversas = false;
let hubConnection = null;
let hubStarting = null;
let ultimaFalhaHubEm = 0;

const badgeHandlers = {
  onNovaMensagem: null,
  onConnected: null,
  onDisconnected: null,
};

let onReceberMensagemAtivo = null;
let conversaAtivaCodigo = null;

function hubBaseUrl() {
  const base = api.defaults.baseURL || '';
  return base.replace(/\/api\/?$/, '');
}

function revalidarConversasEmBackground() {
  if (revalidandoConversas) return;
  revalidandoConversas = true;
  listarConversas({ force: true })
    .catch(() => {})
    .finally(() => {
      revalidandoConversas = false;
    });
}

async function criarHubConnection(signalR, token) {
  const connection = new signalR.HubConnectionBuilder()
    .withUrl(`${hubBaseUrl()}/hubs/chat`, {
      accessTokenFactory: () => token,
    })
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Warning)
    .build();

  connection.on('NovaMensagemNaoLida', (payload) => {
    badgeHandlers.onNovaMensagem?.(payload);
  });

  connection.on('ReceberMensagem', (msg) => {
    onReceberMensagemAtivo?.(msg);
  });

  connection.onreconnected(() => {
    connection.invoke('ConectarUsuario').catch(() => {});
    if (conversaAtivaCodigo) {
      connection.invoke('EntrarConversa', conversaAtivaCodigo).catch(() => {});
    }
    badgeHandlers.onConnected?.({ isReconnect: true });
  });

  connection.onclose(() => {
    badgeHandlers.onDisconnected?.();
  });

  return connection;
}

async function obterHubCompartilhado() {
  const token = await getToken();
  if (!token) return null;

  if (hubConnection?.state === 'Connected' || hubConnection?.state === 'Reconnecting') {
    return hubConnection;
  }

  const agora = Date.now();
  if (ultimaFalhaHubEm && agora - ultimaFalhaHubEm < HUB_RETRY_BACKOFF_MS) {
    return null;
  }

  if (hubStarting) return hubStarting;

  hubStarting = (async () => {
    try {
      const signalR = await import('@microsoft/signalr');

      if (!hubConnection || hubConnection.state === 'Disconnected') {
        if (hubConnection) {
          await hubConnection.stop().catch(() => {});
        }
        hubConnection = await criarHubConnection(signalR, token);
        await hubConnection.start();
      }

      ultimaFalhaHubEm = 0;
      return hubConnection;
    } catch {
      ultimaFalhaHubEm = Date.now();
      if (hubConnection) {
        await hubConnection.stop().catch(() => {});
        hubConnection = null;
      }
      return null;
    } finally {
      hubStarting = null;
    }
  })();

  return hubStarting;
}

export function hubEstaConectado() {
  return hubConnection?.state === 'Connected';
}

export async function garantirHubBadge({ onNovaMensagem, onConnected, onDisconnected }) {
  badgeHandlers.onNovaMensagem = onNovaMensagem;
  badgeHandlers.onConnected = onConnected;
  badgeHandlers.onDisconnected = onDisconnected;

  const conn = await obterHubCompartilhado();
  if (!conn) {
    onDisconnected?.();
    return false;
  }

  try {
    await conn.invoke('ConectarUsuario');
    onConnected?.({ isReconnect: false });
    return true;
  } catch {
    onDisconnected?.();
    return false;
  }
}

export async function entrarConversaHub(conversaCodigo, onMensagem) {
  conversaAtivaCodigo = conversaCodigo;
  onReceberMensagemAtivo = onMensagem;

  const conn = await obterHubCompartilhado();
  if (!conn) return false;

  try {
    await conn.invoke('EntrarConversa', conversaCodigo);
    return true;
  } catch {
    return false;
  }
}

export function sairConversaHub() {
  conversaAtivaCodigo = null;
  onReceberMensagemAtivo = null;
}

export async function encerrarHub() {
  badgeHandlers.onNovaMensagem = null;
  badgeHandlers.onConnected = null;
  badgeHandlers.onDisconnected = null;
  onReceberMensagemAtivo = null;
  conversaAtivaCodigo = null;
  ultimaFalhaHubEm = 0;

  if (hubConnection) {
    await hubConnection.stop().catch(() => {});
    hubConnection = null;
  }
  hubStarting = null;
}

export const CHAT_POLL_INTERVAL_MS = CHAT_POLL_FALLBACK_MS;

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
    revalidarConversasEmBackground();
    return { data: stale, fromCache: true, stale: true };
  }
  const data = await listarConversas({ force: true });
  return { data, fromCache: false, stale: false };
}

export function calcularTotalNaoLidas(conversas) {
  return (conversas ?? []).reduce((acc, c) => acc + (c.naoLidas ?? 0), 0);
}

export async function temNovasMensagens() {
  const { data } = await api.get('/Conversas/tem-novas');
  return data?.temNovas ?? false;
}

export async function verificarBadgeFallback() {
  const cached = obterCache('conversas', {}, CONVERSAS_TTL_MS);
  if (cached) return calcularTotalNaoLidas(cached);

  const stale = obterCacheStale('conversas', {});
  if (stale) {
    revalidarConversasEmBackground();
    return calcularTotalNaoLidas(stale);
  }

  const temNovas = await temNovasMensagens();
  return temNovas ? 1 : 0;
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
