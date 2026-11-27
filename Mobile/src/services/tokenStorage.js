import { Platform } from 'react-native';
import * as SecureStore from 'expo-secure-store';
import { isValidJwtFormat } from '../utils/securityUtils';

const TOKEN_KEY = '@auth_token';

export async function saveToken(token) {
  if (!isValidJwtFormat(token)) {
    throw new Error('Token JWT inválido.');
  }
  const cleanToken = token.trim();
  if (Platform.OS === 'web') {
    // shortcut: Web usa sessionStorage em vez de cookie HttpOnly; Safari ITP bloqueia cookie cross-site vercel.app<->railway.app sem dominio proprio
    if (typeof sessionStorage !== 'undefined') {
      sessionStorage.setItem(TOKEN_KEY, cleanToken);
    }
    if (typeof localStorage !== 'undefined') {
      localStorage.removeItem(TOKEN_KEY);
    }
    return;
  }
  await SecureStore.setItemAsync(TOKEN_KEY, cleanToken);
}

export async function getToken() {
  let stored = null;
  if (Platform.OS === 'web') {
    if (typeof sessionStorage !== 'undefined') {
      stored = sessionStorage.getItem(TOKEN_KEY);
    }
    if (typeof localStorage !== 'undefined' && localStorage.getItem(TOKEN_KEY)) {
      localStorage.removeItem(TOKEN_KEY);
    }
  } else {
    stored = await SecureStore.getItemAsync(TOKEN_KEY);
  }

  if (!stored) return null;
  if (!isValidJwtFormat(stored)) {
    await clearToken();
    return null;
  }
  return stored.trim();
}

export async function clearToken() {
  if (Platform.OS === 'web') {
    if (typeof sessionStorage !== 'undefined') {
      sessionStorage.removeItem(TOKEN_KEY);
    }
    if (typeof localStorage !== 'undefined') {
      localStorage.removeItem(TOKEN_KEY);
    }
    return;
  }
  await SecureStore.deleteItemAsync(TOKEN_KEY);
}
