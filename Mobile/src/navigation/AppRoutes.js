import { View } from 'react-native';
import { useAuth } from '../context/AuthContext';
import UserTabs from './tabUser.routes';
import StoreTabs from './tabStore.routes';

export default function AppRoutes() {
  const { loading, emModoLoja } = useAuth();

  if (loading) return null;

  return (
    <View style={{ flex: 1 }}>
      {emModoLoja ? <StoreTabs /> : <UserTabs />}
    </View>
  );
}
