# BENCHMARK DO PREÇO CERTO

## Rodada 3 — após otimização (2026-08-24)

Código alterado: Feed 2 SQL (COUNT sem Include + página com melhor oferta no SQL); `/media` AVG+COUNT numa query; Count paginado sem Include. Contrato JSON do Feed inalterado.

**Não comparar HTTP isolado 10 VU entre 20/08 e 24/08 como regressão da query.** Controle `GET /api/Lojas` (ainda 2 SQL, Join Endereco igual) subiu sqlMs 62→105 ms no mesmo dia (+43 ms). Feed isolado subiu 69→113 ms (+43 ms). O delta é RTT do pooler, não o JOIN.

Prova da mudança (instrumentação, mesmo código de métricas):

| Sinal | Antes (20/08) | Depois (24/08) |
|-------|---------------|----------------|
| Feed sqlQ | 3,0 | **2,0** |
| Media sqlQ (mixed 10 VU) | 2,0 | **1,0** |
| Feed vs Lojas sqlMs (mesmo dia) | 69 vs 62 (+7 ms) | 113 vs 105 (+8 ms) |

k6 medido 24/08 (think 50 ms, limiter off):

| Teste | RPS | P50 | P95 | Erro | CPU API | RAM API |
|-------|-----|-----|-----|------|---------|---------|
| Feed 10 VU 20s | 59,88 | 140 ms | 158 ms | 0% | 2,4% | 277 MB |
| Feed termo=arroz 10 VU | 58,70 | 143 ms | 158 ms | 0% | 1,5% | 311 MB |
| Lojas 10 VU | 63,25 | 134 ms | 157 ms | 0% | 1,5% | 315 MB |
| Mixed 10 VU 30s | 97,10 | 37,6 ms | 142 ms | 0% | 2,5% | 322 MB |
| Mixed 25 VU 30s | **213,19** | 35,8 ms | **161 ms** | **2,12%** | 6,0% | 344 MB |

Mixed 25 VU vs 20/08 (mesmo script; RTT diferente entre dias — tratar com cautela): RPS 125→213 (+70,9%), P95 234→161 (−31,1%), erro 3,38%→2,12%. Arquivos: `results/pos-*.json`.

Não reexecutado: mixed 50/100 VU, health 1500 RPS, login, SignalR.

---

Data: 2026-08-20  
Modo: diagnóstico apenas. Sem Rate Limit novo. Sem otimização de queries. Sem mudança de arquitetura.

**Rodada 2 (fonte de verdade do catálogo):** Postgres connected via pooler Supabase `sa-east-1`. k6 mixed 1→50 VU. Parado em 50 VU (timeouts). Não subi a 100.

**Rodada 1:** banco unreachable. Só health 1500 RPS. Conservada abaixo como histórico HTTP puro.

Dataset real (EXPLAIN `actual rows`, não `n_live_tup` desatualizado): **7 produtos, 2 lojas, 3 ofertas**. Seq Scan em tudo é o plano certo nessa escala — custo de CPU do planner irrelevante. Latência = RTT até o pooler + N round-trips EF.

Pool desta rodada: `Maximum Pool Size=20` na connection string de Development. Postgres `max_connections=60`.

---

## Rodada 2 — capacidade do catálogo (medida)

### Mixed GET (Feed 40%, busca, Lojas, mapa, avaliações, health), 30 s, think 50 ms

| VU | RPS | Média | P50 | P95 | Máx | Erros |
|----|-----|-------|-----|-----|-----|-------|
| 1 | 7,18 | 88,8 ms | 78,6 ms | 169 ms | 228 ms | 0% |
| 5 | 49,53 | 49,8 ms | 48,4 ms | 110 ms | 244 ms | 0% |
| 10 | **111,29** | 39,0 ms | 35,0 ms | **60,6 ms** | 248 ms | **0%** |
| 25 | 124,74 | 134 ms | 48,8 ms | 234 ms | 10001 ms | **3,38%** |
| 50 | 38,66 | 1197 ms | 717 ms | **5877 ms** | 10001 ms | 3,70% |
| 100 | não executado | — | — | — | — | STOP |

RPS 1 VU limitado pelo sleep 50 ms + ~90 ms de DB, não pela API.

