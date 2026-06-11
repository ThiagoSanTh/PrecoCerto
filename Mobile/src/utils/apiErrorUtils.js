/**
 * Extrai mensagem legível de erros axios / ASP.NET.
 */
export function formatApiError(error) {
  if (!error) return 'Erro desconhecido';

  if (!error.response) {
    return 'Sem conexão com a API. Verifique se o backend está rodando e se EXPO_PUBLIC_API_URL aponta para o IP correto.';
  }

  const { status, data } = error.response;

  if (typeof data === 'string' && data.trim()) return data;

  if (data?.errors) {
    const msgs = Object.values(data.errors).flat();
    if (msgs.length) return msgs.join('\n');
  }

  if (data?.detail) return data.detail;

  if (data?.title && data?.title !== 'One or more validation errors occurred.') {
    return data.title;
  }

  if (data?.message) return data.message;

  if (status === 401) return 'Email ou senha incorretos.';
  if (status === 400) return data?.message || 'Dados inválidos. Verifique os campos.';
  if (status === 403) return 'Acesso negado.';
  if (status === 404) return data?.message || 'Recurso não encontrado.';
  if (status === 409) return data?.message || 'Este e-mail já está cadastrado.';
  if (status === 429) return data?.message || 'Muitas tentativas. Aguarde um minuto.';
  if (status === 503) return data?.message || 'Serviço temporariamente indisponível. Tente novamente em instantes.';
  if (status === 500) return data?.message || data?.detail || 'Erro interno no servidor. Tente novamente.';

  return `Erro ${status}`;
}
