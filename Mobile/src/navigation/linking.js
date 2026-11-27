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

// linkingScreens mapeia URLs web (SPA/Vercel) para a hierarquia real de Stacks e Tabs,
// garantindo que links diretos e refresh (F5) abram a screen exata sem cair em NotFound.
export const linkingScreens = {
  Home: {
    path: '',
    alias: ['home', 'Home', 'Buscar', 'buscar', 'Search', 'search'],
    screens: {
      Buscar: {
        path: '',
        screens: {
          Search: '',
          // Rotas aninhadas da vitrine preservam a sidebar no desktop e os parâmetros no refresh.
          ProductDetail: 'vitrine/produto',
          StoreCatalog: 'vitrine/catalogo',
        },
      },
      IA: 'ia',
      Favoritos: 'favoritos',
      Histórico: 'historico',
      // Mensagens usa MensagensStack (lista + conversa ativa), evitando URLs órfãs em /mensagens/ConversasLista.
      Mensagens: {
        path: 'mensagens',
        screens: {
          ConversasLista: '',
          Chat: 'conversa',
        },
      },
      Perfil: 'perfil',
      // Abas exclusivas do StoreTabs (modo lojista), protegidas pelo guard de sessão.
      Painel: { path: 'painel', alias: ['Painel'] },
      Produtos: { path: 'produtos', alias: ['Produtos'] },
      Loja: { path: 'loja', alias: ['Loja'] },
      Conta: { path: 'conta', alias: ['Conta'] },
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
