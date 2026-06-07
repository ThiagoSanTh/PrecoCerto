import { View, Text, Pressable, Alert } from 'react-native';
import { useState, useEffect } from 'react';
import { listarProdutosParaFeed } from '../services/productService';
import { nomeProduto } from '../utils/produtoUtils';
import { criarOferta } from '../services/ofertaService';
import { useAuth } from '../context/AuthContext';
import {
  FormScreen,
  FormField,
  PrimaryButton,
  formStyles,
} from '../components/form';

export default function CreateOfertaScreen({ navigation }) {
  const { session } = useAuth();
  const lojaId = session?.perfil?.lojaId;

  const [produtos, setProdutos] = useState([]);
  const [produtoId, setProdutoId] = useState('');
  const [preco, setPreco] = useState('');
  const [quantidadeEstoque, setQuantidadeEstoque] = useState('');
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!lojaId) return;
    listarProdutosParaFeed(lojaId).then(setProdutos).catch(() => {});
  }, [lojaId]);

  async function handleSalvar() {
    if (!lojaId || !produtoId || !preco) {
      Alert.alert('Erro', 'Selecione produto e informe o preço');
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
        produtoId,
        lojaId,
        preco: precoNum,
        disponivel: true,
        emPromocao: false,
        quantidadeEstoque: quantidadeEstoque ? parseInt(quantidadeEstoque, 10) : null,
      });
      Alert.alert('Sucesso', 'Oferta criada');
      navigation.goBack();
    } catch (error) {
      Alert.alert('Erro', String(error.response?.data || error.message));
    } finally {
      setLoading(false);
    }
  }

  return (
    <FormScreen
      title="Nova oferta"
      subtitle="Vincule um produto à sua loja"
      onBack={() => navigation.goBack()}
      footer={<PrimaryButton label="Salvar oferta" onPress={handleSalvar} loading={loading} />}
    >
      <Text style={formStyles.sectionHint}>Toque em um produto para selecionar:</Text>

      {produtos.length > 0 ? (
        <View style={{ marginBottom: 12 }}>
          {produtos.slice(0, 8).map((p) => {
            const selected = produtoId === p.id;
            return (
              <Pressable
                key={p.id}
                onPress={() => setProdutoId(p.id)}
                style={[
                  formStyles.listCard,
                  selected && { borderColor: '#2DD4BF', borderWidth: 2 },
                ]}
              >
                <Text style={formStyles.listCardTitle}>{nomeProduto(p)}</Text>
                <Text style={formStyles.listCardText}>{p.marca}</Text>
              </Pressable>
            );
          })}
        </View>
      ) : null}

      <FormField
        label="ID do produto *"
        value={produtoId}
        onChangeText={setProdutoId}
        autoCapitalize="none"
        placeholder="ou selecione acima"
      />
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
