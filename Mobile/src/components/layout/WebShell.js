import { View, StyleSheet, Platform } from 'react-native';
import { useLayoutProfile } from '../../hooks/useLayoutProfile';
import { useTheme } from '../../context/ThemeContext';

/**
 * Wrapper visual só para telas de auth no web (card centralizado).
 * Telas do app (tabs, mapa) não devem usar este wrapper — quebra flex no RN Web.
 */
export default function WebShell({ children, variant = 'default' }) {
  const { isWeb, isDesktopWeb, authCardMaxWidth } = useLayoutProfile();
  const { colors } = useTheme();

  if (!isWeb || variant !== 'auth') {
    return children;
  }

  return (
    <View
      style={[
        styles.root,
        isDesktopWeb && styles.rootDesktop,
        { backgroundColor: isDesktopWeb ? '#E2E8F0' : colors.background },
      ]}
    >
      <View
        style={[
          styles.authInner,
          isDesktopWeb && styles.authCard,
          {
            maxWidth: authCardMaxWidth,
            backgroundColor: colors.surface,
            borderColor: colors.border,
          },
        ]}
      >
        {children}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  root: {
    flex: 1,
    alignItems: 'center',
    width: '100%',
  },
  rootDesktop: {
    justifyContent: 'center',
  },
  authInner: {
    width: '100%',
    flex: 1,
    maxHeight: '100%',
  },
  authCard: {
    flex: 0,
    marginVertical: 32,
    borderRadius: 16,
    borderWidth: 1,
    overflow: 'hidden',
    maxHeight: '92%',
    ...(Platform.OS === 'web'
      ? {
          boxShadow: '0 8px 32px rgba(15, 23, 42, 0.12)',
        }
      : {}),
  },
});
