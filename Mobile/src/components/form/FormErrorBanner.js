import { View, Text, Pressable } from 'react-native';
import { useFormStyles } from '../../hooks/useFormStyles';

export default function FormErrorBanner({ error, onAction }) {
  const s = useFormStyles();

  if (!error) return null;

  const title = typeof error === 'string' ? 'Erro' : error.title || 'Erro';
  const message = typeof error === 'string' ? error : error.message;
  const actionLabel = typeof error === 'object' ? error.actionLabel : null;
  const isSuccess = typeof error === 'object' && error.variant === 'success';

  if (!message) return null;

  return (
    <View
      style={[
        s.errorBanner,
        isSuccess && {
          backgroundColor: '#F0FDF4',
          borderColor: '#BBF7D0',
        },
      ]}
      accessibilityRole="alert"
    >
      {title ? (
        <Text style={[s.errorBannerTitle, isSuccess && { color: '#15803D' }]}>{title}</Text>
      ) : null}
      <Text style={[s.errorBannerText, isSuccess && { color: '#16A34A' }]}>{message}</Text>
      {actionLabel && onAction ? (
        <Pressable onPress={onAction}>
          <Text style={s.errorBannerAction}>{actionLabel}</Text>
        </Pressable>
      ) : null}
    </View>
  );
}
