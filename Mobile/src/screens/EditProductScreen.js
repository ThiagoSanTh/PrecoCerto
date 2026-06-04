import { Alert, Image, Text, StyleSheet, View, ActivityIndicator } from 'react-native';
import { useCallback, useState } from 'react';
import { useFocusEffect } from '@react-navigation/native';
import * as ImagePicker from 'expo-image-picker';
import {
  buscarProdutoPorId,
  atualizarProduto,
  removerProduto,
} from '../services/productService';
import { uploadImagemProduto } from '../services/storageService';
import { useAuth } from '../context/AuthContext';
import { FormScreen, FormField, PrimaryButton, SecondaryButton } from '../components/form';
import { colors } from '../style';
import { produtoPertenceALoja } from '../utils/produtoUtils';

export default function EditProductScreen({ navigation, route }) {
  const productId = route.params?.productId;
  const { session } = useAuth();
  const lojaId = session?.perfil?.lojaId;

  const [nomeProduto, setNomeProduto] = useState('');
  const [descricao, setDescricao] = useState('');
  const [marca, setMarca] = useState('');
  const [codigoBarras, setCodigoBarras] = useState('');
  const [preco, setPreco] = useState('');
  const [imagemUrlAtual, setImagemUrlAtual] = useState(null);
  const [imagemUri, setImagemUri] = useState(null);
  const [imagemMime, setImagemMime] = useState('image/jpeg');
  const [carregandoProduto, setCarregandoProduto] = useState(true);
  const [loading, setLoading] = useState(false);

  const imagemExibida = imagemUri || imagemUrlAtual;

  useFocusEffect(
    useCallback(() => {
      carregarProduto();
    }, [productId, lojaId])
  );

  async function carregarProduto() {
    if (!productId || !lojaId) {
      setCarregandoProduto(false);
      return;
    }

    setCarregandoProduto(true);
    try {
      const produto = await buscarProdutoPorId(productId);

      if (!produtoPertenceALoja(produto, lojaId)) {
        Alert.alert(
          'Acesso negado',
          'Somente a loja que cadastrou este produto pode editá-lo.',
          [{ text: 'OK', onPress: () => navigation.goBack() }]
        );
        return;
      }

      setNomeProduto(produto.nomeProduto || produto.nome || '');
      setDescricao(produto.descricao || '');
      setMarca(produto.marca || '');
      setCodigoBarras(produto.codigoBarras || '');
      setPreco(String(produto.preco ?? '').replace('.', ','));
      setImagemUrlAtual(produto.imagemUrl || null);
      setImagemUri(null);
    } catch (error) {
      console.error(error?.response?.data || error.message);
      Alert.alert('Erro', 'Não foi possível carregar o produto.', [
        { text: 'OK', onPress: () => navigation.goBack() },
      ]);
    } finally {
      setCarregandoProduto(false);
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

  async function handleSalvar() {
    try {
      const precoConvertido = Number(preco.replace(',', '.'));

      if (!nomeProduto.trim() || !marca.trim() || !codigoBarras.trim()) {
        Alert.alert('Erro', 'Preencha os campos obrigatórios');
        return;
      }

      if (Number.isNaN(precoConvertido) || precoConvertido <= 0) {
        Alert.alert('Erro', 'Informe um preço válido');
        return;
      }

      if (!lojaId) {
        Alert.alert('Erro', 'Loja não identificada no perfil');
        return;
      }

      if (!imagemExibida) {
        Alert.alert('Foto obrigatória', 'Adicione uma foto do produto para exibir no mapa.');
        return;
      }

      setLoading(true);

      let imagemUrl = imagemUrlAtual;
      if (imagemUri) {
        imagemUrl = await uploadImagemProduto({
          uri: imagemUri,
          lojaId,
          mimeType: imagemMime,
        });
      }

      await atualizarProduto(productId, {
        nomeProduto,
        descricao,
        marca,
        codigoBarras,
        preco: precoConvertido,
        lojaId,
        imagemUrl,
      });

      Alert.alert('Sucesso', 'Produto atualizado com sucesso');
      navigation.goBack();
    } catch (error) {
      const status = error?.response?.status;
      const mensagem =
        status === 403
          ? 'Somente a loja que cadastrou este produto pode editá-lo.'
          : error.message || 'Não foi possível atualizar o produto';
      console.error(error?.response?.data || error.message);
      Alert.alert('Erro', mensagem);
    } finally {
      setLoading(false);
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
    if (!lojaId) {
      Alert.alert('Erro', 'Loja não identificada no perfil');
      return;
    }

    setLoading(true);
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
      console.error(error?.response?.data || error.message);
      Alert.alert('Erro', mensagem);
    } finally {
      setLoading(false);
    }
  }

  if (!lojaId) {
    return (
      <FormScreen
        title="Editar produto"
        subtitle="Vincule uma loja ao perfil"
        onBack={() => navigation.goBack()}
      >
        <Text style={styles.aviso}>Cadastre sua loja para gerenciar produtos.</Text>
      </FormScreen>
    );
  }

  if (carregandoProduto) {
    return (
      <FormScreen title="Editar produto" onBack={() => navigation.goBack()} scrollable={false}>
        <ActivityIndicator color={colors.primary} style={{ marginTop: 24 }} />
      </FormScreen>
    );
  }

  return (
    <FormScreen
      title="Editar produto"
      subtitle="Altere os dados do seu produto"
      onBack={() => navigation.goBack()}
      footer={
        <>
          <PrimaryButton label="Salvar alterações" onPress={handleSalvar} loading={loading} />
          <SecondaryButton
            label="Excluir produto"
            onPress={confirmarExclusao}
            disabled={loading}
            style={styles.botaoExcluir}
          />
        </>
      }
    >
      <View style={styles.fotoSection}>
        {imagemExibida ? (
          <Image source={{ uri: imagemExibida }} style={styles.preview} />
        ) : (
          <View style={styles.previewPlaceholder}>
            <Text style={styles.previewPlaceholderText}>Sem foto</Text>
          </View>
        )}
        <SecondaryButton label="Alterar foto" onPress={handleEscolherFoto} />
      </View>

      <FormField label="Nome *" value={nomeProduto} onChangeText={setNomeProduto} />
      <FormField label="Descrição" value={descricao} onChangeText={setDescricao} />
      <FormField label="Marca *" value={marca} onChangeText={setMarca} />
      <FormField label="Código de barras *" value={codigoBarras} onChangeText={setCodigoBarras} />
      <FormField
        label="Preço *"
        value={preco}
        onChangeText={setPreco}
        keyboardType="decimal-pad"
      />
    </FormScreen>
  );
}

const styles = StyleSheet.create({
  fotoSection: {
    alignItems: 'center',
    marginBottom: 16,
    gap: 12,
  },
  preview: {
    width: 120,
    height: 120,
    borderRadius: 12,
    borderWidth: 2,
    borderColor: colors.primary,
  },
  previewPlaceholder: {
    width: 120,
    height: 120,
    borderRadius: 12,
    backgroundColor: '#e2e8f0',
    alignItems: 'center',
    justifyContent: 'center',
  },
  previewPlaceholderText: {
    color: '#64748b',
  },
  botaoExcluir: {
    marginTop: 12,
    borderColor: '#dc2626',
  },
  aviso: {
    color: '#64748b',
    textAlign: 'center',
    marginTop: 16,
  },
});
