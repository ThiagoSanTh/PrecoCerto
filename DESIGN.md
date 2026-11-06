# DESIGN.md — Design System & Diretrizes Visuais (Preço Certo)

## 1. Identidade & Direção Estética
- **Conceito**: Inteligência de varejo hiper-local com alto contraste e legibilidade sob luz solar (uso em rua/mercado) e conforto visual em ambientes internos (Dark Mode).
- **Tipografia**:
  - **Display (`Sora` 600/700)**: Títulos de página (`PageHeader`), preços em destaque, KPIs do painel BI e cabeçalhos hero.
  - **UI & Corpo (`Manrope` 400/500/600/700/800)**: Rótulos, inputs, tabelas, badges e navegação.
  - **Números Tabulares (`fontVariant: ['tabular-nums']`)**: Obrigatório em preços, distâncias, contagens de estoque, rankings e notas de avaliação.

## 2. Tokens Semânticos de Cor (`Mobile/src/theme/index.js`)
Toda superfície deve consumir `const { colors } = useTheme()`. É proibido hardcodar hexadecimais (`#0F172A`, `#F8FAFC`, `#64748B`) diretamente nas telas.

| Token | Light Mode | Dark Mode | Uso Semântico |
|---|---|---|---|
| `background` | `#F4F7F5` | `#0F1D22` | Fundo raiz das telas |
| `surface` / `card` | `#FFFFFF` | `#1C3038` | Cards principais, modais e containers |
| `surfaceSubtle` | `#EEF3F0` | `#16272D` | Cabeçalhos de tabela (`tHead`), caixas de ícone, barras de busca internas |
| `primary` | `#0E7C66` | `#26B597` | Ações primárias, links ativos, preço vencedor |
| `primarySoft` | `#E2F5EE` | `#15342E` | Fundos suaves de seleção e badges `soft`/`primarySoft` |
| `highlight` | `#B6F035` | `#B6F035` | Sinalizador de `"MELHOR PREÇO"`, ícones de marca e destaques IA |
| `ink` | `#0F1D22` | `#0B1519` | Sidebar, banners IA e painéis hero |
| `text` | `#0F1D22` | `#F2FAF6` | Texto principal e títulos (contraste AAA) |
| `textSecondary` | `#5B6E74` | `#8FA4AA` | Alias de `textMuted` (subtítulos e descrições) |
| `textMuted` | `#5B6E74` | `#8FA4AA` | Metadados, timestamps e distâncias |
| `placeholder` | `#98ACB1` | `#8CA1A7` | Placeholders de `<TextInput>` |
| `border` | `#DDE5E1` | `#263C45` | Divisores e bordas de `1px` |
| `success` / `destructive` | `#179A6B` / `#DC3B32` | `#2AD195` / `#F0584E` | Badges de sucesso/promoção e alerta/erro (paleta original preservada) |

## 3. Contratos de Primitivos (`DesignPrimitives.js` & `FormScreen.js`)
- **`SurfaceCard`**: Suporta `padded={false}` ou `noPadding` para tabelas e listas sem margem interna morta.
- **`AiBannerCard`**: Suporta `onPress` (renderizando `<Pressable accessibilityRole="button">`).
- **`PillBadge`**: Variantes `highlight`, `soft`/`primarySoft`, `muted`, `success`, `destructive`.
- **`FormScreen`**: Suporta `maxContentWidth` para limitar a largura máxima em monitores largos (`780px` em Perfil, `820px` em Histórico/IA, `1180px` em Favoritos, `1220px` em Produtos).
- **`StarRating`**: Usa exclusivamente ícones vetoriais `Ionicons` (`star`, `star-half`, `star-outline`) — nunca caracteres Unicode raros como `⯨` (`U+2BE8`).

## 4. Adaptação por Aparelho & Ergonomia (`deviceProfile.js`)
- **Alvos de Toque Mínimos (`minTouchTarget`)**: `48×48px` no Android, `44×44px` no iOS/Touch Web, `36×36px` em Desktop com mouse.
- **Regras de Split-View e Tabela (`availableContentWidth >= 760px`)**:
  - `useSplitView` (`SearchScreen` e `ConversasScreen`) e `useTableView` (`ProductsScreen`) só ativam quando a largura livre descontando a sidebar é `>= 760px` (`width >= 1024px`).
  - Em Tablets em Retrato (`768px–1023px`), a sidebar encolhe para `208px` (`isCompactSidebar`) e o conteúdo principal usa layout de coluna única / cards.
