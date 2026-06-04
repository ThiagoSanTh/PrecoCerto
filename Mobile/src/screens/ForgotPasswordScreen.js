import { Alert } from 'react-native';
import { useState } from 'react';
import { FormScreen, FormField, PrimaryButton, SecondaryButton } from '../components/form';

export default function ForgotPasswordScreen({ navigation }) {
  const [email, setEmail] = useState('');

  function handleRecover() {
    if (!email.trim()) {
      Alert.alert('Erro', 'Informe o e-mail da conta');
      return;
    }

    Alert.alert(
      'Recuperação de senha',
      'Por segurança, a senha não pode ser exibida no app. Entre em contato com o suporte ou, se já estiver logado, altere a senha nas configurações da conta.',
      [{ text: 'OK', onPress: () => navigation.goBack() }]
    );
  }

  return (
    <FormScreen
      title="Recuperar senha"
      subtitle="Informe o e-mail da conta"
      onBack={() => navigation.goBack()}
      footer={
        <>
          <PrimaryButton label="Continuar" onPress={handleRecover} />
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
