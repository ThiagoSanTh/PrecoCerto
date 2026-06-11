import { View, Text } from 'react-native';
import { formStyles as s } from './formStyles';
import { useTheme } from '../../context/ThemeContext';

export default function ListCard({ title, children, style }) {
  const { colors } = useTheme();

  return (
    <View
      style={[
        s.listCard,
        { backgroundColor: colors.surface, borderColor: colors.border },
        style,
      ]}
    >
      {title ? <Text style={[s.listCardTitle, { color: colors.text }]}>{title}</Text> : null}
      {children}
    </View>
  );
}

export function ListCardText({ children, style }) {
  const { colors } = useTheme();
  return <Text style={[s.listCardText, { color: colors.textMuted }, style]}>{children}</Text>;
}
