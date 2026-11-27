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
      IA: 'ia',
      Favoritos: 'favoritos',
      Histórico: 'historico',
      Mensagens: 'mensagens',
      Perfil: 'perfil',
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
