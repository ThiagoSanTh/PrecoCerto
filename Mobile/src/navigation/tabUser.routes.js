import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { useEffect, useState } from 'react';
import BuscarStack from './buscarStack.routes';
import FavoritosScreen from '../screens/user/FavoritosScreen';
import HistoricoScreen from '../screens/user/HistoricoScreen';
import MensagensStack from './mensagensStack.routes';
import ProfileScreen from '../screens/user/ProfileScreen';
import CustomTabBar from '../components/CustomTabBar';
import { contarNaoLidas } from '../services/chatService';

const Tab = createBottomTabNavigator();

export default function UserTabs() {
  const [badge, setBadge] = useState(0);

  useEffect(() => {
    let ativo = true;
    async function carregar() {
      try {
        const total = await contarNaoLidas();
        if (ativo) setBadge(total);
      } catch {
        if (ativo) setBadge(0);
      }
    }
    carregar();
    const interval = setInterval(carregar, 15000);
    return () => {
      ativo = false;
      clearInterval(interval);
    };
  }, []);

  return (
    <Tab.Navigator
      tabBar={(props) => <CustomTabBar {...props} badgeCount={badge} />}
      screenOptions={{ headerShown: false }}
    >
      <Tab.Screen name="Buscar" component={BuscarStack} options={{ tabBarLabel: 'Buscar' }} />
      <Tab.Screen name="Favoritos" component={FavoritosScreen} />
      <Tab.Screen name="Histórico" component={HistoricoScreen} />
      <Tab.Screen name="Mensagens" component={MensagensStack} />
      <Tab.Screen name="Perfil" component={ProfileScreen} />
    </Tab.Navigator>
  );
}
