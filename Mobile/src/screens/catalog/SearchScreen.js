import { FlatList, ActivityIndicator, View, StyleSheet } from 'react-native';
import { useCallback, useState, useMemo, useEffect, useRef } from 'react';
import { useFocusEffect, useNavigation } from '@react-navigation/native';
import { listarFeedComStale, listarFeed } from '../../services/feedService';
import { listarLojasMapa } from '../../services/lojaService';
import { registrarPesquisa } from '../../services/historicoService';
import { obterLocalizacaoAtual } from '../../services/locationService';
import { useAuth } from '../../context/AuthContext';
import { useTheme } from '../../context/ThemeContext';
import { useLayoutProfile } from '../../hooks/useLayoutProfile';
import { mapApiError } from '../../utils/apiErrorUtils';
import LojasMapView from '../../components/LojasMapView';
import ProductGridCard from '../../components/feed/ProductGridCard';
import MapSearchOverlay, {
  MODO_LISTA,
  MODO_MAPA,
  MAP_SEARCH_OVERLAY_HEIGHT,
} from '../../components/feed/MapSearchOverlay';
import {
  FormScreen,
  FormField,
  PrimaryButton,
  ListCardText,
  FormTabs,
} from '../../components/form';
import { montarMapaBuscaPorLoja } from '../../utils/mapaPinUtils';

const DEBOUNCE_BUSCA_MS = 400;
const PAGE_SIZE = 20;
const MAX_PRODUTOS_POR_PIN = 4;
const GRID_PADDING_H = 8;
const GRID_GAP = 8;

function feedItemParaCard(item) {
  return {
    id: item.id,
    nome: item.nome,
    imagemUrl: item.imagemUrl,
    lojaId: item.lojaId,
    preco: item.preco,
    lojaNomeFantasia: item.lojaNome,
  };
}

function feedItemParaOferta(item) {
  return {
    preco: item.preco,
    precoAnterior: item.precoAnterior,
    emPromocao: item.emPromocao,
  };
}

