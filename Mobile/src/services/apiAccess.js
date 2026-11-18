const LEITURA_PUBLICA = [
  /^\/Feed(?:\/|$)/i,
  /^\/Lojas(?:\/|$)/i,
  /^\/Produtos(?:\/|$)/i,
  /^\/Ofertas(?:\/|$)/i,
  /^\/Weather(?:\/|$)/i,
  /^\/Avaliacoes\/loja(?:\/|$)/i,
  /^\/HistoricoPesquisa\/sugestoes$/i,
];

const BUSCA_PUBLICA = [/^\/Produtos\/Buscar$/i, /^\/Lojas\/buscar$/i];

export function caminhoDaRequisicao(url = '') {
  const bruto = String(url).split('?')[0].trim();
  if (!bruto) return '/';

  let path = bruto;
  if (/^https?:\/\//i.test(bruto)) {
    try {
      path = new URL(bruto).pathname;
    } catch {
      path = bruto;
    }
  }

  path = path.replace(/^\/api(?=\/|$)/i, '') || '/';
  if (!path.startsWith('/')) path = `/${path}`;
  return path.replace(/\/+$/, '') || '/';
}

/** GET de catálogo/mapa e POST de busca. Compra, IA, chat e escrita ficam de fora. */
export function isPedidoPublicoDeCatalogo(config) {
  const method = String(config?.method || 'get').toLowerCase();
  const path = caminhoDaRequisicao(config?.url);
  if (method === 'get') return LEITURA_PUBLICA.some((regra) => regra.test(path));
  if (method === 'post') return BUSCA_PUBLICA.some((regra) => regra.test(path));
  return false;
}

function ehRotaDeCredencial(url = '') {
  return /\/Auth\/|\/Clientes\/login|\/Lojistas\/login|\/registrar/i.test(String(url));
}

/**
 * Só 401 de rota protegida leva ao login.
 * 403 é papel errado e 429 é limite; catálogo público não redireciona.
 */
export function deveEncaminharAoLogin(status, config) {
  if (status !== 401) return false;
  if (ehRotaDeCredencial(config?.url)) return false;
  if (isPedidoPublicoDeCatalogo(config)) return false;
  return true;
}

/** 401 em rota pública limpa a sessão e fica na vitrine. Só ação autenticada abre o login. */
export function deveAbrirLoginNo401(status, config, routeName) {
  if (!deveEncaminharAoLogin(status, config)) return false;
  if (!routeName) return false;
  const publicas = new Set([
    'Login',
    'Cadastro',
    'EsqueciSenha',
    'NotFound',
    'Home',
    'Buscar',
    'Search',
    'ProductDetail',
    'StoreCatalog',
  ]);
  return !publicas.has(routeName);
}

export const CODIGO_LOJA_CLAIM = 'loja_claim_ausente';

function codigoNoHeader(headers) {
  if (!headers) return '';
  if (typeof headers.get === 'function') {
    return headers.get('x-error-code') || headers.get('X-Error-Code') || '';
  }
  return headers['x-error-code'] || headers['X-Error-Code'] || '';
}

/** 403 de funcionário sem loja no token, ou HubException com o mesmo prefixo. */
export function ehLojaClaimAusente(erro) {
  if (!erro) return false;
  const texto = typeof erro === 'string' ? erro : String(erro.message || '');
  if (texto.startsWith(`${CODIGO_LOJA_CLAIM}:`)) return true;

  const response = erro.response;
  const status = response?.status ?? erro.status;
  if (status !== 403) return false;

  const data = response?.data ?? erro.data;
  if (data && String(data.code || data.Code || '') === CODIGO_LOJA_CLAIM) return true;
  return String(codigoNoHeader(response?.headers ?? erro.headers)).toLowerCase() === CODIGO_LOJA_CLAIM;
}

export function pageSizeSeguro(pageSize) {
  const n = Number(pageSize);
  if (!Number.isFinite(n) || n < 1) return 20;
  return Math.min(100, Math.floor(n));
}
