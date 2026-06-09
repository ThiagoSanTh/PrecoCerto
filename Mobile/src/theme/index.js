import { StyleSheet } from 'react-native';

// Paleta clara (padrão). Mantida como `colors` para compatibilidade com telas
// que importam o objeto estático diretamente.
export const lightColors = {
  primary: '#2DD4BF',
  primaryDark: '#14B8A6',
  background: '#FFFFFF',
  surface: '#FFFFFF',
  card: '#F1F5F9',
  text: '#0F172A',
  textMuted: '#64748B',
  border: '#E2E8F0',
};

export const darkColors = {
  primary: '#2DD4BF',
  primaryDark: '#5EEAD4',
  background: '#0F172A',
  surface: '#1E293B',
  card: '#1E293B',
  text: '#F8FAFC',
  textMuted: '#94A3B8',
  border: '#334155',
};

export const colors = lightColors;

export const styles = StyleSheet.create({
    container: {
      flex: 1,
      backgroundColor: colors.background,
      alignItems: 'center',
      justifyContent: 'center',
    },
    formTitle: {
        fontSize: 36,
        fontWeight: 'bold',
        color: colors.primary,
        margin: 10,
    },
    formInput: {
        borderColor: colors.primary,
        borderWidth: 1,
        borderRadius: 10,
        fontSize: 22,
        width: '80%',
        padding: 10,
        margin: 10,
    },
    formButton: {
        backgroundColor: colors.primary,
        width: '80%',
        margin: 10,
        padding: 10,
        borderRadius: 10,
        alignItems: 'center',
    },
    textButton: {
        color: '#fff',
        fontSize: 20,
        fontWeight: 'bold',
    },
    subContainer: {
        flexDirection: 'row',
        justifyContent: 'space-between',
        width: '80%',
    },
    subButton: {
        padding: 10,
    },
    subTextButton: {
        color: colors.primary,
        fontSize: 14
    },
    card: {
        flex: 1,
        aspectRatio: 1.6,
        margin: 6,
        backgroundColor: colors.primaryDark,
        padding: 10,
        borderRadius: 10,
      },
    screenContainer: {
      flex: 1,
      backgroundColor: colors.background,
      paddingTop: 48,
      paddingHorizontal: 16,
    },
    listCard: {
      backgroundColor: colors.primaryDark,
      padding: 14,
      borderRadius: 10,
      marginBottom: 10,
    },
    listCardTitle: {
      color: colors.text,
      fontWeight: 'bold',
      fontSize: 16,
    },
    listCardText: {
      color: colors.text,
      marginTop: 4,
      fontSize: 14,
    },
    hint: {
      color: '#64748B',
      fontSize: 13,
      marginVertical: 8,
      textAlign: 'center',
    },
    tabRow: {
      flexDirection: 'row',
      marginBottom: 16,
      gap: 8,
    },
    tabButton: {
      flex: 1,
      padding: 10,
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
    },
    tabTextActive: {
      color: '#fff',
    },
  });