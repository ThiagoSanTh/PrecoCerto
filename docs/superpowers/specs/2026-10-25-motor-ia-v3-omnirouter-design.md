# MotorIA v3 + OmniRouter (Gemini/NVIDIA) + Lojista BI + UI Cards — Design Spec

## 1. Intent & Why
Transformar o `MotorIA` do **Preço Certo** (hoje puramente baseado em regex/regras estáticas, com queries N+1 lentas de ~3s e sem compreensão real de linguagem natural) em um **Agente Híbrido Zero-Alucinação de Custo Zero (MotorIA v3)**.

O usuário final (consumidor ou lojista em Saquarema/RJ) precisa conversar naturalmente com o assistente ("Quero montar uma cesta básica barata", "O que fazer para um churrasco?", "Quais produtos a loja Bink vende?", "Quais produtos meus estão mais caros que a concorrência?"), receber respostas rápidas (<1.2s com LLM ou <300ms no fallback determinístico), sem jamais inventar preços/lojas (100% ancorado no PostgreSQL), e visualizar **cards clicáveis** na interface para navegar direto ao produto ou loja.

Para manter custo operacional **R$ 0,00**, o sistema utiliza um **OmniRouter de LLMs com Circuit Breaker**:
1. **Primário**: Google Gemini (`gemini-2.5-flash`) via Free Tier (`GEMINI_API_KEY` / `Rag:ApiKey`).
2. **Failover Automático**: NVIDIA NIM API (`meta/llama-3.3-70b-instruct`) via Free Tier (`NVIDIA_API_KEY` / `Llm:NvidiaApiKey`) quando o Gemini retornar `429`, timeout (`>3.5s`) ou erro `5xx`, colocando o Gemini em *cooldown* temporário (5 minutos).
3. **Fallback Determinístico Local**: Classificador e extrator determinísticos aprimorados se ambas as chaves estiverem ausentes ou indisponíveis.

---

## 2. Goals, Non-Goals & Anti-Goals

### Goals
- **OmniRouter Resiliente (`ILlmRouterService`)**: Chaveamento automático em memória (Circuit Breaker) entre Gemini -> NVIDIA NIM -> Determinístico.
- **Interpretação Estruturada & Multi-turn**: Extrair JSON tipado da mensagem do usuário + últimas 6 mensagens da sessão (`historico`), suportando múltiplos itens (`ItensBusca`), loja específica (`LojaTermo`), orçamento, distância máxima e intenção.
- **4 Novas Capacidades de Domínio**:
  1. `MontarCesta`: Buscar múltiplos itens simultaneamente e ranquear as lojas pelo menor custo total da cesta e maior cobertura de itens.
  2. `ProdutosDaLoja`: Listar produtos/ofertas de uma loja específica mencionada pelo usuário (ex.: "Quais produtos a loja Bink vende?").
  3. `RecomendarPorOcasiao` (Guardrail de Conversão): Desdobrar pedidos de receitas, churrasco, café da manhã ou sintomas leves em itens reais do catálogo e recomendar onde comprar mais barato.
  4. `AnalisarConcorrenciaLojista` (Modo Lojista BI): Quando o usuário autenticado operar no modo Lojista (`LojaId` presente) e perguntar sobre seus preços/competitividade, comparar os produtos da loja dele com a média/menor preço das lojas concorrentes.
- **Performance de Banco (<300ms)**:
  - Eliminar N+1 de avaliações criando `ObterResumosPorLojasAsync(IEnumerable<Guid> lojaIds)` em query única `GROUP BY`.
  - Eliminar count redundante em `DocumentoRagRepositorio.BuscarPorSimilaridadeAsync`.
  - Corrigir `GeminiEmbeddingService` para suportar `taskType` dinâmico (`RETRIEVAL_QUERY` na busca vs `RETRIEVAL_DOCUMENT` na indexação) e não travar requests de chat por 45s em caso de `429`.
- **Limpeza de Código Morto**:
  - Remover motor v1 legado (`IAServico`, `IAIntencaoClassificador`, `IAEntidadeExtrator`, `RespostaDeterministicaServico`, `IAIntencao`, `IRespostaIAServico`, `IIAServico`, `IAChatPedido`, `IAChatResultado`, `IAContextoResposta`, `IAIntencaoClassificadorTests`).
  - Manter `TextoNormalizador` (movido/acessível para o `MotorIA`).
  - Redirecionar `POST /api/IA/chat` no `IAController` para usar `IMotorIA.AnalisarAsync` por retrocompatibilidade.
  - Remover todos os 7 blocos `#region agent log` que gravam em `d:\Dev\PrecoCerto\debug-6c7c29.log`.
- **UX Rica no `AiChatScreen.js`**:
  - Enviar coordenadas sempre que disponíveis (removendo o bloqueio restritivo de `perguntaPrecisaLocalizacao`), histórico recente da conversa (`historico`) e contexto da sessão (`lojaId`, `modoOperacional`).
  - Exibir chips de sugestão rápida adaptados ao modo Cliente ou Lojista.
  - Renderizar cards interativos para cada item em `resultados` com preço, promoção, distância e botões `"Ver Produto"` (`ProductDetail`) e `"Ver Loja"` (`StoreCatalog`).

