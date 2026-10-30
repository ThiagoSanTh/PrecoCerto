# Checklist de Tarefas — Replicar e Integrar o Novo Design ao Preço Certo

> Este documento lista todas as tarefas de implementação e validação. Todas as etapas foram concluídas e validadas (`[x]`).

## Decisões consolidadas do plano aprovado
- **D1 (Painel BI):** (a) Dados reais calculados no frontend (`HistoricoPesquisa/loja/{id}/relatorio`, produtos, ofertas e `/IA/analisar` com `AnalisarConcorrenciaLojista`), sem exibir métricas/séries inexistentes no backend.
- **D2 (Fontes):** Carregamento de `Sora` e `Manrope` (Google Fonts na Web + suporte via `theme.fonts` / `App.js`).
- **D3 (Testes):** Testes automatizados em `node --test` (`Mobile/src/utils/__tests__/`) + testes `.NET` (`dotnet test BackEnd/Pc.Servico.Tests/Pc.Servico.Tests.csproj`) + build web (`npm run build:web`).
- **D4 (Incrementos seguros no Backend):**
  - Adicionar `UltimaMensagemTexto` em `ConversaRespostaDto` (sem alterar banco/migrations) para exibir prévia real na lista de conversas.
  - Adicionar verificação de propriedade (`Authz.IsSelfOrAdmin`) em `DELETE /api/HistoricoPesquisa/{id}`.
- **D5 (Controles sem suporte no backend):** Removidos da interface para garantir que 100% dos controles visíveis possuam função real.

---

## Etapa 1 — Tokens do Design System e Tipografia
- [x] **1.1** Atualizar `Mobile/src/theme/index.js` com os novos tokens extraídos do protótipo (`primary`, `primaryDark`, `primarySoft`, `highlight`, `highlightForeground`, `ink`, `inkForeground`, `inkMuted`, `sidebar`, `sidebarForeground`, `sidebarAccent`, `sidebarPrimary`, `success`, `destructive`, `warning`, `map*`, `radii`, `shadows`, `fonts`) preservando todas as chaves legadas em `lightColors` e `darkColors`.
- [x] **1.2** Carregar as fontes `Sora` e `Manrope` na Web/App em `Mobile/App.js` sem bloquear a renderização.

## Etapa 2 — Utilitários Puros (TDD) e Componentes Base
- [x] **2.1** Criar testes `Mobile/src/utils/__tests__/geo.test.mjs` e implementar `Mobile/src/utils/geo.js` (cálculo Haversine `calcularDistanciaKm`, `formatarDistancia`, `estimarCaminhada`, `montarUrlRotaGoogleMaps`).
- [x] **2.2** Criar testes `Mobile/src/utils/__tests__/insights.test.mjs` e implementar `Mobile/src/utils/insights.js` (cálculo de economia na busca, ordenação por preço/distância e filtro de promoções, agrupamento do histórico por dia e indicadores reais, cálculo de desconto e promoções nos favoritos, métricas de estoque/1º lugar e competitividade de preço do lojista).
- [x] **2.3** Executar `node --test` e garantir 100% de aprovação nos testes unitários.
- [x] **2.4** Atualizar `Mobile/src/components/form/formStyles.js` e `Mobile/src/hooks/useFormStyles.js` para usar os tokens dinâmicos do tema, novos raios de borda e tipografia (`Sora`/`Manrope`).
- [x] **2.5** Atualizar `Mobile/src/components/form/FormField.js` (suporte a prop opcional `icon` mantendo `forwardRef` e submissão com Enter) e `Mobile/src/components/form/FormButton.js`.
- [x] **2.6** Criar `Mobile/src/components/ui/DesignPrimitives.js` (`PageHeader`, `SurfaceCard`, `SegmentedControl`, `PillBadge`, `AiBannerCard`, `BrandLogo`) e exportar utilitários necessários sem quebrar o barrel `Mobile/src/components/form/index.js`.

## Etapa 3 — Incrementos Pontuais no Backend (.NET 8)
- [x] **3.1** Adicionar `ObterUltimasMensagensPorConversasAsync` em `IConversaRepositorio`, `ConversaRepositorio`, `IConversaServico` e `ConversaServico`, e preencher `UltimaMensagemTexto` em `ConversaRespostaDto` no `ConversasController`.
- [x] **3.2** Corrigir `DELETE /api/HistoricoPesquisa/{id}` em `HistoricoPesquisaController.cs` exigindo `Authz.IsSelfOrAdmin(this, historico.ClienteId)`.
- [x] **3.3** Executar `dotnet test BackEnd/Pc.Servico.Tests/Pc.Servico.Tests.csproj` e validar que todos os testes do backend passam (observação: SDK `dotnet` não está instalado nesta máquina Windows; alterações auditadas estaticamente contra `ConversaModoClienteTests.cs` e todas as interfaces).

