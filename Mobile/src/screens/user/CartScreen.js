import { View, Text, FlatList, Pressable, Alert, ActivityIndicator, StyleSheet } from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import { useCallback } from 'react';
import { useCarrinho } from '../../context/CarrinhoContext';
import { formatarPrecoBrl } from '../../utils/mapaUtils';
import { FormScreen, PrimaryButton, SecondaryButton, ListCardText } from '../../components/form';
import { colors } from '../../theme';

export default function CartScreen() {
  const { carrinho, loading, recarregar, atualizarQuantidade, remover, limpar } = useCarrinho();

  useFocusEffect(
    useCallback(() => {
      recarregar();
    }, [recarregar])
  );

  async function alterar(itemId, novaQuantidade) {
    try {
      await atualizarQuantidade(itemId, novaQuantidade);
    } catch {
      Alert.alert('Erro', 'Não foi possível atualizar o item.');
    }
  }

  async function removerItem(itemId) {
    try {
      await remover(itemId);
    } catch {
      Alert.alert('Erro', 'Não foi possível remover o item.');
    }
  }

  function confirmarLimpar() {
    Alert.alert('Esvaziar carrinho', 'Deseja remover todos os itens?', [
      { text: 'Cancelar', style: 'cancel' },
      { text: 'Esvaziar', style: 'destructive', onPress: () => limpar().catch(() => {}) },
    ]);
  }

  function renderItem({ item }) {
    return (
      <View style={styles.card}>
        <View style={{ flex: 1 }}>
          <Text style={styles.nome}>{item.nomeProduto}</Text>
          <Text style={styles.preco}>{formatarPrecoBrl(item.precoUnitario)} cada</Text>
          <Text style={styles.subtotal}>Subtotal: {formatarPrecoBrl(item.subtotal)}</Text>
        </View>

        <View style={styles.acoes}>
          <View style={styles.qtdRow}>
            <Pressable style={styles.qtdBtn} onPress={() => alterar(item.id, item.quantidade - 1)}>
              <Text style={styles.qtdBtnText}>−</Text>
            </Pressable>
            <Text style={styles.qtd}>{item.quantidade}</Text>
            <Pressable style={styles.qtdBtn} onPress={() => alterar(item.id, item.quantidade + 1)}>
              <Text style={styles.qtdBtnText}>＋</Text>
            </Pressable>
          </View>
          <Pressable onPress={() => removerItem(item.id)}>
            <Text style={styles.remover}>Remover</Text>
          </Pressable>
        </View>
      </View>
    );
  }

  const itens = carrinho?.itens ?? [];

  return (
    <FormScreen
      title="Carrinho"
      subtitle="Seus produtos selecionados"
      scrollable={false}
      footer={
        itens.length > 0 ? (
          <>
            <View style={styles.totalRow}>
              <Text style={styles.totalLabel}>Total</Text>
              <Text style={styles.totalValor}>{formatarPrecoBrl(carrinho?.total ?? 0)}</Text>
            </View>
            <PrimaryButton
              label="Finalizar compra"
              onPress={() => Alert.alert('Em breve', 'O checkout estará disponível em breve.')}
            />
            <SecondaryButton label="Esvaziar carrinho" onPress={confirmarLimpar} />
          </>
        ) : undefined
      }
    >
      {loading ? (
        <ActivityIndicator size="large" color={colors.primary} style={{ marginTop: 32 }} />
      ) : (
        <FlatList
          data={itens}
          keyExtractor={(item) => item.id}
          renderItem={renderItem}
          contentContainerStyle={{ paddingVertical: 8 }}
          ListEmptyComponent={
            <ListCardText style={{ textAlign: 'center', marginTop: 32 }}>
              Seu carrinho está vazio.
            </ListCardText>
          }
        />
      )}
    </FormScreen>
  );
}

const styles = StyleSheet.create({
  card: {
    flexDirection: 'row',
    backgroundColor: '#F8FAFC',
    borderWidth: 1,
    borderColor: '#E2E8F0',
    borderRadius: 10,
    padding: 12,
    marginBottom: 10,
  },
  nome: { fontSize: 15, fontWeight: '700', color: '#0F172A' },
  preco: { fontSize: 13, color: '#64748B', marginTop: 4 },
  subtotal: { fontSize: 13, color: '#0F172A', marginTop: 4, fontWeight: '600' },
  acoes: { alignItems: 'flex-end', justifyContent: 'space-between' },
  qtdRow: { flexDirection: 'row', alignItems: 'center', gap: 10 },
  qtdBtn: {
    width: 30,
    height: 30,
    borderRadius: 15,
    borderWidth: 1,
    borderColor: colors.primary,
    alignItems: 'center',
    justifyContent: 'center',
  },
  qtdBtnText: { color: colors.primary, fontSize: 18, fontWeight: '700' },
  qtd: { fontSize: 16, fontWeight: '600', color: '#0F172A', minWidth: 20, textAlign: 'center' },
  remover: { color: '#EF4444', fontWeight: '600', fontSize: 13, marginTop: 8 },
  totalRow: { flexDirection: 'row', justifyContent: 'space-between', marginBottom: 8 },
  totalLabel: { fontSize: 16, fontWeight: '700', color: '#0F172A' },
  totalValor: { fontSize: 16, fontWeight: '700', color: colors.primaryDark },
});
