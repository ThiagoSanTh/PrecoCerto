import { View, Text, Pressable, Alert, Image, StyleSheet } from 'react-native';
import { useState, useEffect, useMemo } from 'react';
import { listarProdutosParaFeed } from '../../services/productService';
import { nomeProduto, filtrarProdutosPorTermo } from '../../utils/produtoUtils';
import { criarOferta } from '../../services/ofertaService';
import { invalidarFeedCache } from '../../services/feedService';
import { useAuth } from '../../context/AuthContext';
import { useTheme } from '../../context/ThemeContext';
import {
  FormScreen,
  FormField,
  PrimaryButton,
  formStyles,
} from '../../components/form';

const MAX_LISTA = 20;

function ProdutoThumbnail({ produto, size, style }) {
  const { colors } = useTheme();
  const titulo = nomeProduto(produto);

  if (produto?.imagemUrl) {
    return (
      <Image
        source={{ uri: produto.imagemUrl }}
        style={[styles.thumbnail, { width: size, height: size }, style]}
        resizeMode="cover"
      />
    );
  }

  return (
    <View
      style={[
        styles.thumbnail,
        styles.thumbnailPlaceholder,
        { width: size, height: size, backgroundColor: colors.card },
        style,
      ]}
    >
      <Text style={[styles.thumbnailLetter, { color: colors.textMuted }]}>
        {titulo.charAt(0).toUpperCase()}
      </Text>
    </View>
  );
}

export default function CreateOfertaScreen({ navigation }) {
  const { session } = useAuth();
  const { colors } = useTheme();
  const lojaId = session?.perfil?.lojaId;

  const [produtos, setProdutos] = useState([]);
  const [termoBusca, setTermoBusca] = useState('');
  const [produtoId, setProdutoId] = useState('');
  const [preco, setPreco] = useState('');
  const [quantidadeEstoque, setQuantidadeEstoque] = useState('');
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!lojaId) return;
    listarProdutosParaFeed(lojaId).then(setProdutos).catch(() => {});
  }, [lojaId]);

  const produtosFiltrados = useMemo(
    () => filtrarProdutosPorTermo(produtos, termoBusca).slice(0, MAX_LISTA),
    [produtos, termoBusca]
  );

  const produtoSelecionado = useMemo(
    () => produtos.find((p) => String(p.id) === String(produtoId)) ?? null,
    [produtos, produtoId]
  );

  async function handleSalvar() {
    if (!lojaId || !preco) {
      Alert.alert('Erro', 'Informe o preço da oferta');
      return;
    }

    if (!produtoSelecionado) {
      Alert.alert('Erro', 'Selecione um produto na lista');
      return;
    }

    const precoNum = Number(preco.replace(',', '.'));
    if (Number.isNaN(precoNum) || precoNum <= 0) {
      Alert.alert('Erro', 'Preço inválido');
      return;
    }

    setLoading(true);
    try {
      await criarOferta({
        produtoId: produtoSelecionado.id,
        lojaId,
        preco: precoNum,
        disponivel: true,
        emPromocao: false,
        quantidadeEstoque: quantidadeEstoque ? parseInt(quantidadeEstoque, 10) : null,
      });
      invalidarFeedCache('feed');
      Alert.alert('Sucesso', 'Oferta criada');
      navigation.goBack();
    } catch (error) {
      Alert.alert('Erro', String(error.response?.data || error.message));
    } finally {
      setLoading(false);
    }
  }

  function selecionarProduto(id) {
    setProdutoId(String(id));
  }

  return (
    <FormScreen
      title="Nova oferta"
      subtitle="Vincule um produto à sua loja"
      onBack={() => navigation.goBack()}
      narrowContent
      footer={<PrimaryButton label="Salvar oferta" onPress={handleSalvar} loading={loading} />}
    >
      <FormField
        label="Buscar produto"
        value={termoBusca}
        onChangeText={setTermoBusca}
        placeholder="Digite o nome do produto..."
        autoCapitalize="none"
      />

      <Text style={formStyles.sectionHint}>Toque em um produto para selecionar:</Text>

      {produtosFiltrados.length > 0 ? (
        <View style={styles.lista}>
          {produtosFiltrados.map((p) => {
            const selected = String(produtoId) === String(p.id);
            return (
              <Pressable
                key={p.id}
                onPress={() => selecionarProduto(p.id)}
                style={[
                  formStyles.listCard,
                  styles.cardRow,
                  selected && styles.cardSelected,
                ]}
              >
                <ProdutoThumbnail produto={p} size={56} />
                <View style={styles.cardInfo}>
                  <Text style={formStyles.listCardTitle}>{nomeProduto(p)}</Text>
                  {p.marca ? (
                    <Text style={formStyles.listCardText}>{p.marca}</Text>
                  ) : null}
                </View>
              </Pressable>
            );
          })}
        </View>
      ) : termoBusca.trim() ? (
        <Text style={[formStyles.emptyText, styles.emptyBusca]}>
          Nenhum produto encontrado para essa busca.
        </Text>
      ) : produtos.length === 0 ? (
        <Text style={[formStyles.emptyText, styles.emptyBusca]}>
          Nenhum produto cadastrado na loja.
        </Text>
      ) : null}

      {produtoSelecionado ? (
        <View style={[styles.preview, { borderColor: colors.border, backgroundColor: colors.surface }]}>
          <Text style={[styles.previewHint, { color: colors.textMuted }]}>
            Produto selecionado para a oferta
          </Text>
          <ProdutoThumbnail produto={produtoSelecionado} size={120} style={styles.previewImage} />
          <Text style={[styles.previewNome, { color: colors.text }]}>
            {nomeProduto(produtoSelecionado)}
          </Text>
          {produtoSelecionado.marca ? (
            <Text style={[styles.previewMarca, { color: colors.textMuted }]}>
              {produtoSelecionado.marca}
            </Text>
          ) : null}
        </View>
      ) : null}

      <FormField
        label="Preço na loja *"
        value={preco}
        onChangeText={setPreco}
        keyboardType="decimal-pad"
      />
      <FormField
        label="Quantidade em estoque"
        value={quantidadeEstoque}
        onChangeText={setQuantidadeEstoque}
        keyboardType="number-pad"
        placeholder="opcional"
      />
    </FormScreen>
  );
}

const styles = StyleSheet.create({
  lista: {
    marginBottom: 12,
  },
  cardRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
  },
  cardSelected: {
    borderColor: '#2DD4BF',
    borderWidth: 2,
  },
  cardInfo: {
    flex: 1,
  },
  thumbnail: {
    borderRadius: 8,
    overflow: 'hidden',
  },
  thumbnailPlaceholder: {
    alignItems: 'center',
    justifyContent: 'center',
  },
  thumbnailLetter: {
    fontSize: 22,
    fontWeight: '700',
  },
  emptyBusca: {
    marginBottom: 12,
  },
  preview: {
    alignItems: 'center',
    borderWidth: 1,
    borderRadius: 12,
    padding: 16,
    marginBottom: 16,
  },
  previewHint: {
    fontSize: 13,
    marginBottom: 12,
  },
  previewImage: {
    borderRadius: 10,
    marginBottom: 10,
  },
  previewNome: {
    fontSize: 18,
    fontWeight: '700',
    textAlign: 'center',
  },
  previewMarca: {
    fontSize: 14,
    marginTop: 4,
    textAlign: 'center',
  },
});
