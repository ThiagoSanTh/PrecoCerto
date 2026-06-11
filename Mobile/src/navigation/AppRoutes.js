<<<<<<< HEAD
=======
import { useEffect, useState } from 'react';
import { View } from 'react-native';
import AsyncStorage from '@react-native-async-storage/async-storage';
>>>>>>> fd252bd9 (fix: resolve conflitos de stash e corrige layout do mapa no web)
import { useAuth } from '../context/AuthContext';
import UserTabs from './tabUser.routes';
import StoreTabs from './tabStore.routes';

export default function AppRoutes() {
  const { loading, emModoLoja } = useAuth();

  if (loading) return null;

<<<<<<< HEAD
  if (emModoLoja) {
    return <StoreTabs />;
  }

  return <UserTabs />;
=======
  return (
    <View style={{ flex: 1 }}>
      {session?.tipo === 'lojista' || mode === 'store' ? (
        <StoreTabs />
      ) : (
        <UserTabs />
      )}
    </View>
  );
>>>>>>> fd252bd9 (fix: resolve conflitos de stash e corrige layout do mapa no web)
}
