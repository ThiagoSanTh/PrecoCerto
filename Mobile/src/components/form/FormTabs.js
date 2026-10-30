import { View, Text, Pressable } from 'react-native';
import { useFormStyles } from '../../hooks/useFormStyles';
import { useTheme } from '../../context/ThemeContext';

export default function FormTabs({ options, value, onChange }) {
  const s = useFormStyles();
  const { colors } = useTheme();

  return (
    <View style={[s.tabRow, { backgroundColor: colors.surface, borderColor: colors.border }]}>
      {options.map((opt) => {
        const active = value === opt.value;
        return (
          <Pressable
            key={opt.value}
            accessibilityRole="tab"
            accessibilityState={{ selected: active }}
            style={[
              s.tabButton,
              active && { backgroundColor: colors.ink },
            ]}
            onPress={() => onChange(opt.value)}
          >
            <Text
              style={[
                s.tabText,
                { color: active ? colors.inkForeground : colors.textMuted },
              ]}
            >
              {opt.label}
            </Text>
          </Pressable>
        );
      })}
    </View>
  );
}
