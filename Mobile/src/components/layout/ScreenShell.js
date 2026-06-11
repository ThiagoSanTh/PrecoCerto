import { View, StyleSheet, Platform } from 'react-native';
import { useLayoutProfile } from '../../hooks/useLayoutProfile';
import { useTheme } from '../../context/ThemeContext';

/**
 * Wrapper centralizado (80% da largura) para todas as telas.
 * variant: default | auth | fullBleed
 */
export default function ScreenShell({ children, variant = 'default', fullBleed = false }) {
  const { isWeb, isDesktopWeb, contentWidthPercent, contentMaxWidth, authCardMaxWidth } =
    useLayoutProfile();
  const { colors } = useTheme();

  const shellVariant = fullBleed ? 'fullBleed' : variant;
  const isAuth = shellVariant === 'auth';
  const showAuthCard = isAuth && isWeb && isDesktopWeb;

  const inner = (
    <View
      style={[
        styles.contentColumn,
        {
          width: `${contentWidthPercent * 100}%`,
          maxWidth: isAuth && showAuthCard ? authCardMaxWidth : contentMaxWidth,
        },
        showAuthCard && styles.authCard,
        showAuthCard && {
          backgroundColor: colors.surface,
          borderColor: colors.border,
        },
      ]}
    >
      {children}
    </View>
  );

  if (isAuth && isWeb) {
    return (
      <View
        style={[
          styles.authRoot,
          isDesktopWeb && styles.authRootDesktop,
          { backgroundColor: isDesktopWeb ? '#E2E8F0' : colors.background },
        ]}
      >
        {inner}
      </View>
    );
  }

  return (
    <View style={[styles.root, { backgroundColor: colors.background }]}>
      {inner}
    </View>
  );
}

const styles = StyleSheet.create({
  root: {
    flex: 1,
    alignItems: 'center',
    width: '100%',
  },
  authRoot: {
    flex: 1,
    alignItems: 'center',
    width: '100%',
  },
  authRootDesktop: {
    justifyContent: 'center',
  },
  contentColumn: {
    flex: 1,
    alignSelf: 'center',
    overflow: 'hidden',
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
