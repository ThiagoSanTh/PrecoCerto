import { View, Text, Switch, StyleSheet } from 'react-native';
import { FormField } from '../form';
import { colors } from '../../theme';

export default function PromocaoSection({
  emPromocao,
  onTogglePromocao,
  precoPromocional,
  onChangePrecoPromocional,
  precoAnterior,
  onChangePrecoAnterior,
  dataInicio,
  onChangeDataInicio,
  dataFim,
  onChangeDataFim,
  quantidadeEstoque,
  onChangeQuantidadeEstoque,
}) {
  return (
    <View style={styles.container}>
      <Text style={styles.titulo}>Promoção</Text>
      <Text style={styles.hint}>
        Configure uma oferta promocional para este produto na sua loja.
      </Text>

      <View style={styles.switchRow}>
        <Text style={styles.switchLabel}>Produto em promoção</Text>
        <Switch
          value={emPromocao}
          onValueChange={onTogglePromocao}
          trackColor={{ false: '#CBD5E1', true: colors.primary }}
          thumbColor="#fff"
        />
      </View>

      {emPromocao ? (
        <>
          <FormField
            label="Preço promocional *"
            value={precoPromocional}
            onChangeText={onChangePrecoPromocional}
            keyboardType="decimal-pad"
            placeholder="Ex: 9,90"
          />
          <FormField
            label="Preço anterior"
            value={precoAnterior}
            onChangeText={onChangePrecoAnterior}
            keyboardType="decimal-pad"
            placeholder="Preço antes da promoção"
          />
          <FormField
            label="Data início (DD/MM/AAAA)"
            value={dataInicio}
            onChangeText={onChangeDataInicio}
            placeholder="Opcional"
            keyboardType="number-pad"
          />
          <FormField
            label="Data fim (DD/MM/AAAA)"
            value={dataFim}
            onChangeText={onChangeDataFim}
            placeholder="Opcional"
            keyboardType="number-pad"
          />
        </>
      ) : null}

      <FormField
        label="Quantidade em estoque"
        value={quantidadeEstoque}
        onChangeText={onChangeQuantidadeEstoque}
        keyboardType="number-pad"
        placeholder="Opcional"
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    marginTop: 8,
    marginBottom: 16,
    padding: 14,
    backgroundColor: '#F8FAFC',
    borderRadius: 10,
    borderWidth: 1,
    borderColor: '#E2E8F0',
  },
  titulo: {
    fontSize: 18,
    fontWeight: '700',
    color: '#0F172A',
    marginBottom: 4,
  },
  hint: {
    fontSize: 13,
    color: '#64748B',
    marginBottom: 12,
  },
  switchRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: 12,
  },
  switchLabel: {
    fontSize: 15,
    fontWeight: '600',
    color: '#0F172A',
    flex: 1,
    marginRight: 8,
  },
});
