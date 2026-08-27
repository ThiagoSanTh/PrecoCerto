# Preço Certo — guia do repositório

Documento de referência para entender a organização do monorepo. O fluxo geral é:

```
Mobile (Expo)  →  Pc.WebApi (Controllers)  →  Pc.Servico  →  Pc.Repositorio  →  PostgreSQL
```

Cada camada tem uma responsabilidade clara: a API recebe HTTP, os serviços aplicam regras de negócio, os repositórios acessam o banco, e o domínio define as entidades.

---

## Raiz do repositório

| Arquivo / pasta | O que é |
|-----------------|---------|
| `README.md` | Resumo do produto e como executar |
| `docs/ESTRUTURA.md` | Índice rápido das pastas principais |
| `render.yaml` | Configuração de deploy da API no Render |
| `.cursor/rules/` | Regras e diretrizes para o Cursor (arquitetura, planos, ferramentas) |
| `.cursor/PROJETO.md` | Este arquivo — mapa detalhado do código |

---

## BackEnd/

Solução .NET em camadas. Entry point: `BackEnd/Pc.WebApi`.

### `Pc.Dominio/` — núcleo do negócio

Define **o que existe** no sistema, sem depender de banco ou HTTP.

| Pasta / arquivo | O que é |
|-----------------|---------|
| `Entities/Base/BaseEntity.cs` | Campos comuns a todas as entidades (`Id`, `DataCriacao`, `Ativo`, etc.) |
| `Entities/Usuarios/Usuario.cs` | Usuário unificado (cliente, lojista ou vendedor) |
| `Entities/Usuarios/Admin.cs` | Administrador do sistema (tabela separada) |
| `Entities/Estabelecimentos/Loja.cs` | Loja vinculada ao lojista, com CNPJ e endereço |
| `Entities/Estabelecimentos/Oferta.cs` | Preço de um produto em uma loja (promoção, estoque) |
| `Entities/Catalogo/Produto.cs` | Produto do catálogo (nome, marca, categoria, imagem) |
| `Entities/Catalogo/Endereco.cs` | Endereço com latitude/longitude para mapa |
| `Entities/Interacoes/Favorito.cs` | Produto ou loja favoritado pelo cliente |
| `Entities/Interacoes/HistoricoPesquisa.cs` | Termos que o cliente buscou |
| `Entities/Interacoes/Avaliacao.cs` | Nota e comentário sobre uma loja |
| `Entities/Interacoes/Conversa.cs` | Thread de chat entre cliente e loja |
| `Entities/Interacoes/Mensagem.cs` | Mensagem individual dentro de uma conversa |
| `Entities/Interacoes/PreferenciaCliente.cs` | Preferências chave-valor (ex.: tema escuro) |
| `Enums/` | Constantes tipadas (`PapelUsuario`, `CategoriaProduto`, etc.) |
| `Validacoes/` | Validadores puros de CPF, e-mail e telefone (sem EF, sem HTTP) |
| `Comum/` | Tipos compartilhados como `PaginacaoParametros` e `PaginacaoResultado` |

---

### `Pc.Infraestrutura/` — banco de dados

| Pasta / arquivo | O que é |
|-----------------|---------|
| `AppDbContext.cs` | Configuração do EF Core: tabelas, relacionamentos e índices |
| `Migrations/` | Histórico de alterações do schema (aplicadas no startup ou via `dotnet ef`) |

---

### `Pc.Repositorio/` — acesso a dados

Abstrai consultas SQL/EF. Controllers **não** acessam o banco diretamente.

| Pasta / arquivo | O que é |
|-----------------|---------|
| `Interfaces/IRepositorio.cs` | Contrato genérico CRUD (`ObterPorId`, `Listar`, `Adicionar`, etc.) |
| `Interfaces/IProdutoRepositorio.cs` (e similares) | Contratos específicos por entidade |
| `Implementacoes/Repositorio.cs` | Implementação base genérica |
| `Implementacoes/ProdutoRepositorio.cs` | Queries de produto: busca com `ILike`, paginação, `AsNoTracking` |
| `Implementacoes/LojaRepositorio.cs` | Lojas, busca por nome e filtro por raio geográfico |
| `Implementacoes/OfertaRepositorio.cs` | Ofertas e agregação de melhor preço por produto |
| `Implementacoes/ConversaRepositorio.cs` | Conversas, mensagens paginadas e contagem de não-lidas |
| `Implementacoes/FavoritoRepositorio.cs` | Favoritos do cliente com paginação |
| `Implementacoes/ClienteRepositorio.cs` | Operações sobre usuários clientes |
| `Implementacoes/HistoricoPesquisaRepositorio.cs` | Histórico e sugestões de busca |
| `Implementacoes/AvaliacaoRepositorio.cs` | Avaliações por loja ou cliente |
| `Implementacoes/AdminRepositorio.cs` | Admins e verificação de e-mail duplicado |
| `Comum/GeoHelper.cs` | Cálculo de distância (Haversine) para lojas próximas |