**RPS máximo observado (catálogo misto):** 125 RPS @ 25 VU, já com timeout.  
**RPS seguro:** **~100 RPS / 10 VU** — 0% erro, P95 61 ms.  
**Ponto de degradação:** **25 VU** — P95 234 ms, máx 10 s, 3,4% fail (timeout 10 s).  
**Ponto de falha:** **50 VU** — RPS cai para 39, P95 5,9 s, pool espera. CPU API **cai** (1,3%). SQL médio do Feed continua ~50 ms. Gargalo = **espera de conexão Npgsql/pooler**, não query.

Health no mixed 10 VU: média 0,5 ms. Prova: ASP.NET não é o teto.

### Endpoints isolados, 10 VU, 20 s, 0% erro

| Endpoint | RPS | Média | P95 | SQL/req | SQL ms | App ms |
|----------|-----|-------|-----|---------|--------|--------|
| GET /api/Feed | 81,7 | 71,6 | 149 | **3,0** | 69,4 | 1,9 |
| GET /api/Feed?termo=arroz | 78,2 | 77,2 | 156 | 2,0 | 75,5 | 1,4 |
| GET /api/Lojas | 87,6 | 63,4 | 142 | 2,0 | 61,8 | 1,2 |
| GET /api/Lojas/mapa | 88,8 | 61,9 | 142 | **1,0** | 60,3 | 1,2 |

Controller+Service+JSON ≈ **2 ms**. Quase 100% do tempo é **PostgreSQL/rede**. Feed piora porque são **3 round-trips** (COUNT + página + ofertas).

### Camada (10 VU mixed, Feed)

REQUEST ~49 ms → Controller/Service ~2 ms → EF/SQL ~47 ms → Postgres (Seq Scan 7 rows, EXPLAIN 0,05 ms) → RTT pooler preenche o resto.

EXPLAIN Feed: Seq Scan + Sort, Execution Time **0,052 ms**. O banco localmente é instantâneo. Os ~47 ms são **ida e volta sa-east-1 × N queries**.

Mapa: Nested Loop Seq Scan Lojas (2 rows) + Index PK Enderecos. Execution 0,13 ms. HTTP 20 ms @ mixed. Lista vazia neste lat/lng (lojas fora de SP) — ainda paga o SQL.

Índices presentes: PK/FK, trgm `NomeProduto` e `NomeFantasia`. `Usuarios` login: Seq Scan, `LOWER(email)` (9 rows filtradas). `pg_stat_statements` nesta instância está cheio de queries internas do Supabase, não das nossas.

Não testado de propósito: POST/DELETE, login em carga, CNPJ externo, SignalR, 100 VU.

---

## 1. Ambiente

| Item | Valor |
|------|--------|
| .NET | 8.0.424 (TFM `net8.0`) |
| ASP.NET Core | 8.0 (JWT Bearer 8.0.5) |
| EF Core | 8.0.5 |
| Npgsql EF | 8.0.4 |
| PostgreSQL | alvo remoto (Supabase, pela connection string da API). **Não foi possível conectar.** Versão do servidor: não medida. |
| Infraestrutura | API local `http://0.0.0.0:5132` (Development). Banco remoto. Sem Postgres local na porta 5432. |
| CPU | AMD Ryzen 5 5600H, 12 threads lógicos |
| RAM host | 5769 MB; disponível durante os testes ~900–1500 MB |
| Processo API (1500 RPS em `/api/health`) | CPU média 11,8% da máquina, RAM ~329 MB, 39 threads |
| Pool Npgsql | padrão (MaxPoolSize 100). Não medido sob carga real de banco. |
| Rate Limit já existente | `UseRateLimiter` global por IP: login 10/min, catálogo 120/min, resto 300/min. **Desligado só com `PRECOCERTO_BENCHMARK=1`.** |

Arquitetura: WebApi → Servico → Repositorio → EF Core → Npgsql → PostgreSQL. Camadas claras. SignalR em `/hubs/chat`. Serilog. Response compression Brotli/Gzip. MemoryCache (CNPJ). HostedService de migration no startup.

Auth: JWT Bearer. Integrações externas: SMTP, OpenCNPJ + BrasilAPI (CNPJ), SignalR.

---

## 2. Capacidade atual

### HTTP puro (`GET /api/health`, sem banco)

Executor k6 `constant-arrival-rate`, 15 s (12 s em 800/1500), 0% erro, 0 iterações dropped:

