import { useAuth } from '../../context/AuthContext';
import { PrimaryButton } from '../../components/form';
import AuthSplitLayout from '../../components/ui/AuthSplitLayout';

export default function NotFoundScreen({ navigation }) {
  const { session } = useAuth();

  return (
    <AuthSplitLayout
      title="Página não encontrada"
      subtitle="Esse endereço não existe no Preço Certo."
    >
      <PrimaryButton
        label="Voltar ao início"
        onPress={() =>
          navigation.reset({
            index: 0,
            routes: [{ name: session ? 'Home' : 'Login' }],
          })
        }
      />
    </AuthSplitLayout>
  );
}