---

### `Pc.Servico/` — regras de negócio

Orquestra repositórios, validações e exceções de domínio.

| Pasta / arquivo | O que é |
|-----------------|---------|
| `Interfaces/` | Contratos dos serviços (`IProdutoServico`, `IFeedServico`, etc.) |
| `Implementacoes/ProdutoServico.cs` | CRUD e busca de produtos |
| `Implementacoes/LojaServico.cs` | Criação de loja, promoção a lojista, listagem paginada |
| `Implementacoes/OfertaServico.cs` | Gestão de ofertas |
| `Implementacoes/FeedServico.cs` | Monta o feed: produto + melhor preço/oferta em uma consulta |
| `Implementacoes/ClienteServico.cs` | Cadastro, perfil, alteração de e-mail/senha |
| `Implementacoes/ConversaServico.cs` | Abrir conversa, enviar mensagem, contagem de não-lidas |
| `Implementacoes/FavoritoServico.cs` | Adicionar/remover favoritos |
| `Implementacoes/HistoricoPesquisaServico.cs` | Registrar e listar buscas |
| `Implementacoes/AvaliacaoServico.cs` | Criar e listar avaliações |
| `Implementacoes/ConsultaCnpjServico.cs` | Consulta externa de CNPJ (Receita) |
| `Implementacoes/ClimaServico.cs` | Clima com cache (`IClimaServico` → `IClimaProvedor`) |
| `Implementacoes/OpenMeteoClimaProvedor.cs` | Open-Meteo + Nominatim; não vaza JSON do fornecedor |
| `Implementacoes/ValidadorEmailServico.cs` | Valida formato e disponibilidade de e-mail |
| `Implementacoes/IdCodificadorServico.cs` | Codifica/decodifica IDs públicos (Sqids) nas URLs |
| `Implementacoes/BcryptPasswordHasher.cs` | Hash e verificação de senhas |
| `Excecoes/` | Exceções de negócio (`EmailJaRegistradoException`, etc.) |
| `Modelos/FeedItem.cs` | Modelo interno usado pelo serviço de feed |
| `Modelos/ClimaResposta.cs` | Clima normalizado (localidade, atual, horária, diária) |

---

### `Pc.WebApi/` — API HTTP

Expõe endpoints REST e SignalR. Traduz HTTP ↔ serviços ↔ DTOs.

#### `Controllers/` — rotas da API

| Controller | Responsabilidade |
|------------|------------------|
| `AuthController` | Login, registro, confirmação de e-mail, reset de senha |
| `FeedController` | `GET /api/Feed` — feed paginado com preço embutido |
| `ProdutosController` | CRUD e busca de produtos |
| `LojasController` | CRUD de lojas + `GET /mapa` por raio GPS |
| `OfertasController` | CRUD de ofertas |
| `ClientesController` | Perfil e dados do cliente autenticado |
| `LojistasController` | Promoção de vendedores |
| `FavoritosController` | Favoritos paginados com snapshot de preço |
| `ConversasController` | Chat REST (listar, mensagens, enviar) |
| `HistoricoPesquisaController` | Histórico e sugestões |
| `AvaliacoesController` | Avaliações de lojas |
| `PreferenciasClienteController` | Preferências do usuário |
| `ConsultasController` | Consulta de CNPJ |
| `WeatherController` | `GET /api/Weather` — clima normalizado por lat/lng (Open-Meteo, cache) |
| `AdminsController` | Gestão de administradores |
| `HealthController` | Health check leve (`/api/Health`) |

#### `DTOs/` — contratos de entrada e saída

DTOs (**Data Transfer Objects**) definem o formato exato do JSON que entra e sai da API. Eles validam dados na entrada (via DataAnnotations) e evitam expor entidades internas ou campos sensíveis na resposta.

