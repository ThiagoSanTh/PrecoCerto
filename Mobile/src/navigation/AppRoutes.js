import { useEffect, useState } from 'react';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { useAuth } from '../context/AuthContext';
import UserTabs from './tabUser.routes';
import StoreTabs from './tabStore.routes';

export default function AppRoutes() {
  const { session, loading } = useAuth();
  const [mode, setMode] = useState('user');

  useEffect(() => {
    async function loadMode() {
      const saved = await AsyncStorage.getItem('@userMode');
      const ehLojaUser = session?.tipo === 'lojista' || session?.tipo === 'vendedor';
      if (saved) {
        setMode(saved);
      } else if (ehLojaUser) {
        setMode('store');
      } else {
        setMode('user');
      }
    }
    loadMode();
  }, [session]);

  if (loading) return null;

  const ehLojaUser = session?.tipo === 'lojista' || session?.tipo === 'vendedor';
  if (ehLojaUser || mode === 'store') {
    return <StoreTabs />;
  }

  return <UserTabs />;
}
