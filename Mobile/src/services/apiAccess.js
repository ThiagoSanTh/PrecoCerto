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

export function pageSizeSeguro(pageSize) {
  const n = Number(pageSize);
  if (!Number.isFinite(n) || n < 1) return 20;
  return Math.min(100, Math.floor(n));
}
