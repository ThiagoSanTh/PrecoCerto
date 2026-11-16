# Como rodar o Preço Certo

O Preço Certo compara preços de lojas em Saquarema, RJ, no modo cliente e no modo lojista. Esta página diz como subir o app e a API na sua máquina, quais nomes de variável preencher e onde cada parte está publicada. A [visão do produto](PRODUCT.md), o [estado do projeto](ESTADO.md), o [design system](DESIGN.md) e o [checklist visual](TAREFAS.md) ficam em páginas separadas.

## O que o produto faz

Você usa o mesmo app nos dois modos. As personas e as telas estão na [visão do produto](PRODUCT.md).

- **Cliente**: busca ofertas, vê o mapa, guarda favoritos e conversa com a loja
- **Lojista**: acompanha o painel, o catálogo, as promoções e a equipe

O app fica em `Mobile/` com Expo 54, React Native 0.81 e React 19, para Android, iOS e Web. A API fica em `BackEnd/` com .NET 8, Entity Framework Core, SignalR e Serilog. O banco é PostgreSQL no Supabase, com a extensão pgvector.

O Motor de inteligência artificial (MotorIA) atende `POST /api/IA/analisar`. Se o Gemini falhar, a chamada segue para a NVIDIA NIM. O desenho está em [MotorIA v2](BackEnd/docs/MotorIA-v2.md).

## Onde está cada pasta

O repositório não tem `package.json` na raiz. Instale e rode o app dentro de `Mobile/`.

- `Mobile/`: app Expo, com [exemplo de ambiente](Mobile/.env.example)
- `BackEnd/Pc.WebApi/`: API HTTP e [exemplo de configuração](BackEnd/Pc.WebApi/appsettings.Exemplo.json)
- `BackEnd/Pc.Dominio/`, `BackEnd/Pc.Servico/`, `BackEnd/Pc.Repositorio/`, `BackEnd/Pc.Infraestrutura/`: regras, acesso a dados e migrations
- `BackEnd/Pc.Servico.Tests/`: testes da API
- `BackEnd/Pc.RagReindexCli/`: reindexação dos embeddings
- `Dockerfile` e `railway.toml`: imagem da API no Railway
- `render.yaml`: blueprint do Render, fora do caminho descrito no [estado do projeto](ESTADO.md)

## O que instalar antes

Instale Node.js 20 ou 22 e o .NET SDK 8. O app usa o lockfile de `Mobile/`. A API usa os projetos em `BackEnd/`.

## Como subir o app

Entre em `Mobile/`, instale as dependências do lockfile e copie o exemplo de ambiente. Preencha o arquivo `.env` com os nomes da seção de variáveis.

```bash
cd Mobile
npm ci
cp .env.example .env
```

Escolha a plataforma em outro comando:

```bash
npm start
npm run web
npm run android
npm run ios
```

`npm start` abre o Expo. `npm run web` sobe o site de desenvolvimento. O exemplo de ambiente aponta a API para `http://localhost:5132/api` e cita o navegador em `http://localhost:8081`. `npm run android` e `npm run ios` pedem emulador ou aparelho. O comando de iOS pede macOS.

`npm run build:web` gera a pasta `Mobile/dist`. O script `preview:web` serve essa pasta na porta 3000.

## Como subir a API

O perfil `http` em `launchSettings.json` escuta `http://localhost:5132`. Em Development, o Swagger abre em `/swagger`. O health check público é `GET /health`.

```bash
cd BackEnd/Pc.WebApi
dotnet restore
dotnet run
```

Na subida, a API aplica as migrations do Entity Framework Core. A connection string vem do Supabase. O passo a passo, sem gravar senha no Git, está em [segredos da API](BackEnd/docs/SECRETS.md). Rode os dois comandos dentro de `BackEnd/Pc.WebApi`:

```bash
dotnet user-secrets set \
  "ConnectionStrings:DefaultConnection" \
  "your_connection_string_here"
dotnet user-secrets set "Jwt:Secret" "your_jwt_secret_here"
```

## Quais variáveis preencher

Copie os nomes. Não commite valores. No ambiente da API, troque `:` por `__`.

Variáveis do app, em `Mobile/.env`:

- `EXPO_PUBLIC_API_URL`
- `EXPO_PUBLIC_SUPABASE_URL`
- `EXPO_PUBLIC_SUPABASE_ANON_KEY`
- `EXPO_PUBLIC_MAPBOX_TOKEN`
- `EXPO_PUBLIC_GOOGLE_MAPS_API_KEY`

Variáveis da API:

- `ConnectionStrings__DefaultConnection`
- `Jwt__Secret`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Cors__AllowedOrigins__0`
- `Email__Host`
- `Email__Port`
- `Email__User`
- `Email__Password`
- `Email__From`
- `Email__AppBaseUrl`
- `Rag__ApiKey`
- `Rag__Provider`
- `Llm__GeminiApiKey`
- `Llm__NvidiaApiKey`
- `ASPNETCORE_ENVIRONMENT`

`ConsultaCnpj` e `Clima` já têm URL padrão no código. Preencha `Clima__ApiKey` se você trocar o provedor de clima.

## Onde o projeto está publicado

O site que responde com o título “Preço Certo” é [Preço Certo na Vercel](https://preco-certo-saqua.vercel.app). A Vercel usa a pasta `Mobile/`, o comando `npm run build:web` e a saída `dist`, como em [configuração da Vercel](Mobile/vercel.json). O endereço `preco-certo-tan.vercel.app` devolve `DEPLOYMENT_NOT_FOUND`.

A API sobe no Railway pela imagem do [Dockerfile](Dockerfile) e pelo [railway.toml](railway.toml). O health check configurado lá é `/health`. O banco publicado é o PostgreSQL do Supabase.

O arquivo `render.yaml` ainda descreve um serviço no Render. O último commit desse arquivo é `1295ce78`, de 2026-06-09. O [estado do projeto](ESTADO.md) descreve Railway para a API e Vercel para o site. Não trate o blueprint do Render como o passo atual.

## Como rodar os testes

Os testes do app não usam banco:

```bash
cd Mobile
node --test src/utils/__tests__/*.mjs
```

A suíte da API:

```bash
cd BackEnd
dotnet test Pc.Servico.Tests/Pc.Servico.Tests.csproj -c Release
```

No SDK 8, `dotnet test PrecoCerto.slnx` para com o erro MSB4068. Use o `.csproj` acima. O teste `AnalisarAsync_mercado_perto_de_loja_ancora_busca_lojas_proximas_e_nao_repete_camera` falha nessa suíte: a lista de resultados volta vazia. Não há workflow em `.github/workflows`, então esta página não traz selo de CI.

## Como contribuir

Abra uma issue ou um pull request contra `Main`. O repositório não inclui `CONTRIBUTING.md` nem `LICENSE`. O código público está em [ThiagoSanTh/PrecoCerto](https://github.com/ThiagoSanTh/PrecoCerto).
