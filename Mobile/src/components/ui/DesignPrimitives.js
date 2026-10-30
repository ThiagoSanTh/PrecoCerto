import { View, Text, Pressable, StyleSheet, Platform } from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useTheme } from '../../context/ThemeContext';
import { fonts, radii, shadows } from '../../theme';

export function BrandLogo({ dark = true, subtitle = 'Saquarema · RJ' }) {
  const { colors } = useTheme();
  return (
    <View style={styles.logoRow}>
      <View
        style={[
          styles.logoIconBox,
          { backgroundColor: colors.highlight },
        ]}
      >
        <Ionicons name="location" size={19} color={colors.highlightForeground} />
      </View>
      <View>
        <Text
          style={[
            styles.logoTitle,
            { color: dark ? colors.sidebarAccentForeground : colors.text },
          ]}
        >
          Preço Certo
        </Text>
        <Text
          style={[
            styles.logoSubtitle,
            { color: dark ? 'rgba(201, 216, 219, 0.65)' : colors.textMuted },
          ]}
        >
          {subtitle}
        </Text>
      </View>
    </View>
  );
}

export function PageHeader({ eyebrow, title, subtitle, action, style }) {
  const { colors } = useTheme();
  return (
    <View style={[styles.headerRow, style]}>
      <View style={styles.headerTextCol}>
        {eyebrow ? (
          <Text style={[styles.eyebrow, { color: colors.primary }]}>{eyebrow}</Text>
        ) : null}
        <Text style={[styles.pageTitle, { color: colors.text }]}>{title}</Text>
        {subtitle ? (
          <Text style={[styles.pageSubtitle, { color: colors.textMuted }]}>{subtitle}</Text>
        ) : null}
      </View>
      {action ? <View style={styles.headerAction}>{action}</View> : null}
    </View>
  );
}

export function SurfaceCard({ children, style, padded = true }) {
  const { colors } = useTheme();
  return (
    <View
      style={[
        styles.surfaceCard,
        padded && styles.surfaceCardPadded,
        {
          backgroundColor: colors.card,
          borderColor: colors.border,
        },
        style,
      ]}
    >
      {children}
    </View>
  );
}

export function SegmentedControl({ options = [], value, onChange, style }) {
  const { colors } = useTheme();
  return (
    <View
      style={[
        styles.segmentedContainer,
        {
          backgroundColor: colors.card,
          borderColor: colors.border,
        },
        style,
      ]}
    >
      {options.map((opt) => {
        const optValue = typeof opt === 'object' ? opt.value : opt;
        const optLabel = typeof opt === 'object' ? opt.label : opt;
        const active = value === optValue;
        return (
          <Pressable
            key={String(optValue)}
            accessibilityRole="button"
            accessibilityState={{ selected: active }}
            onPress={() => onChange?.(optValue)}
            style={[
              styles.segmentedBtn,
              active && { backgroundColor: colors.ink },
            ]}
          >
            <Text
              style={[
                styles.segmentedText,
                { color: active ? colors.inkForeground : colors.textMuted },
              ]}
            >
              {optLabel}
            </Text>
          </Pressable>
        );
      })}
    </View>
  );
}

export function PillBadge({ label, variant = 'highlight', style, textStyle }) {
  const { colors } = useTheme();

  let bg = colors.highlight;
  let fg = colors.highlightForeground;
  if (variant === 'soft') {
    bg = colors.primarySoft;
    fg = colors.accentForeground;
  } else if (variant === 'muted') {
    bg = colors.cardMuted;
    fg = colors.textMuted;
  } else if (variant === 'success') {
    bg = colors.primarySoft;
    fg = colors.success;
  } else if (variant === 'destructive') {
    bg = '#FEE2E2';
    fg = colors.destructive;
  }

  return (
    <View style={[styles.pillBadge, { backgroundColor: bg }, style]}>
      <Text style={[styles.pillBadgeText, { color: fg }, textStyle]}>{label}</Text>
    </View>
  );
}

export function AiBannerCard({ children, icon = 'sparkles', style }) {
  const { colors } = useTheme();
  return (
    <View
      style={[
        styles.aiBanner,
        { backgroundColor: colors.ink },
        Platform.OS === 'web'
          ? {
              backgroundImage: 'linear-gradient(135deg, #0F1D22 0%, #103A35 100%)',
            }
          : null,
        style,
      ]}
    >
      <View
        style={[
          styles.aiIconBox,
          { backgroundColor: colors.highlight },
        ]}
      >
        <Ionicons name={icon} size={18} color={colors.highlightForeground} />
      </View>
      <View style={styles.aiContent}>{children}</View>
    </View>
  );
}

const styles = StyleSheet.create({
  logoRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 10,
  },
  logoIconBox: {
    width: 36,
    height: 36,
    borderRadius: radii.md,
    alignItems: 'center',
    justifyContent: 'center',
  },
  logoTitle: {
    fontFamily: fonts.display,
    fontSize: 16,
    fontWeight: '700',
    letterSpacing: -0.3,
  },
  logoSubtitle: {
    fontFamily: fonts.sans,
    fontSize: 11,
    marginTop: 1,
  },
  headerRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    alignItems: 'flex-end',
    justifyContent: 'space-between',
    gap: 12,
  },
  headerTextCol: {
    flexShrink: 1,
  },
  eyebrow: {
    fontFamily: fonts.sans,
    fontSize: 11,
    fontWeight: '700',
    textTransform: 'uppercase',
    letterSpacing: 1.4,
  },
  pageTitle: {
    fontFamily: fonts.display,
    fontSize: 26,
    fontWeight: '700',
    letterSpacing: -0.5,
    marginTop: 3,
  },
  pageSubtitle: {
    fontFamily: fonts.sans,
    fontSize: 13,
    marginTop: 4,
  },
  headerAction: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  surfaceCard: {
    borderWidth: 1,
    borderRadius: radii.xl,
    ...shadows.soft,
  },
  surfaceCardPadded: {
    padding: 18,
  },
  segmentedContainer: {
    flexDirection: 'row',
    borderWidth: 1,
    borderRadius: radii.md,
    padding: 4,
    gap: 4,
  },
  segmentedBtn: {
    borderRadius: radii.sm - 2,
    paddingHorizontal: 12,
    paddingVertical: 6,
    alignItems: 'center',
    justifyContent: 'center',
  },
  segmentedText: {
    fontFamily: fonts.sans,
    fontSize: 12,
    fontWeight: '700',
  },
  pillBadge: {
    borderRadius: 6,
    paddingHorizontal: 7,
    paddingVertical: 3,
    alignSelf: 'flex-start',
  },
  pillBadgeText: {
    fontFamily: fonts.sans,
    fontSize: 10,
    fontWeight: '800',
    letterSpacing: 0.3,
  },
  aiBanner: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    borderRadius: radii.xl,
    padding: 15,
  },
  aiIconBox: {
    width: 38,
    height: 38,
    borderRadius: radii.md,
    alignItems: 'center',
    justifyContent: 'center',
  },
  aiContent: {
    flex: 1,
  },
});
