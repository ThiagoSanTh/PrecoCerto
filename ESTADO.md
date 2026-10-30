# Estado do Projeto — Preço Certo

## Arquitetura Resumida
- **Mobile**: React Native / Expo SDK 54 (Android, iOS, Web Vercel), React Navigation, Context API.
- **Backend**: .NET 8 (Web API, Ef Core, SignalR, Serilog), Npgsql, PostgreSQL + pgvector (Supabase).
- **Inteligência**: MotorIA v2 determinístico (`/api/IA/analisar`) + RAG auxiliar (Embeddings Gemini/OpenAI).
- **Deploy**: Railway (API Docker raiz), Vercel (Web `Mobile/dist`).

## Skills Instaladas (.agents/skills)
- `obra/superpowers`: brainstorming, writing-plans, executing-plans, test-driven-development, systematic-debugging, verification-before-completion, requesting-code-review, using-superpowers.
- `anthropics/skills`: frontend-design, skill-creator.
- `vercel-labs/agent-skills`: vercel-react-best-practices, web-design-guidelines.

## Decisões Recentes
1. Higienização: removidos do Git `node_modules`, `.expo`, `appsettings` locais e `.env`.
2. Limpeza: eliminados arquivos mortos, duplicatas de deploy (BackEnd/Dockerfile/railway.toml), scripts SQL manuais redundantes e infra de benchmark.
3. Barrel mantido: `Mobile/src/components/form/index.js` preservado para compatibilidade das 23 telas.
4. Solução: `Pc.RagReindexCli` incorporado a `PrecoCerto.slnx`.
5. Interatividade e Acessibilidade Frontend:
   - Suporte nativo à tecla Enter na Web e Mobile no `ChatScreen` e no `AiChatScreen` (assistente inteligente).
   - `FormField` com `forwardRef`, detecção de Enter via `onKeyPress` para submissão imediata e avanço de foco.
   - Telas de autenticação (`LoginScreen`, `RegisterScreen`, `ForgotPasswordScreen`) e telas de perfil/loja configuradas com navegação sequencial por teclado e envio com Enter.
   - `PrimaryButton` e `SecondaryButton` enriquecidos com atributos ARIA e papéis de acessibilidade (`accessibilityRole="button"`, estados de desabilitado e ocupado).
6. Inicialização do Mapa em Saquarema e Destravamento de Navegação:
   - Coordenadas de referência padrão unificadas para Saquarema/RJ (`-22.9329, -42.5114`, zoom 13) em `mapaUtils.js` e `leafletMapHtml.js`.
   - Abertura do app/site agora inicializa diretamente na cidade de Saquarema sem travar a câmera ou limitar limites geográficos.
   - Habilitadas todas as interações e controles: zoom in/out (scroll do mouse, gestos de pinça, botões `+`/`-`), arrastar e mover livremente para qualquer outra cidade.
   - Controles de zoom do Leaflet reposicionados para `bottomright` para evitar sobreposição pelos cabeçalhos/buscas do feed.
   - Corrigido `scrollEnabled={true}` no `WebView` nativo em `LeafletMapFrame.js` para garantir recepção de gestos de toque no iOS/Android.
7. Integração do Novo Design System e Experiência Funcional:
   - Tokens visuais atualizados em `Mobile/src/theme/index.js` (`primary: #0E7C66`, `highlight: #B6F035`, `ink: #0F1D22`, `primarySoft: #E2F5EE`, `radii`, `shadows`) e tipografia `Sora` + `Manrope` em `Mobile/App.js`.
   - AppShell redesenhado em `CustomTabBar.js` com sidebar escura (`ink`) no desktop (seções `CLIENTE` e `LOJISTA` com alternância real via `setAppMode`, badge `Novo` na IA, badge de não lidas em Mensagens e cartão do usuário) e barra inferior flutuante em pílula no mobile.
   - Utilitários puros testados (`Mobile/src/utils/geo.js` e `Mobile/src/utils/insights.js` + suíte `node --test` com 11 testes automatizados).
   - Telas integradas ao novo design com dados 100% reais: `LoginScreen`, `RegisterScreen` (split layout), `SearchScreen` (split `440px + 1fr` com mapa Leaflet real, distância Haversine, banner de economia, filtros e card flutuante com rota Google Maps, favoritar e chat), `AiChatScreen` (tabela estruturada do `MotorIA`), `FavoritosScreen` (abas Produtos/Lojas e promoções), `HistoricoScreen` (KPIs reais, agrupamento por data e exclusão individual), `ConversasScreen` & `ChatScreen` (mestre-detalhe com `UltimaMensagemTexto` via SignalR/API), `ProfileScreen`, `MerchantDashboardScreen` (Painel BI real do lojista) e `ProductsScreen` (tabela/cards com estoque, posição de preço e edição).
   - Incrementos seguros no Backend `.NET 8`: `UltimaMensagemTexto` adicionado em `ConversaRespostaDto` e verificação de propriedade (`Authz.IsSelfOrAdmin`) em `DELETE /api/HistoricoPesquisa/{id}`.

## Próximos Passos
- Conforme demanda do usuário para novas telas, componentes ou integrações.
