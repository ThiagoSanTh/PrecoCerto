import * as SecureStore from 'expo-secure-store';

const TOKEN_KEY = 'preco_certo_jwt';

export async function salvarToken(token) {
  await SecureStore.setItemAsync(TOKEN_KEY, token);
}

export async function obterToken() {
  return SecureStore.getItemAsync(TOKEN_KEY);
}

export async function removerToken() {
  await SecureStore.deleteItemAsync(TOKEN_KEY);
}
