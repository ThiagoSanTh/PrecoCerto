import { StyleSheet, Platform } from 'react-native';
import { colors, fonts, radii, shadows } from '../../theme';

function scaleFont(size, scale) {
  return Math.round(size * scale);
}

export function createFormStyles(scale = 1) {
  return StyleSheet.create({
    safe: {
      flex: 1,
      backgroundColor: colors.background,
    },
    flex: {
      flex: 1,
    },
    header: {
      paddingHorizontal: 20,
      paddingTop: 12,
      paddingBottom: 14,
      borderBottomWidth: 1,
      borderBottomColor: colors.border,
    },
    backLink: {
      color: colors.primary,
      fontSize: scaleFont(14, scale),
      fontWeight: '600',
      fontFamily: fonts.sans,
      marginBottom: 6,
    },
    title: {
      fontSize: scaleFont(26, scale),
      fontWeight: '700',
      fontFamily: fonts.display,
      letterSpacing: -0.5,
      color: colors.text,
    },
    subtitle: {
      fontSize: scaleFont(14, scale),
      fontFamily: fonts.sans,
      color: colors.textMuted,
      marginTop: 4,
    },
    progressRow: {
      flexDirection: 'row',
      gap: 6,
      marginTop: 10,
    },
    progressDot: {
      flex: 1,
      height: 4,
      borderRadius: 2,
      backgroundColor: colors.border,
    },
    progressDotActive: {
      backgroundColor: colors.primary,
    },
    scrollContent: {
      paddingHorizontal: 20,
      paddingTop: 16,
      paddingBottom: 28,
      flexGrow: 1,
    },
    body: {
      flex: 1,
      paddingHorizontal: 20,
      paddingTop: 16,
      paddingBottom: 12,
    },
    listFlex: {
      flex: 1,
    },
    section: {
      gap: 4,
    },
    sectionHint: {
      fontSize: scaleFont(14, scale),
      fontFamily: fonts.sans,
      color: colors.textMuted,
      marginBottom: 12,
      lineHeight: scaleFont(20, scale),
    },
    field: {
      marginBottom: 12,
    },
    fieldCompact: {
      marginBottom: 0,
    },
    fieldLabel: {
      fontSize: scaleFont(12, scale),
      fontWeight: '600',
      fontFamily: fonts.sans,
      color: colors.label,
      marginBottom: 6,
    },
    input: {
      borderWidth: 1,
      borderColor: colors.inputBorder,
      borderRadius: radii.lg,
      paddingHorizontal: 14,
      paddingVertical: Platform.OS === 'ios' ? 13 : 11,
      fontSize: scaleFont(15, scale),
      fontFamily: fonts.sans,
      minHeight: 46,
      color: colors.text,
      backgroundColor: colors.inputBackground,
      ...(Platform.OS === 'web' ? { outlineStyle: 'none' } : null),
    },
    row: {
      flexDirection: 'row',
      gap: 8,
    },
    rowItemLarge: {
      flex: 2,
    },
    rowItemSmall: {
      flex: 1,
    },
    tabRow: {
      flexDirection: 'row',
      marginBottom: 14,
      gap: 6,
      backgroundColor: colors.surface,
      borderWidth: 1,
      borderColor: colors.border,
      borderRadius: radii.lg,
      padding: 4,
    },
    tabButton: {
      flex: 1,
      paddingVertical: 9,
      paddingHorizontal: 12,
      borderRadius: radii.md - 2,
      alignItems: 'center',
      justifyContent: 'center',
    },
    tabButtonActive: {
      backgroundColor: colors.ink,
    },
    tabText: {
      color: colors.textMuted,
      fontWeight: '600',
      fontFamily: fonts.sans,
      fontSize: scaleFont(13, scale),
    },
    tabTextActive: {
      color: colors.inkForeground,
    },
    summaryCard: {
      backgroundColor: colors.cardMuted,
      borderRadius: radii.xl,
      padding: 14,
      marginBottom: 12,
      borderWidth: 1,
      borderColor: colors.border,
    },
    summaryTitle: {
      fontSize: scaleFont(17, scale),
      fontWeight: '700',
      fontFamily: fonts.display,
      color: colors.text,
    },
    summaryText: {
      fontSize: scaleFont(14, scale),
      fontFamily: fonts.sans,
      color: colors.textMuted,
      marginTop: 4,
    },
    listCard: {
      backgroundColor: colors.surface,
      borderWidth: 1,
      borderColor: colors.border,
      borderRadius: radii.xl,
      padding: 16,
      marginBottom: 12,
      ...shadows.soft,
    },
    listCardTitle: {
      fontSize: scaleFont(16, scale),
      fontWeight: '700',
      fontFamily: fonts.display,
      color: colors.text,
    },
    listCardText: {
      fontSize: scaleFont(14, scale),
      fontFamily: fonts.sans,
      color: colors.textMuted,
      marginTop: 4,
    },
    linkText: {
      color: colors.primary,
      fontWeight: '600',
      fontFamily: fonts.sans,
      fontSize: scaleFont(14, scale),
    },
    emptyText: {
      fontSize: scaleFont(14, scale),
      fontFamily: fonts.sans,
      color: colors.textMuted,
      textAlign: 'center',
      marginTop: 28,
    },
    footer: {
      paddingHorizontal: 20,
      paddingVertical: 14,
      borderTopWidth: 1,
      borderTopColor: colors.border,
      backgroundColor: colors.surface,
    },
    footerRow: {
      flexDirection: 'row',
      gap: 8,
    },
    primaryButton: {
      backgroundColor: colors.primary,
      borderRadius: radii.lg,
      paddingVertical: 13,
      paddingHorizontal: 18,
      alignItems: 'center',
      minHeight: 46,
      justifyContent: 'center',
      flexDirection: 'row',
      gap: 8,
    },
    primaryButtonText: {
      color: colors.primaryForeground,
      fontSize: scaleFont(15, scale),
      fontWeight: '600',
      fontFamily: fonts.sans,
    },
    secondaryButton: {
      borderWidth: 1,
      borderColor: colors.border,
      backgroundColor: colors.surface,
      borderRadius: radii.lg,
      paddingVertical: 12,
      paddingHorizontal: 16,
      alignItems: 'center',
      marginBottom: 8,
      minHeight: 46,
      justifyContent: 'center',
      flexDirection: 'row',
      gap: 8,
    },
    secondaryButtonText: {
      color: colors.text,
      fontWeight: '600',
      fontFamily: fonts.sans,
      fontSize: scaleFont(14, scale),
    },
    dangerText: {
      color: colors.destructive,
      fontWeight: '600',
      fontFamily: fonts.sans,
      fontSize: scaleFont(14, scale),
    },
    errorBanner: {
      backgroundColor: '#FEF2F2',
      borderWidth: 1,
      borderColor: '#FECACA',
      borderRadius: radii.lg,
      padding: 14,
      marginBottom: 14,
    },
    errorBannerTitle: {
      color: '#B91C1C',
      fontWeight: '700',
      fontFamily: fonts.display,
      fontSize: scaleFont(14, scale),
      marginBottom: 4,
    },
    errorBannerText: {
      color: '#DC2626',
      fontFamily: fonts.sans,
      fontSize: scaleFont(13, scale),
      lineHeight: scaleFont(19, scale),
    },
    errorBannerAction: {
      color: colors.primaryDark,
      fontWeight: '700',
      fontFamily: fonts.sans,
      fontSize: scaleFont(13, scale),
      marginTop: 8,
    },
    coordsStatus: {
      fontSize: scaleFont(13, scale),
      fontFamily: fonts.sans,
      color: colors.primary,
      marginBottom: 12,
    },
    buttonDisabled: {
      opacity: 0.6,
    },
    cepRow: {
      flexDirection: 'row',
      alignItems: 'flex-end',
      gap: 8,
      marginBottom: 10,
    },
    cepInputWrap: {
      flex: 1,
    },
    cepButton: {
      backgroundColor: colors.primary,
      borderRadius: radii.lg,
      paddingHorizontal: 16,
      height: 46,
      justifyContent: 'center',
      alignItems: 'center',
      marginBottom: 12,
    },
    cepButtonText: {
      color: colors.primaryForeground,
      fontWeight: '600',
      fontFamily: fonts.sans,
      fontSize: scaleFont(14, scale),
    },
  });
}

/** Estilos base (escala 1.0) — compatibilidade com imports estáticos. */
export const formStyles = createFormStyles(1);
