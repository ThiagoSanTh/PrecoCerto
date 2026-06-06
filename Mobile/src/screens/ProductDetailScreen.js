import { useCallback, useState } from 'react';
import {
  View,
  Text,
  Alert,
  Share,
  ActivityIndicator,
  StyleSheet,
} from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import * as ImagePicker from 'expo-image-picker';
import {
  buscarProdutoPorId,
  atualizarProduto,
  removerProduto,
} from '../services/productService';
import {
  listarOfertasPorProduto,
  criarOferta,
  atualizarOferta,
} from '../services/ofertaService';
import {
  listarAvaliacoesLoja,
  obterMediaAvaliacoesLoja,
} from '../services/avaliacaoService';
import { uploadImagemProduto } from '../services/storageService';
import {
  adicionarFavorito,
  removerFavoritoProduto,
  verificarFavorito,
} from '../services/favoritoService';
import { useAuth } from '../context/AuthContext';
import { nomeProduto, produtoPertenceALoja } from '../utils/produtoUtils';
import { formatarPrecoBrl } from '../utils/mapaUtils';
import { formatarDataBr, parseDataBr } from '../utils/dataUtils';
import {
  montarHistoricoPrecos,
  selecionarOfertaPrincipal,
} from '../utils/precoUtils';
import ProductImageGallery from '../components/product/ProductImageGallery';
import PriceHistoryBlock from '../components/product/PriceHistoryBlock';
import ReviewList from '../components/product/ReviewList';
import PromocaoSection from '../components/product/PromocaoSection';
import ProductActionBar from '../components/product/ProductActionBar';
import {
  FormScreen,
  FormField,
  PrimaryButton,
  SecondaryButton,
} from '../components/form';
import { colors } from '../style';

function preencherFormularioProduto(prod, ofertaLoja) {
  return {
    nome: prod.nomeProduto || prod.nome || '',
    descricao: prod.descricao || '',
    marca: prod.marca || '',
    codigoBarras: prod.codigoBarras || '',
    preco: String(prod.preco ?? '').replace('.', ','),
    imagemUrlAtual: prod.imagemUrl || null,
    emPromocao: Boolean(ofertaLoja?.emPromocao),
    precoPromocional: ofertaLoja?.emPromocao
      ? String(ofertaLoja.preco ?? '').replace('.', ',')
      : '',
    precoAnterior: ofertaLoja?.precoAnterior
      ? String(ofertaLoja.precoAnterior).replace('.', ',')
      : String(prod.preco ?? '').replace('.', ','),
    dataInicio: formatarDataBr(ofertaLoja?.dataInicioPromocao),
    dataFim: formatarDataBr(ofertaLoja?.dataFimPromocao),
    quantidadeEstoque:
      ofertaLoja?.quantidadeEstoque != null
        ? String(ofertaLoja.quantidadeEstoque)
        : '',
  };
}