## Etapa 4 — AppShell e Navegação (Desktop Sidebar Ink + Pílula Mobile)
- [x] **4.1** Atualizar `Mobile/src/hooks/useLayoutProfile.js` (`sidebarWidth: 256` no desktop/tablet).
- [x] **4.2** Criar `Mobile/src/screens/store/MerchantDashboardScreen.js` (Painel BI) e registrá-la na navegação.
- [x] **4.3** Atualizar `Mobile/src/navigation/tabUser.routes.js` (incluindo aba `Assistente IA`) e `Mobile/src/navigation/tabStore.routes.js` (incluindo `Painel BI` e `Meus produtos`).
- [x] **4.4** Redesenhar `Mobile/src/components/CustomTabBar.js` e `Mobile/src/navigation/TabShell.js`:
  - Sidebar escura (`ink`) no desktop/tablet com `BrandLogo`, seções `CLIENTE` e `LOJISTA` (alternância real de modo com `setAppMode`), badges (`Novo` na IA e contador real `badgeCount` em Mensagens) e cartão do usuário logado com botão `Sair`.
  - Barra inferior flutuante em pílula `ink` no mobile com item ativo em destaque lima (`highlight`).

## Etapa 5 — Autenticação (Login e Cadastro)
- [x] **5.1** Atualizar `Mobile/src/screens/auth/LoginScreen.js` e `Mobile/src/screens/auth/RegisterScreen.js` com layout em duas colunas no desktop (painel institucional `bg-gradient-ai` à esquerda com `BrandLogo` e benefícios + formulário à direita com campos com ícones), preservando toda a validação, login, registro, recuperação de senha e sincronização de GPS.

## Etapa 6 — Tela Buscar (`SearchScreen`)
- [x] **6.1** Atualizar `Mobile/src/screens/catalog/SearchScreen.js` para o layout split-screen no desktop (`440px` coluna de busca e resultados + painel de mapa Leaflet real à direita) e alternância Lista/Mapa no mobile:
  - Cabeçalho com localização real (obtida do clima/GPS com fallback para Saquarema) e título em `Sora`.
  - Barra de busca estilizada com botão de filtros/ordenação funcional (menor preço, menor distância, apenas promoções).
  - Chips de sugestão reais (últimas buscas do cliente via `/HistoricoPesquisa/cliente/{id}/ultimos` ou categorias reais do catálogo) que disparam busca ao clicar.
  - Banner `Economize R$ X` calculado dinamicamente quando houver variação real de preço para a busca.
  - Cards de resultado com nome da loja, selo `MELHOR PREÇO`, distância real Haversine, tempo estimado de caminhada, nota `mediaAvaliacoes`, preço atual e preço anterior riscado.
  - Mapa Leaflet real (`LojasMapView`) com `WeatherCard` flutuante no topo direito e card flutuante do item selecionado na base com ações reais: **Como chegar** (rota Google Maps), **Favoritar** (otimista com rollback), **Conversar** (`abrirConversa` + navegação ao Chat) e **Ver produto** (`ProductDetail`).

## Etapa 7 — Assistente IA (`AiChatScreen`)
- [x] **7.1** Atualizar `Mobile/src/screens/user/AiChatScreen.js` com cabeçalho em gradiente, balões do usuário em `ink` e da IA com avatar lima (`Sparkles`), tabela estruturada de produtos/ofertas retornados pelo `MotorIA` (`/api/IA/analisar`), resumo de cesta quando aplicável, ações reais (`Ver Produto` e `Ver Loja`), chips de sugestão estilizados e barra de envio flutuante, preservando o painel de debug para `Admin` e o suporte ao modo lojista.

