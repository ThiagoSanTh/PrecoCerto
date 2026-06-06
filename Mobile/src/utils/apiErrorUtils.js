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

  if (data?.title && data?.errors) {
    const msgs = Object.values(data.errors).flat();
    if (msgs.length) return msgs.join('\n');
    return data.title;
  }

  if (data?.message) return data.message;

  if (status === 401) return 'Email ou senha incorretos.';
  if (status === 403) return 'Acesso negado.';
  if (status === 429) return 'Muitas tentativas. Aguarde um minuto.';

  return `Erro ${status}`;
}