export default function ProductDetailScreen({ route, navigation }) {
  const { productId } = route.params;
  const { session, isCliente } = useAuth();
  const lojaId = session?.perfil?.lojaId;
  const clienteId = isCliente ? session?.perfil?.id : null;

  const [produto, setProduto] = useState(null);
  const [oferta, setOferta] = useState(null);
  const [ofertaLoja, setOfertaLoja] = useState(null);
  const [isDono, setIsDono] = useState(false);
  const [avaliacoes, setAvaliacoes] = useState([]);
  const [media, setMedia] = useState(null);
  const [loading, setLoading] = useState(true);
  const [salvando, setSalvando] = useState(false);
  const [ehFavorito, setEhFavorito] = useState(false);
  const [favoritoLoading, setFavoritoLoading] = useState(false);

  const [nome, setNome] = useState('');
  const [descricao, setDescricao] = useState('');
  const [marca, setMarca] = useState('');
  const [codigoBarras, setCodigoBarras] = useState('');
  const [preco, setPreco] = useState('');
  const [imagemUrlAtual, setImagemUrlAtual] = useState(null);
  const [imagemUri, setImagemUri] = useState(null);
  const [imagemMime, setImagemMime] = useState('image/jpeg');

  const [emPromocao, setEmPromocao] = useState(false);
  const [precoPromocional, setPrecoPromocional] = useState('');
  const [precoAnterior, setPrecoAnterior] = useState('');
  const [dataInicio, setDataInicio] = useState('');
  const [dataFim, setDataFim] = useState('');
  const [quantidadeEstoque, setQuantidadeEstoque] = useState('');

  const imagemExibida = imagemUri || imagemUrlAtual || produto?.imagemUrl;

  useFocusEffect(
    useCallback(() => {
      carregar();
    }, [productId, lojaId, clienteId])
  );

  function aplicarFormulario(prod, ofertaDaLoja) {
    const form = preencherFormularioProduto(prod, ofertaDaLoja);
    setNome(form.nome);
    setDescricao(form.descricao);
    setMarca(form.marca);
    setCodigoBarras(form.codigoBarras);
    setPreco(form.preco);
    setImagemUrlAtual(form.imagemUrlAtual);
    setImagemUri(null);
    setEmPromocao(form.emPromocao);
    setPrecoPromocional(form.precoPromocional);
    setPrecoAnterior(form.precoAnterior);
    setDataInicio(form.dataInicio);
    setDataFim(form.dataFim);
    setQuantidadeEstoque(form.quantidadeEstoque);
  }

  async function carregar() {
    setLoading(true);
    try {
      const prod = await buscarProdutoPorId(productId);
      const ofertas = await listarOfertasPorProduto(productId);
      const dono = lojaId && produtoPertenceALoja(prod, lojaId);
      const ofertaDaLoja =
        lojaId && dono
          ? ofertas.find((o) => String(o.lojaId) === String(lojaId)) ?? null
          : null;
      const ofertaPrincipal = selecionarOfertaPrincipal(
        ofertas,
        dono ? lojaId : prod.lojaId
      );

      const lojaIdAvaliacao = prod.lojaId ?? ofertaPrincipal?.lojaId;
      let avs = [];
      let med = null;

      if (lojaIdAvaliacao) {
        [avs, med] = await Promise.all([
          listarAvaliacoesLoja(lojaIdAvaliacao).catch(() => []),
          obterMediaAvaliacoesLoja(lojaIdAvaliacao).catch(() => null),
        ]);
      }

      setProduto(prod);
      setOferta(ofertaPrincipal);
      setOfertaLoja(ofertaDaLoja);
      setIsDono(Boolean(dono));
      setAvaliacoes(Array.isArray(avs) ? avs : []);
      setMedia(med);
      setImagemUrlAtual(prod.imagemUrl || null);
      setImagemUri(null);

      if (dono) {
        aplicarFormulario(prod, ofertaDaLoja);
      }

      if (!dono && clienteId) {
        const favorito = await verificarFavorito(clienteId, productId).catch(() => false);
        setEhFavorito(Boolean(favorito));
      } else {
        setEhFavorito(false);
      }
    } catch {
      Alert.alert('Erro', 'Não foi possível carregar o produto.');
      navigation.goBack();
    } finally {
      setLoading(false);
    }
  }

  function handleTogglePromocao(ativo) {
    setEmPromocao(ativo);
    if (ativo && !precoAnterior.trim()) {
      setPrecoAnterior(preco || String(produto?.preco ?? '').replace('.', ','));
    }
    if (ativo && !precoPromocional.trim() && preco) {
      setPrecoPromocional(preco);
    }
  }

  async function handleEscolherFoto() {
    const { status } = await ImagePicker.requestMediaLibraryPermissionsAsync();
    if (status !== 'granted') {
      Alert.alert('Permissão', 'Permita o acesso à galeria para alterar a foto do produto.');
      return;
    }

    const result = await ImagePicker.launchImageLibraryAsync({
      mediaTypes: ['images'],
      allowsEditing: true,
      aspect: [1, 1],
      quality: 0.8,
    });

    if (!result.canceled && result.assets?.[0]?.uri) {
      const asset = result.assets[0];
      setImagemUri(asset.uri);
      setImagemMime(asset.mimeType || 'image/jpeg');
    }
  }

  async function salvarOferta(precoCatalogoNum) {
    const precoOferta = emPromocao
      ? Number(precoPromocional.replace(',', '.'))
      : precoCatalogoNum;

    if (emPromocao) {
      if (Number.isNaN(precoOferta) || precoOferta <= 0) {
        throw new Error('Informe um preço promocional válido');
      }
    }

    const inicioIso = parseDataBr(dataInicio);
    const fimIso = parseDataBr(dataFim);

    if (dataInicio.trim() && !inicioIso) {
      throw new Error('Data de início inválida. Use DD/MM/AAAA');
    }
    if (dataFim.trim() && !fimIso) {
      throw new Error('Data de fim inválida. Use DD/MM/AAAA');
    }

    const payload = {
      produtoId: productId,
      lojaId,
      preco: precoOferta,
      precoAnterior: emPromocao
        ? Number(precoAnterior.replace(',', '.')) || precoCatalogoNum
        : null,
      emPromocao,
      dataInicioPromocao: emPromocao ? inicioIso : null,
      dataFimPromocao: emPromocao ? fimIso : null,
      disponivel: true,
      quantidadeEstoque: quantidadeEstoque.trim()
        ? parseInt(quantidadeEstoque, 10)
        : null,
    };

    if (ofertaLoja?.id) {
      await atualizarOferta(ofertaLoja.id, payload);
    } else {
      await criarOferta(payload);
    }
  }

  async function handleSalvar() {
    const precoConvertido = Number(preco.replace(',', '.'));

    if (!nome.trim() || !marca.trim() || !codigoBarras.trim()) {
      Alert.alert('Erro', 'Preencha os campos obrigatórios');
      return;
    }

    if (Number.isNaN(precoConvertido) || precoConvertido <= 0) {
      Alert.alert('Erro', 'Informe um preço válido');
      return;
    }

    if (!imagemExibida) {
      Alert.alert('Foto obrigatória', 'Adicione uma foto do produto para exibir no mapa.');
      return;
    }

    setSalvando(true);
    try {
      let imagemUrl = imagemUrlAtual;
      if (imagemUri) {
        imagemUrl = await uploadImagemProduto({
          uri: imagemUri,
          lojaId,
          mimeType: imagemMime,
        });
      }

      await atualizarProduto(productId, {
        nomeProduto: nome,
        descricao,
        marca,
        codigoBarras,
        preco: precoConvertido,
        lojaId,
        imagemUrl,
      });

      await salvarOferta(precoConvertido);

      Alert.alert('Sucesso', 'Produto e promoção atualizados');
      await carregar();
    } catch (error) {
      const status = error?.response?.status;
      const mensagem =
        status === 403
          ? 'Somente a loja que cadastrou este produto pode editá-lo.'
          : error.message || 'Não foi possível salvar as alterações';
      Alert.alert('Erro', mensagem);
    } finally {
      setSalvando(false);
    }
  }

  function confirmarExclusao() {
    Alert.alert(
      'Excluir produto',
      'Esta ação não pode ser desfeita. Deseja excluir este produto?',
      [
        { text: 'Cancelar', style: 'cancel' },
        { text: 'Excluir', style: 'destructive', onPress: handleExcluir },
      ]
    );
  }

  async function handleExcluir() {
    setSalvando(true);
    try {
      await removerProduto(productId, lojaId);
      Alert.alert('Sucesso', 'Produto excluído');
      navigation.goBack();
    } catch (error) {
      const status = error?.response?.status;
      const mensagem =
        status === 403
          ? 'Somente a loja que cadastrou este produto pode excluí-lo.'
          : error.message || 'Não foi possível excluir o produto';
      Alert.alert('Erro', mensagem);
    } finally {
      setSalvando(false);
    }
  }

  function handleCarrinho() {
    Alert.alert('Em breve', 'O carrinho de compras estará disponível em breve.');
  }

  async function handleToggleFavorito() {
    if (!clienteId) return;

    setFavoritoLoading(true);
    try {
      if (ehFavorito) {
        await removerFavoritoProduto(clienteId, productId);
        setEhFavorito(false);
      } else {
        await adicionarFavorito({
          clienteId,
          produtoId: productId,
          lojaId: produto?.lojaId ?? null,
        });
        setEhFavorito(true);
      }
    } catch {
      Alert.alert('Erro', 'Não foi possível atualizar os favoritos.');
    } finally {
      setFavoritoLoading(false);
    }
  }

  async function handleCompartilhar() {
    if (!produto) return;
    const historico = montarHistoricoPrecos(produto, oferta);
    const nomeExibicao = nomeProduto(produto);
    const loja = produto.lojaNomeFantasia || oferta?.nomeLoja || '';

    try {
      await Share.share({
        message: `${nomeExibicao}\n${formatarPrecoBrl(historico.precoAtual)}${loja ? `\n${loja}` : ''}`,
      });
    } catch {
      /* usuário cancelou */
    }
  }

  if (loading || !produto) {
    return (
      <FormScreen title="Produto" onBack={() => navigation.goBack()} scrollable={false}>
        <ActivityIndicator size="large" color={colors.primary} style={{ marginTop: 48 }} />
      </FormScreen>
    );
  }

  const produtoPreview = isDono
    ? { ...produto, preco: Number(preco.replace(',', '.')) || produto.preco }
    : produto;

  const ofertaPreview =
    isDono && emPromocao
      ? {
          preco: Number(precoPromocional.replace(',', '.')) || produto.preco,
          precoAnterior:
            Number(precoAnterior.replace(',', '.')) || Number(preco.replace(',', '.')),
          emPromocao: true,
        }
      : isDono
        ? { preco: Number(preco.replace(',', '.')) || produto.preco, emPromocao: false }
        : oferta;

  const historico = montarHistoricoPrecos(produtoPreview, ofertaPreview);
  const endereco = [produto.logradouro, produto.cidade].filter(Boolean).join(' — ');
  const lojaNome = produto.lojaNomeFantasia || oferta?.nomeLoja;

  const footer = isDono ? (
    <View style={{ gap: 8 }}>
      <PrimaryButton label="Salvar alterações" onPress={handleSalvar} loading={salvando} />
      <SecondaryButton
        label="Excluir produto"
        onPress={confirmarExclusao}
        disabled={salvando}
        style={styles.botaoExcluir}
      />
    </View>
  ) : undefined;

  const acoesCliente = !isDono ? (
    <ProductActionBar
      ehFavorito={ehFavorito}
      onFavorito={handleToggleFavorito}
      favoritoLoading={favoritoLoading}
      mostrarFavorito={Boolean(clienteId)}
      onCarrinho={handleCarrinho}
      onCompartilhar={handleCompartilhar}
    />
  ) : null;

  return (
    <FormScreen
      title={isDono ? 'Editar produto' : nomeProduto(produto)}
      subtitle={isDono ? 'Gerencie dados e promoção do produto' : undefined}
      onBack={() => navigation.goBack()}
      scrollable
      footer={footer}
    >
      <View style={{ marginHorizontal: -16, marginTop: -12 }}>
        <ProductImageGallery imagens={imagemExibida ? [imagemExibida] : []} />
      </View>

      {isDono ? (
        <View style={styles.fotoActions}>
          <SecondaryButton label="Alterar foto" onPress={handleEscolherFoto} />
        </View>
      ) : null}

      {isDono ? (
        <View style={{ marginTop: 8 }}>
          <FormField label="Nome *" value={nome} onChangeText={setNome} />
          <FormField label="Descrição" value={descricao} onChangeText={setDescricao} multiline />
          <FormField label="Marca *" value={marca} onChangeText={setMarca} />
          <FormField
            label="Código de barras *"
            value={codigoBarras}
            onChangeText={setCodigoBarras}
          />
          <FormField
            label="Preço de catálogo *"
            value={preco}
            onChangeText={setPreco}
            keyboardType="decimal-pad"
          />

          <PromocaoSection
            emPromocao={emPromocao}
            onTogglePromocao={handleTogglePromocao}
            precoPromocional={precoPromocional}
            onChangePrecoPromocional={setPrecoPromocional}
            precoAnterior={precoAnterior}
            onChangePrecoAnterior={setPrecoAnterior}
            dataInicio={dataInicio}
            onChangeDataInicio={setDataInicio}
            dataFim={dataFim}
            onChangeDataFim={setDataFim}
            quantidadeEstoque={quantidadeEstoque}
            onChangeQuantidadeEstoque={setQuantidadeEstoque}
          />
        </View>
      ) : (
        <View style={{ marginTop: 16 }}>
          {produto.marca ? (
            <Text style={{ fontSize: 13, color: '#64748B', marginBottom: 4 }}>
              {produto.marca}
            </Text>
          ) : null}

          <Text style={{ fontSize: 22, fontWeight: '600', color: '#0F172A', lineHeight: 28 }}>
            {nomeProduto(produto)}
          </Text>

          {produto.descricao ? (
            <Text style={{ fontSize: 15, color: '#475569', marginTop: 12, lineHeight: 22 }}>
              {produto.descricao}
            </Text>
          ) : null}
        </View>
      )}

      {lojaNome ? (
        <Text style={{ fontSize: 14, color: '#64748B', marginTop: 12 }}>
          Vendido por {lojaNome}
        </Text>
      ) : null}

      {endereco ? (
        <Text style={{ fontSize: 13, color: '#94A3B8', marginTop: 4 }}>
          {endereco}
        </Text>
      ) : null}

      <View style={{ marginTop: 20 }}>
        <PriceHistoryBlock
          precoAtual={historico.precoAtual}
          precosAntigos={historico.precosAntigos}
          emPromocao={historico.emPromocao}
          actions={acoesCliente}
        />
      </View>

      <ReviewList media={media} avaliacoes={avaliacoes} />
    </FormScreen>
  );
}

const styles = StyleSheet.create({
  fotoActions: {
    marginTop: 8,
    marginBottom: 4,
  },
  botaoExcluir: {
    borderColor: '#dc2626',
  },
});
