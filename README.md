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
# Backend
cd BackEnd/Pc.WebApi
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=...;..."
dotnet user-secrets set "Jwt:Secret" "chave-com-pelo-menos-32-caracteres"
dotnet run
# → http://localhost:5132/swagger

# Mobile
cd Mobile
npm install
# EXPO_PUBLIC_API_URL=http://localhost:5132
npm start
```

Segredos, connection strings e deploy: [`BackEnd/docs/SECRETS.md`](BackEnd/docs/SECRETS.md)

Estrutura detalhada do código: [`.cursor/PROJETO.md`](.cursor/PROJETO.md)

## Autores

**Thiago Almeida Sant'Ana** · **Gabriel Almeida** — Engenharia de Software