### Non-Goals
- Persistir histórico de conversas da IA no banco PostgreSQL (continua em memória na sessão do cliente e enviado via payload).
- Alterar o schema do banco de dados / criar novas migrations EF Core.
- Criar fluxo de checkout ou pagamento dentro do chat.

### Anti-Goals (O que seria uma falha mesmo funcionando)
- **Alucinação de Preço ou Loja**: A LLM **nunca** pode inventar um preço, endereço, distância ou nome de loja que não esteja na lista `Candidatos` retornada pelo PostgreSQL.
- **Travamento por Rate Limit**: Uma falha `429` no Gemini jamais pode fazer o usuário esperar 45 segundos na tela de chat. O timeout máximo por provedor LLM no chat é de `3.5s`.

---

## 3. Technical Design & Interfaces

### 3.1. Configuração e OmniRouter (`LlmSettings` & `ILlmRouterService`)
Configuração em `Pc.Servico/Modelos/MotorIA/LlmSettings.cs` (seção `"Llm"` ou variáveis de ambiente):
- `GeminiApiKey`: fallback automático para `Rag:ApiKey`, `GEMINI_API_KEY`, `GOOGLE_API_KEY`.
- `GeminiModel`: default `"gemini-2.5-flash"`.
- `NvidiaApiKey`: lido de `Llm:NvidiaApiKey` ou `NVIDIA_API_KEY`.
- `NvidiaModel`: default `"meta/llama-3.3-70b-instruct"`.
- `NvidiaBaseUrl`: default `"https://integrate.api.nvidia.com/v1"`.
- `TimeoutMs`: default `3500` (3.5s).
- `CooldownSegundos`: default `300` (5 min).

Interface `ILlmRouterService`:
- `Task<LlmInterpretacaoResultado?> InterpretarAsync(PedidoAnaliseIA pedido, CancellationToken ct)`
- `Task<string?> GerarRespostaNaturalAsync(ContextoIA contexto, CancellationToken ct)`

Comportamento do Circuit Breaker:
- Mantém estado estático/Singleton (`ConcurrentDictionary<string, DateTimeOffset> _cooldownAte`).
- Tenta Gemini se `GeminiApiKey` presente e `DateTimeOffset.UtcNow >= _cooldownAte["Gemini"]`.
- Se falhar (`429`, `5xx`, `TaskCanceledException`), registra `_cooldownAte["Gemini"] = UtcNow.AddSeconds(CooldownSegundos)` e chama NVIDIA NIM imediatamente na mesma requisição.
- Se NVIDIA NIM falhar ou não tiver chave, retorna `null` e o `MotorIA` usa o pipeline determinístico local (`ProvedorUtilizado = "Deterministico"`).

### 3.2. Evolução de `IntencaoIA`, `ContextoIA` e DTOs
Em `Pc.Dominio/Enums/MotorIA/IntencaoIA.cs`:
- Adicionar valores ao enum `IntencaoIA`:
  - `ProdutosDaLoja = 16`
  - `RecomendarPorOcasiao = 17`
  - `AnalisarConcorrenciaLojista = 18`

Em `Pc.Servico/Modelos/MotorIA/ContextoIA.cs`:
- `MensagemHistoricoIA`: `{ string Papel ("user" | "ia"), string Texto }`
- `PedidoAnaliseIA`: adicionar `List<MensagemHistoricoIA> Historico`, `Guid? LojaIdUsuario`, `string? ModoOperacional`.
- `ContextoIA`: adicionar `List<string> ItensBusca`, `Guid? LojaIdUsuario`, `string? ModoOperacional`, `string ProvedorLlm = "Deterministico"`, `List<MensagemHistoricoIA> Historico`.
- `ResultadoItemAnaliseIA` e `IAAnalisarItemDto`: adicionar `Guid? ProdutoId`, `Guid? LojaId`, `bool EmPromocao`.
- `ResultadoAnaliseIA` e `IAAnalisarResponseDto`: adicionar `string ProvedorLlm`.

### 3.3. Correções no Extrator e Classificador Determinísticos (Fallback Garantido)
- `ClassificadorIntencaoIA`:
  - Detectar `"quais produtos"`, `"o que a loja ... vende"`, `"catalogo da loja"` -> `IntencaoIA.ProdutosDaLoja`.
  - Detectar perguntas de lojista (`"meus produtos"`, `"minha loja"`, `"concorrencia"`, `"mais caros que"`) quando `LojaIdUsuario.HasValue` -> `IntencaoIA.AnalisarConcorrenciaLojista`.
  - Detectar ocasiões (`"churrasco"`, `"receita"`, `"cafe da manha"`, `"almoco"`, `"festa"`, `"dor de cabeca"`) -> `IntencaoIA.RecomendarPorOcasiao`.
  - Incluir `"copa do mundo"`, `"quem ganhou"`, `"politica"`, `"religiao"` em `VocabularioIA.ForaDominio`.