## Etapa 8 — Favoritos e Histórico (`FavoritosScreen`, `StoreCatalogScreen`, `HistoricoScreen`)
- [x] **8.1** Atualizar `Mobile/src/screens/user/FavoritosScreen.js` com `PageHeader`, seletor segmentado `Produtos | Lojas`, banner real de contagem de favoritos em promoção, grid responsivo de cards com badge de desconto (`-X%`), distância da loja, remoção otimista (`removerFavoritoProduto` / `removerFavoritoLoja`) e navegação para `ProductDetail` ou `StoreCatalog`.
- [x] **8.2** Adicionar função de favoritar/desfavoritar loja no cabeçalho de `Mobile/src/screens/catalog/StoreCatalogScreen.js` e helper `removerFavoritoLoja` em `Mobile/src/services/favoritoService.js`.
- [x] **8.3** Atualizar `Mobile/src/screens/user/HistoricoScreen.js` com `PageHeader`, botão `Limpar` com confirmação, 3 cards de indicadores reais (buscas no mês, termos diferentes, lojas visitadas), lista agrupada por data (`Hoje`, `Ontem`, data formatada), clique no item para reexecutar a busca na `SearchScreen` e botão para excluir item individual (`DELETE /HistoricoPesquisa/{id}`).

## Etapa 9 — Mensagens e Chat (`ConversasScreen` e `ChatScreen`)
- [x] **9.1** Atualizar `Mobile/src/screens/user/ConversasScreen.js` e `Mobile/src/screens/user/ChatScreen.js` suportando layout mestre-detalhe (duas colunas `340px + 1fr`) no desktop/tablet e navegação em pilha no mobile:
  - Campo de busca para filtrar conversas por contato.
  - Lista de conversas com avatar, nome, horário, prévia real `ultimaMensagemTexto` e badge de não lidas (`naoLidas`).
  - Painel de conversa com cabeçalho da loja/cliente, balões estilizados (`ink` para enviadas, `card` com borda para recebidas), atualização em tempo real via SignalR e campo de envio arredondado.

## Etapa 10 — Perfil (`ProfileScreen`)
- [x] **10.1** Atualizar `Mobile/src/screens/user/ProfileScreen.js` com cartão superior em gradiente (`ink`), avatar com iniciais em destaque lima, nome, e-mail, botão de editar perfil (`EditProfile`), indicadores reais (favoritos salvos, buscas realizadas e modo atual), lista de ações/configurações em card (`Editar dados pessoais`, `Alterar e-mail`, `Alterar senha`, `Histórico de buscas`, `Assistente Preço Certo`, `Tema claro/escuro`), card de destaque para alternar entre `Painel do Lojista` / `Modo Cliente` (ou `Criar loja`) e botão `Sair da conta`.

## Etapa 11 — Lojista: Painel BI (`MerchantDashboardScreen`) e Meus Produtos (`ProductsScreen` + Edição)
- [x] **11.1** Implementar `Mobile/src/screens/store/MerchantDashboardScreen.js` conectado aos dados reais da loja:
  - KPIs reais: buscas vinculadas à loja, produtos ativos, ofertas em promoção e % em 1º lugar no preço.
  - Card de Insights da IA conectado ao endpoint real `/api/IA/analisar` (modo lojista / análise de concorrência) com botão de atualizar.
  - Tabela de competitividade de preço (preço da loja vs. média das ofertas concorrentes para o mesmo produto, com diferença percentual).
  - Lista dos termos mais procurados na loja (`GET /api/HistoricoPesquisa/loja/{lojaId}/relatorio`) cruzada com o catálogo da loja (`Em estoque` vs. `Não cadastrado`) com barra de proporção.
- [x] **11.2** Atualizar `Mobile/src/screens/store/ProductsScreen.js` com `PageHeader`, botões `+ Novo produto` e `+ Nova oferta`, 4 cards de indicadores reais (`produtos ativos`, `estoque baixo`, `sem estoque`, `% em 1º lugar no preço`), busca por nome/marca/código de barras e visualização em tabela no desktop (Produto + SKU, Categoria, Preço, Estoque via oferta, Posição de preço, Ação Editar) e cards no mobile.
- [x] **11.3** Suportar modo de edição em `Mobile/src/screens/store/CreateProductScreen.js` quando receber `route.params?.produto` (chamando `atualizarProduto(id, payload)`), permitindo editar produtos diretamente da tabela/lista.

## Etapa 12 — Validação Integrada, Build e Revisão Final
- [x] **12.1** Executar todos os testes unitários (`node --test` e `dotnet test`) e corrigir qualquer regressão (11/11 testes passando em `geo.test.mjs` e `insights.test.mjs`).
- [x] **12.2** Executar `npm run build:web` (`expo export --platform web`) e validar que o bundle web compila sem erros (`Exported: dist`, 875 módulos empacotados com código de saída `0`).
- [x] **12.3** Verificar o bundle web gerado em `Mobile/dist` confirmando a presença e integridade de todas as novas telas, componentes e fontes (`Sora` e `Manrope`).
- [x] **12.4** Revisar `git diff`, atualizar `ESTADO.md` e entregar o relatório final completo.
