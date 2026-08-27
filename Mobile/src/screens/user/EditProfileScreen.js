import { Alert } from 'react-native';
import { useState } from 'react';
import { useAuth } from '../../context/AuthContext';
import { atualizarCliente } from '../../services/clienteService';
import { isTelefoneValido } from '../../utils/validacaoUtils';
import { FormScreen, FormField, PrimaryButton, formStyles } from '../../components/form';

export default function EditProfileScreen({ navigation }) {
  const { session, atualizarPerfilSessao } = useAuth();
  const [nomeUsuario, setNomeUsuario] = useState(session?.perfil?.nomeUsuario || '');
  const [telefone, setTelefone] = useState(session?.perfil?.telefone || '');
  const [loading, setLoading] = useState(false);

  async function handleSalvar() {
    if (!nomeUsuario.trim()) {
      Alert.alert('Erro', 'Informe o nome de usuário.');
      return;
    }
    if (telefone.trim() && !isTelefoneValido(telefone)) {
      Alert.alert('Erro', 'Telefone inválido.');
      return;
    }

    setLoading(true);
    try {
      const atualizado = await atualizarCliente(session.perfil.id, {
        nomeUsuario: nomeUsuario.trim(),
        email: session.perfil.email,
        telefone: telefone.trim() || null,
        senha: '',
      });
      await atualizarPerfilSessao(atualizado);
      Alert.alert('Sucesso', 'Perfil atualizado', [{ text: 'OK', onPress: () => navigation.goBack() }]);
    } catch (error) {
      Alert.alert('Erro', String(error.response?.data?.message || error.message));
    } finally {
      setLoading(false);
    }
  }

  return (
    <FormScreen
      title="Editar perfil"
      onBack={() => navigation.goBack()}
      footer={<PrimaryButton label="Salvar" onPress={handleSalvar} loading={loading} />}
    >
      <FormField label="Nome de usuário" value={nomeUsuario} onChangeText={setNomeUsuario} />
      <FormField label="Telefone" value={telefone} onChangeText={setTelefone} keyboardType="phone-pad" />
    </FormScreen>
  );
}
