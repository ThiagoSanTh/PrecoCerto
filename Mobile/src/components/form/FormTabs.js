import { View, Text, Pressable } from 'react-native';
import { useFormStyles } from '../../hooks/useFormStyles';
import { useTheme } from '../../context/ThemeContext';

export default function FormTabs({ options, value, onChange }) {
  const s = useFormStyles();
  const { colors } = useTheme();

  return (
    <View style={s.tabRow}>
      {options.map((opt) => {
        const active = value === opt.value;
        return (
          <Pressable
            key={opt.value}
            style={[
              s.tabButton,
              { borderColor: colors.primary },
              active && { backgroundColor: colors.primary },
            ]}
            onPress={() => onChange(opt.value)}
          >
            <Text style={[s.tabText, { color: colors.primary }, active && s.tabTextActive]}>
              {opt.label}
            </Text>
          </Pressable>
        );
      })}
    </View>
  );
}
