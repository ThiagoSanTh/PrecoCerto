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

  const columnWidth = `${contentWidthPercent * 100}%`;
  const columnMaxWidth = isAuth && showAuthCard ? authCardMaxWidth : contentMaxWidth;

  const inner = (
    <View
      style={[
        showAuthCard ? styles.authCard : styles.contentColumn,
        {
          width: columnWidth,
          maxWidth: columnMaxWidth,
          backgroundColor: showAuthCard ? colors.surface : undefined,
          borderColor: showAuthCard ? colors.border : undefined,
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
    paddingVertical: 24,
  },
  contentColumn: {
    flex: 1,
    alignSelf: 'center',
    overflow: 'hidden',
  },
  authCard: {
    alignSelf: 'center',
    overflow: 'hidden',
    borderRadius: 16,
    borderWidth: 1,
    maxHeight: '92%',
    ...(Platform.OS === 'web'
      ? {
          boxShadow: '0 8px 32px rgba(15, 23, 42, 0.12)',
        }
      : {}),
  },
});
