import { Alert } from 'react-native';
import { useState } from 'react';
import { FormScreen, FormField, PrimaryButton, SecondaryButton } from '../../components/form';

export default function ForgotPasswordScreen({ navigation }) {
  const [email, setEmail] = useState('');

  function handleRecover() {
    Alert.alert(
      'Recuperação indisponível',
      'A redefinição de senha por e-mail ainda não está disponível. Entre em contato com o suporte ou crie uma nova conta.',
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
          <PrimaryButton label="Solicitar recuperação" onPress={handleRecover} />
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
