import { FlatList, Alert, ActivityIndicator, Pressable } from 'react-native';
import { useCallback, useState, useMemo } from 'react';
import { useFocusEffect } from '@react-navigation/native';
import { listarProdutosParaFeed, buscarProdutosPorNome } from '../services/productService';
import { useAuth } from '../context/AuthContext';
import {
  FormScreen,
  FormField,
  PrimaryButton,
  ListCard,
  ListCardText,
  formStyles,
} from '../components/form';
import { colors } from '../style';
import { filtrarProdutosPorTermo, nomeProduto, produtoPertenceALoja } from '../utils/produtoUtils';

export default function ProductsScreen({ navigation }) {
  const { session } = useAuth();
  const lojaId = session?.perfil?.lojaId;

  const [termoBusca, setTermoBusca] = useState('');
  const [produtosBase, setProdutosBase] = useState([]);
  const [resultadosBusca, setResultadosBusca] = useState(null);
  const [loading, setLoading] = useState(true);

  useFocusEffect(
    useCallback(() => {
      carregarProdutos();
    }, [lojaId])
  );

  async function carregarProdutos() {
    if (!lojaId) {
      setProdutosBase([]);
      setLoading(false);
      return;
    }

    setLoading(true);
    setResultadosBusca(null);
    try {
      const dados = await listarProdutosParaFeed(lojaId);
      setProdutosBase(dados);
    } catch (error) {
      console.error(error?.response?.data || error.message);
      Alert.alert('Erro', 'Não foi possível carregar os produtos');
    } finally {
      setLoading(false);
    }
  }

  async function handleSearch() {
    const termo = termoBusca.trim();
    if (!termo) {
      setResultadosBusca(null);
      return;
    }

    if (!lojaId) return;

    setLoading(true);
    try {
      let filtrados = filtrarProdutosPorTermo(produtosBase, termo);
      if (filtrados.length === 0) {
        filtrados = await buscarProdutosPorNome(termo, lojaId);
      }
      setResultadosBusca(filtrados);
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

  function formatarPreco(valor) {
    return Number(valor).toLocaleString('pt-BR', {
      style: 'currency',
      currency: 'BRL',
    });
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
    <FormScreen title="Meus produtos" subtitle="Produtos da sua loja" scrollable={false}>
      <PrimaryButton
        label="+ Novo produto"
        onPress={() => navigation.navigate('CreateProduct')}
        style={{ marginBottom: 12 }}
      />

      <FormField
        label="Buscar"
        value={termoBusca}
        onChangeText={(texto) => {
          setTermoBusca(texto);
          if (!texto.trim()) setResultadosBusca(null);
        }}
        placeholder="Nome, marca..."
        onSubmitEditing={handleSearch}
        returnKeyType="search"
      />
      <PrimaryButton label="Buscar" onPress={handleSearch} style={{ marginBottom: 12 }} />

      {loading ? (
        <ActivityIndicator color={colors.primary} style={{ marginTop: 16 }} />
      ) : (
        <FlatList
          style={formStyles.listFlex}
          data={produtosExibidos}
          keyExtractor={(item) => item.id}
          showsVerticalScrollIndicator={false}
          keyboardShouldPersistTaps="handled"
          renderItem={({ item }) => {
            const editavel = produtoPertenceALoja(item, lojaId);

            return (
              <Pressable
                disabled={!editavel}
                onPress={() =>
                  navigation.navigate('EditProduct', { productId: item.id })
                }
              >
                <ListCard title={nomeProduto(item)}>
                  <ListCardText>{item.marca}</ListCardText>
                  <ListCardText>{item.descricao}</ListCardText>
                  <ListCardText>{formatarPreco(item.preco)}</ListCardText>
                  {editavel ? (
                    <ListCardText style={{ marginTop: 4, color: colors.primary }}>
                      Toque para editar
                    </ListCardText>
                  ) : null}
                </ListCard>
              </Pressable>
            );
          }}
          ListEmptyComponent={
            <ListCardText>
              {termoBusca.trim()
                ? 'Nenhum produto encontrado.'
                : 'Nenhum produto cadastrado para esta loja.'}
            </ListCardText>
          }
        />
      )}
    </FormScreen>
  );
}
