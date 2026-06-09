<div align="center">

<h1>🛒 Preço Certo</h1>

<p>
  <strong>Comparador de preços local com foco em geolocalização, economia e fortalecimento do comércio regional.</strong>
</p>

<p>
  O <strong>Preço Certo</strong> é um projeto desenvolvido para permitir que usuários encontrem produtos e serviços com melhores preços em lojas próximas, comparando valores, promoções e disponibilidade de forma simples, rápida e eficiente.
</p>

</div>

<br>

<h2>🎯 Objetivo do Projeto</h2>

<p>
  O principal objetivo do <strong>Preço Certo</strong> é criar uma solução capaz de conectar consumidores e comércios locais através de uma plataforma inteligente de comparação de preços.
</p>

<ul>
  <li>Encontrar produtos mais baratos na região</li>
  <li>Comparar preços entre estabelecimentos locais</li>
  <li>Exibir promoções e disponibilidade</li>
  <li>Utilizar geolocalização</li>
  <li>Fortalecer o comércio local</li>
</ul>

<br>

<h2>🧱 Estrutura do Projeto</h2>

<pre>
PrecoCerto/
├── BackEnd/                    → API .NET 8 (camadas)
│   ├── Pc.Dominio/             → Entidades e enums
│   ├── Pc.Infraestrutura/      → EF Core + migrations
│   ├── Pc.Repositorio/         → Acesso a dados
│   ├── Pc.Servico/             → Regras de negócio
│   ├── Pc.WebApi/              → Controllers e endpoints
│   ├── scripts/                → SQL (Supabase / migrations manuais)
│   ├── docs/                   → Documentação do backend
│   ├── Dockerfile
│   └── PrecoCerto.slnx         → Solution .NET
├── Mobile/                     → App Expo (React Native)
│   └── src/
│       ├── components/         → UI reutilizável
│       ├── context/            → Estado global (auth)
│       ├── navigation/         → Rotas e tabs
│       ├── screens/            → Telas por domínio
│       │   ├── auth/
│       │   ├── catalog/
│       │   ├── store/
│       │   └── user/
│       ├── services/           → API, Supabase, GPS
│       ├── theme/              → Cores e estilos globais
│       └── utils/
├── docs/                       → Documentação geral
├── render.yaml                 → Deploy da API (Render)
└── README.md
</pre>

<br>

<h2>🧩 Entidades Principais</h2>

<ul>
  <li>Usuario</li>
  <li>Cliente</li>
  <li>Lojista</li>
  <li>Loja</li>
  <li>Endereco</li>
  <li>Produto</li>
  <li>Oferta</li>
  <li>Favorito</li>
  <li>HistoricoPesquisa</li>
  <li>Avaliacao</li>
  <li>Cupom</li>
  <li>Promocao</li>
</ul>

<br>

<h2>⚙️ Como Executar o Projeto</h2>

<h3>Pré-requisitos</h3>

<ul>
  <li>.NET SDK 8.0+</li>
  <li>PostgreSQL (local ou Supabase)</li>
  <li>Node.js 18+ e npm</li>
  <li>Expo CLI (<code>npx expo</code>)</li>
</ul>

<h3>Backend (API .NET)</h3>

```bash
cd BackEnd

# 1. Restaurar dependências
dotnet restore Pc.WebApi/Pc.WebApi.csproj

# 2. Configurar segredos de desenvolvimento (User Secrets)
cd Pc.WebApi
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=...;Database=...;Username=...;Password=..."
dotnet user-secrets set "Jwt:Secret" "uma-chave-secreta-com-no-minimo-32-caracteres"

# 3. Aplicar migrations (também são aplicadas automaticamente no startup)
dotnet ef database update --project ../Pc.Infraestrutura --startup-project .

# 4. Rodar a API (Swagger em /swagger no ambiente Development)
dotnet run
```

