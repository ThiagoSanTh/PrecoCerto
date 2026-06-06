import axios from 'axios';
import { Platform } from 'react-native';
import { getToken, clearToken } from './tokenStorage';

const LOCALHOST_PC = 'http://localhost:5132/api';

/** Garante https:// e sufixo /api (evita URL relativa na Vercel). */
function normalizeApiBaseUrl(url) {
  if (!url || typeof url !== 'string') return null;

  let normalized = url.trim().replace(/\/+$/, '');
  if (!normalized) return null;

  if (!/^https?:\/\//i.test(normalized)) {
    normalized = `https://${normalized}`;
  }

  if (!normalized.endsWith('/api')) {
    normalized = `${normalized}/api`;
  }

  return normalized;
}

const baseURL =
  normalizeApiBaseUrl(process.env.EXPO_PUBLIC_API_URL) ||
  Platform.select({
    web: LOCALHOST_PC,
    default: LOCALHOST_PC,
  });

if (__DEV__) {
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
