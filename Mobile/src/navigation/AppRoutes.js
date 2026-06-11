import { useAuth } from '../context/AuthContext';
import UserTabs from './tabUser.routes';
import StoreTabs from './tabStore.routes';

export default function AppRoutes() {
  const { loading, emModoLoja } = useAuth();

  if (loading) return null;

  if (emModoLoja) {
    return <StoreTabs />;
  }

  return <UserTabs />;
}