A API sobe por padrão em <code>http://localhost:5132</code> em desenvolvimento.

<h3>Mobile (Expo / React Native)</h3>

```bash
cd Mobile

# 1. Instalar dependências
npm install

# 2. Apontar o app para a API (crie um arquivo .env ou exporte a variável)
#    EXPO_PUBLIC_API_URL=http://localhost:5132

# 3. Iniciar o Expo
npm run start      # ou: npm run web / npm run android / npm run ios
```

<br>

<h2>🔐 Variáveis de Ambiente</h2>

<table>
  <thead>
    <tr>
      <th>Variável</th>
      <th>Onde</th>
      <th>Descrição</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td><code>ConnectionStrings__DefaultConnection</code></td>
      <td>Backend</td>
      <td>String de conexão PostgreSQL/Supabase. Obrigatória em produção.</td>
    </tr>
    <tr>
      <td><code>Jwt__Secret</code></td>
      <td>Backend</td>
      <td>Chave de assinatura do JWT (mínimo 32 caracteres). Obrigatória em produção.</td>
    </tr>
    <tr>
      <td><code>Jwt__Issuer</code> / <code>Jwt__Audience</code></td>
      <td>Backend</td>
      <td>Emissor e audiência do token (ver <code>appsettings.json</code>).</td>
    </tr>
    <tr>
      <td><code>Cors__AllowedOrigins__0</code></td>
      <td>Backend</td>
      <td>Origens liberadas no CORS (array). Domínios <code>*.vercel.app</code> já são aceitos.</td>
    </tr>
    <tr>
      <td><code>Email__*</code></td>
      <td>Backend</td>
      <td>SMTP para confirmação de e-mail (Host, Port, User, Password, From). Ver seção SMTP.</td>
    </tr>
    <tr>
      <td><code>EXPO_PUBLIC_API_URL</code></td>
      <td>Mobile</td>
      <td>URL base da API (sem <code>/api</code>; o app adiciona automaticamente).</td>
    </tr>
  </tbody>
</table>

<p>
  Observação: em <code>Development</code> a API usa valores de fallback (connection string opcional e um <code>Jwt:Secret</code> de desenvolvimento), mas em <code>Production</code> as variáveis acima são obrigatórias.
</p>

<br>

<h2>☁️ Deploy</h2>

<ul>
  <li><strong>API (Render):</strong> o arquivo <code>render.yaml</code> na raiz define o serviço web .NET, com <code>healthCheckPath: /api/health</code>. Defina os segredos (<code>ConnectionStrings__DefaultConnection</code>, <code>Jwt__Secret</code>, <code>Cors__AllowedOrigins__0</code> e, se for usar e-mail, <code>Email__*</code>) no painel do Render. As migrations são aplicadas automaticamente no startup.</li>
  <li><strong>API (Docker/Railway):</strong> use o <code>BackEnd/Dockerfile</code> (multi-stage, com <code>entrypoint.sh</code> e <code>HEALTHCHECK</code> em <code>/api/health</code>). O container escuta na porta da env <code>PORT</code> (padrão 8080).</li>
  <li><strong>Mobile web (Vercel):</strong> <code>Mobile/vercel.json</code> faz o build com <code>expo export</code>. Configure <code>EXPO_PUBLIC_API_URL</code> apontando para a URL pública da API.</li>
</ul>

<br>

<h2>🚀 Funcionalidades do MVP</h2>

<table>
  <thead>
    <tr>
      <th>Funcionalidade</th>
      <th>Descrição</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td>Cadastro de usuários</td>
      <td>Registro de clientes e lojistas</td>
    </tr>
    <tr>
      <td>Cadastro de lojas</td>
      <td>Informações + localização</td>
    </tr>
    <tr>
      <td>Cadastro de produtos</td>
      <td>Organização dos itens</td>
    </tr>
    <tr>
      <td>Cadastro de ofertas</td>
      <td>Produto + loja + preço</td>
    </tr>
    <tr>
      <td>Busca</td>
      <td>Pesquisa por nome e filtros</td>
    </tr>
    <tr>
      <td>Comparação de preços</td>
      <td>Ranking de ofertas</td>
    </tr>
    <tr>
      <td>Geolocalização</td>
      <td>Prioriza lojas próximas</td>
    </tr>
  </tbody>
