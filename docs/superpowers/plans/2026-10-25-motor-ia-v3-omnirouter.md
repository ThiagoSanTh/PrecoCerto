# MotorIA v3 + OmniRouter (Gemini/NVIDIA) + Lojista BI + UI Cards Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refactor the Preço Certo AI assistant into MotorIA v3 with a zero-cost Gemini/NVIDIA NIM OmniRouter (with automatic circuit-breaker failover and deterministic fallback), basket building, store catalog queries, merchant competitor BI, batch SQL evaluation queries, dead v1 engine removal, and interactive offer/store cards in `AiChatScreen`.

**Architecture:** `MotorIA` orchestrates a hybrid pipeline: `ILlmRouterService` attempts structured JSON intent/entity extraction via Google Gemini (`gemini-2.5-flash`), failing over within 3.5s to NVIDIA NIM (`meta/llama-3.3-70b-instruct`) on 429/5xx/timeout with a 5-minute circuit-breaker cooldown, and falling back to an improved deterministic `InterpretadorIA`. All product prices, stock, distances, basket totals, and store competitor comparisons are queried strictly from PostgreSQL (eliminating N+1 queries), scored by `MotorPontuacaoIA`, and finally summarized by `ILlmRouterService` (or deterministic `GeradorRespostaIA`) alongside rich `produtoId`/`lojaId` metadata rendered as clickable cards in React Native/Web.

