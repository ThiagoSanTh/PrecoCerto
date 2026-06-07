import { FlatList, Alert, ActivityIndicator, View, StyleSheet } from 'react-native';
import { useCallback, useState, useMemo } from 'react';
import { useFocusEffect, useNavigation } from '@react-navigation/native';
import { listarProdutosParaFeed, buscarProdutosPorNome } from '../services/productService';
import { listarOfertas } from '../services/ofertaService';
import { registrarPesquisa } from '../services/historicoService';
import { useAuth } from '../context/AuthContext';
import SearchMapView from '../components/SearchMapView';
import ProductGridCard from '../components/feed/ProductGridCard';
import {
  FormScreen,
  FormField,
  PrimaryButton,
  ListCardText,
  FormTabs,
} from '../components/form';
import { colors } from '../style';
import {
  filtrarProdutosPorTermo,
  normalizarListaProdutos,
} from '../utils/produtoUtils';
import { mapaOfertasPorProduto } from '../utils/precoUtils';

const MODO_LISTA = 'lista';
const MODO_MAPA = 'mapa';

export default function SearchScreen() {
  const navigation = useNavigation();
  const [termoBusca, setTermoBusca] = useState('');
  const [produtosBase, setProdutosBase] = useState([]);
  const [resultadosBusca, setResultadosBusca] = useState(null);
  const [ofertasMap, setOfertasMap] = useState(new Map());
  const [modoVisualizacao, setModoVisualizacao] = useState(MODO_LISTA);
  const [loading, setLoading] = useState(true);
  const { session, isCliente, sincronizarGpsCliente } = useAuth();

  const clienteId = session?.perfil?.id;

  useFocusEffect(
    useCallback(() => {
      carregar();
      if (isCliente) sincronizarGpsCliente();
    }, [clienteId, isCliente])
  );

  async function carregar() {
    setLoading(true);
    setResultadosBusca(null);
    setModoVisualizacao(MODO_LISTA);
    try {
      const [lista, ofertas] = await Promise.all([
        listarProdutosParaFeed(null),
        listarOfertas().catch(() => []),
      ]);
      setProdutosBase(Array.isArray(lista) ? lista : []);
      setOfertasMap(mapaOfertasPorProduto(ofertas));
    } catch (error) {
      const detalhe =
        error.response?.data?.title ||
        error.response?.data ||
        error.message ||
        'Erro desconhecido';
      console.error('carregar produtos:', detalhe);
      Alert.alert(
        'Erro',
        'Não foi possível carregar os produtos. Verifique se a API está rodando e se a migration AddLojaIdToProduto foi aplicada no banco.'
      );
    } finally {
      setLoading(false);
    }
  }

  async function handleSearch() {
    const termo = termoBusca.trim();

    if (!termo) {
      setResultadosBusca(null);
      setModoVisualizacao(MODO_LISTA);
      return;
    }

    setLoading(true);
    try {
      const local = filtrarProdutosPorTermo(produtosBase, termo);
      let daApi = [];
      try {
        daApi = await buscarProdutosPorNome(termo);
      } catch {
        /* usa só resultado local se API falhar */
      }

      const merged = new Map();
      local.forEach((p) => merged.set(p.id, p));
      daApi.forEach((p) => merged.set(p.id, p));

      const normalizados = normalizarListaProdutos([...merged.values()]);
      setResultadosBusca(normalizados);

      if (normalizados.length > 0) {
        setModoVisualizacao(MODO_MAPA);
      } else {
        setModoVisualizacao(MODO_LISTA);
      }

      if (clienteId) {
        await registrarPesquisa(clienteId, termo);
      }
    } catch {
      Alert.alert('Erro', 'Falha na busca');
    } finally {
      setLoading(false);
    }
  }

  const produtosExibidos = useMemo(() => {
    const base = resultadosBusca ?? produtosBase;
    if (resultadosBusca) return resultadosBusca;
    return filtrarProdutosPorTermo(base, termoBusca);
  }, [produtosBase, resultadosBusca, termoBusca]);

  function abrirProduto(productId) {
    navigation.navigate('ProductDetail', { productId });
  }

  function renderItem({ item }) {
    return (
      <ProductGridCard
        produto={item}
        oferta={ofertasMap.get(item.id)}
        onPress={() => abrirProduto(item.id)}
      />
    );
  }

  const mostrarToggle = resultadosBusca !== null && resultadosBusca.length > 0;

  return (
    <FormScreen
      title="Buscar produtos"
      subtitle="Encontre as melhores ofertas"
      scrollable={false}
    >
      <View style={styles.searchRow}>
        <View style={styles.searchInputWrap}>
          <FormField
            label=""
            value={termoBusca}
            onChangeText={(texto) => {
              setTermoBusca(texto);
              if (!texto.trim()) {
                setResultadosBusca(null);
                setModoVisualizacao(MODO_LISTA);
              }
            }}
            placeholder="Buscar produtos..."
            onSubmitEditing={handleSearch}
            returnKeyType="search"
            compact
          />
        </View>
        <PrimaryButton
          label="Buscar"
          onPress={handleSearch}
          style={styles.searchButton}
        />
      </View>

      {mostrarToggle ? (
        <FormTabs
          options={[
            { value: MODO_MAPA, label: 'Mapa' },
            { value: MODO_LISTA, label: 'Lista' },
          ]}
          value={modoVisualizacao}
          onChange={setModoVisualizacao}
        />
      ) : null}

      {loading ? (
        <ActivityIndicator size="large" color={colors.primary} style={{ marginTop: 24 }} />
      ) : modoVisualizacao === MODO_MAPA && resultadosBusca?.length > 0 ? (
        <SearchMapView
          produtos={resultadosBusca}
          onProductPress={abrirProduto}
        />
      ) : (
        <FlatList
          style={styles.gridList}
          contentContainerStyle={styles.gridContent}
          data={produtosExibidos}
          keyExtractor={(item) => item.id}
          renderItem={renderItem}
          numColumns={2}
          columnWrapperStyle={styles.gridRow}
          showsVerticalScrollIndicator={false}
          keyboardShouldPersistTaps="handled"
          ListEmptyComponent={
            <ListCardText style={styles.emptyText}>
              {termoBusca.trim()
                ? 'Nenhum produto encontrado para essa busca.'
                : 'Nenhum produto cadastrado ainda.'}
            </ListCardText>
          }
        />
      )}
    </FormScreen>
  );
}

const styles = StyleSheet.create({
  searchRow: {
    flexDirection: 'row',
    alignItems: 'flex-end',
    gap: 8,
    marginBottom: 12,
  },
  searchInputWrap: {
    flex: 1,
  },
  searchButton: {
    marginBottom: 10,
    paddingHorizontal: 16,
    paddingVertical: 12,
  },
  gridList: {
    flex: 1,
    backgroundColor: '#EBEBEB',
    marginHorizontal: -16,
  },
  gridContent: {
    paddingHorizontal: 8,
    paddingTop: 8,
    paddingBottom: 16,
  },
  gridRow: {
    gap: 8,
  },
  emptyText: {
    textAlign: 'center',
    marginTop: 24,
    color: '#64748B',
  },
});