- `ExtratorEntidadesIA`:
  - Remover números, unidades (`reais`, `real`, `km`, `kg`, `g`, `l`, `ml`) e nomes de cidades/bairros (`saquarema`, `bacaxa`, `itauna`, `jacone`) antes de selecionar tokens de produto.
  - Em `TokensSignificativos`: preferir o **primeiro** substantivo de produto (ou substantivos conhecidos no vocabulário) em vez de `tokens[^1]` (que pegava `"saquarema"` ou `"caro"` no fim da frase).
  - Extrair lista `ItensBusca` separando por vírgula e `" e "` quando `Intencao == MontarCesta` ou múltiplos produtos forem citados.
  - Em `ExtrairLoja`: remover verbos de cauda (`"vende"`, `"tem"`, `"fica"`, `"possui"`, `"esta"`).

### 3.4. Novas Rotinas de Carga no `MotorIA.cs`
1. `CarregarCestaAsync(ContextoIA ctx, CancellationToken ct)`:
   - Para cada item em `ctx.ItensBusca` (ex: `["arroz", "feijao", "oleo"]`), busca produtos e ofertas ativas.
   - Agrupa por `LojaId`: calcula quantos itens da cesta a loja possui, a soma dos menores preços de cada item na loja e a distância.
   - Gera `CandidatoIA` por loja com `Titulo = $"{NomeLoja} ({itensEncontrados}/{totalItens} itens da cesta)"`, `Preco = somaPreco`, e `Motivos` detalhando o preço de cada item encontrado.
2. `CarregarProdutosDaLojaAsync(ContextoIA ctx, CancellationToken ct)`:
   - Localiza a loja por `ctx.LojaTermo` (ou `ctx.LojaIdUsuario` se o usuário disser `"minha loja"`).
   - Busca os produtos/ofertas dessa loja e popula `ctx.Candidatos` com `ProdutoId`, `LojaId`, `Preco`, `EmPromocao`.
3. `CarregarConcorrenciaLojistaAsync(ContextoIA ctx, CancellationToken ct)`:
   - Usa `ctx.LojaIdUsuario`. Lista os produtos/ofertas da loja do usuário.
   - Busca ofertas ativas de outras lojas para produtos de mesmo nome/termo.
   - Monta candidatos destacando a diferença percentual/nominal entre o preço do lojista e o menor preço da concorrência.

### 3.5. Otimização de Banco e RAG
- `IAvaliacaoRepositorio` / `AvaliacaoRepositorio` / `IAvaliacaoServico` / `AvaliacaoServico`:
  - Novo método `Task<Dictionary<Guid, (double Media, int Quantidade)>> ObterResumosPorLojasAsync(IEnumerable<Guid> lojaIds)`.
- `IEmbeddingService.GerarEmbeddingAsync(string texto, CancellationToken ct = default, bool isQuery = false)`:
  - Em `GeminiEmbeddingService`, quando `isQuery == true`, envia `taskType = "RETRIEVAL_QUERY"` e faz no máximo 1 tentativa sem `Task.Delay(45s)` (para nunca prender o chat). Na indexação em background (`isQuery == false`), mantém `RETRIEVAL_DOCUMENT` e o retry de 45s.
- `DocumentoRagRepositorio`:
  - Remover `CountAsync` antes da busca vetorial e remover `#region agent log`.
- `ProdutoRepositorio.BuscarPorTermosAsync`:
  - Otimizar e manter `TentarFormaComAcento` expandido para incluir `"acucar" -> "açúcar"`, `"oleo" -> "óleo"`, `"macarrao" -> "macarrão"`, `"linguica" -> "linguiça"`, `"pao" -> "pão"`, `"agua" -> "água"`, `"sabao" -> "sabão"`, `"limao" -> "limão"`.
∘
### 3.6. Frontend (`Mobile/src/services/iaService.js` & `Mobile/src/screens/user/AiChatScreen.js`)
- `iaService.js`:
  - `enviarPerguntaIA(mensagem, { incluirLocalizacao = true, usuarioId, lojaId, modoOperacional, historico = [] })`: sempre inclui localização com `allowCached: true` e envia `historico` (últimas 6 mensagens), `lojaId` e `modoOperacional`.
- `AiChatScreen.js`:
  - Exibir chips clicáveis de sugestão acima do input (ou quando só houver a mensagem de boas-vindas).
  - Para cada mensagem da IA com `item.resultados?.length > 0`, renderizar até 5 cards compactos abaixo do texto:
    - Título, nome da loja, badge `"PROMO"` se `emPromocao`, preço formatado e distância em km.
    - Botão `"Ver Produto"` (se `r.produtoId` existir -> `navigation.navigate('ProductDetail', { productId: r.produtoId })`).
    - Botão `"Ver Loja"` (se `r.lojaId` existir -> `navigation.navigate('StoreCatalog', { lojaId: r.lojaId, lojaNome: r.loja })`).
  - No `AdminDebugPanel`: mostrar também `Provedor LLM: {item.provedorLlm}`.
