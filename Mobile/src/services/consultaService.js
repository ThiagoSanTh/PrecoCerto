import api from './api';

export async function consultarCnpj(cnpj) {
  const digitos = String(cnpj || '').replace(/\D/g, '');
  const { data } = await api.get(`/Consultas/cnpj/${encodeURIComponent(digitos)}`);
  return data;
}
