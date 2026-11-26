import { useMemo } from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { formatarPrecoBrl } from '../../utils/mapaUtils';
import { formatarPrecoMl } from '../../utils/precoUtils';
import { useLayoutProfile } from '../../hooks/useLayoutProfile';
import { useTheme } from '../../context/ThemeContext';
import { fonts, radii, tabularNums } from '../../theme';

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
    paddingHorizontal: 8,
    paddingVertical: 4,
    borderRadius: radii.sm,
    marginBottom: 8,
  },
  badgeText: {
    fontFamily: fonts.sansBold,
    fontSize: 11,
    fontWeight: '800',
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
    fontFamily: fonts.sansSemiBold,
    fontSize: 18,
    fontWeight: '600',
    marginTop: 6,
    marginRight: 2,
  },
  inteiro: {
    fontFamily: fonts.display,
    fontSize: 36,
    fontWeight: '700',
    lineHeight: 40,
  },
  centavos: {
    fontFamily: fonts.display,
    fontSize: 18,
    fontWeight: '700',
    marginTop: 4,
  },
  historico: {
    marginTop: 12,
    paddingTop: 12,
    borderTopWidth: 1,
  },
  historicoTitulo: {
    fontFamily: fonts.sansSemiBold,
    fontSize: 13,
    fontWeight: '600',
    marginBottom: 6,
  },
  antigoRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    marginBottom: 4,
  },
  antigoPreco: {
    fontFamily: fonts.sans,
    fontSize: 14,
    textDecorationLine: 'line-through',
  },
  antigoData: {
    fontFamily: fonts.sans,
    fontSize: 12,
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
  const { colors } = useTheme();
  const { inteiro, centavos } = formatarPrecoMl(precoAtual);

  const styles = useMemo(
    () => ({
      container: baseStyles.container,
      badge: [
        baseStyles.badge,
        { backgroundColor: colors.highlight },
      ],
      badgeText: [
        baseStyles.badgeText,
        isWeb && webStyles.badgeText,
        { color: colors.highlightForeground },
      ],
      precoHeaderRow: baseStyles.precoHeaderRow,
      precoCol: baseStyles.precoCol,
      actions: baseStyles.actions,
      precoAtualRow: baseStyles.precoAtualRow,
      moeda: [
        baseStyles.moeda,
        isWeb && webStyles.moeda,
        { color: colors.text },
        tabularNums,
      ],
      inteiro: [
        baseStyles.inteiro,
        isWeb && webStyles.inteiro,
        { color: colors.text },
        tabularNums,
      ],
      centavos: [
        baseStyles.centavos,
        isWeb && webStyles.centavos,
        { color: colors.text },
        tabularNums,
      ],
      historico: [baseStyles.historico, { borderTopColor: colors.border }],
      historicoTitulo: [
        baseStyles.historicoTitulo,
        isWeb && webStyles.historicoTitulo,
        { color: colors.textSecondary || colors.textMuted },
      ],
      antigoRow: baseStyles.antigoRow,
      antigoPreco: [
        baseStyles.antigoPreco,
        isWeb && webStyles.antigoPreco,
        { color: colors.textMuted },
        tabularNums,
      ],
      antigoData: [
        baseStyles.antigoData,
        isWeb && webStyles.antigoData,
        { color: colors.textMuted },
      ],
    }),
    [isWeb, colors]
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
