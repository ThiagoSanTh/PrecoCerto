import axios from 'axios';
import { Platform } from 'react-native';
import { getToken, clearToken } from './tokenStorage';
import { getContextoOperacional } from '../utils/modoUsuario';
import { normalizeApiBaseUrl } from '../utils/securityUtils';

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

api.interceptors.request.use(async (config) => {
  const token = await getToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  config.headers['X-Contexto-Operacional'] = getContextoOperacional();
  return config;
});

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const url = error.config?.url || '';
    const isAuthRoute =
      url.includes('/Auth/login') ||
      url.includes('/Clientes/login') ||
      url.includes('/Lojistas/login') ||
      url.includes('/registrar');

    if (error.response?.status === 401 && !isAuthRoute) {
      await clearToken();
    }
    return Promise.reject(error);
  }
);

export default api;
