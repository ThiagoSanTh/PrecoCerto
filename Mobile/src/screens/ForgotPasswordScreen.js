import { Alert } from 'react-native';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { useState } from 'react';
import { FormScreen, FormField, PrimaryButton, SecondaryButton } from '../components/form';

export default function ForgotPasswordScreen({ navigation }) {
  const [email, setEmail] = useState('');

  async function handleRecover() {
    const data = await AsyncStorage.getItem('@user');

    if (!data) {
      Alert.alert('Erro', 'Nenhum usuário cadastrado');
      return;
    }

    const user = JSON.parse(data);

    if (user.email === email) {
      Alert.alert('Senha encontrada', `Sua senha é: ${user.senha}`);
    } else {
      Alert.alert('Erro', 'Email não encontrado');
    }
  }

  return (
    <FormScreen
      title="Recuperar senha"
      subtitle="Informe o e-mail da conta"
      onBack={() => navigation.goBack()}
      footer={
        <>
          <PrimaryButton label="Recuperar" onPress={handleRecover} />
          <SecondaryButton label="Voltar ao login" onPress={() => navigation.goBack()} />
        </>
      }
    >
      <FormField
        label="E-mail"
        value={email}
        onChangeText={setEmail}
        autoCapitalize="none"
        keyboardType="email-address"
      />
    </FormScreen>
  );
}