| Alvo RPS | RPS observado | Média | P50 | P95 | Máx | Erros |
|----------|---------------|-------|-----|-----|-----|-------|
| 50 | 50,0 | 1,04 ms | 0,95 ms | 1,39 ms | 6,16 ms | 0% |
| 100 | 100,1 | 0,95 ms | 0,88 ms | 1,37 ms | 8,58 ms | 0% |
| 200 | 200,0 | 0,77 ms | 0,66 ms | 1,26 ms | 7,23 ms | 0% |
| 400 | 400,0 | 0,64 ms | 0,58 ms | 1,01 ms | 5,96 ms | 0% |
| 800 | 800,0 | 0,51 ms | 0,46 ms | 0,73 ms | 7,66 ms | 0% |
| 1500 | 1499,9 | 0,57 ms | 0,47 ms | 0,84 ms | 12,55 ms | 0% |

Instrumentação in-process em 1500 RPS: P50 0,28 ms, P95 0,41 ms, P99 1,02 ms.

**RPS máximo observado (HTTP/health):** 1500 RPS.  
**RPS seguro (HTTP/health):** ≥ 1500 RPS neste host — saturação **não atingida**. Não subi acima de 1500 por RAM do host (~900 MB livres).  
**Ponto de degradação (HTTP/health):** não observado.  
**Ponto de falha (HTTP/health):** não observado.

### Catálogo / EF / PostgreSQL

k6 mixed, 1 VU, 15 s (Feed 40%, busca 15%, Lojas 15%, mapa 12%, resto health/avaliações):

- requests: 71
- RPS: 3,94 (limitado por falha de conexão ~100–5000 ms, não pela API)
- média: 203 ms
- P95: 265 ms
- máx: 5196 ms
- erros HTTP (k6): **90,14%** (64/71)
- sucessos: 7 (compatível com fatia de `/api/health`)

Instrumentação: 0 queries SQL. Tempo = tentativa de conexão Npgsql + `EnableRetryOnFailure(3)`.

**RPS máximo observado (catálogo):** não aplicável — API não completou queries.  
**RPS seguro (catálogo):** não medido.  
**Ponto de degradação (catálogo):** não medido.  
**Ponto de falha observado nesta máquina:** 1 VU já produz ~90% de erro porque o banco não responde.

1 VU com think time 50 ms em `/api/health`: 19,3 RPS — teto do cliente (sleep), **não** da API.

---

## 3. Resumo dos endpoints (medido)

ENDPOINT | RPS | MÉDIA | P95 | P99 | ERROS | CLASSIFICAÇÃO
--- | --- | --- | --- | --- | --- | ---
GET /api/health | 1500 | 0,57 ms (k6) / 0,31 ms (API) | 0,84 / 0,41 ms | 1,02 ms (API) | 0% | leve; teto HTTP não achado
GET /api/Feed | n/d | 272 ms | 359 ms | 5193 ms | ~100% HTTP 500 (k6) | conexão DB, 0 SQL
GET /api/Lojas | n/d | 161 ms | 252 ms | 252 ms | ~100% HTTP 500 | conexão DB, 0 SQL
GET /api/Lojas/mapa | n/d | 137 ms | 309 ms | 309 ms | ~100% HTTP 500 | conexão DB, 0 SQL
GET /api/HistoricoPesquisa/sugestoes | n/d | 179 ms | 254 ms | 254 ms | ~100% HTTP 500 | conexão DB, 0 SQL
GET /api/Consultas/cnpj/{inválido} | n/d | 6,3 ms | — | — | 400 validação | sem rede externa
GET /api/Produtos | n/d | ~180–300 ms | — | — | 500 | controller existe; DB down
GET /api/Ofertas | n/d | ~295 ms | — | — | 500 | idem
GET /api/health/ready | n/d | 187–419 ms | — | — | 503 | ping de banco falhou

Classificação de custo abaixo é **hipótese de código**, não ranking de carga real.

---

## 4. Top 10 endpoints mais pesados (hipótese de código + evidência)

Ordem de risco quando o banco voltar. Sem ranking de RPS de catálogo.