</table>

<br>

<h2>📋 Checklist de Desenvolvimento</h2>

<table>
  <thead>
    <tr>
      <th>Status</th>
      <th>Categoria</th>
      <th>Tarefa</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td>✅</td>
      <td>Infraestrutura</td>
      <td>AppDbContext criado</td>
    </tr>
    <tr>
      <td>⚠️</td>
      <td>Infraestrutura</td>
      <td>Configuração PostgreSQL (em ajuste)</td>
    </tr>
    <tr>
      <td>⚠️</td>
      <td>Infraestrutura</td>
      <td>Migrations (em validação)</td>
    </tr>
    <tr>
      <td>⬜</td>
      <td>Repositório</td>
      <td>Interfaces de repositório</td>
    </tr>
    <tr>
      <td>⬜</td>
      <td>Repositório</td>
      <td>Implementação de repositórios</td>
    </tr>
    <tr>
      <td>⬜</td>
      <td>Serviço</td>
      <td>Serviços principais</td>
    </tr>
    <tr>
      <td>⬜</td>
      <td>Serviço</td>
      <td>BuscaService</td>
    </tr>
    <tr>
      <td>⬜</td>
      <td>Serviço</td>
      <td>ComparacaoService</td>
    </tr>
    <tr>
      <td>⚠️</td>
      <td>API</td>
      <td>Controllers iniciais criados</td>
    </tr>
    <tr>
      <td>⬜</td>
      <td>API</td>
      <td>AuthController</td>
    </tr>
    <tr>
      <td>⬜</td>
      <td>Segurança</td>
      <td>JWT</td>
    </tr>
    <tr>
      <td>⬜</td>
      <td>Performance</td>
      <td>Paginação</td>
    </tr>
    <tr>
      <td>⬜</td>
      <td>Extras</td>
      <td>Favoritos / Avaliações</td>
    </tr>
  </tbody>
</table>

<br>

<h2>📌 Status Atual</h2>

<ul>
  <li>✅ Domínio estruturado e entidades criadas</li>
  <li>✅ Projeto organizado em camadas (Repository + Service Pattern)</li>
  <li>✅ API rodando com Swagger (documentação via XML comments)</li>
  <li>✅ Integração com PostgreSQL/Supabase via EF Core + migrations</li>
  <li>✅ Controllers e regras de negócio implementados</li>
  <li>✅ Autenticação JWT + hashing de senha (BCrypt) + rate limiting no login</li>
  <li>✅ Geolocalização, favoritos, histórico e avaliações</li>
  <li>✅ App mobile (Expo) integrado à API</li>
  <li>⚠️ Confirmação de e-mail (SMTP), carrinho e dark mode em evolução</li>
</ul>

<br>

<h2>🔮 Próximos Passos</h2>

<ol>
  <li>Estabilizar conexão com PostgreSQL</li>
  <li>Finalizar migrations</li>
  <li>Implementar repositórios</li>
  <li>Criar serviços</li>
  <li>Finalizar controllers</li>
  <li>Integrar com React Native (Expo)</li>
</ol>

<br>

<h2>👨‍💻 Autor</h2>

<p>
  <strong>Thiago Almeida Sant’Ana</strong><br>
  <strong>Gabriel Almeida</strong><br>
  Engenharia de Software<br>
</p>

<br>

<h2>📎 Observação Final</h2>

<p>
  O projeto está em evolução contínua. O foco atual está na consolidação do backend para permitir integração com o aplicativo mobile.
</p>
