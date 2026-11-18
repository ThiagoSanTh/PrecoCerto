const GET_EXATAS = new Set([
  '/feed',
  '/lojas',
  '/lojas/mapa',
  '/produtos',
  '/ofertas',
  '/weather',
  '/historicopesquisa/sugestoes',
]);

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

function leituraPublicaExata(path) {
  if (GET_EXATAS.has(path)) return true;
  if (/^\/lojas\/[^/]+$/.test(path)) return path !== '/lojas/buscar';
  if (/^\/produtos\/[^/]+$/.test(path)) return path !== '/produtos/buscar';
  if (/^\/ofertas\/produto\/[^/]+$/.test(path)) return true;
  if (/^\/ofertas\/[^/]+$/.test(path)) return path !== '/ofertas/produto';
  if (/^\/avaliacoes\/loja\/[^/]+\/media$/.test(path)) return true;
  if (/^\/avaliacoes\/loja\/[^/]+$/.test(path)) return true;
  return false;
}

/** GET e POST públicos do catálogo, método e caminho exatos. */
export function isPedidoPublicoDeCatalogo(config) {
  const method = String(config?.method || 'get').toLowerCase();
  const path = caminhoDaRequisicao(config?.url).toLowerCase();
  if (method === 'get') return leituraPublicaExata(path);
  if (method === 'post') return path === '/produtos/buscar' || path === '/lojas/buscar';
  return false;
}

/** Token válido no boot e revogado no servidor: o 401 do catálogo repete sem Authorization. */
export function deveRepetirCatalogoSemToken(status, config) {
  return status === 401 && isPedidoPublicoDeCatalogo(config) && !config?._semToken;
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

export function pageSizeSeguro(pageSize) {
  const n = Number(pageSize);
  if (!Number.isFinite(n) || n < 1) return 20;
  return Math.min(100, Math.floor(n));
}
