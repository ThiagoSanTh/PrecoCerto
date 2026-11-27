import axios from 'axios';
import { Platform } from 'react-native';
import { getToken, clearToken } from './tokenStorage';
import { getContextoOperacional } from '../utils/modoUsuario';
import { normalizeApiBaseUrl } from '../utils/securityUtils';
import { deveAbrirLoginNo401, deveEncaminharAoLogin, deveRepetirCatalogoSemToken } from './apiAccess';
import { emitAuthFailure, peekPendingAuthAction, setPendingAuthAction } from '../navigation/authIntent';
import { navigationRef } from '../navigation/navigationRef';

const LOCAL_API_PORT = 5132;
const LOCALHOST_PC = `http://localhost:${LOCAL_API_PORT}/api`;

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

  const fromEnv = normalizeApiBaseUrl(process.env.EXPO_PUBLIC_API_URL, __DEV__);
  if (fromEnv) return fromEnv;

  if (!__DEV__) {
    console.error(
      '[API] EXPO_PUBLIC_API_URL ausente, inválida ou bloqueada (http:// não-local). ' +
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
    if (deveRepetirCatalogoSemToken(status, error.config)) {
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
