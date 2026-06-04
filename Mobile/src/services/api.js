import axios from 'axios';
import { Platform } from 'react-native';
import { obterToken, removerToken } from './tokenStorage';

const LOCALHOST_PC = 'http://localhost:5132/api';

const baseURL =
  process.env.EXPO_PUBLIC_API_URL ||
  Platform.select({
    web: LOCALHOST_PC,
    android: LOCALHOST_PC,
    ios: LOCALHOST_PC,
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
  const token = await obterToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401) {
      await removerToken();
    }
    return Promise.reject(error);
  }
);

export default api;
