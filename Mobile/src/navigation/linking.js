export const PUBLIC_ROUTE_NAMES = new Set([
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

const TITULO_ROTA = {
  Home: 'Início',
  Buscar: 'Buscar',
  Search: 'Buscar',
  ProductDetail: 'Produto',
  StoreCatalog: 'Catálogo',
  NotFound: 'Página não encontrada',
  Chat: 'Conversa',
  ConversasLista: 'Mensagens',
  IA: 'Assistente',
  AiChat: 'Assistente',
  Favoritos: 'Favoritos',
  Histórico: 'Histórico',
  Mensagens: 'Mensagens',
  Perfil: 'Perfil',
  Painel: 'Painel',
  Produtos: 'Produtos',
  Loja: 'Minha loja',
  Conta: 'Perfil',
  Login: 'Entrar',
  Cadastro: 'Cadastro',
  EsqueciSenha: 'Recuperar senha',
  CreateStore: 'Criar loja',
  CreateProduct: 'Novo produto',
  CreateOferta: 'Nova oferta',
  Vendedores: 'Equipe',
  ChangePassword: 'Alterar senha',
  EditProfile: 'Editar perfil',
  EditEmail: 'Editar e-mail',
};

const TELAS_SOB_HOME = /^(Painel|Produtos|Loja|Conta|Mensagens|IA|favoritos|historico|perfil|buscar|search)$/i;

/** O alias `home` não se estende às telas filhas; /home/Painel vira /Painel. */
export function ajustarCaminho(path) {
  const texto = String(path || '');
  const [caminho, busca] = texto.split('?');
  const partes = caminho.split('/').filter(Boolean);
  if (partes[0]?.toLowerCase() !== 'home' || !TELAS_SOB_HOME.test(partes[1] || '')) {
    return texto;
  }
  partes.shift();
  const next = `/${partes.join('/')}`;
  return busca ? `${next}?${busca}` : next;
}

export function tituloDocumento(routeName, title) {
  const nome = title && title !== routeName ? title : TITULO_ROTA[routeName];
  if (!nome) return 'Preço Certo';
  return `Preço Certo · ${nome}`;
}

export const linkingScreens = {
  Home: {
    path: '',
    alias: ['home', 'Home', 'Buscar', 'buscar', 'Search', 'search'],
    screens: {
      Buscar: {
        path: '',
        screens: {
          Search: '',
        },
      },
      IA: { path: 'ia', alias: ['IA'] },
      Favoritos: 'favoritos',
      Histórico: 'historico',
      Mensagens: {
        path: 'mensagens',
        alias: ['Mensagens'],
        screens: {
          ConversasLista: '',
          Chat: ':conversaCodigo',
        },
      },
      Perfil: 'perfil',
      Painel: { path: 'Painel', alias: ['painel'] },
      Produtos: { path: 'Produtos', alias: ['produtos'] },
      Loja: { path: 'Loja', alias: ['loja'] },
      Conta: { path: 'Conta', alias: ['conta'] },
    },
  },
  Login: { path: 'login', alias: ['Login'] },
  Cadastro: { path: 'cadastro', alias: ['Cadastro'] },
  EsqueciSenha: { path: 'esqueci-senha', alias: ['EsqueciSenha'] },
  CreateStore: { path: 'criar-loja', alias: ['CreateStore'] },
  CreateProduct: { path: 'criar-produto', alias: ['CreateProduct'] },
  ProductDetail: { path: 'produto', alias: ['ProductDetail'] },
  StoreCatalog: { path: 'catalogo', alias: ['StoreCatalog'] },
  CreateOferta: { path: 'criar-oferta', alias: ['CreateOferta'] },
  Vendedores: { path: 'vendedores', alias: ['Vendedores'] },
  ChangePassword: { path: 'alterar-senha', alias: ['ChangePassword'] },
  EditProfile: { path: 'editar-perfil', alias: ['EditProfile'] },
  EditEmail: { path: 'editar-email', alias: ['EditEmail'] },
  Chat: { path: 'chat', alias: ['Chat'] },
  AiChat: { path: 'assistente', alias: ['AiChat'] },
  NotFound: '*',
};
