import axios from 'axios';
import { Platform } from 'react-native';
import { getToken, clearToken } from './tokenStorage';
import { getContextoOperacional } from '../utils/modoUsuario';
import { deveAbrirLoginNo401, deveEncaminharAoLogin, ehLojaClaimAusente, isPedidoPublicoDeCatalogo } from './apiAccess';
import { sinalizarLojaClaimAusente } from './lojaClaim';
import { emitAuthFailure, peekPendingAuthAction, setPendingAuthAction } from '../navigation/authIntent';
import { navigationRef } from '../navigation/navigationRef';

const LOCAL_API_PORT = 5132;
const LOCALHOST_PC = `http://localhost:${LOCAL_API_PORT}/api`;

/**
 * Garante URL absoluta com https:// e sufixo /api.
 * Sem https:// o axios no browser trata o host como path relativo à Vercel
 * (ex.: vercel.app/railway.app/Auth/login → 405).
 */
function normalizeApiBaseUrl(url) {
  if (!url || typeof url !== 'string') return null;

  let normalized = url.trim();
  if (!normalized) return null;

  // Evita path relativo (/host...) ou host sem protocolo
  normalized = normalized.replace(/^\/+/, '');
  normalized = normalized.replace(/\/+$/, '');
  if (!normalized) return null;

  if (!/^https?:\/\//i.test(normalized)) {
    normalized = `https://${normalized}`;
  }

  try {
    const parsed = new URL(normalized);
    if (parsed.hostname.includes('railway.internal')) {
      console.error(
        '[API] EXPO_PUBLIC_API_URL usa *.railway.internal — isso é rede privada. ' +
          'No Railway: Settings → Networking → Public Domain (algo como xxx.up.railway.app). ' +
          'Na Vercel: EXPO_PUBLIC_API_URL=https://xxx.up.railway.app/api e Redeploy.'
      );
      return null;
    }
    let path = parsed.pathname.replace(/\/+$/, '') || '';
    if (!path.endsWith('/api')) {
      path = `${path}/api`.replace(/\/+/g, '/');
    }
    return `${parsed.origin}${path}`;
  } catch {
    return null;
  }
}

function resolveBaseUrl() {
  // Web local: ignora IP velho no .env. localhost:8081 fala com localhost:5132.
  // Aberto via LAN (http://192.168.x.x:8081) usa o mesmo host na porta da API.
  if (__DEV__ && Platform.OS === 'web' && typeof window !== 'undefined') {
    const host = window.location.hostname;
    if (host === 'localhost' || host === '127.0.0.1') {
      return LOCALHOST_PC;
    }
    return `http://${host}:${LOCAL_API_PORT}/api`;
  }

  const fromEnv = normalizeApiBaseUrl(process.env.EXPO_PUBLIC_API_URL);
  if (fromEnv) return fromEnv;

  if (typeof window !== 'undefined' && !__DEV__) {
    console.error(
      '[API] EXPO_PUBLIC_API_URL não definida no build da Vercel. ' +
        'Configure https://SUA-URL-RAILWAY.up.railway.app/api e faça redeploy.'
    );
  }

  return Platform.select({
    web: LOCALHOST_PC,
    default: LOCALHOST_PC,
  });
}

const baseURL = resolveBaseUrl();

if (__DEV__ || (typeof window !== 'undefined' && !baseURL.startsWith('http'))) {
  console.log('[API] baseURL:', baseURL);
}

const api = axios.create({
  baseURL,
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json',
  },
});

function semAuthorization(headers) {
  if (!headers) return;
  if (typeof headers.delete === 'function') {
    headers.delete('Authorization');
    return;
  }
  delete headers.Authorization;
}

function redirecionarLoginSePrecisar() {
  if (!navigationRef.isReady()) return;
  const name = navigationRef.getCurrentRoute()?.name;
  if (!name || !deveAbrirLoginNo401(401, { method: 'get', url: '/Conversas' }, name)) return;
  if (!peekPendingAuthAction()) {
    setPendingAuthAction({ type: 'route', name });
  }
  navigationRef.navigate('Login');
}

api.interceptors.request.use(async (config) => {
  const token = await getToken();
  if (token && !config._semToken) {
    config.headers.Authorization = `Bearer ${token}`;
  } else {
    semAuthorization(config.headers);
  }
  config.headers['X-Contexto-Operacional'] = getContextoOperacional();
  return config;
});

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const status = error.response?.status;
    if (ehLojaClaimAusente(error)) {
      sinalizarLojaClaimAusente();
      return Promise.reject(error);
    }
    if (status === 401 && isPedidoPublicoDeCatalogo(error.config) && !error.config?._semToken) {
      await clearToken();
      emitAuthFailure();
      const headers = { ...(error.config.headers || {}) };
      delete headers.Authorization;
      return api.request({ ...error.config, headers, _semToken: true });
    }
    const routeName = navigationRef.isReady() ? navigationRef.getCurrentRoute()?.name : null;
    if (deveAbrirLoginNo401(status, error.config, routeName)) {
      await clearToken();
      emitAuthFailure();
      redirecionarLoginSePrecisar();
    } else if (deveEncaminharAoLogin(status, error.config)) {
      await clearToken();
      emitAuthFailure();
    }
    return Promise.reject(error);
  }
);

export default api;
