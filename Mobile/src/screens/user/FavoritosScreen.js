import { FlatList, Alert, ActivityIndicator, StyleSheet } from 'react-native';
import { useCallback, useState } from 'react';
import { useFocusEffect, useNavigation } from '@react-navigation/native';
import { listarFavoritosCliente } from '../../services/favoritoService';
import { useAuth } from '../../context/AuthContext';
import { useTheme } from '../../context/ThemeContext';
import FavoritoListCard from '../../components/feed/FavoritoListCard';
import { FormScreen, ListCardText } from '../../components/form';

const PAGE_SIZE = 20;

export default function FavoritosScreen() {
  const navigation = useNavigation();
  const [itens, setItens] = useState([]);
  const [loading, setLoading] = useState(true);
  const [carregandoMais, setCarregandoMais] = useState(false);
  const [page, setPage] = useState(1);
  const [hasNext, setHasNext] = useState(false);
  const { session } = useAuth();
  const { colors } = useTheme();
  const clienteId = session?.perfil?.id;

  const carregar = useCallback(
    async (pagina = 1, append = false) => {
      if (!clienteId) return;
      if (pagina === 1 && !append) setLoading(true);
      else setCarregandoMais(true);

      try {
        const res = await listarFavoritosCliente(clienteId, pagina, PAGE_SIZE);
        setPage(res.page);
        setHasNext(res.hasNext);
        setItens((prev) => (append ? [...prev, ...res.items] : res.items));
      } catch {
        Alert.alert('Erro', 'Não foi possível carregar favoritos.');
      } finally {
        setLoading(false);
        setCarregandoMais(false);
      }
    },
    [clienteId]
  );

  useFocusEffect(
    useCallback(() => {
      carregar(1, false);
    }, [carregar])
  );

  function carregarMais() {
    if (!hasNext || carregandoMais || loading) return;
    carregar(page + 1, true);
  }

  function abrirProdutoFavorito(item) {
    const id = item?.produto?.id ?? item?.produtoId ?? item?.ProdutoId;
    if (!id) return;
    navigation.navigate({
      name: 'ProductDetail',
      params: { productId: String(id) },
      merge: true,
    });
  }

  return (
    <FormScreen title="Favoritos" subtitle="Produtos que você salvou">
      {loading ? (
        <ActivityIndicator size="large" color={colors.primary} style={{ marginTop: 24 }} />
      ) : (
        <FlatList
          data={itens}
          keyExtractor={(item) => String(item.id)}
          renderItem={({ item }) => (
            <FavoritoListCard
              produto={item.produto}
              oferta={item.oferta}
              onPress={() => abrirProdutoFavorito(item)}
            />
          )}
          contentContainerStyle={styles.list}
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
            <ListCardText style={[styles.empty, { color: colors.textMuted }]}>
              Nenhum favorito ainda.
            </ListCardText>
          }
        />
      )}
    </FormScreen>
  );
}

const styles = StyleSheet.create({
  list: { paddingBottom: 16 },
  empty: { textAlign: 'center', marginTop: 24 },
});