| Subpasta | Exemplos | Para que serve |
|----------|----------|----------------|
| `DTOs/Catalogo/` | `ProdutoCriarDto`, `ProdutoFeedDto` | Criar/buscar produtos; DTOs leves para listagem |
| `DTOs/Estabelecimentos/` | `LojaCriarDto`, `LojaMapaDto` | Cadastro de loja; versão mínima para o mapa |
| `DTOs/Usuarios/` | `ClienteCriarDto`, `LojistaCriarDto` | Cadastro de usuários por papel |
| `DTOs/Interacoes/` | `FavoritoCriarDto`, `ConversaDtos` | Favoritos, chat, avaliações, histórico |
| `DTOs/Comum/` | `LoginDto`, `PaginacaoConsultaDto` | Login, paginação, alteração de senha/e-mail |

#### Outras pastas da WebApi

| Pasta / arquivo | O que é |
|-----------------|---------|
| `Mappings/` | Converte entidade → DTO (`ProdutoMapper`, `LojaMapper`) |
| `Helpers/PaginacaoHelper.cs` | Normaliza `page`/`pageSize` e monta resposta paginada |
| `Validacao/` | Atributos customizados (`CnpjValido`, `EmailValido`, `TelefoneValido`) |
| `Authorization/Authz.cs` | Políticas de autorização por papel |
| `Extensions/ClaimsPrincipalExtensions.cs` | Lê claims do JWT (`UserId`, `LojaId`, papel) |
| `Hubs/ChatHub.cs` | SignalR — mensagens em tempo real |
| `Services/JwtTokenService.cs` | Gera tokens JWT |
| `Services/SmtpEmailService.cs` | Envio de e-mails (confirmação, reset de senha) |
| `Configuration/` | Classes tipadas para `appsettings` (JWT, e-mail) |
| `Program.cs` | Bootstrap: DI, CORS, JWT, rate limit, compressão, migrations |

---

### `BackEnd/scripts/` — utilitários SQL e benchmark

| Arquivo | O que é |
|---------|---------|
| `seed-performance.sql` | Popula lojas/produtos/ofertas para teste de carga (só dev) |
| `perf-load-test.mjs` | Benchmark HTTP da API (Feed, paginação, concorrência) |
| `perf-microbench/` | Microbenchmark de validadores e BCrypt |
| `limpar-usuarios-nao-confirmados.sql` | Remove contas sem e-mail confirmado |
| `preflight-unificar-usuarios.sql` | Script de migração manual de usuários |
| `remover-carrinho.sql` | Remove tabelas legadas de carrinho |
| `setup-produtos-imagens-storage.sql` | Configuração de storage de imagens no Supabase |

### `BackEnd/docs/`

| Arquivo | O que é |
|---------|---------|
| `SECRETS.md` | Connection strings, User Secrets, pool Supabase, deploy |

### Outros arquivos do BackEnd

| Arquivo | O que é |
|---------|----------|
| `PrecoCerto.slnx` | Solution .NET que agrupa todos os projetos |
| `Dockerfile` | Imagem Docker multi-stage para deploy |
| `entrypoint.sh` | Script de inicialização do container |

---

## Mobile/

App Expo (React Native) em `Mobile/src/`.

### `screens/` — telas por domínio

| Pasta | Telas | O que faz |
|-------|-------|-----------|
| `auth/` | Login, Register, ForgotPassword | Autenticação e recuperação de senha |
| `catalog/` | SearchScreen, ProductDetailScreen | Feed/busca e detalhe do produto |
| `store/` | StoreScreen, ProductsScreen, CreateProductScreen, CreateOfertaScreen, CreateStoreScreen, VendedoresScreen | Gestão da loja (lojista) |
| `user/` | ProfileScreen, FavoritosScreen, HistoricoScreen, ConversasScreen, ChatScreen, EditProfileScreen, EditEmailScreen, ChangePasswordScreen | Área do cliente |

### `services/` — comunicação com API e infra

| Arquivo | O que é |
|---------|---------|
| `api.js` | Instância Axios com base URL e interceptors |
| `authService.js` | Login, registro, logout, refresh de sessão |
| `feedService.js` | Consome `GET /Feed` com cache |
| `feedCache.js` | Cache em memória com TTL (60s) e stale-while-revalidate |
| `productService.js` | Produtos paginados |
| `lojaService.js` | Lojas e mapa por raio GPS |
| `ofertaService.js` | Ofertas paginadas |
| `favoritoService.js` | Favoritos paginados |
| `chatService.js` | Conversas, mensagens e hub SignalR |
| `clienteService.js` | Perfil do cliente |
| `historicoService.js` | Histórico de buscas |
| `avaliacaoService.js` | Avaliações |
| `consultaService.js` | Consulta de CNPJ |
| `weatherService.js` | `GET /Weather` com cache 15 min (não chama o provedor meteorológico) |
| `locationService.js` | GPS do dispositivo |
| `storageService.js` | Upload de imagens (Supabase Storage) |
| `tokenStorage.js` | Persistência segura do JWT |
| `preferenciaService.js` | Preferências do usuário |
| `supabaseClient.js` | Cliente Supabase (storage/realtime auxiliar) |

