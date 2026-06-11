import api from './api';

export async function consultarCnpj(cnpj) {
  const digitos = (cnpj || '').replace(/\D/g, '');
  const { data } = await api.get(`/Consultas/cnpj/${digitos}`);
  return data;
}
