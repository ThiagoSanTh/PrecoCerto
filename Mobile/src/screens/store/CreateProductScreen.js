import { Alert, Image, Text, StyleSheet, View } from 'react-native';
import { useState, useRef } from 'react';
import * as ImagePicker from 'expo-image-picker';
import { criarProduto, atualizarProduto } from '../../services/productService';
import { invalidarFeedCache } from '../../services/feedService';
import { uploadImagemProduto } from '../../services/storageService';
import { useAuth } from '../../context/AuthContext';
import { useTheme } from '../../context/ThemeContext';
import {
  FormScreen,
  FormField,
  PrimaryButton,
  SecondaryButton,
} from '../../components/form';
import CategoriaPicker from '../../components/product/CategoriaPicker';
import { fonts, radii } from '../../theme';

export default function CreateProductScreen({ navigation, route }) {
  const { session } = useAuth();
  const { colors } = useTheme();
  const lojaId = session?.perfil?.lojaId;
  const produtoEditando = route?.params?.produto || null;
  const isEdicao = Boolean(produtoEditando?.id);

  const [nomeProduto, setNomeProduto] = useState(
    produtoEditando?.nomeProduto || produtoEditando?.nome || ''
  );
  const [descricao, setDescricao] = useState(produtoEditando?.descricao || '');
  const [marca, setMarca] = useState(produtoEditando?.marca || '');
  const [codigoBarras, setCodigoBarras] = useState(
    produtoEditando?.codigoBarras || ''
  );
  const [preco, setPreco] = useState(
    produtoEditando?.preco != null
      ? String(produtoEditando.preco).replace('.', ',')
      : ''
  );
  const [categoria, setCategoria] = useState(produtoEditando?.categoria ?? 0);
  const [imagemUrlAtual] = useState(produtoEditando?.imagemUrl || null);
  const [imagemUri, setImagemUri] = useState(null);
  const [imagemMime, setImagemMime] = useState('image/jpeg');
  const [loading, setLoading] = useState(false);

  const descricaoRef = useRef(null);
  const marcaRef = useRef(null);
  const codigoBarrasRef = useRef(null);
  const precoRef = useRef(null);

  const imagemExibida = imagemUri || imagemUrlAtual;

  async function handleEscolherFoto() {
    const { status } = await ImagePicker.requestMediaLibraryPermissionsAsync();
    if (status !== 'granted') {
      Alert.alert(
        'Permissão',
        'Permita o acesso à galeria para adicionar a foto do produto.'
      );
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

  async function handleSaveProduct() {
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
        Alert.alert('Erro', 'Cadastre sua loja antes de gerenciar produtos');
        return;
      }

      if (!imagemExibida) {
        Alert.alert(
          'Foto obrigatória',
          'Adicione uma foto do produto para exibir no mapa.'
        );
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

      const payload = {
        nomeProduto,
        descricao,
        marca,
        codigoBarras,
        preco: precoConvertido,
        categoria,
        lojaId,
        imagemUrl,
      };

      if (isEdicao) {
        await atualizarProduto(produtoEditando.id, payload);
      } else {
        await criarProduto(payload);
      }

      invalidarFeedCache('feed');
      Alert.alert(
        'Sucesso',
        isEdicao ? 'Produto atualizado com sucesso' : 'Produto criado com sucesso'
      );
      navigation.goBack();
    } catch (error) {
      console.error(error?.response?.data || error.message);
      Alert.alert(
        'Erro',
        error.message ||
          (isEdicao
            ? 'Não foi possível atualizar o produto'
            : 'Não foi possível criar o produto')
      );
    } finally {
      setLoading(false);
    }
  }

  return (
    <FormScreen
      title={isEdicao ? 'Editar produto' : 'Novo produto'}
      subtitle={
        isEdicao
          ? 'Atualize os dados do produto no catálogo'
          : 'Produto vinculado à sua loja'
      }
      onBack={() => navigation.goBack()}
      narrowContent
      footer={
        <PrimaryButton
          label={isEdicao ? 'Salvar alterações' : 'Salvar produto'}
          onPress={handleSaveProduct}
          loading={loading}
        />
      }
    >
      <View style={styles.fotoSection}>
        {imagemExibida ? (
          <Image
            source={{ uri: imagemExibida }}
            style={[styles.preview, { borderColor: colors.primary }]}
          />
        ) : (
          <View
            style={[
              styles.previewPlaceholder,
              { backgroundColor: colors.surfaceSubtle },
            ]}
          >
            <Text
              style={[
                styles.previewPlaceholderText,
                { color: colors.textMuted },
              ]}
            >
              Sem foto
            </Text>
          </View>
        )}
        <SecondaryButton
          label={imagemExibida ? 'Alterar foto' : 'Adicionar foto *'}
          onPress={handleEscolherFoto}
        />
      </View>

      <FormField
        label="Nome *"
        value={nomeProduto}
        onChangeText={setNomeProduto}
        autoFocus={!isEdicao}
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
        onSubmitEditing={handleSaveProduct}
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
    borderRadius: radii.md,
    borderWidth: 2,
  },
  previewPlaceholder: {
    width: 120,
    height: 120,
    borderRadius: radii.md,
    alignItems: 'center',
    justifyContent: 'center',
  },
  previewPlaceholderText: {
    fontFamily: fonts.sans,
    fontSize: 13,
  },
});
