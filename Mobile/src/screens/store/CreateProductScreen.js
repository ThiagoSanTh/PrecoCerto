import { Alert, Image, Pressable, Text, StyleSheet, View } from 'react-native';
import { useState, useRef } from 'react';
import * as ImagePicker from 'expo-image-picker';
import { criarProduto } from '../../services/productService';
import { invalidarFeedCache } from '../../services/feedService';
import { uploadImagemProduto } from '../../services/storageService';
import { useAuth } from '../../context/AuthContext';
import { FormScreen, FormField, PrimaryButton, SecondaryButton } from '../../components/form';
import CategoriaPicker from '../../components/product/CategoriaPicker';
import { colors } from '../../theme';

export default function CreateProductScreen({ navigation }) {
  const { session } = useAuth();
  const lojaId = session?.perfil?.lojaId;
  const [nomeProduto, setNomeProduto] = useState('');
  const [descricao, setDescricao] = useState('');
  const [marca, setMarca] = useState('');
  const [codigoBarras, setCodigoBarras] = useState('');
  const [preco, setPreco] = useState('');
  const [categoria, setCategoria] = useState(0);
  const [imagemUri, setImagemUri] = useState(null);
  const [imagemMime, setImagemMime] = useState('image/jpeg');
  const [loading, setLoading] = useState(false);
  const descricaoRef = useRef(null);
  const marcaRef = useRef(null);
  const codigoBarrasRef = useRef(null);
  const precoRef = useRef(null);

  async function handleEscolherFoto() {
    const { status } = await ImagePicker.requestMediaLibraryPermissionsAsync();
    if (status !== 'granted') {
      Alert.alert('Permissão', 'Permita o acesso à galeria para adicionar a foto do produto.');
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

  async function handleCreateProduct() {
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
        Alert.alert('Erro', 'Cadastre sua loja antes de criar produtos');
        return;
      }

      if (!imagemUri) {
        Alert.alert('Foto obrigatória', 'Adicione uma foto do produto para exibir no mapa.');
        return;
      }

      setLoading(true);

      const imagemUrl = await uploadImagemProduto({
        uri: imagemUri,
        lojaId,
        mimeType: imagemMime,
      });

      await criarProduto({
        nomeProduto,
        descricao,
        marca,
        codigoBarras,
        preco: precoConvertido,
        categoria,
        lojaId,
        imagemUrl,
      });

      invalidarFeedCache('feed');
      Alert.alert('Sucesso', 'Produto criado com sucesso');
      navigation.goBack();
    } catch (error) {
      console.error(error?.response?.data || error.message);
      Alert.alert('Erro', error.message || 'Não foi possível criar o produto');
    } finally {
      setLoading(false);
    }
  }

  return (
    <FormScreen
      title="Novo produto"
      subtitle="Produto vinculado à sua loja"
      onBack={() => navigation.goBack()}
      narrowContent
      footer={<PrimaryButton label="Salvar produto" onPress={handleCreateProduct} loading={loading} />}
    >
      <View style={styles.fotoSection}>
        {imagemUri ? (
          <Image source={{ uri: imagemUri }} style={styles.preview} />
        ) : (
          <View style={styles.previewPlaceholder}>
            <Text style={styles.previewPlaceholderText}>Sem foto</Text>
          </View>
        )}
        <SecondaryButton label="Adicionar foto *" onPress={handleEscolherFoto} />
      </View>

      <FormField
        label="Nome *"
        value={nomeProduto}
        onChangeText={setNomeProduto}
        autoFocus
        returnKeyType="next"
        onSubmitEditing={() => descricaoRef.current?.focus()}
      />
      <FormField
        ref={descricaoRef}
        label="Descrição"
        value={descricao}
        onChangeText={setDescricao}
        returnKeyType="next"
        onSubmitEditing={() => marcaRef.current?.focus()}
      />
      <FormField
        ref={marcaRef}
        label="Marca *"
        value={marca}
        onChangeText={setMarca}
        returnKeyType="next"
        onSubmitEditing={() => codigoBarrasRef.current?.focus()}
      />
      <FormField
        ref={codigoBarrasRef}
        label="Código de barras *"
        value={codigoBarras}
        onChangeText={setCodigoBarras}
        returnKeyType="next"
        onSubmitEditing={() => precoRef.current?.focus()}
      />
      <CategoriaPicker value={categoria} onChange={setCategoria} />
      <FormField
        ref={precoRef}
        label="Preço *"
        value={preco}
        onChangeText={setPreco}
        keyboardType="decimal-pad"
        returnKeyType="go"
        onSubmitEditing={handleCreateProduct}
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
});
