import { FlatList, Alert, ActivityIndicator, StyleSheet } from 'react-native';
import { useCallback, useState } from 'react';
import { useFocusEffect, useNavigation } from '@react-navigation/native';
import { listarFavoritosCliente } from '../../services/favoritoService';
import { listarProdutos } from '../../services/productService';
import { listarOfertas } from '../../services/ofertaService';
import { useAuth } from '../../context/AuthContext';
import FavoritoListCard from '../../components/feed/FavoritoListCard';
import { FormScreen, ListCardText } from '../../components/form';
import { colors } from '../../theme';
import { mapaOfertasPorProduto } from '../../utils/precoUtils';

export default function FavoritosScreen() {
  const navigation = useNavigation();
  const [itens, setItens] = useState([]);
  const [loading, setLoading] = useState(true);
  const { session } = useAuth();
  const clienteId = session?.perfil?.id;

  useFocusEffect(
    useCallback(() => {
      carregar();
    }, [clienteId])
  );

  async function carregar() {
    if (!clienteId) return;
    setLoading(true);
    try {
      const [favoritos, produtos, ofertas] = await Promise.all([
        listarFavoritosCliente(clienteId),
        listarProdutos(),
        listarOfertas().catch(() => []),
      ]);

      const prodMap = new Map((produtos || []).map((p) => [p.id, p]));
      const ofertasMap = mapaOfertasPorProduto(ofertas);

      const enriched = (Array.isArray(favoritos) ? favoritos : [])
        .filter((f) => f.produtoId)
        .map((fav) => ({
          ...fav,
          produto: prodMap.get(fav.produtoId) ?? null,
          oferta: ofertasMap.get(fav.produtoId) ?? null,
        }))
        .filter((f) => f.produto);

      setItens(enriched);
    } catch {
      Alert.alert('Erro', 'Não foi possível carregar favoritos');
    } finally {
      setLoading(false);
    }
  }

  function abrirProduto(produtoId) {
    navigation.navigate('ProductDetail', { productId: produtoId });
  }

  if (!clienteId) {
    return (
      <FormScreen title="Favoritos" subtitle="Faça login como cliente">
        <ListCardText>Nenhuma sessão de cliente ativa.</ListCardText>
      </FormScreen>
    );
  }

  return (
    <FormScreen title="Favoritos" subtitle="Produtos salvos" scrollable={false}>
      {loading ? (
        <ActivityIndicator color={colors.primary} style={{ marginTop: 24 }} />
      ) : (
        <FlatList
          style={styles.list}
          contentContainerStyle={styles.listContent}
          data={itens}
          keyExtractor={(item) => item.id}
          showsVerticalScrollIndicator={false}
          renderItem={({ item }) => (
            <FavoritoListCard
              produto={item.produto}
              oferta={item.oferta}
              onPress={() => abrirProduto(item.produtoId)}
            />
          )}
          ListEmptyComponent={
            <ListCardText style={styles.empty}>Nenhum favorito ainda.</ListCardText>
          }
        />
      )}
    </FormScreen>
  );
}

const styles = StyleSheet.create({
  list: {
    flex: 1,
    backgroundColor: '#EBEBEB',
    marginHorizontal: -16,
  },
  listContent: {
    paddingHorizontal: 8,
    paddingTop: 8,
    paddingBottom: 16,
  },
  empty: {
    textAlign: 'center',
    marginTop: 24,
    color: '#64748B',
  },
});