### `components/` — UI reutilizável

| Pasta | O que é |
|-------|---------|
| `form/` | FormScreen, FormField, FormButton, ListCard — base visual de formulários |
| `feed/` | ProductGridCard, FavoritoListCard, WeatherCard, MapSearchOverlay |
| `product/` | Galeria, rating, barra de ações, seletor de categoria |
| `LojasMapView.js` | Mapa Leaflet com lojas próximas |
| `LeafletMapFrame.js` | WebView que renderiza o HTML do mapa |
| `CustomTabBar.js` | Tab bar customizada do app |

### `navigation/` — rotas

| Arquivo | O que é |
|---------|---------|
| `index.js` | Navigator raiz (auth vs app) |
| `AppRoutes.js` | Stacks principais |
| `tabUser.routes.js` | Tabs do cliente (busca, favoritos, mensagens, perfil) |
| `tabStore.routes.js` | Tabs do lojista (loja, produtos) |
| `mensagensStack.routes.js` | Stack de conversas → chat |
| `buscarStack.routes.js` | Stack de busca → detalhe do produto |

### `context/` — estado global

| Arquivo | O que é |
|---------|---------|
| `AuthContext.js` | Sessão do usuário, token, papel (cliente/lojista) |
| `ThemeContext.js` | Tema claro/escuro |

### `utils/` — funções auxiliares

| Arquivo | O que é |
|---------|---------|
| `produtoUtils.js` | Normalização e filtros de produto |
| `precoUtils.js` | Formatação e mapa de ofertas por produto |
| `leafletMapHtml.js` | Gera HTML do mapa (marcadores, clustering >100 pins) |
| `validacaoUtils.js` | Validações no front (CPF, telefone, etc.) |
| `apiErrorUtils.js` | Traduz erros da API em mensagens amigáveis |
| `categoriasProduto.js` | Labels das categorias |
| `mapaUtils.js` | Helpers de coordenadas e distância |
| `dataUtils.js` | Formatação de datas |

### `theme/`

| Arquivo | O que é |
|---------|---------|
| `index.js` | Paleta de cores e tokens visuais globais |

### Outros arquivos do Mobile

| Arquivo | O que é |
|---------|---------|
| `App.js` | Entry point do Expo |
| `package.json` | Dependências e scripts (`start`, `web`, `android`) |
| `vercel.json` | Deploy da versão web na Vercel |
| `assets/` | Ícones e imagens estáticas |

---

## Fluxo típico de uma requisição

1. **Mobile** chama `feedService.listarFeed()` → `GET /api/Feed?page=1&pageSize=20`
2. **FeedController** valida parâmetros e chama `IFeedServico`
3. **FeedServico** consulta `ProdutoRepositorio` + `OfertaRepositorio`, monta `FeedItem`
4. **Controller** mapeia para `ProdutoFeedDto` e retorna JSON paginado
5. **Mobile** normaliza resposta, guarda no `feedCache` e renderiza na `SearchScreen`

---

## Papéis de usuário

| Papel | Como obtém | O que pode fazer |
|-------|------------|------------------|
| **Cliente** | Cadastro padrão | Buscar, favoritar, avaliar, chat |
| **Lojista** | Abre uma loja com CNPJ válido | Tudo do cliente + gerenciar loja, produtos e ofertas |
| **Vendedor** | Promovido pelo lojista | Gerenciar produtos/ofertas da loja |
| **Admin** | Tabela `Admins` separada | Administração do sistema |

Após abrir loja, o usuário precisa **fazer login novamente** para obter JWT com permissões de lojista.

---

## Convenções importantes

- **IDs públicos** na API usam códigos Sqids (`codigoPublico`), não GUIDs expostos
- **Paginação**: `page`, `pageSize` (máx. 100) → resposta `{ items, page, pageSize, total, hasNext }`
- **Migrations** aplicam automaticamente no startup da API
- **Rate limit**: login (10/min), catálogo anônimo (120/min), autenticado (300/min)
- **Testes unitários** ficam na lógica de domínio/serviço; não há projeto `Pc.Tests` separado