1. **GET /api/Lojas/mapa** — carrega todas as lojas com lat/lng e filtra Haversine **em memória** (`LojaRepositorio.ListarPorProximidadeAsync`). Sem índice geo. Sem paginação.
2. **GET /api/Feed?termo=** — `Count` + página com `Include(Loja)` + ILike em Nome/Marca/Descrição + 2ª query de ofertas (`Include Loja`) + melhor preço em memória.
3. **GET /api/Feed** — mesmo padrão sem ILike; `CountAsync` sobre query com `Include`.
4. **GET /api/Produtos/Buscar** e listagens de produto com `Include(Loja).ThenInclude(Endereco)` no detalhe.
5. **GET /api/Ofertas** e **GET /api/Ofertas/produto/{id}** — Include Produto+Loja; por produto **sem paginação**.
6. **GET /api/Avaliacoes/loja/{id}** — lista inteira + Include Cliente; **sem paginação**. `/media` dispara 2 idas: `ToList`+Average e de novo `ObterPorLoja` só para Count.
7. **GET /api/HistoricoPesquisa/loja/{id}/relatorio** — carrega histórico da loja (join produto) e agrega em memória. `LimparHistorico` é N+1 deletes.
8. **GET /api/Clientes/proximidade/buscar** — `ToList` de **todos** os usuários ativos, Haversine em memória (Admin).
9. **GET /api/Conversas** + não-lidas — Include + `GroupBy` com Include Conversa.
10. **POST /api/Auth/login** — BCrypt workFactor 12: **229 ms hash / 231 ms verify** (microbench Release nesta CPU). ~4 verify/s por core.

Mobile chama `GET /Lojas?pageSize=100` (teto da paginação).

---

## 5. Gargalos encontrados

### GARGALO 1 — PostgreSQL inacessível neste host
**Severidade:** CRÍTICO (bloqueia o benchmark de catálogo)  
**Componente:** rede / DNS / PostgreSQL remoto (não a CPU da API)  
**Evidência:** ready=503; Feed/Lojas/mapa/Produtos/Ofertas=500; 0 SQL no interceptor; k6 fail 90% em 1 VU; log Npgsql `Failed to connect` / `Network is unreachable` (IPv6) e, com IPv4 forçado, still unreachable. Exceção `DivideByZeroException` dentro de `NpgsqlConnector.ConnectAsync` em falha de connect (efeito colateral Npgsql, não regra de negócio).  
**Impacto:** catálogo inutilizável daqui; latência 100 ms–5 s só de retry.  
**Endpoint:** todos que abrem DbContext.  
**Causa provável:** rota até o host do banco (IPv6 sem rota; IPv4 também não abriu TCP 5432).  
**Como confirmar:** `GET /api/health/ready` = 200 e um `GET /api/Feed` 200. Depois repetir k6.  
**Possível solução:** corrigir conectividade/IPv4/firewall/pooler Supabase. **Não implementado.**

### GARGALO 2 — Pipeline HTTP da API **não** é o teto atual
**Severidade:** BAIXO (para health); informativo  
**Componente:** API ASP.NET / Kestrel  
**Evidência:** 1500 RPS health, P95 < 1 ms, CPU API ~12%, RAM ~330 MB.  
**Impacto:** folga grande no host local para JSON mínimo.  
**Causa provável:** `/api/health` não toca EF.  
**Como confirmar:** repetir arrival-rate com Feed 200 quando o banco responder.

### GARGALO 3 — Geo em memória (código; não cronometrado no SQL)
**Severidade:** ALTO (quando o banco voltar e o volume de lojas crescer)  
**Componente:** Repository / CPU / ausência de PostGIS ou bounding-box SQL  
**Evidência:** `ListarPorProximidadeAsync` faz `ToListAsync` de todas as lojas com coordenada e filtra com `GeoHelper`. Idem `ClienteRepositorio.ObterPorProximidadeAsync` para **todos** os usuários.  
**Impacto:** O(N) memória+CPU por request de mapa.  
**Endpoint:** `GET /api/Lojas/mapa`, `GET /api/Clientes/proximidade/buscar`.  
**Como confirmar:** EXPLAIN + k6 só em `/Lojas/mapa` com N lojas conhecido.  
**Possível solução:** filtro SQL por bounding box + índice em lat/lng; PostGIS se volume exigir.

### GARGALO 4 — Feed = 3 round-trips + Include no Count + melhor oferta em memória
**Severidade:** ALTO  
**Componente:** EF Core / Service  
**Evidência de código:** `ListarPorLojaPaginadoAsync` Count+Skip/Take com `Include(Loja)`; `ObterMelhorOfertaPorProdutosAsync` traz todas as ofertas dos IDs e `GroupBy` em memória.  
**SQL desta rodada:** não capturado (0 commands).  
**Possível solução:** Count sem Include; `Select` DTO; `DISTINCT ON` / subquery de min(preco) no SQL.

