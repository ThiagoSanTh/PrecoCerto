# Estrutura do repositório — Preço Certo

Monorepo com backend .NET e app mobile Expo.

Documentação detalhada (pastas, arquivos, fluxos): [`.cursor/PROJETO.md`](../.cursor/PROJETO.md)

## BackEnd

Solução em camadas (`BackEnd/PrecoCerto.slnx`):

| Pasta | Responsabilidade |
|-------|------------------|
| `Pc.Dominio` | Entidades de domínio e enums |
| `Pc.Infraestrutura` | `AppDbContext`, migrations EF Core |
| `Pc.Repositorio` | Interfaces e implementações de repositório |
| `Pc.Servico` | Regras de negócio e serviços de aplicação |
| `Pc.WebApi` | API REST, controllers, DTOs, JWT |
| `scripts/` | Scripts SQL para Supabase, ajustes manuais e benchmarks de desempenho (`perf-load-test.mjs`, `perf-microbench/`) |
| `docs/` | Segredos, setup e notas do backend |

**Rodar a API:** `BackEnd/Pc.WebApi` → `dotnet run`

**Benchmarks de desempenho:** `BackEnd/scripts/perf-load-test.mjs` (API) e `BackEnd/scripts/perf-microbench/` (validadores/bcrypt)

**Deploy:** `render.yaml` aponta para `BackEnd/Pc.WebApi`

## Mobile

App Expo SDK 54 em `Mobile/`. Código-fonte em `Mobile/src/`:

| Pasta | Responsabilidade |
|-------|------------------|
| `components/` | Componentes reutilizáveis (form, product, feed, mapas) |
| `context/` | Context API (autenticação) |
| `navigation/` | Stack, tabs e rotas por perfil (cliente / lojista) |
| `screens/auth/` | Login, cadastro, recuperação de senha |
| `screens/catalog/` | Busca e detalhe de produto |
| `screens/store/` | Gestão da loja (produtos, ofertas) |
| `screens/user/` | Perfil, favoritos, histórico |
| `services/` | Clientes HTTP (API .NET, Supabase, GPS) |
| `theme/` | Paleta de cores e estilos globais |
| `utils/` | Helpers (preço, mapa, datas, erros) |

**Rodar o app:** `Mobile/` → `npm start`

## Raiz

| Item | Função |
|------|--------|
| `README.md` | Visão geral do produto |
| `docs/` | Documentação transversal |
| `render.yaml` | Configuração de deploy |
| `.cursor/rules/` | Regras e guias para desenvolvimento |
