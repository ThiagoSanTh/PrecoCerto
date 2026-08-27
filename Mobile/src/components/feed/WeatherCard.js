import { View, Text, StyleSheet } from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useTheme } from '../../context/ThemeContext';

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

export default function WeatherCard({ status = 'loading', dados = null, stale = false, style }) {
  const { colors } = useTheme();
  const cardStyle = [
    styles.card,
    { backgroundColor: colors.card, borderColor: colors.border },
    style,
  ];

  if (status === 'loading' && !dados) {
    return (
      <View style={cardStyle} accessibilityLabel="Carregando clima">
        <View style={[styles.skelLine, { backgroundColor: colors.border, width: '42%' }]} />
        <View style={[styles.skelLine, { backgroundColor: colors.border, width: '70%', marginTop: 8 }]} />
      </View>
    );
  }

  if (status === 'sem-localizacao') {
    return (
      <View style={cardStyle}>
        <Text style={[styles.muted, { color: colors.textMuted }]}>
          Ative a localização para ver o clima da região.
        </Text>
      </View>
    );
  }

  if ((status === 'erro' || status === 'offline') && !dados) {
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
  const temp = formatarGraus(dados.atual.temperatura);
  const sensacao = formatarGraus(dados.atual.sensacaoTermica);
  const icone = ICONES[dados.atual.icone] || ICONES.cloudy;
  const hoje = dados.diaria?.[0];
  const min = formatarGraus(hoje?.temperaturaMinima);
  const max = formatarGraus(hoje?.temperaturaMaxima);
  const mostrarStale = stale || dados.desatualizado;
  const hora = formatarAtualizacao(dados.obtidoEm);

  return (
    <View style={cardStyle} accessibilityLabel={`Clima em ${cidade}`}>
      <View style={styles.row}>
        <Ionicons name={icone} size={18} color={colors.primary} />
        <Text style={[styles.cidade, { color: colors.text }]} numberOfLines={1}>
          {cidade}
        </Text>
        {temp ? (
          <Text style={[styles.temp, { color: colors.text }]}>{temp}</Text>
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
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    borderWidth: 1,
    borderRadius: 10,
    paddingHorizontal: 10,
    paddingVertical: 8,
    minHeight: 52,
    justifyContent: 'center',
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
  },
  cidade: {
    flex: 1,
    fontSize: 13,
    fontWeight: '600',
  },
  temp: {
    fontSize: 16,
    fontWeight: '700',
  },
  muted: {
    fontSize: 11,
    marginTop: 2,
  },
  stale: {
    fontSize: 10,
    marginTop: 2,
  },
  skelLine: {
    height: 8,
    borderRadius: 4,
  },
});