### GARGALO 5 — BCrypt workFactor 12 no login
**Severidade:** MÉDIO (CPU), já mitigado em parte pelo limiter 10/min  
**Componente:** API / CPU  
**Evidência:** microbench 231 ms/verify. 12 cores ≈ teto teórico ~50 verify/s se só login. Não load-testei login (escreve `UltimoLogin`).  
**Endpoint:** `POST /api/Auth/login`, `POST /api/Clientes/login`, `POST /api/Admins/login`.

### GARGALO 6 — Rate Limit já existe e mascara capacidade
**Severidade:** MÉDIO para medição futura  
**Componente:** middleware ASP.NET RateLimiter  
**Evidência:** `Program.cs` GlobalLimiter 120 req/min/IP no catálogo (= 2 RPS/IP). Sem `PRECOCERTO_BENCHMARK=1` um k6 de um IP bate 429 muito antes do Kestrel.  
**Impacto:** capacidade “de produção com RL” ≠ capacidade real da API+DB.

Não foi possível separar Controller vs Service vs EF vs Postgres no catálogo: EF não executou. Residual medido no health = quase 100% ASP.NET (0 SQL).

---

## 6. PostgreSQL

Não foi possível medir índices ao vivo, `pg_stat_statements`, `EXPLAIN ANALYZE`, nem `n_live_tup`.

Índices **no modelo/migrations** (código):

- FK: Produtos.LojaId, Ofertas.ProdutoId/LojaId, Favoritos/Avaliacoes/Historico ClienteId+LojaId+ProdutoId, Conversas (ClienteId,LojaId) unique, Mensagens (ConversaId, EnviadaEm), Lojas.EnderecoId, Lojas.UsuarioId unique, Usuarios.LojaVinculadaId
- Migration `OtimizacaoPerformanceIndices`: `pg_trgm` GIN em `Produtos.NomeProduto` e `Lojas.NomeFantasia`

Índices **ausentes no modelo** (hipótese, sem EXPLAIN):

- `Produtos.Marca`, `Produtos.Descricao` — ILike no Feed/busca; trgm só no nome
- `Produtos.Categoria` (filtro do feed)
- `Ofertas (ProdutoId, Disponivel, Preco)` — “melhor oferta”
- `Enderecos (Latitude, Longitude)` — mapa
- `Usuarios.Email` — login usa `Email.ToLower() == email.ToLower()` (**não sargable**; impede índice)
- `HistoricosPesquisa.TermoPesquisa` — ILike sugestões
- `Avaliacoes.LojaId` já tem índice FK; média ainda materializa lista

Sequential Scan: **não confirmado** (EXPLAIN não rodou).

---

## 7. Entity Framework

### N+1 (código)
- `HistoricoPesquisaServico.LimparHistoricoAsync`: lista + `RemoverAsync` por id.
- Mapa/proximidade: 1 query grande, não N+1 clássico.
- Feed ofertas: 1 query extra (não N+1 por item), mas carrega todas as ofertas dos produtos da página.

### Include / ThenInclude
- Produto detalhe: `Include(Loja).ThenInclude(Endereco)`
- Oferta listagens: Produto + Loja
- Favorito: Produto + Loja
- Avaliação: Cliente ou Loja
- Conversa: Cliente/Loja; mensagens não-lidas `Include(Conversa)` até em `CountAsync`

### Sem paginação
- `GET /api/Avaliacoes/loja/{id}` e `/cliente/{id}`
- `GET /api/Ofertas/produto/{id}`
- `GET /api/HistoricoPesquisa/cliente/{id}`
- `GET /api/Lojas/mapa` (lista filtrada, limite 500 **depois** do load)
- `GET /api/Conversas`
- `GET /api/PreferenciasCliente/cliente/{id}`
- `ListarPorLojaAsync` / `BuscarPorNomeAsync` ainda existem (Take 50 na busca não paginada)

### Sem AsNoTracking (leituras)
- `AvaliacaoRepositorio` listagens
- `HistoricoPesquisaRepositorio` quase tudo
- `ConversaRepositorio` listagens (exceto mensagens)
- `OfertaRepositorio.ObterPorIdAsync`
- `ClienteRepositorio` / `AdminRepositorio` (login precisa tracking pontual; listagens não)
- `Repositorio.ListarAsync` genérico

