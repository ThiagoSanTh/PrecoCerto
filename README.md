# Preço Certo

Comparador de preços local que conecta consumidores a lojas da região. O usuário busca produtos, compara ofertas, vê lojas no mapa e conversa diretamente com o comerciante — tudo pelo app mobile, com API .NET e banco PostgreSQL (Supabase).

## O que o projeto faz

**Para o consumidor**
- Buscar produtos por nome, categoria e proximidade
- Ver feed paginado com melhor preço e promoções
- Localizar lojas no mapa (GPS + raio configurável)
- Favoritar produtos e lojas
- Avaliar estabelecimentos
- Conversar com lojistas em tempo real (chat)
- Manter histórico de buscas

**Para o lojista**
- Cadastrar loja com endereço e geolocalização
- Gerenciar catálogo de produtos (com foto)
- Publicar ofertas (preço, estoque, promoção)
- Promover vendedores da equipe
- Receber mensagens de clientes interessados

**Para o sistema**
- Autenticação JWT com confirmação de e-mail
- Papéis: Cliente, Lojista, Vendedor e Admin
- Validação de CPF, CNPJ, e-mail e telefone
- API paginada, rate limiting e compressão de resposta
- Deploy via Render/Docker; app mobile via Expo (Android, iOS e web)

## Stack

| Camada | Tecnologia |
|--------|------------|
| API | .NET 8, EF Core, PostgreSQL |
| Mobile | Expo SDK 54, React Native |
| Auth | JWT + BCrypt |
| Tempo real | SignalR (chat) |
| Infra | Supabase, Render, Vercel |

## Como rodar (resumo)

```bash
# Backend — http://localhost:5132/swagger  e  /api/health
cd BackEnd/Pc.WebApi
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=...;..."
dotnet user-secrets set "Jwt:Secret" "chave-com-pelo-menos-32-caracteres"
dotnet run

# Mobile (web: http://localhost:8081)
cd Mobile
npm install
npm start
# no terminal do Expo: tecla w
```

Browser em `localhost:8081` chama `http://localhost:5132/api` sozinho. Celular (Expo Go): `EXPO_PUBLIC_API_URL=http://IP-DO-PC:5132/api` em `Mobile/.env`, depois reinicie o Metro.

## Deploy

**Railway (API)** — monorepo. Pasta do código é `BackEnd` (E maiúsculo), **não** `Backend`.

No serviço Railway:

1. **Root Directory** = `/` (vazio). Não use `/Backend` — Linux diferencia maiúscula; o Railpack aí lê a raiz e quebra (`could not determine how to build`).
2. Commit na raiz: `Dockerfile` + `railway.toml` (builder Docker, health `/api/health`).
3. Se quiser Root Directory na subpasta, o valor exato é `/BackEnd` e o Config File Path é `/railway.toml` (o arquivo de config **não** segue o Root Directory).

Variáveis:

- `ConnectionStrings__DefaultConnection` (pooler Supabase, IPv4)
- `Jwt__Secret` (≥ 32 caracteres)
- `Jwt__Issuer` = `PrecoCerto`
- `Jwt__Audience` = `PrecoCertoApp`
- `ASPNETCORE_ENVIRONMENT` = `Production`
- `Cors__AllowedOrigins__0` = URL do Vercel (`https://seu-app.vercel.app`)

`*.vercel.app` já entra na política CORS. Chat (SignalR) usa WebSocket em `/hubs/chat`.

**Vercel (web)** — Root Directory = `Mobile`. Build = `npm run build:web`, Output = `dist`. Variáveis **no build** (redeploy após mudar):

- `EXPO_PUBLIC_API_URL` = `https://SEU-SERVICO.up.railway.app/api` (https + `/api`)
- `EXPO_PUBLIC_SUPABASE_URL`
- `EXPO_PUBLIC_SUPABASE_ANON_KEY`

Sem `https://` o browser trata o host como path (`vercel.app/railway.app/...`) e o login vira 405.

Segredos e connection strings: [`BackEnd/docs/SECRETS.md`](BackEnd/docs/SECRETS.md). Mapa do código: [`.cursor/PROJETO.md`](.cursor/PROJETO.md).

## Autores

**Thiago Almeida Sant'Ana** · **Gabriel Almeida** — Engenharia de Software
