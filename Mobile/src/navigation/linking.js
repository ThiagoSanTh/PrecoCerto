export const PUBLIC_ROUTE_NAMES = new Set(['Login', 'Cadastro', 'EsqueciSenha', 'NotFound']);

export const linkingScreens = {
  Login: { path: '', alias: ['login', 'Login'] },
  Cadastro: { path: 'cadastro', alias: ['Cadastro'] },
  EsqueciSenha: { path: 'esqueci-senha', alias: ['EsqueciSenha'] },
  Home: { path: 'home', alias: ['Home', 'Buscar', 'buscar', 'Search', 'search'] },
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