### LINQ caro / em memória
- Haversine após `ToList`
- Melhor oferta: `GroupBy` + `OrderBy Preco` em memória
- Média de avaliação: `ToList` + `Average` em vez de `AverageAsync`
- `ObterQuantidadeAvaliacoesAsync` recarrega a lista com Include
- Relatório de loja: GroupBy em memória
- Sugestões: GroupBy no controller após 20 rows
- `Email.ToLower() == email.ToLower()`
- `CountAsync` com `Include` no feed

### ToList prematuro
- Ofertas para “melhor preço” da página
- Avaliações para média
- Lojas/usuários para geo

SQL gerado nesta rodada: **nenhum.** Interceptor não viu `CommandExecuted`.

---

## 8. Infraestrutura

| Recurso | Medido |
|---------|--------|
| CPU API @ 1500 RPS health | média 11,8% / máx 12,0% da máquina |
| RAM API | ~203 MB idle → ~329 MB @ 1500 RPS health |
| Rede localhost health | k6 P95 0,84 ms vs API P95 0,41 ms (delta cliente/stack) |
| Conexões DB | falha ao abrir; pool não saturado porque não houve sessão |
| Banco | unreachable |
| Host RAM | apertada (5,7 GB total); limite prático do teste |

---

## 9. Ponto de saturação

- **Kestrel + `/api/health`:** não saturado até 1500 RPS. Latência até caiu com mais RPS (warmup).
- **Catálogo:** saturado na prática em **1 VU** nesta máquina porque **não há banco**. Isso não é saturação de query; é falha de conectividade.
- Degradação típica (P95 sobe + RPS cai + 5xx) **em SQL** não foi vista.

---

## 10. Rate Limit recomendado

Limiter **já está no código**. Sem RPS de catálogo medido, **não calculei limites novos a partir do health 1500**. 1500 RPS de `{status:ok}` não autoriza 1500 RPS de Feed.

Com os dados reais desta rodada + custo de código + bcrypt:

| Escopo | Proposta | Base |
|--------|----------|------|
| Global / IP (API geral) | manter 300/min até haver bench de DB | já existe; HTTP puro aguenta muito mais, mas DB é o risco |
| Catálogo GET (Feed, Produtos, Lojas, Ofertas) | **manter 120/min/IP** até repetir k6 com ready=200 | sem evidência para afrouxar |
| `GET /api/Lojas/mapa` | **mais baixo que o catálogo** quando houver dado: começar 30–60/min/IP | O(N) memória; mobile cache 60 s |
| `GET /api/Feed?termo=` e `POST /Produtos/Buscar` | mais baixo que listagem: 30–60/min/IP (hipótese) | ILike 3 colunas + Count |
| Login | **manter 10/min/IP** | bcrypt 231 ms; já existe |
| `GET /api/Consultas/cnpj/*` | **limite próprio 10–20/min/IP** | serviço externo; hoje só cai no global 300 |
| `POST` escritas | manter no global; não load-testadas | risco de write |
| `/api/health` | continuar sem limit | já `DisableRateLimiting` |
| QueueLimit | 0 (já está) até existir bench de fila | fila esconde latência |
| ConcurrencyLimiter | não introduzir agora | sem medição de pool/DB |

Não usar 1500 RPS como teto de produto. Recalcular depois de:

1. ready=200  
2. k6 Feed/mapa 1→5→10→25 VU  
3. EXPLAIN das queries do feed e do mapa  

---

## 11. Prioridade de correções

Não implementadas.

🔥 **CRÍTICO** — Conectividade PostgreSQL neste host (IPv4/IPv6/firewall/pooler). Sem isso não há capacidade de catálogo.  
🔥 **CRÍTICO** (quando DB voltar) — repetir este benchmark; os RPS de Feed/mapa ainda não existem.  
🟠 **ALTO** — `GET /api/Lojas/mapa` e proximidade de clientes: filtro SQL, não `ToList` + Haversine.  
🟠 **ALTO** — Feed: Count sem Include; melhor oferta no SQL; ILike só em colunas indexadas.  
🟠 **ALTO** — Login `Email.ToLower()` impede índice.  
🟡 **MÉDIO** — Avaliações/histórico/ofertas-por-produto sem paginação; média via `AverageAsync`/`CountAsync`.  
🟡 **MÉDIO** — Relatório de loja e `LimparHistorico` N+1.  
🟡 **MÉDIO** — AsNoTracking nas leituras; `Include` em Count de conversas.  
🟡 **MÉDIO** — Rate limit específico para CNPJ.  
🟢 **BAIXO** — Instrumentação permanente (OpenTelemetry) em vez do middleware de bench.  
🟢 **BAIXO** — `bytesOut=0` no middleware (chunked); `ContentLength` não preenchido.  
🟢 **BAIXO** — Warning `AdminRepositorio.ListarAsync` hide.

