import { View, Text, StyleSheet } from 'react-native';
import { useAuth } from '../../context/AuthContext';
import { useTheme } from '../../context/ThemeContext';
import { podeCriarLoja } from '../../utils/modoUsuario';
import { FormScreen, PrimaryButton, SecondaryButton, formStyles } from '../../components/form';

export default function ProfileScreen({ navigation }) {
  const { session, logout, temModoLoja, emModoLoja, setAppMode } = useAuth();
  const { colors: tema, isDark, alternarTema } = useTheme();
  const perfil = session?.perfil;

  async function handleLogout() {
    await logout();
    navigation.replace('Login');
  }

  return (
    <FormScreen
      title={emModoLoja ? 'Conta lojista' : 'Meu perfil'}
      subtitle="Gerencie sua conta"
      scrollable
    >
      <View
        style={{
          alignItems: 'center',
          paddingVertical: 16,
          borderBottomWidth: 1,
          borderBottomColor: tema.border,
          marginBottom: 16,
        }}
      >
        <View
          style={{
            width: 72,
            height: 72,
            borderRadius: 36,
            backgroundColor: tema.primary,
            alignItems: 'center',
            justifyContent: 'center',
            marginBottom: 8,
          }}
        >
          <Text style={{ color: '#fff', fontSize: 28, fontWeight: '700' }}>
            {(perfil?.nomeUsuario || '?').charAt(0).toUpperCase()}
          </Text>
        </View>
        <Text style={{ fontSize: 18, fontWeight: '700', color: tema.text }}>{perfil?.nomeUsuario}</Text>
        <Text style={{ color: tema.textMuted, marginTop: 4 }}>{perfil?.email}</Text>
      </View>

      <View style={styles.actionsColumn}>
        {!emModoLoja ? (
          <Text style={[formStyles.sectionHint, styles.hintCentered]}>
            Sua localização é atualizada automaticamente pelo GPS quando você busca produtos.
          </Text>
        ) : null}

        <PrimaryButton label="Editar dados pessoais" onPress={() => navigation.navigate('EditProfile')} />
        <SecondaryButton label="Alterar e-mail" onPress={() => navigation.navigate('EditEmail')} />
        <SecondaryButton label="Alterar senha" onPress={() => navigation.navigate('ChangePassword')} />

        {!emModoLoja ? (
          <PrimaryButton
            label="Assistente Preço Certo"
            onPress={() => navigation.navigate('AiChat')}
            style={{ marginTop: 8 }}
          />
        ) : null}

        {String(session?.tipo ?? '').toLowerCase() === 'admin' ? (
          <Text style={[formStyles.sectionHint, styles.hintCentered, { marginTop: 4 }]}>
            Conta admin: o assistente mostra detalhes do MotorIA (intenção, score, fallbacks).
          </Text>
        ) : null}

        {podeCriarLoja(session) ? (
          <PrimaryButton
            label="Criar loja"
            onPress={() => navigation.navigate('CreateStore')}
            style={{ marginTop: 8 }}
          />
        ) : null}

        {temModoLoja && !emModoLoja ? (
          <PrimaryButton
            label="Modo lojista"
            onPress={() => setAppMode('store')}
            style={{ marginTop: 8 }}
          />
        ) : null}

        {temModoLoja && emModoLoja ? (
          <PrimaryButton
            label="Modo cliente"
            onPress={() => setAppMode('user')}
            style={{ marginTop: 8 }}
          />
        ) : null}

        <Text style={[formStyles.summaryTitle, styles.aparenciaTitle, { color: tema.text }]}>
          Aparência
        </Text>
        <SecondaryButton label={isDark ? 'Usar tema claro' : 'Usar tema escuro'} onPress={alternarTema} />

        <SecondaryButton label="Sair" onPress={handleLogout} style={{ marginTop: 16 }} />
      </View>
    </FormScreen>
  );
}

const styles = StyleSheet.create({
  actionsColumn: {
    width: '100%',
    maxWidth: 420,
    alignSelf: 'center',
    alignItems: 'stretch',
  },
  hintCentered: {
    textAlign: 'center',
  },
  aparenciaTitle: {
    marginTop: 16,
    marginBottom: 4,
  },
});
