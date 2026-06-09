import api from './api';
import { saveToken } from './tokenStorage';

export async function login(email, senha, tipo = undefined) {
  const { data } = await api.post('/Auth/login', {
    email: email.trim(),
    senha,
    tipo,
  });

  if (data.token) {
    await saveToken(data.token);
  }

  return data;
}

export async function logoutApi() {
  // Stateless JWT — nada no servidor; token removido no cliente.
}