---

## 12. Conclusão

**Qual é o principal gargalo do Preço Certo atualmente?**  
**PostgreSQL remoto + round-trips EF**, depois **pool de 20 conexões**. Kestrel não. EXPLAIN Feed = 0,05 ms; HTTP Feed @ 10 VU = 72 ms (3 SQL, 69 ms no banco/rede, 2 ms na API). Em 50 VU o SQL do Feed segue ~50 ms e a API espera conexão (P95 5,9 s).

**Quantas requisições por segundo ele suporta?**  
- Health: **≥ 1500 RPS**.  
- Catálogo misto (7 produtos): **111 RPS @ 10 VU, 0% erro**. Máx 125 RPS @ 25 VU já com 3,4% timeout.

**Qual é uma capacidade segura?**  
**~80–100 RPS** GET catálogo neste dataset. Não extrapolar para catálogo grande.

**Rate Limit realmente é necessário?**  
**Sim, já existe.** 120/min/IP segura abusador. Teto do sistema = pool/DB. ConcurrencyLimiter 15–20 faria mais sentido que subir o 120. Login 10/min mantém.

**Qual deve ser a próxima otimização?**  
Cortar queries do Feed (hoje 3). Não criar índice por Seq Scan de 7 rows. Repetir k6 quando o volume crescer.

---

## Apêndice A — FASE 1 hipótese (antes dos testes)

ENDPOINT | MÉTODO | CONTROLLER | SERVICE | ACESSA BANCO? | COMPLEXIDADE ESTIMADA | POSSÍVEL CUSTO
--- | --- | --- | --- | --- | --- | ---
GET /api/health | GET | Health | — | NÃO | BAIXA | BAIXO
GET /api/health/ready | GET | Health | DbContext | SIM (ping) | BAIXA | BAIXO
GET /api/Feed | GET | Feed | FeedServico | SIM | ALTA | ALTO
GET /api/Produtos | GET | Produtos | ProdutoServico | SIM | MÉDIA | MÉDIO
POST /api/Produtos/Buscar | POST | Produtos | ProdutoServico | SIM | ALTA | ALTO
GET /api/Ofertas | GET | Ofertas | OfertaServico | SIM | MÉDIA | MÉDIO
GET /api/Ofertas/produto/{id} | GET | Ofertas | OfertaServico | SIM | MÉDIA | MÉDIO
GET /api/Lojas | GET | Lojas | LojaServico | SIM | MÉDIA | MÉDIO
GET /api/Lojas/mapa | GET | Lojas | LojaServico | SIM | ALTA | ALTO
POST /api/Lojas/buscar | POST | Lojas | LojaServico | SIM | MÉDIA | MÉDIO
GET /api/Avaliacoes/loja/{id} | GET | Avaliacoes | AvaliacaoServico | SIM | MÉDIA | MÉDIO
GET /api/Avaliacoes/loja/{id}/media | GET | Avaliacoes | AvaliacaoServico | SIM | MÉDIA | MÉDIO
GET /api/HistoricoPesquisa/sugestoes | GET | Historico | HistoricoServico | SIM | MÉDIA | MÉDIO
GET /api/Favoritos/cliente/{id} | GET | Favoritos | FavoritoServico | SIM | MÉDIA | MÉDIO
GET /api/Conversas | GET | Conversas | ConversaServico | SIM | MÉDIA | MÉDIO
POST /api/Auth/login | POST | Auth | Cliente/Admin + BCrypt | SIM | MÉDIA | ALTO (CPU)
GET /api/Consultas/cnpj/{cnpj} | GET | Consultas | ConsultaCnpj | NÃO (HTTP externo + cache) | MÉDIA | ALTO (externo)
GET /api/Clientes/proximidade/buscar | GET | Clientes | ClienteServico | SIM | ALTA | ALTO

---

## Apêndice B — Inventário de endpoints

Público (AllowAnonymous) vs autenticado. Escrita = POST/PUT/DELETE.

