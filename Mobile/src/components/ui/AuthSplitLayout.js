import { View, Text, ScrollView, StyleSheet, Platform } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Ionicons } from '@expo/vector-icons';
import { useLayoutProfile } from '../../hooks/useLayoutProfile';
import { useTheme } from '../../context/ThemeContext';
import FormErrorBanner from '../form/FormErrorBanner';
import { BrandLogo } from './DesignPrimitives';
import { fonts, radii } from '../../theme';

const FEATURES = [
  { icon: 'location-outline', text: 'Preços das lojas físicas da sua cidade' },
  { icon: 'sparkles-outline', text: 'Assistente com IA que monta sua lista' },
  { icon: 'storefront-outline', text: 'Painel de inteligência para lojistas' },
];

export default function AuthSplitLayout({
  title,
  subtitle,
  error,
  onErrorAction,
  children,
}) {
  const { isDesktopWeb } = useLayoutProfile();
  const { colors } = useTheme();

  return (
    <SafeAreaView
      style={[styles.safe, { backgroundColor: colors.background }]}
      edges={['top', 'bottom', 'left', 'right']}
    >
      <View style={[styles.container, isDesktopWeb && styles.containerSplit]}>
        {isDesktopWeb ? (
          <View
            style={[
              styles.heroPanel,
              { backgroundColor: colors.ink },
              Platform.OS === 'web'
                ? {
                    backgroundImage:
                      'linear-gradient(135deg, #0F1D22 0%, #103A35 100%)',
                  }
                : null,
            ]}
          >
            <BrandLogo dark />
            <View style={styles.heroMiddle}>
              <Text
                style={[
                  styles.heroTitle,
                  { color: colors.inkForeground },
                ]}
              >
                Encontre o produto certo, na loja mais perto, pelo menor preço.
              </Text>
              <View style={styles.featureList}>
                {FEATURES.map((f) => (
                  <View key={f.text} style={styles.featureRow}>
                    <View
                      style={[
                        styles.featureIconBox,
                        { backgroundColor: colors.sidebarAccent },
                      ]}
                    >
                      <Ionicons
                        name={f.icon}
                        size={16}
                        color={colors.highlight}
                      />
                    </View>
                    <Text
                      style={[
                        styles.featureText,
                        { color: colors.inkForeground },
                      ]}
                    >
                      {f.text}
                    </Text>
                  </View>
                ))}
              </View>
            </View>
            <Text style={[styles.heroFooter, { color: colors.inkMuted }]}>
              © 2026 Preço Certo · Saquarema, RJ
            </Text>
          </View>
        ) : null}

        <ScrollView
          style={styles.formPane}
          contentContainerStyle={styles.formScrollContent}
          keyboardShouldPersistTaps="handled"
          showsVerticalScrollIndicator={false}
        >
          <View style={styles.formBox}>
            {!isDesktopWeb ? (
              <View style={styles.mobileLogoWrap}>
                <BrandLogo dark={false} />
              </View>
            ) : null}

            <Text style={[styles.formTitle, { color: colors.text }]}>
              {title}
            </Text>
            {subtitle ? (
              <Text style={[styles.formSubtitle, { color: colors.textMuted }]}>
                {subtitle}
              </Text>
            ) : null}

            <View style={styles.formBody}>
              <FormErrorBanner error={error} onAction={onErrorAction} />
              {children}
            </View>
          </View>
        </ScrollView>
      </View>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safe: {
    flex: 1,
  },
  container: {
    flex: 1,
    width: '100%',
  },
  containerSplit: {
    flexDirection: 'row',
  },
  heroPanel: {
    flex: 1,
    padding: 48,
    justifyContent: 'space-between',
  },
  heroMiddle: {
    maxWidth: 460,
  },
  heroTitle: {
    fontFamily: fonts.display,
    fontSize: 36,
    fontWeight: '700',
    lineHeight: 44,
    letterSpacing: -0.8,
  },
  featureList: {
    marginTop: 32,
    gap: 14,
  },
  featureRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
  },
  featureIconBox: {
    width: 34,
    height: 34,
    borderRadius: radii.sm,
    alignItems: 'center',
    justifyContent: 'center',
  },
  featureText: {
    fontFamily: fonts.sans,
    fontSize: 14,
    fontWeight: '500',
  },
  heroFooter: {
    fontFamily: fonts.sans,
    fontSize: 12,
  },
  formPane: {
    flex: 1,
  },
  formScrollContent: {
    flexGrow: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: 24,
  },
  formBox: {
    width: '100%',
    maxWidth: 384,
  },
  mobileLogoWrap: {
    marginBottom: 28,
  },
  formTitle: {
    fontFamily: fonts.display,
    fontSize: 30,
    fontWeight: '700',
    letterSpacing: -0.6,
  },
  formSubtitle: {
    fontFamily: fonts.sans,
    fontSize: 14,
    lineHeight: 20,
    marginTop: 8,
  },
  formBody: {
    marginTop: 28,
  },
});
