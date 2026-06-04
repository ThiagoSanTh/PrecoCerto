import { FlatList, Alert, ActivityIndicator, View, Image, StyleSheet } from 'react-native';
import { useCallback, useState, useMemo } from 'react';
import { useFocusEffect } from '@react-navigation/native';
import { listarProdutosParaFeed, buscarProdutosPorNome } from '../services/productService';
import { registrarPesquisa } from '../services/historicoService';
import { useAuth } from '../context/AuthContext';
import SearchMapView from '../components/SearchMapView';
import {
  FormScreen,
  FormField,
  PrimaryButton,
  ListCard,
  ListCardText,
  FormTabs,
  formStyles,
} from '../components/form';
import { colors } from '../style';
import {
  filtrarProdutosPorTermo,
  nomeProduto,
  normalizarListaProdutos,
} from '../utils/produtoUtils';
import { formatarPrecoBrl } from '../utils/mapaUtils';

const MODO_LISTA = 'lista';
const MODO_MAPA = 'mapa';

export default function SearchScreen() {
  const [termoBusca, setTermoBusca] = useState('');
  const [produtosBase, setProdutosBase] = useState([]);
  const [resultadosBusca, setResultadosBusca] = useState(null);
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
      const lista = await listarProdutosParaFeed(null);
      setProdutosBase(Array.isArray(lista) ? lista : []);
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

  function renderItem({ item }) {
    return (
      <ListCard title={nomeProduto(item)}>
        <View style={styles.listRow}>
          {item.imagemUrl ? (
            <Image
              source={{ uri: item.imagemUrl }}
              style={styles.thumb}
              resizeMode="cover"
            />
          ) : null}
          <View style={styles.listBody}>
            <ListCardText>{item.marca}</ListCardText>
            {item.descricao ? <ListCardText>{item.descricao}</ListCardText> : null}
            <ListCardText>{formatarPrecoBrl(item.preco)}</ListCardText>
            {item.lojaNomeFantasia ? (
              <ListCardText>{item.lojaNomeFantasia}</ListCardText>
            ) : null}
          </View>
        </View>
      </ListCard>
    );
  }

  const mostrarToggle = resultadosBusca !== null && resultadosBusca.length > 0;

  return (
    <FormScreen
      title="Buscar produtos"
      subtitle="Todos os produtos do catálogo"
      scrollable={false}
    >
      <FormField
        label="Buscar"
        value={termoBusca}
        onChangeText={(texto) => {
          setTermoBusca(texto);
          if (!texto.trim()) {
            setResultadosBusca(null);
            setModoVisualizacao(MODO_LISTA);
          }
        }}
        placeholder="Nome, marca ou descrição..."
        onSubmitEditing={handleSearch}
        returnKeyType="search"
      />
      <PrimaryButton label="Buscar" onPress={handleSearch} style={{ marginBottom: 12 }} />

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
        <SearchMapView produtos={resultadosBusca} />
      ) : (
        <FlatList
          style={formStyles.listFlex}
          data={produtosExibidos}
          keyExtractor={(item) => item.id}
          renderItem={renderItem}
          showsVerticalScrollIndicator={false}
          keyboardShouldPersistTaps="handled"
          ListEmptyComponent={
            <ListCardText>
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
  listRow: {
    flexDirection: 'row',
    marginTop: 8,
    gap: 10,
  },
  thumb: {
    width: 56,
    height: 56,
    borderRadius: 8,
    backgroundColor: '#e2e8f0',
  },
  listBody: {
    flex: 1,
  },
});
