# PRODUCT.md — Preço Certo (Saquarema · RJ)

## Visão do Produto
Plataforma hiper-local de comparação de preços e inteligência comercial para **Saquarema · RJ**, conectando consumidores caçadores de ofertas e pequenos/médios lojistas locais em uma única arquitetura adaptativa (`Mobile/` com Expo + React Native Web).

## Personas Principais
1. **Consumidor Local (Modo Cliente)**:
   - Busca itens individuais ou cestas completas (via **Assistente IA**) ordenados por menor preço ou menor distância/tempo de caminhada.
   - Visualiza ofertas em lista e mapa interativo (`SearchScreen` + `LojasMapView`), acompanha histórico de preços (`PriceHistoryBlock`), salva favoritos (`FavoritosScreen`) e tira dúvidas de estoque via chat em tempo real (`ConversasScreen` / `ChatScreen`).
2. **Comerciante Local (Modo Lojista)**:
   - Acompanha KPIs reais de competitividade e cruzamento entre termos mais buscados na cidade vs. catálogo próprio (`MerchantDashboardScreen`).
   - Gerencia catálogo (`ProductsScreen`, `CreateProductScreen`), promoções (`PromocaoSection`, `CreateOfertaScreen`) e equipe de vendedores (`VendedoresScreen`).

## Diferenciação e Adaptação por Aparelho (`deviceProfile.js` / `useLayoutProfile.js`)
O sistema identifica em tempo de execução o **Sistema Operacional** (`android`, `ios`, `windows`, `macos`, `linux`), **Ambiente** (`isNative`, `isMobileWeb`, `isPWA`, `isWeb`), **Ponteiro** (`isTouch`, `hasHover`) e **Classe de Aparelho**:
- **`compactPhone` (`< 380px`)**: `Bottom Pill Bar`, padding horizontal `12px`, títulos compactos (`22px`), listas empilhadas e alvos de toque `44px–48px`.
- **`phone` (`380px–767px`)**: `Bottom Pill Bar` com `safe-area-inset-bottom`, alternador `Ofertas | Mapa`, navegação em pilha (`Stack`) e `keyboardBehavior` nativo (`padding` no iOS, `height` no Android).
- **`tabletPortrait` (`768px–1023px`)**: Sidebar compacta (`208px`), mas **`useSplitView = false`** e **`useTableView = false`** (`availableContentWidth < 760px`), usando cards responsivos em `ProductsScreen` e navegação em pilha em `ConversasScreen` para nunca esmagar colunas.
- **`tabletLandscape` (`>= 1024px` touch/nativo)**: Sidebar completa (`256px`), `useSplitView = true` (Lista + Mapa e Lista + Chat lado a lado) e `useTableView = true` com alvos de toque `>= 44px`.
- **`desktop` / `wideDesktop` (`>= 1024px` / `>= 1440px`)**: Sidebar `256px`, split-views ativos, tabelas densas de 6 colunas e suporte a `maxContentWidth` para leitura confortável.