**Tech Stack:** .NET 8 (C# 12, ASP.NET Core Web API, EF Core, Npgsql, pgvector, xUnit, Moq), React Native / Expo SDK 54.

**Spec:** `docs/superpowers/specs/2026-10-25-motor-ia-v3-omnirouter-design.md`

## Global Constraints

- Zero hallucination: LLM never invents prices, distances, availability, or store names; only candidates present in `ContextoIA.Candidatos` may be cited.
- Max chat LLM latency budget: `3500ms` per provider (`LlmSettings.TimeoutMs = 3500`), never blocking chat requests with 45s retry sleeps.
- Circuit breaker cooldown: `300s` (`5 min`) in-memory when a provider returns HTTP `429`, `401`, `403`, `5xx`, or times out.
- Backward compatibility: `POST /api/IA/chat` remains functional by delegating to `IMotorIA.AnalisarAsync`.
- Remove all 7 hardcoded `#region agent log` blocks referencing `d:\Dev\PrecoCerto\debug-6c7c29.log`.
- Preserve `TextoNormalizador` while removing the obsolete v1 engine (`IAServico`, `IAIntencaoClassificador`, `IAEntidadeExtrator`, `RespostaDeterministicaServico`).

## Review Focus

- Budget and distance numbers mixed with product names (`"Quero comprar arroz até 15 reais até 5 km"`) must extract `"arroz"` as `ProdutoTermo`, `15m` as `OrcamentoMax`, and `5m` as `DistanciaMaxKm` (never `"reais"` or `"5"`).
- Negation + city suffix (`"Não quero arroz caro, quero o mais em conta em Saquarema"`) must extract `"arroz"` as `ProdutoTermo` (never `"saquarema"` or `"caro"`).
- Store catalog query (`"Quais produtos a loja Bink vende?"`) must classify as `IntencaoIA.ProdutosDaLoja` with `LojaTermo = "bink"` (stripping trailing `"vende"`).
- Out-of-domain queries (`"Quem ganhou a copa do mundo?"`) must classify as `IntencaoIA.ForaDoDominio` without hitting SQL or RAG.
- Gemini 429 rate-limit during `ILlmRouterService` or `RagServico` query must trip the circuit breaker and switch to NVIDIA NIM / fallback in `< 100ms` without waiting 45 seconds.

---

### Task 1: Remove Legacy v1 AI Engine & Hardcoded Debug Logs

**Files:**
- Delete: `BackEnd/Pc.Servico/Implementacoes/IA/IAServico.cs`
- Delete: `BackEnd/Pc.Servico/Implementacoes/IA/IAIntencaoClassificador.cs`
- Delete: `BackEnd/Pc.Servico/Implementacoes/IA/IAEntidadeExtrator.cs`
- Delete: `BackEnd/Pc.Servico/Implementacoes/IA/RespostaDeterministicaServico.cs`
- Delete: `BackEnd/Pc.Servico/Interfaces/IIAServico.cs`
- Delete: `BackEnd/Pc.Servico/Interfaces/IRespostaIAServico.cs`
- Delete: `BackEnd/Pc.Servico/Modelos/IA/IAChatPedido.cs`
- Delete: `BackEnd/Pc.Servico/Modelos/IA/IAChatResultado.cs`
- Delete: `BackEnd/Pc.Servico/Modelos/IA/IAContextoResposta.cs`
- Delete: `BackEnd/Pc.Dominio/Enums/IAIntencao.cs`
- Delete: `BackEnd/Pc.Servico.Tests/IAIntencaoClassificadorTests.cs`
- Modify: `BackEnd/Pc.WebApi/Controllers/IAController.cs`
- Modify: `BackEnd/Pc.WebApi/Program.cs:439-441`
- Modify: `BackEnd/Pc.Servico/Implementacoes/MotorIA/MotorIA.cs:72-95,375-420`
- Modify: `BackEnd/Pc.Servico/Implementacoes/MotorIA/InterpretadorIA.cs:47-70`
- Modify: `BackEnd/Pc.Servico/Implementacoes/MotorIA/RagConhecimentoIA.cs:33-101`
- Modify: `BackEnd/Pc.Repositorio/Implementacoes/DocumentoRagRepositorio.cs:69-116`

**Interfaces:**
- Consumes: `IMotorIA.AnalisarAsync(PedidoAnaliseIA, CancellationToken)`
- Produces: Clean `IAController` where `POST /api/IA/chat` delegates to `IMotorIA.AnalisarAsync`, and `DocumentoRagRepositorio.BuscarPorSimilaridadeAsync` executes a single vector query without `CountAsync` or file logging.

- [ ] **Step 1: Delete obsolete v1 AI files and test file**

Remove the 11 files listed under `Delete:` above, keeping `BackEnd/Pc.Servico/Implementacoes/IA/TextoNormalizador.cs` intact.

- [ ] **Step 2: Remove `#region agent log` blocks and redundant `CountAsync` in `DocumentoRagRepositorio`, `MotorIA`, `InterpretadorIA`, and `RagConhecimentoIA`**

In `BackEnd/Pc.Repositorio/Implementacoes/DocumentoRagRepositorio.cs`, remove `var totalComEmbedding = await _context.DocumentosRag.AsNoTracking().CountAsync(...)` and the `#region agent log` block. Remove all `#region agent log` blocks in `MotorIA.cs`, `InterpretadorIA.cs`, and `RagConhecimentoIA.cs`.

- [ ] **Step 3: Update `IAController.cs` and `Program.cs` DI registrations**

In `BackEnd/Pc.WebApi/Program.cs`, remove:
```csharp
builder.Services.AddScoped<IRespostaIAServico, RespostaDeterministicaServico>();
builder.Services.AddScoped<IIAServico, IAServico>();
```
In `BackEnd/Pc.WebApi/Controllers/IAController.cs`, remove `IIAServico _iaServico` from constructor and update `Chat(IAChatRequestDto request, CancellationToken cancellationToken)` to call `_motorIA.AnalisarAsync(new PedidoAnaliseIA { Mensagem = mensagem, Latitude = request.Latitude, Longitude = request.Longitude, UsuarioId = User.GetUserId() }, cancellationToken)` and map its result to `IAChatResponseDto`.

- [ ] **Step 4: Run test suite to verify clean build and passing tests**

Run: `dotnet test BackEnd/PrecoCerto.slnx`
Expected: PASS (0 errors).

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "refactor(ia): remove legacy v1 IA engine and hardcoded debug log blocks"
```

---

### Task 2: Domain Models, DTOs & Deterministic Classifier/Extractor Fixes

**Files:**
- Modify: `BackEnd/Pc.Dominio/Enums/MotorIA/IntencaoIA.cs`
- Modify: `BackEnd/Pc.Servico/Modelos/MotorIA/ContextoIA.cs`
- Modify: `BackEnd/Pc.WebApi/DTOs/IA/IAAnalisarDtos.cs`
- Modify: `BackEnd/Pc.Servico/Implementacoes/MotorIA/Vocabulario/VocabularioIA.cs`
- Modify: `BackEnd/Pc.Servico/Implementacoes/MotorIA/ClassificadorIntencaoIA.cs`
- Modify: `BackEnd/Pc.Servico/Implementacoes/MotorIA/ExtratorEntidadesIA.cs`
- Modify: `BackEnd/Pc.Servico/Implementacoes/MotorIA/InterpretadorIA.cs`
- Modify: `BackEnd/Pc.Servico.Tests/MotorIA/MotorIATests.cs`

**Interfaces:**
- Produces:
  - `IntencaoIA`: adds `ProdutosDaLoja = 16`, `RecomendarPorOcasiao = 17`, `AnalisarConcorrenciaLojista = 18`
  - `MensagemHistoricoIA { string Papel, string Texto }`
  - `PedidoAnaliseIA`: adds `List<MensagemHistoricoIA> Historico`, `Guid? LojaIdUsuario`, `string? ModoOperacional`
  - `ContextoIA`: adds `List<string> ItensBusca`, `Guid? LojaIdUsuario`, `string? ModoOperacional`, `string ProvedorLlm`, `List<MensagemHistoricoIA> Historico`
  - `ResultadoItemAnaliseIA` & `IAAnalisarItemDto`: adds `Guid? ProdutoId`, `Guid? LojaId`, `bool EmPromocao`
  - `ResultadoAnaliseIA` & `IAAnalisarResponseDto`: adds `string ProvedorLlm`

- [ ] **Step 1: Write failing tests in `BackEnd/Pc.Servico.Tests/MotorIA/MotorIATests.cs` for all 5 Review Focus cases and multi-item basket extraction**

```csharp
[Theory]
[InlineData("Quero comprar arroz até 15 reais até 5 km", IntencaoIA.BuscarProduto, "arroz")]
[InlineData("Não quero arroz caro, quero o mais em conta em Saquarema", IntencaoIA.BuscarProduto, "arroz")]
[InlineData("Onde encontro açúcar cristal?", IntencaoIA.BuscarProduto, "acucar")]
public void Extrai_produto_correto_com_numeros_negacao_e_cidade(string msg, IntencaoIA intencaoEsperada, string termoEsperado)
{
    var ctx = _sut.Interpretar(new PedidoAnaliseIA { Mensagem = msg });
    Assert.Equal(intencaoEsperada, ctx.Intencao);
    Assert.NotNull(ctx.ProdutoTermo);
    Assert.Contains(termoEsperado, TextoNormalizador.Normalizar(ctx.ProdutoTermo!), StringComparison.Ordinal);
}

[Fact]
public void Classifica_produtos_da_loja_e_limpa_verbo_vende()
{
    var ctx = _sut.Interpretar(new PedidoAnaliseIA { Mensagem = "Quais produtos a loja Bink vende?" });
    Assert.Equal(IntencaoIA.ProdutosDaLoja, ctx.Intencao);
    Assert.Equal("bink", ctx.LojaTermo);
}

[Fact]
public void Classifica_copa_do_mundo_como_fora_do_dominio()
{
    var ctx = _sut.Interpretar(new PedidoAnaliseIA { Mensagem = "Quem ganhou a copa do mundo?" });
    Assert.Equal(IntencaoIA.ForaDoDominio, ctx.Intencao);
}

[Fact]
public void Extrai_multiplos_itens_na_cesta_basica()
{
    var ctx = _sut.Interpretar(new PedidoAnaliseIA { Mensagem = "Quero montar uma cesta básica com arroz, feijão e óleo" });
    Assert.Equal(IntencaoIA.MontarCesta, ctx.Intencao);
    Assert.Contains("arroz", ctx.ItensBusca);
    Assert.Contains("feijao", ctx.ItensBusca);
    Assert.Contains("oleo", ctx.ItensBusca);
}

[Fact]
public void Classifica_analise_concorrencia_lojista_quando_em_modo_loja()
{
    var ctx = _sut.Interpretar(new PedidoAnaliseIA
    {
        Mensagem = "Quais produtos meus estão mais caros que a concorrência?",
        LojaIdUsuario = Guid.NewGuid(),
        ModoOperacional = "loja"
    });
    Assert.Equal(IntencaoIA.AnalisarConcorrenciaLojista, ctx.Intencao);
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test BackEnd/PrecoCerto.slnx --filter MotorIAInterpretacaoTests`
Expected: FAIL on the new tests.

- [ ] **Step 3: Update `IntencaoIA.cs`, `ContextoIA.cs`, `IAAnalisarDtos.cs`, `VocabularioIA.cs`, `ClassificadorIntencaoIA.cs`, `ExtratorEntidadesIA.cs`, and `InterpretadorIA.cs`**

1. Add the enum values and model/DTO properties defined in **Interfaces**.
2. In `VocabularioIA.cs`:
   - Add `"copa do mundo"`, `"quem ganhou"`, `"politica"`, `"religiao"`, `"programacao"` to `ForaDominio`.
   - Add occasion dictionary `public static readonly Dictionary<string, string[]> ItensPorOcasiao` mapping:
     - `"churrasco"` -> `["carne", "linguica", "frango", "refrigerante", "cerveja", "pao"]`
     - `"cesta"` / `"cesta basica"` -> `["arroz", "feijao", "oleo", "acucar", "cafe", "leite", "macarrao"]`
     - `"cafe da manha"` -> `["cafe", "leite", "pao", "manteiga", "queijo"]`
     - `"dor de cabeca"` / `"remedio"` / `"gripado"` -> `["dipirona", "paracetamol", "analgesico", "vitamina"]`
   - Expand `SinonimosProduto` with `"acucar"`, `"oleo"`, `"macarrao"`, `"linguica"`, `"carne"`, `"frango"`, `"cerveja"`, `"remedio"`.
3. In `ClassificadorIntencaoIA.cs`:
   - If `contexto.LojaIdUsuario.HasValue` and message contains `"meus produtos"`, `"minha loja"`, `"concorrencia"`, `"meus precos"`, or `"mais caros que"`, set `IntencaoIA.AnalisarConcorrenciaLojista`.
   - If message matches `"quais produtos"`, `"que produtos"`, `"o que a loja"`, `"o que o mercado"`, `"catalogo da loja"`, or (`t.Contains("loja")` + `t.Contains("vende")` without specific product before `"loja"`), set `IntencaoIA.ProdutosDaLoja`.
   - If message contains occasion keywords (`"churrasco"`, `"cafe da manha"`, `"dor de cabeca"`, `"fazer pro almoco"`, `"receita de"`), set `IntencaoIA.RecomendarPorOcasiao`.
4. In `ExtratorEntidadesIA.cs`:
   - Add `"caro"`, `"cara"`, `"conta"`, `"reais"`, `"real"`, `"km"`, `"kg"`, `"saquarema"`, `"bacaxa"`, `"itauna"`, `"jacone"`, `"cesta"`, `"basica"`, `"montar"`, `"compare"`, `"comparar"`, `"precos"` to `Stopwords`.
   - Strip budget (`ate \d+ reais`) and distance (`ate \d+ km`) substrings from `t` before running `ExtrairProduto(ctx, t)`.
   - In `TokensSignificativos`: filter out pure numbers (`!Regex.IsMatch(x, @"^\d+$")`) and select `tokens[0]` (first product noun) instead of `tokens[^1]`.
   - In `ExtrairLoja`: strip trailing stop verbs (`"vende"`, `"vendem"`, `"tem"`, `"fica"`, `"possui"`, `"esta"`).
   - Populate `ctx.ItensBusca`: if multiple products are separated by `,` or ` e `, or if `Intencao == MontarCesta` / `RecomendarPorOcasiao`, populate `ctx.ItensBusca` from extracted tokens or `VocabularioIA.ItensPorOcasiao`.
   - Support multi-turn history resolution in `InterpretadorIA`: if `ctx.ProdutoTermo` is empty and `pedido.Historico` has a recent user message, inherit `LojaTermo` or `ProdutoTermo` when the user says `"nessa loja"`, `"e feijao?"`, etc.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test BackEnd/PrecoCerto.slnx --filter MotorIAInterpretacaoTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(ia): expand MotorIA v3 domain models and fix deterministic classifier/extractor"
```

---

### Task 3: OmniRouter LLM Service (Gemini 2.5 Flash + NVIDIA NIM + Circuit Breaker)

**Files:**
- Create: `BackEnd/Pc.Servico/Modelos/MotorIA/LlmSettings.cs`
- Create: `BackEnd/Pc.Servico/Interfaces/MotorIA/ILlmRouterService.cs`
- Create: `BackEnd/Pc.Servico/Implementacoes/MotorIA/LlmRouterService.cs`
- Modify: `BackEnd/Pc.WebApi/Program.cs`
- Create: `BackEnd/Pc.Servico.Tests/MotorIA/LlmRouterServiceTests.cs`

**Interfaces:**
- Produces:
  - `LlmSettings`: `GeminiApiKey`, `GeminiModel` (`"gemini-2.5-flash"`), `NvidiaApiKey`, `NvidiaModel` (`"meta/llama-3.3-70b-instruct"`), `NvidiaBaseUrl` (`"https://integrate.api.nvidia.com/v1"`), `TimeoutMs` (`3500`), `CooldownSegundos` (`300`).
  - `LlmInterpretacaoResultado`: `{ string Provedor, string? Intencao, string? Objetivo, List<string> ItensBusca, string? ProdutoTermo, string? LojaTermo, string? Categoria, decimal? OrcamentoMax, decimal? DistanciaMaxKm, bool UrgenciaAlta, bool NecessitaEntrega }`
  - `ILlmRouterService`:
    - `Task<LlmInterpretacaoResultado?> InterpretarAsync(PedidoAnaliseIA pedido, CancellationToken ct = default)`
    - `Task<(string? Texto, string Provedor)> GerarRespostaAsync(ContextoIA contexto, CancellationToken ct = default)`
    - `void ResetarCircuitBreaker()`

- [ ] **Step 1: Write failing unit tests in `BackEnd/Pc.Servico.Tests/MotorIA/LlmRouterServiceTests.cs`**

Test cases using mocked `HttpMessageHandler`:
1. `Quando_Gemini_retorna_200_interpreta_json_e_usa_provedor_Gemini`: verifies `Provedor == "Gemini"` and parsed `ItensBusca` / `Intencao`.
2. `Quando_Gemini_retorna_429_aciona_circuit_breaker_e_faz_failover_imediato_para_NvidiaNim`: verifies Gemini returns `429`, router immediately calls NVIDIA NIM (`https://integrate.api.nvidia.com/v1/chat/completions`), returns `Provedor == "NvidiaNim"`, and a subsequent call skips Gemini while in cooldown.
3. `Quando_ambos_falham_ou_sem_chaves_retorna_null_sem_lancar_excecao`: verifies graceful `null` return so `MotorIA` uses deterministic fallback.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test BackEnd/PrecoCerto.slnx --filter LlmRouterServiceTests`
Expected: FAIL (types do not exist yet).

- [ ] **Step 3: Implement `LlmSettings`, `ILlmRouterService`, and `LlmRouterService` and register in `Program.cs`**

1. In `LlmRouterService.cs`:
   - Use `IHttpClientFactory` (`"GeminiLlm"` and `"NvidiaLlm"` named clients).
   - Maintain a thread-safe `ConcurrentDictionary<string, DateTimeOffset>` for provider cooldowns.
   - Use a per-call linked `CancellationTokenSource(TimeSpan.FromMilliseconds(_settings.TimeoutMs))` so neither provider can ever exceed `3.5s`.
   - Prompt Gemini (`models/{model}:generateContent?key=...` with `generationConfig.responseMimeType = "application/json"`) and NVIDIA NIM (`POST chat/completions` with `Bearer {NvidiaApiKey}` and system prompt requesting raw JSON) to extract `LlmInterpretacaoResultado` considering `pedido.Mensagem`, `pedido.Historico` (last 6 messages), and `pedido.ModoOperacional`.
   - In `GerarRespostaAsync(ContextoIA ctx, CancellationToken ct)`: serialize the top 5 `ctx.Decisao.Ranking` items (product, store, exact price, distance, promo, motives) and ask the active LLM provider for a concise (max 3 sentences) helpful Portuguese response strictly grounded in those numbers.
2. In `BackEnd/Pc.WebApi/Program.cs`:
   - Configure `LlmSettings` from `"Llm"` section + environment variables (`GEMINI_API_KEY`, `Rag__ApiKey`, `NVIDIA_API_KEY`, `Llm__NvidiaApiKey`).
   - Register named `HttpClient`s `"GeminiLlm"` and `"NvidiaLlm"` and `builder.Services.AddSingleton<ILlmRouterService, LlmRouterService>()`.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test BackEnd/PrecoCerto.slnx --filter LlmRouterServiceTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(ia): add OmniRouter LLM service with Gemini and NVIDIA NIM failover"
```

---

### Task 4: Batch SQL Evaluations, Accent-Insensitive Search & Fast RAG Query Embeddings

**Files:**
- Modify: `BackEnd/Pc.Repositorio/Interfaces/IAvaliacaoRepositorio.cs`
- Modify: `BackEnd/Pc.Repositorio/Implementacoes/AvaliacaoRepositorio.cs`
- Modify: `BackEnd/Pc.Servico/Interfaces/IAvaliacaoServico.cs`
- Modify: `BackEnd/Pc.Servico/Implementacoes/AvaliacaoServico.cs`
- Modify: `BackEnd/Pc.Repositorio/Implementacoes/ProdutoRepositorio.cs:57-72,158-194`
- Modify: `BackEnd/Pc.Servico/Interfaces/IEmbeddingService.cs`
- Modify: `BackEnd/Pc.Servico/Implementacoes/Rag/GeminiEmbeddingService.cs`
- Modify: `BackEnd/Pc.Servico/Implementacoes/Rag/OpenAIEmbeddingService.cs`
- Modify: `BackEnd/Pc.Servico/Implementacoes/Rag/RagServico.cs`
- Modify: `BackEnd/Pc.Servico.Tests/RagTests.cs`

**Interfaces:**
- Produces:
  - `IAvaliacaoRepositorio.ObterResumosPorLojasAsync(IEnumerable<Guid> lojaIds)` -> `Task<Dictionary<Guid, (double Media, int Quantidade)>>`
  - `IAvaliacaoServico.ObterResumosPorLojasAsync(IEnumerable<Guid> lojaIds)` -> `Task<Dictionary<Guid, (double Media, int Quantidade)>>`
  - `IEmbeddingService.GerarEmbeddingAsync(string texto, CancellationToken cancellationToken = default, bool isQuery = false)`

- [ ] **Step 1: Write failing tests in `RagTests.cs` for `isQuery = true` (`RETRIEVAL_QUERY` and no 45s delay on 429)**

Add a test in `BackEnd/Pc.Servico.Tests/RagTests.cs` verifying that calling `GerarEmbeddingAsync("arroz", ct, isQuery: true)` sends `"taskType":"RETRIEVAL_QUERY"` in the JSON body and throws immediately on HTTP `429` without waiting 45 seconds.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test BackEnd/PrecoCerto.slnx --filter RagTests`
Expected: FAIL.

- [ ] **Step 3: Implement batch evaluation summary, expanded accent mapping, and fast query embeddings**

1. In `AvaliacaoRepositorio.cs` and `AvaliacaoServico.cs`:
   - Implement `ObterResumosPorLojasAsync(IEnumerable<Guid> lojaIds)` executing a single `.Where(a => ids.Contains(a.LojaId)).GroupBy(a => a.LojaId).Select(...)` query.
2. In `ProdutoRepositorio.cs`:
   - Expand `TentarFormaComAcento` with `"acucar" => "açúcar"`, `"oleo" => "óleo"`, `"macarrao" => "macarrão"`, `"linguica" => "linguiça"`, `"agua" => "água"`, `"sabao" => "sabão"`, `"limao" => "limão"`, `"remedio" => "remédio"`, `"analgesico" => "analgésico"`, `"laticinios" => "laticínios"`.
3. In `IEmbeddingService.cs`, `GeminiEmbeddingService.cs`, `OpenAIEmbeddingService.cs`, and `RagServico.cs`:
   - Add `bool isQuery = false` default parameter to `GerarEmbeddingAsync`.
   - In `GeminiEmbeddingService.cs`: set `taskType = isQuery ? "RETRIEVAL_QUERY" : "RETRIEVAL_DOCUMENT"` and set `var maxAttempts = isQuery ? 1 : Math.Clamp(_settings.MaxRetries, 1, 8);` so chat queries never sleep 45s.
   - In `RagServico.BuscarAsync`: pass `isQuery: true` to `_embeddings.GerarEmbeddingAsync(consulta.Trim(), cancellationToken, isQuery: true)`.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test BackEnd/PrecoCerto.slnx`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "perf(ia): batch store ratings query, expand PT-BR accents, and fast-fail RAG query embeddings"
```

---

### Task 5: MotorIA v3 Orchestration (OmniRouter Integration, Basket, Store Catalog & Merchant Competitor BI)

**Files:**
- Modify: `BackEnd/Pc.Servico/Implementacoes/MotorIA/MotorIA.cs`
- Modify: `BackEnd/Pc.Servico/Implementacoes/MotorIA/GeradorRespostaIA.cs`
- Modify: `BackEnd/Pc.WebApi/Controllers/IAController.cs`
- Modify: `BackEnd/Pc.Servico.Tests/MotorIA/MotorIATests.cs`

**Interfaces:**
- Consumes: `ILlmRouterService`, `IAvaliacaoServico.ObterResumosPorLojasAsync`, `ContextoIA`, `IntencaoIA`
- Produces:
  - Complete `MotorIA.AnalisarAsync` handling:
    - LLM interpretation via `ILlmRouterService.InterpretarAsync` merged with `InterpretadorIA.Interpretar` fallback
    - `CarregarCestaAsync` (`IntencaoIA.MontarCesta`)
    - `CarregarProdutosDaLojaAsync` (`IntencaoIA.ProdutosDaLoja`)
    - `CarregarPorOcasiaoAsync` (`IntencaoIA.RecomendarPorOcasiao`)
    - `CarregarConcorrenciaLojistaAsync` (`IntencaoIA.AnalisarConcorrenciaLojista`)
    - `ResultadoItemAnaliseIA` populated with `ProdutoId`, `LojaId`, `EmPromocao`
    - `ResultadoAnaliseIA.ProvedorLlm` populated (`"Gemini"`, `"NvidiaNim"`, or `"Deterministico"`)

- [ ] **Step 1: Write failing tests in `BackEnd/Pc.Servico.Tests/MotorIA/MotorIATests.cs` for `MontarCesta`, `ProdutosDaLoja`, `AnalisarConcorrenciaLojista`, and `ProdutoId`/`LojaId` mapping**

Write unit tests in `MotorIATests.cs` mocking `IProdutoServico`, `IOfertaServico`, `ILojaServico`, `IAvaliacaoServico`, and `ILlmRouterService`:
1. `AnalisarAsync_MontarCesta_agrupa_ofertas_por_loja_e_soma_total`
2. `AnalisarAsync_ProdutosDaLoja_lista_catalogo_da_loja_especifica`
3. `AnalisarAsync_AnalisarConcorrenciaLojista_compara_precos_da_loja_do_usuario_com_concorrentes`
4. `AnalisarAsync_retorna_ProdutoId_e_LojaId_nos_resultados`

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test BackEnd/PrecoCerto.slnx --filter MotorIATests`
Expected: FAIL.

- [ ] **Step 3: Implement `MotorIA.cs`, `GeradorRespostaIA.cs`, and `IAController.cs` updates**

1. In `MotorIA.cs`:
   - Inject `ILlmRouterService? llmRouter = null` (optional/default null for backward-compatible test constructors, registered in DI).
   - First run deterministic `_interpretador.Interpretar(pedido)`. Then if `_llmRouter != null`, call `await _llmRouter.InterpretarAsync(pedido, cancellationToken)`. When LLM returns a valid result, enrich `ctx` (`Intencao`, `Objetivo`, `ProdutoTermo`, `ItensBusca`, `LojaTermo`, `OrcamentoMax`, `DistanciaMaxKm`, `ProvedorLlm = llmRes.Provedor`).
   - Only call `TentarClimaAsync` if `ctx.NecessitaEntrega || ctx.Intencao == IntencaoIA.ConsultarEntrega || ctx.MensagemNormalizada.Contains("chuva") || ctx.MensagemNormalizada.Contains("clima")` (saving ~400ms on normal product queries).
   - Replace the sequential N+1 loop `foreach (var lojaId in lojaIds.Take(15)) await _avaliacoes.ObterResumoAvaliacaoAsync(lojaId)` in both `CarregarCandidatosAsync` and `CarregarLojasAsync` with a single call to `await _avaliacoes.ObterResumosPorLojasAsync(lojaIds)`.
   - Implement `CarregarCestaAsync`, `CarregarProdutosDaLojaAsync`, `CarregarPorOcasiaoAsync`, and `CarregarConcorrenciaLojistaAsync`.
   - In `FinalizarAsync`: if `_llmRouter != null` and `ctx.Candidatos.Count > 0`, call `_llmRouter.GerarRespostaAsync(ctx, ct)`; if `null`, fall back to `_gerador.Gerar(ctx)`. Map `ProdutoId = c.ProdutoId`, `LojaId = c.LojaId`, `EmPromocao = c.EmPromocao` onto `ResultadoItemAnaliseIA`, and set `ProvedorLlm = ctx.ProvedorLlm`.
2. In `GeradorRespostaIA.cs`:
   - Add deterministic response templates for `IntencaoIA.MontarCesta`, `IntencaoIA.ProdutosDaLoja`, `IntencaoIA.RecomendarPorOcasiao`, and `IntencaoIA.AnalisarConcorrenciaLojista`.
3. In `IAController.cs`:
   - Pass `Historico`, `LojaIdUsuario`, and `ModoOperacional` from `IAAnalisarRequestDto` into `PedidoAnaliseIA`, and map `ProdutoId`, `LojaId`, `EmPromocao`, and `ProvedorLlm` onto `IAAnalisarResponseDto`.

- [ ] **Step 4: Run all backend tests to verify they pass**

Run: `dotnet test BackEnd/PrecoCerto.slnx`
Expected: PASS (all tests green).

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(ia): implement MotorIA v3 orchestration, basket, store catalog, and merchant BI"
```

---

### Task 6: Frontend Interactive Cards, Quick Suggestion Chips & Multi-turn Context (`AiChatScreen` & `iaService`)

**Files:**
- Modify: `Mobile/src/services/iaService.js`
- Modify: `Mobile/src/screens/user/AiChatScreen.js`

**Interfaces:**
- Consumes: `POST /api/IA/analisar` returning `{ resposta, intencao, objetivo, confianca, provedorLlm, resultados: [{ tipo, titulo, score, preco, distanciaKm, loja, produto, produtoId, lojaId, emPromocao, motivos }] }`
- Produces:
  - `enviarPerguntaIA(mensagem, { incluirLocalizacao = true, usuarioId = null, lojaId = null, modoOperacional = null, historico = [] })`
  - Interactive `ResultCard` and suggestion chips in `AiChatScreen.js` navigating to `ProductDetail` (`{ productId }`) and `StoreCatalog` (`{ lojaId, lojaNome }`).

- [ ] **Step 1: Update `Mobile/src/services/iaService.js` to always include cached/current location, conversation history, and store context**

In `Mobile/src/services/iaService.js`:
- Update `enviarPerguntaIA` signature to accept `{ incluirLocalizacao = true, usuarioId = null, lojaId = null, modoOperacional = null, historico = [] } = {}`.
- Send `lojaIdUsuario: lojaId`, `modoOperacional`, and `historico: historico.slice(-6).map(m => ({ papel: m.papel, texto: m.texto }))` in the `POST /IA/analisar` body.

- [ ] **Step 2: Upgrade `Mobile/src/screens/user/AiChatScreen.js` with suggestion chips, interactive offer/store cards, and provider badge in admin panel**

In `Mobile/src/screens/user/AiChatScreen.js`:
- Read `emModoLoja` and `session?.perfil?.lojaId` from `useAuth()`.
- Always pass `incluirLocalizacao: true`, `lojaId: session?.perfil?.lojaId || null`, `modoOperacional: emModoLoja ? 'loja' : 'cliente'`, and `historico: mensagens.filter(m => m.id !== 'welcome')` to `enviarPerguntaIA`.
- Add suggestion chips based on `emModoLoja`:
  - Cliente mode: `["Arroz mais barato perto de mim", "Montar cesta básica barata", "Itens para churrasco", "Promoções de hoje"]`
  - Loja mode: `["Meus produtos vs concorrência", "Quais produtos minha loja vende?", "Menor preço de arroz na região"]`
- Render a list of interactive cards (`item.resultados?.slice(0, 5)`) below every assistant message for **all users**:
  - Show product/store title, store name, formatted price (`R$ XX,XX`), `"PROMOÇÃO"` badge when `r.emPromocao`, distance (`~X,X km`), and motives.
  - Render action buttons:
    - If `r.produtoId`: `"Ver Produto"` -> `navigation.navigate('ProductDetail', { productId: r.produtoId })`
    - If `r.lojaId`: `"Ver Loja"` -> `navigation.navigate('StoreCatalog', { lojaId: r.lojaId, lojaNome: r.loja || r.titulo })`
- In `AdminDebugPanel`: display `Provedor: {item.provedorLlm || 'Deterministico'}`.

- [ ] **Step 3: Verify backend build & tests and inspect git diff**

Run: `dotnet test BackEnd/PrecoCerto.slnx`
Expected: PASS.

- [ ] **Step 4: Commit**

```bash
git add Mobile/src/services/iaService.js Mobile/src/screens/user/AiChatScreen.js
git commit -m "feat(mobile): add interactive product/store cards, suggestion chips, and multi-turn history to AiChatScreen"
```
