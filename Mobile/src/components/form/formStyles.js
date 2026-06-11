import { StyleSheet, Platform } from 'react-native';
import { colors } from '../../theme';

export const formStyles = StyleSheet.create({
  safe: {
    flex: 1,
    backgroundColor: colors.background,
  },
  flex: {
    flex: 1,
  },
  header: {
    paddingHorizontal: 16,
    paddingBottom: 8,
    borderBottomWidth: 1,
    borderBottomColor: '#E2E8F0',
  },
  backLink: {
    color: colors.primary,
    fontSize: 15,
    fontWeight: '600',
    marginBottom: 4,
  },
  title: {
    fontSize: 24,
    fontWeight: '700',
    color: colors.primaryDark,
  },
  subtitle: {
    fontSize: 13,
    color: '#64748B',
    marginTop: 2,
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
    backgroundColor: '#E2E8F0',
  },
  progressDotActive: {
    backgroundColor: colors.primary,
  },
  scrollContent: {
    paddingHorizontal: 16,
    paddingTop: 12,
    paddingBottom: 24,
    flexGrow: 1,
  },
  body: {
    flex: 1,
    paddingHorizontal: 16,
    paddingTop: 12,
    paddingBottom: 8,
  },
  bodyWeb: {
    minHeight: 0,
  },
  listFlex: {
    flex: 1,
  },
  section: {
    gap: 4,
  },
  sectionHint: {
    fontSize: 13,
    color: '#64748B',
    marginBottom: 8,
  },
  field: {
    marginBottom: 10,
  },
  fieldCompact: {
    marginBottom: 0,
  },
  fieldLabel: {
    fontSize: 12,
    fontWeight: '600',
    color: '#475569',
    marginBottom: 4,
  },
  input: {
    borderWidth: 1,
    borderColor: '#CBD5E1',
    borderRadius: 8,
    paddingHorizontal: 12,
    paddingVertical: Platform.OS === 'ios' ? 12 : 10,
    fontSize: 16,
    color: '#0F172A',
    backgroundColor: '#fff',
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
    marginBottom: 12,
    gap: 8,
  },
  tabButton: {
    flex: 1,
    paddingVertical: 10,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: colors.primary,
    alignItems: 'center',
  },
  tabButtonActive: {
    backgroundColor: colors.primary,
  },
  tabText: {
    color: colors.primary,
    fontWeight: '600',
    fontSize: 14,
  },
  tabTextActive: {
    color: '#fff',
  },
  summaryCard: {
    backgroundColor: '#F1F5F9',
    borderRadius: 10,
    padding: 12,
    marginBottom: 12,
  },
  summaryTitle: {
    fontSize: 16,
    fontWeight: '700',
    color: '#0F172A',
  },
  summaryText: {
    fontSize: 13,
    color: '#475569',
    marginTop: 4,
  },
  listCard: {
    backgroundColor: '#F8FAFC',
    borderWidth: 1,
    borderColor: '#E2E8F0',
    borderRadius: 10,
    padding: 12,
    marginBottom: 10,
  },
  listCardTitle: {
    fontSize: 16,
    fontWeight: '700',
    color: '#0F172A',
  },
  listCardText: {
    fontSize: 13,
    color: '#475569',
    marginTop: 4,
  },
  linkText: {
    color: colors.primary,
    fontWeight: '600',
    fontSize: 14,
  },
  emptyText: {
    fontSize: 14,
    color: '#64748B',
    textAlign: 'center',
    marginTop: 24,
  },
  footer: {
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderTopWidth: 1,
    borderTopColor: '#E2E8F0',
    backgroundColor: colors.background,
  },
  footerRow: {
    flexDirection: 'row',
    gap: 8,
  },
  primaryButton: {
    backgroundColor: colors.primary,
    borderRadius: 10,
    paddingVertical: 14,
    alignItems: 'center',
  },
  primaryButtonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: '700',
  },
  secondaryButton: {
    borderWidth: 1,
    borderColor: colors.primary,
    borderRadius: 8,
    paddingVertical: 12,
    alignItems: 'center',
    marginBottom: 8,
  },
  secondaryButtonText: {
    color: colors.primaryDark,
    fontWeight: '600',
    fontSize: 14,
  },
  dangerText: {
    color: '#EF4444',
    fontWeight: '600',
    fontSize: 14,
  },
  coordsStatus: {
    fontSize: 13,
    color: colors.primaryDark,
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
    borderRadius: 8,
    paddingHorizontal: 16,
    height: 46,
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: 10,
  },
  cepButtonText: {
    color: '#fff',
    fontWeight: '700',
    fontSize: 14,
  },
});
