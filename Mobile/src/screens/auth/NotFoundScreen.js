import { PrimaryButton } from '../../components/form';
import AuthSplitLayout from '../../components/ui/AuthSplitLayout';

export default function NotFoundScreen({ navigation }) {
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
            routes: [{ name: 'Home' }],
          })
        }
      />
    </AuthSplitLayout>
  );
}