Método | Rota | Auth | Banco | Write | Paginação | Include | Geo | Externo
--- | --- | --- | --- | --- | --- | --- | --- | ---
GET | /health | não | não | não | — | — | não | não
GET | /api/health | não | não | não | — | — | não | não
GET | /api/health/ready | não | ping | não | — | — | não | não
GET | /api/Feed | não | sim | não | sim | Loja + ofertas | não | não
GET | /api/Produtos | não | sim | não | sim | Loja | não | não
GET | /api/Produtos/{id} | não | sim | não | — | Loja+Endereco | não | não
POST | /api/Produtos/Buscar | não | sim | não | sim | Loja | não | não
POST/PUT/DELETE | /api/Produtos | Lojista/Vendedor/Admin | sim | sim | — | — | não | não
GET | /api/Ofertas | não | sim | não | sim | Produto+Loja | não | não
GET | /api/Ofertas/{id} | não | sim | não | — | Produto+Loja | não | não
GET | /api/Ofertas/produto/{id} | não | sim | não | **não** | Produto+Loja | não | não
POST/PUT/DELETE | /api/Ofertas | Lojista/Vendedor/Admin | sim | sim | — | — | não | não
GET | /api/Lojas | não | sim | não | sim | Endereco | não | não
GET | /api/Lojas/mapa | não | sim | não | limite 500 pós-filtro | Endereco | **sim** | não
GET | /api/Lojas/{id} | não | sim | não | — | Endereco | não | não
POST | /api/Lojas/buscar | não | sim | não | sim | Endereco | não | não
POST | /api/Lojas | JWT | sim | sim | — | — | não | **CNPJ**
PUT/DELETE | /api/Lojas/{id} | Lojista/Admin | sim | sim | — | — | não | não
GET | /api/Avaliacoes/{id} | implícito | sim | não | — | — | não | não
GET | /api/Avaliacoes/loja/{id} | não | sim | não | **não** | Cliente | não | não
GET | /api/Avaliacoes/loja/{id}/media | não | sim | não | — | Cliente (count) | não | não
GET | /api/Avaliacoes/cliente/{id} | JWT | sim | não | **não** | Loja | não | não
POST/PUT/DELETE | /api/Avaliacoes | Cliente/Admin | sim | sim | — | — | não | não
GET | /api/HistoricoPesquisa/sugestoes | não | sim | não | Take 20 | não | não | não
GET/POST/DELETE | /api/HistoricoPesquisa/* | Cliente/Admin/Lojista | sim | misto | **não** na listagem | produto no relatório | não | não
GET/POST/DELETE | /api/Favoritos/* | Cliente/Admin | sim | misto | listagem sim | Produto+Loja | não | não
GET/POST | /api/Conversas/* | JWT | sim | misto | mensagens sim | Cliente/Loja | não | SignalR
POST | /api/Auth/login | não | sim | last login | — | LojaPropria | não | não
POST | /api/Auth/esqueci-senha | não | sim | token | — | — | não | SMTP
GET | /api/Consultas/cnpj/{cnpj} | não | não | não | — | — | não | **OpenCNPJ/BrasilAPI**
* | /api/Clientes, Lojistas, Admins, PreferenciasCliente | JWT (exceto registrar/login) | sim | misto | listagens admin sem paginação | LojaPropria no perfil | proximidade admin | SMTP no registrar

Volume de produção: **não medido** (sem APM). Hipótese pelo app Mobile: Feed, Lojas/mapa, Lojas pageSize=100, Produtos, Ofertas, Avaliacoes, Favoritos, Conversas/tem-novas, Auth.

---

## Apêndice C — O que foi criado

Instrumentação (só ativa com `PRECOCERTO_BENCHMARK=1`; Rate Limit permanece no caminho normal):

- `BackEnd/Pc.WebApi/Diagnostics/*`
- `BackEnd/Pc.WebApi/Controllers/BenchmarkController.cs` (`/api/health/bench`, `/db`, `/explain`, `/reset`)
- `Program.cs`: interceptor EF + middleware + skip `UseRateLimiter` no modo bench

Testes:

- `BackEnd/scripts/perf-benchmark/lib.js`, `mixed.js`, `endpoint.js`, `health-rps.js`, `run.sh`
- `BackEnd/scripts/perf-benchmark/bin/k6` (k6 v0.54.0 baixado)
- `BackEnd/scripts/perf-benchmark/results/*` (JSON k6 + snapshots)

Já existia: `BackEnd/scripts/perf-microbench` (BCrypt/validadores).

API de bench foi **encerrada** ao final (limiter estava desligado em 0.0.0.0:5132).
