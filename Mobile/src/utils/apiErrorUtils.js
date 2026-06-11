const ERROR_MESSAGES = {
  EMAIL_NOT_FOUND: 'Este e-mail não existe. Por favor, insira um e-mail válido.',
  EMAIL_ALREADY_REGISTERED:
    'Este e-mail já está registrado no nosso sistema. Por favor, faça login.',
  WRONG_PASSWORD: 'Senha incorreta.',
  PRODUCT_NOT_FOUND: 'Produto não encontrado.',
  INVALID_CREDENTIALS: 'E-mail ou senha incorretos. Verifique os dados ou cadastre-se.',
  NETWORK: 'Sem conexão com a API. Verifique se o backend está rodando e se EXPO_PUBLIC_API_URL aponta para o IP correto.',
};

const LEGACY_TEXT_MAP = [
  { match: /email ou senha incorretos/i, code: 'INVALID_CREDENTIALS' },
  { match: /produto n[aã]o encontrado/i, code: 'PRODUCT_NOT_FOUND' },
  { match: /e-mail j[aá] est[aá] cadastrado/i, code: 'EMAIL_ALREADY_REGISTERED' },
  { match: /e-mail j[aá] tem conta/i, code: 'EMAIL_ALREADY_REGISTERED' },
];

function extractRawMessage(error) {
  if (!error) return null;

  const { data } = error.response || {};

  if (typeof data === 'string' && data.trim()) return data.trim();
  if (data?.message) return String(data.message);
  if (data?.detail) return String(data.detail);
  if (data?.title && data.title !== 'One or more validation errors occurred.') {
    return String(data.title);
  }
  if (data?.errors) {
    const msgs = Object.values(data.errors).flat();
    if (msgs.length) return msgs.join('\n');
  }
  return null;
}

function resolveErrorCode(error) {
  const data = error?.response?.data;
  if (data?.code && ERROR_MESSAGES[data.code]) return data.code;

  const raw = extractRawMessage(error);
  if (raw) {
    for (const { match, code } of LEGACY_TEXT_MAP) {
      if (match.test(raw)) return code;
    }
  }

  const status = error?.response?.status;
  if (status === 404 && error?.config?.url?.includes('/Produtos')) return 'PRODUCT_NOT_FOUND';
  if (status === 409) return 'EMAIL_ALREADY_REGISTERED';
  if (status === 401) return 'INVALID_CREDENTIALS';

  return null;
}

/**
 * Retorna objeto rico para FormErrorBanner / Alert.
 */
export function mapApiError(error, context = {}) {
  if (!error) {
    return { title: 'Erro', message: 'Erro desconhecido.' };
  }

  if (!error.response) {
    return { title: 'Sem conexão', message: ERROR_MESSAGES.NETWORK };
  }

  const code = resolveErrorCode(error);
  const { status } = error.response;

  if (code === 'EMAIL_ALREADY_REGISTERED') {
    return {
      title: 'E-mail já cadastrado',
      message: ERROR_MESSAGES.EMAIL_ALREADY_REGISTERED,
      actionLabel: 'Ir para login',
      code,
    };
  }

  if (code === 'EMAIL_NOT_FOUND') {
    return { title: 'E-mail não encontrado', message: ERROR_MESSAGES.EMAIL_NOT_FOUND, code };
  }

  if (code === 'WRONG_PASSWORD') {
    return { title: 'Senha incorreta', message: ERROR_MESSAGES.WRONG_PASSWORD, code };
  }

  if (code === 'PRODUCT_NOT_FOUND' || context.resource === 'product') {
    return { title: 'Produto não encontrado', message: ERROR_MESSAGES.PRODUCT_NOT_FOUND, code: 'PRODUCT_NOT_FOUND' };
  }

  const raw = extractRawMessage(error);
  if (raw) {
    return { title: context.title || 'Erro', message: raw, code };
  }

  if (status === 403) return { title: 'Acesso negado', message: 'Acesso negado.' };
  if (status === 429) return { title: 'Aguarde', message: 'Muitas tentativas. Aguarde um minuto.' };
  if (status === 503) {
    return {
      title: 'Indisponível',
      message: 'Serviço temporariamente indisponível. Tente novamente em instantes.',
    };
  }
  if (status === 500) {
    return { title: 'Erro', message: 'Erro interno no servidor. Tente novamente.' };
  }
  if (status === 400) return { title: 'Dados inválidos', message: 'Verifique os campos informados.' };

  return { title: 'Erro', message: `Erro ${status}` };
}

/** Compatibilidade com chamadas existentes que esperam string. */
export function formatApiError(error, context) {
  return mapApiError(error, context).message;
}

export { ERROR_MESSAGES };
