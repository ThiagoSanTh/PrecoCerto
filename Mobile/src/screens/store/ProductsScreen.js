import { FlatList, Alert, ActivityIndicator, StyleSheet } from 'react-native';
import { useCallback, useState, useMemo } from 'react';
import { useFocusEffect } from '@react-navigation/native';
import { listarProdutos } from '../../services/productService';
import { listarOfertas } from '../../services/ofertaService';
import { useAuth } from '../../context/AuthContext';
import ProductGridCard from '../../components/feed/ProductGridCard';
import {
  FormScreen,
  FormField,
  PrimaryButton,
  ListCardText,
} from '../../components/form';
import { colors } from '../../../theme';
import { filtrarProdutosPorTermo, produtoPertenceALoja } from '../../utils/produtoUtils';
import { mapaOfertasPorProduto } from '../../utils/precoUtils';

export default function ProductsScreen({ navigation }) {
  const { session } = useAuth();
  const lojaId = session?.perfil?.lojaId;

  const [termoBusca, setTermoBusca] = useState('');
  const [produtos, setProdutos] = useState([]);
  const [ofertasMap, setOfertasMap] = useState(new Map());
  const [loading, setLoading] = useState(true);

  useFocusEffect(
    useCallback(() => {
      carregarProdutos();
    }, [lojaId])
  );

  async function carregarProdutos() {
    if (!lojaId) {
      setProdutos([]);
      setLoading(false);
      return;
    }

    setLoading(true);
    try {
      const [dados, ofertas] = await Promise.all([
        listarProdutos(lojaId),
        listarOfertas().catch(() => []),
      ]);

      const meusProdutos = (Array.isArray(dados) ? dados : []).filter((p) =>
        produtoPertenceALoja(p, lojaId)
      );

      const ofertasDaLoja = (Array.isArray(ofertas) ? ofertas : []).filter(
        (o) => String(o.lojaId) === String(lojaId)
      );

      setProdutos(meusProdutos);
      setOfertasMap(mapaOfertasPorProduto(ofertasDaLoja));
    } catch (error) {
      console.error(error?.response?.data || error.message);
      Alert.alert('Erro', 'Não foi possível carregar os produtos');
    } finally {
      setLoading(false);
    }
  }

  const produtosExibidos = useMemo(
    () => filtrarProdutosPorTermo(produtos, termoBusca),
    [produtos, termoBusca]
  );

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

  if (!lojaId) {
    return (
      <FormScreen
        title="Meus produtos"
        subtitle="Vincule uma loja ao perfil"
        scrollable={false}
        footer={
          <PrimaryButton
            label="Criar loja"
            onPress={() => navigation.navigate('CreateStore')}
          />
        }
      >
        <ListCardText>Cadastre sua loja para gerenciar produtos.</ListCardText>
      </FormScreen>
    );
  }

  return (
    <FormScreen
      title="Meus produtos"
      subtitle="Produtos que você cadastrou"
      scrollable={false}
    >
      <PrimaryButton
        label="+ Novo produto"
        onPress={() => navigation.navigate('CreateProduct')}
        style={{ marginBottom: 12 }}
      />

      <FormField
        label=""
        value={termoBusca}
        onChangeText={setTermoBusca}
        placeholder="Buscar produtos..."
        returnKeyType="search"
        compact
      />

      {loading ? (
        <ActivityIndicator size="large" color={colors.primary} style={{ marginTop: 24 }} />
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
                ? 'Nenhum produto encontrado.'
                : 'Nenhum produto cadastrado ainda.'}
            </ListCardText>
          }
        />
      )}
    </FormScreen>
  );
}

const styles = StyleSheet.create({
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
