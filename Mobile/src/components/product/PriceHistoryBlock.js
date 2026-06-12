import { useMemo } from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { formatarPrecoBrl } from '../../utils/mapaUtils';
import { formatarPrecoMl } from '../../utils/precoUtils';
import { useLayoutProfile } from '../../hooks/useLayoutProfile';

function formatarData(data) {
  if (!data) return null;
  const d = new Date(data);
  if (Number.isNaN(d.getTime())) return null;
  return d.toLocaleDateString('pt-BR');
}

const baseStyles = StyleSheet.create({
  container: {
    marginBottom: 16,
  },
  badge: {
    alignSelf: 'flex-start',
    backgroundColor: '#DCFCE7',
    paddingHorizontal: 8,
    paddingVertical: 4,
    borderRadius: 4,
    marginBottom: 8,
  },
  badgeText: {
    color: '#15803D',
    fontSize: 12,
    fontWeight: '700',
  },
  precoHeaderRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  precoCol: {
    flex: 1,
    flexShrink: 1,
  },
  actions: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'flex-end',
    marginLeft: 12,
  },
  precoAtualRow: {
    flexDirection: 'row',
    alignItems: 'flex-start',
  },
  moeda: {
    fontSize: 18,
    fontWeight: '400',
    color: '#0F172A',
    marginTop: 6,
    marginRight: 2,
  },
  inteiro: {
    fontSize: 36,
    fontWeight: '300',
    color: '#0F172A',
    lineHeight: 40,
  },
  centavos: {
    fontSize: 18,
    fontWeight: '400',
    color: '#0F172A',
    marginTop: 4,
  },
  historico: {
    marginTop: 12,
    paddingTop: 12,
    borderTopWidth: 1,
    borderTopColor: '#E2E8F0',
  },
  historicoTitulo: {
    fontSize: 13,
    fontWeight: '600',
    color: '#64748B',
    marginBottom: 6,
  },
  antigoRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    marginBottom: 4,
  },
  antigoPreco: {
    fontSize: 14,
    color: '#94A3B8',
    textDecorationLine: 'line-through',
  },
  antigoData: {
    fontSize: 12,
    color: '#CBD5E1',
  },
});

const webStyles = StyleSheet.create({
  badgeText: {
    fontSize: 14,
  },
  moeda: {
    fontSize: 20,
    marginTop: 8,
  },
  inteiro: {
    fontSize: 42,
    lineHeight: 46,
  },
  centavos: {
    fontSize: 20,
    marginTop: 6,
  },
  historicoTitulo: {
    fontSize: 16,
  },
  antigoPreco: {
    fontSize: 16,
  },
  antigoData: {
    fontSize: 14,
  },
});

export default function PriceHistoryBlock({
  precoAtual,
  precosAntigos = [],
  emPromocao,
  actions,
}) {
  const { isWeb } = useLayoutProfile();
  const { inteiro, centavos } = formatarPrecoMl(precoAtual);

  const styles = useMemo(
    () => ({
      container: baseStyles.container,
      badge: baseStyles.badge,
      badgeText: [baseStyles.badgeText, isWeb && webStyles.badgeText],
      precoHeaderRow: baseStyles.precoHeaderRow,
      precoCol: baseStyles.precoCol,
      actions: baseStyles.actions,
      precoAtualRow: baseStyles.precoAtualRow,
      moeda: [baseStyles.moeda, isWeb && webStyles.moeda],
      inteiro: [baseStyles.inteiro, isWeb && webStyles.inteiro],
      centavos: [baseStyles.centavos, isWeb && webStyles.centavos],
      historico: baseStyles.historico,
      historicoTitulo: [baseStyles.historicoTitulo, isWeb && webStyles.historicoTitulo],
      antigoRow: baseStyles.antigoRow,
      antigoPreco: [baseStyles.antigoPreco, isWeb && webStyles.antigoPreco],
      antigoData: [baseStyles.antigoData, isWeb && webStyles.antigoData],
    }),
    [isWeb]
  );

  return (
    <View style={styles.container}>
      {emPromocao ? (
        <View style={styles.badge}>
          <Text style={styles.badgeText}>Promoção</Text>
        </View>
      ) : null}

      <View style={styles.precoHeaderRow}>
        <View style={styles.precoCol}>
          <View style={styles.precoAtualRow}>
            <Text style={styles.moeda}>R$</Text>
            <Text style={styles.inteiro}>{inteiro}</Text>
            <Text style={styles.centavos}>{centavos}</Text>
          </View>
        </View>
        {actions ? <View style={styles.actions}>{actions}</View> : null}
      </View>

      {precosAntigos.length > 0 ? (
        <View style={styles.historico}>
          <Text style={styles.historicoTitulo}>Preços anteriores</Text>
          {precosAntigos.map((item, index) => (
            <View key={index} style={styles.antigoRow}>
              <Text style={styles.antigoPreco}>{formatarPrecoBrl(item.valor)}</Text>
              {item.data ? (
                <Text style={styles.antigoData}>{formatarData(item.data)}</Text>
              ) : null}
            </View>
          ))}
        </View>
      ) : null}
    </View>
  );
}
