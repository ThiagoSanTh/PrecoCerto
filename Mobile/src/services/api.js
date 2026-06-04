import axios from 'axios';
import { Platform } from 'react-native';

const LOCALHOST_PC = 'http://localhost:5132/api';
/** Fallback se EXPO_PUBLIC_API_URL não carregar (ipconfig → Wi-Fi → IPv4). Prefira definir no .env */
const IP_REDE_LOCAL = 'http://172.20.10.7:5132/api';

const baseURL =
  process.env.EXPO_PUBLIC_API_URL ||
  Platform.select({
    web: LOCALHOST_PC,
    android: IP_REDE_LOCAL,
    ios: IP_REDE_LOCAL,
    default: IP_REDE_LOCAL,
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

export default api;