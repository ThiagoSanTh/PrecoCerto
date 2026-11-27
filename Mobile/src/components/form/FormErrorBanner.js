import { View, Text, Pressable } from 'react-native';
import { useFormStyles } from '../../hooks/useFormStyles';
import { useTheme } from '../../context/ThemeContext';

export default function FormErrorBanner({ error, onAction }) {
  const s = useFormStyles();
  const { colors } = useTheme();

  if (!error) return null;

  const title = typeof error === 'string' ? 'Erro' : error.title || 'Erro';
  const message = typeof error === 'string' ? error : error.message;
  const actionLabel = typeof error === 'object' ? error.actionLabel : null;
  const variant = typeof error === 'object' ? error.variant : 'error';
  const isSuccess = variant === 'success';
  const isInfo = variant === 'info';

  if (!message) return null;

  const bannerColors = isInfo
    ? { bg: colors.primarySoft, border: colors.primary, title: colors.accentForeground, text: colors.accentForeground }
    : isSuccess
      ? { bg: colors.successSoft, border: colors.success, title: colors.success, text: colors.success }
      : null;

  return (
    <View
      style={[
        s.errorBanner,
        bannerColors && {
          backgroundColor: bannerColors.bg,
          borderColor: bannerColors.border,
        },
      ]}
      accessibilityRole="alert"
    >
      {title ? (
        <Text style={[s.errorBannerTitle, bannerColors && { color: bannerColors.title }]}>
          {title}
        </Text>
      ) : null}
      <Text style={[s.errorBannerText, bannerColors && { color: bannerColors.text }]}>
        {message}
      </Text>
      {actionLabel && onAction ? (
        <Pressable onPress={onAction}>
          <Text style={s.errorBannerAction}>{actionLabel}</Text>
        </Pressable>
      ) : null}
    </View>
  );
}
