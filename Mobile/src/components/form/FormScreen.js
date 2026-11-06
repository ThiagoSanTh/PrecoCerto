import {
  View,
  Text,
  Pressable,
  ScrollView,
  KeyboardAvoidingView,
  Platform,
  StyleSheet,
} from 'react-native';
import { SafeAreaView, useSafeAreaInsets } from 'react-native-safe-area-context';
import { useFormStyles } from '../../hooks/useFormStyles';
import { useLayoutProfile } from '../../hooks/useLayoutProfile';
import { useTheme } from '../../context/ThemeContext';
import ScreenShell from '../layout/ScreenShell';
import FormErrorBanner from './FormErrorBanner';

export default function FormScreen({
  title,
  subtitle,
  onBack,
  backLabel = 'Voltar',
  children,
  footer,
  scrollable = true,
  steps,
  currentStep = 0,
  webVariant,
  fullBleed = false,
  hideHeader = false,
  noBodyPadding = false,
  narrowContent = false,
  maxContentWidth,
  error,
  onErrorAction,
}) {
  const s = useFormStyles();
  const { colors } = useTheme();
  const { contentFullWidth, isPhone, keyboardBehavior, bodyHorizontalPadding } = useLayoutProfile();
  const insets = useSafeAreaInsets();
  const shellVariant = webVariant === 'auth' ? 'auth' : 'default';
  const isAuthLayout = shellVariant === 'auth';
  const showHeader = !hideHeader;
  const useNarrowLayout = narrowContent && contentFullWidth && !isPhone;
  const hasMaxContentWidth = Number(maxContentWidth) > 0 && contentFullWidth && !isPhone;
  const bodyPaddingStyle =
    noBodyPadding
      ? styles.noBodyPadding
      : isPhone
        ? [styles.phoneBodyPadding, { paddingHorizontal: bodyHorizontalPadding }]
        : contentFullWidth
          ? [styles.fullWidthBodyPadding, { paddingHorizontal: bodyHorizontalPadding }]
          : null;

  const safeEdges = footer
    ? ['top', 'bottom', 'left', 'right']
    : ['top', 'left', 'right'];
  const webSafeTopStyle =
    Platform.OS === 'web' && isPhone
      ? { paddingTop: 'max(env(safe-area-inset-top, 0px), 10px)' }
      : null;

  const rawInnerContent = (
    <>
      <FormErrorBanner error={error} onAction={onErrorAction} />
      {children}
    </>
  );

  const innerContent = hasMaxContentWidth ? (
    <View
      style={[
        styles.maxWidthColumn,
        { maxWidth: maxContentWidth, flex: scrollable ? undefined : 1 },
      ]}
    >
      {rawInnerContent}
    </View>
  ) : (
    rawInnerContent
  );

  const content = scrollable ? (
    <ScrollView
      style={s.flex}
      contentContainerStyle={[
        s.scrollContent,
        bodyPaddingStyle,
        isAuthLayout && styles.authScrollContent,
        useNarrowLayout && styles.narrowScrollContent,
      ]}
      keyboardShouldPersistTaps="handled"
      showsVerticalScrollIndicator={false}
    >
      {useNarrowLayout ? (
        <View style={styles.narrowColumn}>{innerContent}</View>
      ) : (
        innerContent
      )}
    </ScrollView>
  ) : (
    <View style={[s.body, bodyPaddingStyle, useNarrowLayout && styles.narrowScrollContent]}>
      {useNarrowLayout ? (
        <View style={styles.narrowColumn}>{innerContent}</View>
      ) : (
        innerContent
      )}
    </View>
  );

  return (
    <SafeAreaView
      style={[
        s.safe,
        webSafeTopStyle,
        { backgroundColor: isAuthLayout ? 'transparent' : colors.background },
      ]}
      edges={safeEdges}
    >
      <ScreenShell variant={shellVariant} fullBleed={fullBleed}>
        <KeyboardAvoidingView
          style={isAuthLayout ? styles.authBody : s.flex}
          behavior={keyboardBehavior}
        >
          {showHeader ? (
            <View
              style={[
                s.header,
                isPhone ? styles.phoneHeader : contentFullWidth && styles.fullWidthHeader,
                { borderBottomColor: colors.border },
              ]}
            >
              {onBack ? (
                <Pressable onPress={onBack} hitSlop={12}>
                  <Text style={s.backLink}>← {backLabel}</Text>
                </Pressable>
              ) : null}
              <Text style={[s.title, { color: colors.text }]}>{title}</Text>
              {subtitle ? (
                <Text style={[s.subtitle, { color: colors.textMuted }]}>{subtitle}</Text>
              ) : null}
              {steps?.length > 0 ? (
                <>
                  <Text style={[s.subtitle, { marginTop: 4, color: colors.textMuted }]}>
                    Passo {currentStep + 1} de {steps.length} · {steps[currentStep]?.label}
                  </Text>
                  <View style={s.progressRow}>
                    {steps.map((item, index) => (
                      <View
                        key={item.key}
                        style={[
                          s.progressDot,
                          { backgroundColor: colors.border },
                          index <= currentStep && { backgroundColor: colors.primary },
                        ]}
                      />
                    ))}
                  </View>
                </>
              ) : null}
            </View>
          ) : null}

          {content}

          {footer ? (
            <View
              style={[
                s.footer,
                isPhone && {
                  paddingHorizontal: 16,
                  paddingBottom: Math.max(insets.bottom || 0, 14),
                },
                useNarrowLayout && styles.narrowFooter,
                { backgroundColor: colors.surface, borderTopColor: colors.border },
              ]}
            >
              {useNarrowLayout ? <View style={styles.narrowColumn}>{footer}</View> : footer}
            </View>
          ) : null}
        </KeyboardAvoidingView>
      </ScreenShell>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  authBody: {
    flex: 1,
    width: '100%',
  },
  authScrollContent: {
    flexGrow: 1,
  },
  noBodyPadding: {
    paddingHorizontal: 0,
    paddingTop: 0,
    paddingBottom: 0,
  },
  phoneBodyPadding: {
    paddingHorizontal: 16,
    paddingTop: 12,
  },
  fullWidthBodyPadding: {
    paddingHorizontal: 20,
  },
  phoneHeader: {
    paddingHorizontal: 16,
  },
  fullWidthHeader: {
    paddingHorizontal: 20,
  },
  narrowColumn: {
    width: '100%',
    maxWidth: 420,
    alignSelf: 'center',
    alignItems: 'stretch',
  },
  maxWidthColumn: {
    width: '100%',
    alignSelf: 'center',
    alignItems: 'stretch',
  },
  narrowScrollContent: {
    alignItems: 'center',
  },
  narrowFooter: {
    alignItems: 'center',
  },
});
