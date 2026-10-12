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

## Próximos Passos
- Conforme demanda do usuário para novas telas, componentes ou integrações.