export default function SearchScreen() {
  const navigation = useNavigation();
  const [termoBusca, setTermoBusca] = useState('');
  const [produtos, setProdutos] = useState([]);
  const [lojas, setLojas] = useState([]);
  const [modoVisualizacao, setModoVisualizacao] = useState(MODO_MAPA);
  const [loading, setLoading] = useState(true);
  const [buscando, setBuscando] = useState(false);
  const [carregandoMais, setCarregandoMais] = useState(false);
  const [page, setPage] = useState(1);
  const [hasNext, setHasNext] = useState(false);
  const [feedError, setFeedError] = useState(null);
  const { session, isCliente, sincronizarGpsCliente } = useAuth();
  const { colors } = useTheme();
  const { gridColumns, useSidebarNav, width, sidebarWidth } = useLayoutProfile();

  const isSingleProduct = produtos.length === 1;
  const listColumns = isSingleProduct ? 1 : gridColumns;

  const singleCardWidth = useMemo(() => {
    const listAreaWidth = useSidebarNav ? width - sidebarWidth : width;
    return Math.floor(
      (listAreaWidth - GRID_PADDING_H * 2 - GRID_GAP * (gridColumns - 1)) / gridColumns
    );
  }, [width, sidebarWidth, useSidebarNav, gridColumns]);

  const clienteId = session?.perfil?.id;
  const buscaIdRef = useRef(0);
  const feedErroLogadoRef = useRef(false);
  const termoAtivo = termoBusca.trim();

  const carregarLojasMapa = useCallback(async () => {
    try {
      const coords = await obterLocalizacaoAtual();
      const lista = await listarLojasMapa(coords.latitude, coords.longitude, 15);
      setLojas(Array.isArray(lista) ? lista : []);
    } catch {
      setLojas([]);
    }
  }, []);

  const carregarFeed = useCallback(
    async (termo = '', pagina = 1, append = false, useStale = false) => {
      const loader = pagina === 1 && !append ? setLoading : setCarregandoMais;
      loader(true);
      try {
        let resultado;
        if (useStale && pagina === 1 && !append) {
          const res = await listarFeedComStale({
            page: pagina,
            pageSize: PAGE_SIZE,
            termo,
          });
          resultado = res.data;
        } else {
          resultado = await listarFeed({
            page: pagina,
            pageSize: PAGE_SIZE,
            termo,
            force: true,
          });
        }

        setPage(resultado.page);
        setHasNext(resultado.hasNext);
        setProdutos((prev) =>
          append ? [...prev, ...resultado.items] : resultado.items
        );
        setFeedError(null);
        feedErroLogadoRef.current = false;
      } catch (error) {
        if (!feedErroLogadoRef.current) {
          console.error('carregar feed:', error.message);
          feedErroLogadoRef.current = true;
        }
        if (pagina === 1 && !append) {
          setFeedError(mapApiError(error));
        }
      } finally {
        loader(false);
        setBuscando(false);
      }
    },
    []
  );

  useFocusEffect(
    useCallback(() => {
      carregarFeed(termoAtivo, 1, false, true);
      carregarLojasMapa();
      if (isCliente) sincronizarGpsCliente();
    }, [termoAtivo, isCliente, carregarFeed, carregarLojasMapa, sincronizarGpsCliente])
  );

  const executarBusca = useCallback(
    async (termo) => {
      const buscaId = ++buscaIdRef.current;
      setBuscando(true);
      await carregarFeed(termo, 1, false, false);
      if (buscaId !== buscaIdRef.current) return;
    },
    [carregarFeed]
  );

  useEffect(() => {
    if (!termoAtivo) {
      buscaIdRef.current += 1;
      setBuscando(false);
      carregarFeed('', 1, false, true);
      return undefined;
    }

    const timer = setTimeout(() => executarBusca(termoAtivo), DEBOUNCE_BUSCA_MS);
    return () => clearTimeout(timer);
  }, [termoAtivo, executarBusca, carregarFeed]);

  async function handleSearch() {
    if (!termoAtivo) {
      await carregarFeed('', 1, false, true);
      return;
    }

    if (isCliente) sincronizarGpsCliente()?.catch?.(() => {});
    await executarBusca(termoAtivo);

    if (clienteId) {
      registrarPesquisa(clienteId, termoAtivo).catch(() => {});
    }
  }

  function carregarMais() {
    if (!hasNext || carregandoMais || loading) return;
    carregarFeed(termoAtivo, page + 1, true, false);
  }

  const lojaIdsDestaque = useMemo(() => {
    if (!termoAtivo) return null;
    const ids = new Set(produtos.map((p) => p.lojaId).filter(Boolean));
    return [...ids].map(String);
  }, [produtos, termoAtivo]);

  const { produtosPorLoja, imagemPinPorLoja } = useMemo(
    () => montarMapaBuscaPorLoja(produtos, termoAtivo, MAX_PRODUTOS_POR_PIN),
    [produtos, termoAtivo]
  );

  function abrirProduto(productId, produto) {
    const id = productId ?? produto?.id ?? produto?.Id;
    if (!id) return;

    if (clienteId && termoAtivo) {
      registrarPesquisa(clienteId, termoAtivo, {
        produtoId: id,
        lojaId: produto?.lojaId ?? null,
      }).catch(() => {});
    }
    navigation.navigate({
      name: 'ProductDetail',
      params: { productId: String(id) },
      merge: true,
    });
  }

  function abrirProdutoDoMapa(productId) {
    const produto = produtos.find((p) => String(p.id) === String(productId));
    abrirProduto(productId, produto);
  }

  function renderItem({ item }) {
    const card = (
      <ProductGridCard
        produto={feedItemParaCard(item)}
        oferta={feedItemParaOferta(item)}
        onPress={() => abrirProduto(item.id, item)}
        fillCell={!isSingleProduct}
      />
    );

    if (isSingleProduct) {
      return <View style={{ width: singleCardWidth }}>{card}</View>;
    }

    return card;
  }

  function renderClassicLayout() {
    return (
      <>
        <View style={styles.searchRow}>
          <View style={styles.searchInputWrap}>
            <FormField
              label=""
              value={termoBusca}
              onChangeText={setTermoBusca}
              placeholder="Buscar produtos..."
              onSubmitEditing={handleSearch}
              returnKeyType="search"
              compact
            />
          </View>
          <PrimaryButton label="Buscar" onPress={handleSearch} style={styles.searchButton} />
        </View>

        <FormTabs
          options={[
            { value: MODO_MAPA, label: 'Mapa' },
            { value: MODO_LISTA, label: 'Lista' },
          ]}
          value={modoVisualizacao}
          onChange={setModoVisualizacao}
        />

        {buscando ? (
          <ActivityIndicator size="small" color={colors.primary} style={styles.buscandoIndicator} />
        ) : null}

        {renderContent(false)}
      </>
    );
  }

  function renderSidebarLayout() {
    return (
      <View style={styles.mapShell}>
        {renderContent(true)}
        <MapSearchOverlay
          termoBusca={termoBusca}
          onChangeText={setTermoBusca}
          onSubmit={handleSearch}
          modoVisualizacao={modoVisualizacao}
          onModoChange={setModoVisualizacao}
          buscando={buscando}
        />
      </View>
    );
  }

  function renderContent(edgeToEdge) {
    if (loading) {
      return (
        <ActivityIndicator
          size="large"
          color={colors.primary}
          style={edgeToEdge ? styles.loadingOverlay : { marginTop: 24 }}
        />
      );
    }

    if (modoVisualizacao === MODO_MAPA) {
      return (
        <LojasMapView
          lojas={lojas}
          lojaIdsDestaque={lojaIdsDestaque}
          produtosPorLoja={produtosPorLoja}
          imagemPinPorLoja={imagemPinPorLoja}
          onProductPress={abrirProdutoDoMapa}
          edgeToEdge={edgeToEdge}
        />
      );
    }

    return (
      <FlatList
        style={[styles.gridList, { backgroundColor: colors.listBackground }]}
        contentContainerStyle={[
          styles.gridContent,
          edgeToEdge && { paddingTop: MAP_SEARCH_OVERLAY_HEIGHT + 16 },
          isSingleProduct && styles.gridContentSingle,
        ]}
        data={produtos}
        keyExtractor={(item) => String(item.id)}
        renderItem={renderItem}
        numColumns={listColumns}
        key={`grid-${listColumns}-${isSingleProduct ? 'single' : 'multi'}`}
        columnWrapperStyle={listColumns > 1 ? styles.gridRow : undefined}
        showsVerticalScrollIndicator={false}
        keyboardShouldPersistTaps="handled"
        windowSize={5}
        maxToRenderPerBatch={10}
        removeClippedSubviews
        onEndReached={carregarMais}
        onEndReachedThreshold={0.4}
        ListFooterComponent={
          carregandoMais ? (
            <ActivityIndicator size="small" color={colors.primary} style={{ marginVertical: 12 }} />
          ) : null
        }
        ListEmptyComponent={
          <ListCardText style={[styles.emptyText, { color: colors.textMuted }]}>
            {termoAtivo
              ? 'Nenhum produto encontrado para essa busca.'
              : 'Nenhum produto cadastrado ainda.'}
          </ListCardText>
        }
      />
    );
  }

  return (
    <FormScreen
      title="Buscar produtos"
      subtitle="Encontre as melhores ofertas"
      scrollable={false}
      fullBleed={useSidebarNav}
      hideHeader={useSidebarNav}
      noBodyPadding={useSidebarNav}
      error={feedError}
    >
      {useSidebarNav ? renderSidebarLayout() : renderClassicLayout()}
    </FormScreen>
  );
}

const styles = StyleSheet.create({
  mapShell: {
    flex: 1,
    position: 'relative',
  },
  searchRow: {
    flexDirection: 'row',
    alignItems: 'flex-end',
    gap: 8,
    marginBottom: 12,
  },
  searchInputWrap: { flex: 1 },
  searchButton: { marginBottom: 10, paddingHorizontal: 16, paddingVertical: 12 },
  buscandoIndicator: { marginVertical: 4 },
  loadingOverlay: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },
  gridList: { flex: 1 },
  gridContent: { paddingHorizontal: GRID_PADDING_H, paddingTop: 8, paddingBottom: 16 },
  gridContentSingle: { alignItems: 'center' },
  gridRow: { gap: GRID_GAP },
  emptyText: { textAlign: 'center', marginTop: 24 },
});
