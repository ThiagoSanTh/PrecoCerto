import { FlatList, ActivityIndicator, StyleSheet, Alert } from 'react-native';
import { useCallback, useState } from 'react';
import { useFocusEffect } from '@react-navigation/native';
import { listarFeed } from '../../services/feedService';
import { obterLoja } from '../../services/lojaService';
import {
  adicionarFavorito,
  listarFavoritosCliente,
  removerFavoritoProduto,
} from '../../services/favoritoService';
import { useAuth } from '../../context/AuthContext';
import { useTheme } from '../../context/ThemeContext';
import { useLayoutProfile } from '../../hooks/useLayoutProfile';
import { mapApiError } from '../../utils/apiErrorUtils';
import ProductGridCard from '../../components/feed/ProductGridCard';
import { FormScreen, ListCardText } from '../../components/form';

const PAGE_SIZE = 20;
const GRID_PADDING_H = 8;
const GRID_GAP = 8;

export default function StoreCatalogScreen({ route, navigation }) {
  const lojaId = route.params?.lojaId;
  const { session, isCliente } = useAuth();
  const { colors } = useTheme();
  const { gridColumns } = useLayoutProfile();
  const clienteId = isCliente ? session?.perfil?.id : null;

  const [lojaNome, setLojaNome] = useState(route.params?.lojaNome || 'Loja');
  const [produtos, setProdutos] = useState([]);
  const [favoritosIds, setFavoritosIds] = useState(() => new Set());
  const [loading, setLoading] = useState(true);
  const [carregandoMais, setCarregandoMais] = useState(false);
  const [page, setPage] = useState(1);
  const [hasNext, setHasNext] = useState(false);
  const [error, setError] = useState(null);

  const carregarFavoritos = useCallback(async () => {
    if (!clienteId) {
      setFavoritosIds(new Set());
      return;
    }
    try {
      const res = await listarFavoritosCliente(clienteId, 1, 200);
      const ids = new Set(
        (res.items || []).map((f) => String(f.produtoId)).filter((id) => id && id !== 'undefined')
      );
      setFavoritosIds(ids);
    } catch {
      /* catálogo continua sem estado de favorito */
    }
  }, [clienteId]);

  const carregar = useCallback(
    async (pagina = 1, append = false) => {
      if (!lojaId) {
        setError({ title: 'Loja', message: 'Loja não identificada.' });
        setLoading(false);
        return;
      }

      const loader = pagina === 1 && !append ? setLoading : setCarregandoMais;
      loader(true);
      try {
        const resultado = await listarFeed({
          page: pagina,
          pageSize: PAGE_SIZE,
          lojaId,
          force: true,
        });
        setPage(resultado.page);
        setHasNext(resultado.hasNext);
        setProdutos((prev) => (append ? [...prev, ...resultado.items] : resultado.items));
        setError(null);
      } catch (err) {
        if (pagina === 1 && !append) setError(mapApiError(err));
      } finally {
        loader(false);
      }
    },
    [lojaId]
  );

  useFocusEffect(
    useCallback(() => {
      carregar(1, false);
      carregarFavoritos();
      if (lojaId && !route.params?.lojaNome) {
        obterLoja(lojaId)
          .then((loja) => {
            if (loja?.nomeFantasia) setLojaNome(loja.nomeFantasia);
          })
          .catch(() => {});
      }
    }, [carregar, carregarFavoritos, lojaId, route.params?.lojaNome])
  );

  async function handleFavorito(item) {
    if (!clienteId || !item?.id) return;
    const id = String(item.id);
    const jaFavorito = favoritosIds.has(id);
    setFavoritosIds((prev) => {
      const next = new Set(prev);
      if (jaFavorito) next.delete(id);
      else next.add(id);
      return next;
    });
    try {
      if (jaFavorito) {
        await removerFavoritoProduto(clienteId, id);
      } else {
        await adicionarFavorito({
          clienteId,
          produtoId: id,
          lojaId: item.lojaId ?? lojaId ?? null,
        });
      }
    } catch (error) {
      setFavoritosIds((prev) => {
        const next = new Set(prev);
        if (jaFavorito) next.add(id);
        else next.delete(id);
        return next;
      });
      const status = error?.response?.status;
      Alert.alert(
        'Favoritos',
        status === 403
          ? 'Não foi possível favoritar neste modo. Entre de novo e tente outra vez.'
          : 'Não foi possível atualizar os favoritos.'
      );
    }
  }

  function carregarMais() {
    if (!hasNext || carregandoMais || loading) return;
    carregar(page + 1, true);
  }

  return (
    <FormScreen
      title={lojaNome}
      subtitle="Produtos desta loja"
      onBack={() => navigation.goBack()}
      scrollable={false}
      error={error}
    >
      {loading ? (
        <ActivityIndicator size="large" color={colors.primary} style={{ marginTop: 24 }} />
      ) : (
        <FlatList
          data={produtos}
          keyExtractor={(item) => String(item.id)}
          numColumns={gridColumns}
          columnWrapperStyle={gridColumns > 1 ? styles.gridRow : undefined}
          contentContainerStyle={styles.gridContent}
          onEndReached={carregarMais}
          onEndReachedThreshold={0.4}
          renderItem={({ item }) => (
            <ProductGridCard
              produto={{
                id: item.id,
                nome: item.nome,
                imagemUrl: item.imagemUrl,
                lojaId: item.lojaId,
                preco: item.preco,
                lojaNomeFantasia: item.lojaNome,
              }}
              oferta={{
                preco: item.preco,
                precoAnterior: item.precoAnterior,
                emPromocao: item.emPromocao,
              }}
              ehFavorito={favoritosIds.has(String(item.id))}
              mostrarFavorito={Boolean(clienteId)}
              onFavorito={() => handleFavorito(item)}
              onPress={() =>
                navigation.navigate({
                  name: 'ProductDetail',
                  params: { productId: String(item.id) },
                  merge: true,
                })
              }
            />
          )}
          ListFooterComponent={
            carregandoMais ? (
              <ActivityIndicator size="small" color={colors.primary} style={{ marginVertical: 12 }} />
            ) : null
          }
          ListEmptyComponent={
            <ListCardText style={[styles.empty, { color: colors.textMuted }]}>
              Esta loja ainda não possui produtos no catálogo.
            </ListCardText>
          }
        />
      )}
    </FormScreen>
  );
}

const styles = StyleSheet.create({
  gridContent: { paddingHorizontal: GRID_PADDING_H, paddingTop: 8, paddingBottom: 16 },
  gridRow: { gap: GRID_GAP },
  empty: { textAlign: 'center', marginTop: 24 },
});
