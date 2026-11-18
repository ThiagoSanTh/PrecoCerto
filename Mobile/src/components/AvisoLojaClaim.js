import { useEffect, useState } from 'react';
import { View, Text, Pressable, StyleSheet } from 'react-native';
import { useAuth } from '../context/AuthContext';
import { useTheme } from '../context/ThemeContext';
import { navigationRef } from '../navigation/navigationRef';
import { observarAvisoLoja, resetLojaClaim } from '../services/lojaClaim';
import { fonts, radii } from '../theme';

export default function AvisoLojaClaim() {
  const { logout } = useAuth();
  const { colors } = useTheme();
  const [visivel, setVisivel] = useState(false);

  useEffect(() => observarAvisoLoja(() => setVisivel(true)), []);

  if (!visivel) return null;

  async function sair() {
    setVisivel(false);
    await logout();
    resetLojaClaim();
    if (navigationRef.isReady()) {
      navigationRef.reset({ index: 0, routes: [{ name: 'Login' }] });
    }
  }

  return (
    <View
      accessibilityRole="alert"
      accessibilityLiveRegion="polite"
      style={[styles.faixa, { backgroundColor: colors.ink, borderColor: colors.border }]}
    >
      <Text style={[styles.texto, { color: colors.inkForeground }]}>
        Seu acesso à loja mudou. Entre de novo.
      </Text>
      <Pressable
        accessibilityRole="button"
        accessibilityLabel="Sair"
        onPress={sair}
        style={[styles.sair, { backgroundColor: colors.primary }]}
      >
        <Text style={[styles.sairTexto, { color: colors.primaryForeground }]}>Sair</Text>
      </Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
  faixa: {
    position: 'absolute',
    top: 12,
    left: 12,
    right: 12,
    zIndex: 40,
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    borderWidth: 1,
    borderRadius: radii.lg,
    paddingHorizontal: 14,
    paddingVertical: 10,
  },
  texto: {
    flex: 1,
    fontFamily: fonts.sansSemiBold,
    fontSize: 14,
    fontWeight: '600',
  },
  sair: {
    minHeight: 44,
    minWidth: 44,
    paddingHorizontal: 14,
    borderRadius: radii.md,
    alignItems: 'center',
    justifyContent: 'center',
  },
  sairTexto: {
    fontFamily: fonts.sansSemiBold,
    fontSize: 14,
    fontWeight: '700',
  },
});
