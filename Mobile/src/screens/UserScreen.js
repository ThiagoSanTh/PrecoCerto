import { View, Text, Pressable, Alert, ActivityIndicator } from 'react-native';
import { useState, useCallback } from 'react';
import { useFocusEffect } from '@react-navigation/native';
import { useAuth } from '../context/AuthContext';
import { atualizarCliente, alterarSenha } from '../services/clienteService';
import {
  FormScreen,
  FormField,
  PrimaryButton,
  SecondaryButton,
  ListCard,
  ListCardText,
  formStyles,
} from '../components/form';
import { colors } from '../style';

export default function UserScreen({ navigation }) {
  const { session, logout, sincronizarGpsCliente, isCliente, isLojista, salvarSessao } =
    useAuth();

  const [nomeUsuario, setNomeUsuario] = useState('');
  const [email, setEmail] = useState('');
  const [telefone, setTelefone] = useState('');
  const [senhaAtual, setSenhaAtual] = useState('');
  const [novaSenha, setNovaSenha] = useState('');
  const [gpsStatus, setGpsStatus] = useState('');
  const [loadingGps, setLoadingGps] = useState(false);
  const [temLoja, setTemLoja] = useState(false);

  useFocusEffect(
    useCallback(() => {
      if (session?.perfil) {
        setNomeUsuario(session.perfil.nomeUsuario || '');
        setEmail(session.perfil.email || '');
        setTelefone(session.perfil.telefone || '');
        if (session.perfil.lojaId) setTemLoja(true);
      }
      if (isCliente) verificarLojaLojista();
    }, [session])
  );

  async function verificarLojaLojista() {
    try {
      if (session?.perfil?.lojaId) setTemLoja(true);
    } catch {
      setTemLoja(false);
    }
  }

  async function handleAtualizarPerfil() {
    if (!isCliente || !session?.perfil?.id) return;

    try {
      const atualizado = await atualizarCliente(session.perfil.id, {
        nomeUsuario: nomeUsuario.trim(),
        email: email.trim(),
        telefone: telefone.trim() || null,
        senha: '',
      });
      await salvarSessao({ tipo: 'cliente', perfil: atualizado }, 'user');
      Alert.alert('Sucesso', 'Perfil atualizado');
    } catch (error) {
      Alert.alert('Erro', String(error.response?.data || error.message));
    }
  }

  async function handleAlterarSenha() {
    if (!session?.perfil?.id || !senhaAtual || !novaSenha) {
      Alert.alert('Erro', 'Preencha as senhas');
      return;
    }

    if (!isCliente) {
      Alert.alert('Info', 'Alteração de senha do lojista: use o endpoint de lojistas na API.');
      return;
    }

    try {
      await alterarSenha(session.perfil.id, senhaAtual, novaSenha);
      Alert.alert('Sucesso', 'Senha alterada');
      setSenhaAtual('');
      setNovaSenha('');
    } catch (error) {
      Alert.alert('Erro', String(error.response?.data || error.message));
    }
  }

  async function handleSincronizarGps() {
    setLoadingGps(true);
    setGpsStatus('');
    try {
      const coords = await sincronizarGpsCliente();
      if (coords) {
        setGpsStatus(
          `GPS: ${coords.latitude.toFixed(5)}, ${coords.longitude.toFixed(5)}`
        );
      } else {
        setGpsStatus('Não foi possível obter o GPS. Verifique permissões.');
      }
    } finally {
      setLoadingGps(false);
    }
  }

  async function handleLogout() {
    await logout();
    navigation.replace('Login');
  }

  async function goToUserMode() {
    await salvarSessao(session, 'user');
    navigation.replace('Home');
  }

  if (isLojista) {
    return (
      <FormScreen
        title="Perfil lojista"
        subtitle="Dados da sua conta"
        scrollable={false}
        footer={
          <>
            <PrimaryButton label="Modo cliente" onPress={goToUserMode} />
            <SecondaryButton label="Sair" onPress={handleLogout} />
          </>
        }
      >
        <ListCard title={session.perfil.nomeUsuario}>
          <ListCardText>{session.perfil.email}</ListCardText>
          <ListCardText>Loja ID: {session.perfil.lojaId || '—'}</ListCardText>
        </ListCard>
      </FormScreen>
    );
  }

  return (
    <FormScreen
      title="Meu perfil"
      subtitle="Gerencie seus dados e localização"
      scrollable
      footer={<SecondaryButton label="Sair" onPress={handleLogout} />}
    >
      <Text style={formStyles.sectionHint}>
        Localização obtida automaticamente pelo GPS — sem digitar coordenadas.
      </Text>

      <Pressable
        style={[formStyles.primaryButton, loadingGps && formStyles.buttonDisabled]}
        onPress={handleSincronizarGps}
        disabled={loadingGps}
      >
        {loadingGps ? (
          <ActivityIndicator color="#fff" />
        ) : (
          <Text style={formStyles.primaryButtonText}>Atualizar localização (GPS)</Text>
        )}
      </Pressable>

      {gpsStatus ? (
        <Text style={[formStyles.sectionHint, { color: colors.primaryDark }]}>{gpsStatus}</Text>
      ) : null}

      <FormField label="Nome de usuário" value={nomeUsuario} onChangeText={setNomeUsuario} />
      <FormField
        label="E-mail"
        value={email}
        onChangeText={setEmail}
        autoCapitalize="none"
        keyboardType="email-address"
      />
      <FormField
        label="Telefone"
        value={telefone}
        onChangeText={setTelefone}
        keyboardType="phone-pad"
      />

      <PrimaryButton label="Salvar perfil" onPress={handleAtualizarPerfil} />

      <Text style={[formStyles.summaryTitle, { marginTop: 16, marginBottom: 8 }]}>
        Alterar senha
      </Text>
      <FormField label="Senha atual" value={senhaAtual} onChangeText={setSenhaAtual} secureTextEntry />
      <FormField label="Nova senha" value={novaSenha} onChangeText={setNovaSenha} secureTextEntry />
      <SecondaryButton label="Alterar senha" onPress={handleAlterarSenha} />

      {!temLoja ? (
        <PrimaryButton
          label="Criar loja"
          onPress={() => navigation.navigate('CreateStore')}
          style={{ marginTop: 8 }}
        />
      ) : null}
    </FormScreen>
  );
}
