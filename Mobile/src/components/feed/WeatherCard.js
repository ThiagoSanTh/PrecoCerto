import { useState } from 'react';
import { View, Text, Pressable, Platform, StyleSheet } from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useTheme } from '../../context/ThemeContext';
import { fonts, radii, tabularNums } from '../../theme';

export const WEATHER_CARD_SLOT_HEIGHT = 64;

const ICONES = {
  clear: 'sunny-outline',
  'partly-cloudy': 'partly-sunny-outline',
  cloudy: 'cloudy-outline',
  fog: 'cloud-outline',
  drizzle: 'rainy-outline',
  rain: 'rainy-outline',
  snow: 'snow-outline',
  thunder: 'thunderstorm-outline',
};

function formatarGraus(valor) {
  if (valor == null || Number.isNaN(Number(valor))) return null;
  return `${Math.round(Number(valor))}°`;
}

function formatarAtualizacao(iso) {
  if (!iso) return null;
  const data = new Date(iso);
  if (Number.isNaN(data.getTime())) return null;
  return data.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' });
}

export default function WeatherCard({
  status = 'loading',
  dados = null,
  stale = false,
  compact = false,
  style,
}) {
  const { colors, isDark } = useTheme();
  const [expanded, setExpanded] = useState(false);
  const isPill = compact && !expanded;
  const cardBg =
    Platform.OS === 'web'
      ? isDark
        ? 'rgba(28, 48, 56, 0.9)'
        : 'rgba(255, 255, 255, 0.9)'
      : colors.card;
  const cardStyle = [
    styles.card,
    isPill && styles.cardPill,
    { backgroundColor: cardBg, borderColor: colors.border },
    Platform.OS === 'web' ? { backdropFilter: 'blur(12px)' } : null,
    style,
  ];

  if (status === 'loading' && !dados) {
    if (compact) return null;
    return (
      <View style={cardStyle} accessibilityLabel="Carregando clima">
        <View style={[styles.skelLine, { backgroundColor: colors.border, width: '42%' }]} />
        <View style={[styles.skelLine, { backgroundColor: colors.border, width: '70%', marginTop: 8 }]} />
      </View>
    );
  }

  if (status === 'sem-localizacao') {
    if (compact) return null;
    return (
      <View style={cardStyle}>
        <Text style={[styles.muted, { color: colors.textMuted }]}>
          Ative a localização para ver o clima da região.
        </Text>
      </View>
    );
  }

  if ((status === 'erro' || status === 'offline') && !dados) {
    if (compact) return null;
    return (
      <View style={cardStyle}>
        <Text style={[styles.muted, { color: colors.textMuted }]}>
          {status === 'offline'
            ? 'Clima indisponível no momento.'
            : 'Não foi possível atualizar o clima.'}
        </Text>
      </View>
    );
  }

  if (!dados?.atual) return null;

  const cidade = dados.localidade?.cidade
    ? [dados.localidade.cidade, dados.localidade.estado].filter(Boolean).join(', ')
    : 'Sua região';
  const cidadeCurta = dados.localidade?.cidade || 'Clima';
  const temp = formatarGraus(dados.atual.temperatura);
  const sensacao = formatarGraus(dados.atual.sensacaoTermica);
  const icone = ICONES[dados.atual.icone] || ICONES.cloudy;
  const hoje = dados.diaria?.[0];
  const min = formatarGraus(hoje?.temperaturaMinima);
  const max = formatarGraus(hoje?.temperaturaMaxima);
  const mostrarStale = stale || dados.desatualizado;
  const hora = formatarAtualizacao(dados.obtidoEm);

  if (isPill) {
    return (
      <Pressable
        onPress={() => setExpanded(true)}
        style={cardStyle}
        accessibilityRole="button"
        accessibilityLabel={`Clima em ${cidade}: ${temp || ''}. Toque para expandir`}
      >
        <View style={styles.pillRow}>
          <Ionicons name={icone} size={17} color={colors.chart4 || colors.warning} />
          <Text style={[styles.pillCidade, { color: colors.text }]} numberOfLines={1}>
            {cidadeCurta}
          </Text>
          {temp ? (
            <Text style={[styles.pillTemp, tabularNums, { color: colors.text }]}>{temp}</Text>
          ) : null}
        </View>
      </Pressable>
    );
  }

  const Container = compact ? Pressable : View;
  const containerProps = compact
    ? {
        onPress: () => setExpanded(false),
        accessibilityRole: 'button',
      }
    : {};

  return (
    <Container
      {...containerProps}
      style={cardStyle}
      accessibilityLabel={`Clima em ${cidade}`}
    >
      <View style={styles.row}>
        <Ionicons name={icone} size={19} color={colors.chart4 || colors.warning} />
        <Text style={[styles.cidade, { color: colors.text }]} numberOfLines={1}>
          {cidade}
        </Text>
        {temp ? (
          <Text style={[styles.temp, tabularNums, { color: colors.text }]}>{temp}</Text>
        ) : null}
        {compact ? (
          <Ionicons name="chevron-up" size={14} color={colors.textMuted} />
        ) : null}
      </View>
      <View style={styles.row}>
        <Text style={[styles.muted, { color: colors.textMuted, flex: 1 }]} numberOfLines={1}>
          {dados.atual.descricao || '—'}
          {sensacao ? ` · Sensação ${sensacao}` : ''}
          {min && max ? ` · Hoje ${min}–${max}` : ''}
        </Text>
      </View>
      {mostrarStale && hora ? (
        <Text style={[styles.stale, { color: colors.textMuted }]}>Última atualização {hora}</Text>
      ) : null}
    </Container>
  );
}

const styles = StyleSheet.create({
  card: {
    borderWidth: 1,
    borderRadius: radii.xl,
    paddingHorizontal: 14,
    paddingVertical: 10,
    minHeight: 52,
    justifyContent: 'center',
  },
  cardPill: {
    minHeight: 36,
    borderRadius: radii.full,
    paddingHorizontal: 12,
    paddingVertical: 6,
  },
  pillRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
  },
  pillCidade: {
    fontFamily: fonts.sansSemiBold,
    fontSize: 12,
    fontWeight: '600',
    maxWidth: 110,
  },
  pillTemp: {
    fontFamily: fonts.display,
    fontSize: 13,
    fontWeight: '700',
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  cidade: {
    flex: 1,
    fontFamily: fonts.sansSemiBold,
    fontSize: 12.5,
    fontWeight: '600',
  },
  temp: {
    fontFamily: fonts.display,
    fontSize: 15,
    fontWeight: '700',
  },
  muted: {
    fontFamily: fonts.sans,
    fontSize: 11.5,
    marginTop: 2,
  },
  stale: {
    fontFamily: fonts.sans,
    fontSize: 10,
    marginTop: 2,
  },
  skelLine: {
    height: 8,
    borderRadius: 4,
  },
});
