import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { useEffect, useState } from 'react';
import BuscarStack from './buscarStack.routes';
import FavoritosScreen from '../screens/user/FavoritosScreen';
import HistoricoScreen from '../screens/user/HistoricoScreen';
import MensagensStack from './mensagensStack.routes';
import ProfileScreen from '../screens/user/ProfileScreen';
import CustomTabBar from '../components/CustomTabBar';
import { contarNaoLidas } from '../services/chatService';
import { useLayoutProfile } from '../hooks/useLayoutProfile';

const Tab = createBottomTabNavigator();

export default function UserTabs() {
  const [badge, setBadge] = useState(0);
  const { isDesktopWeb, sidebarWidth } = useLayoutProfile();

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
      screenOptions={{
        headerShown: false,
        sceneContainerStyle: isDesktopWeb ? { paddingLeft: sidebarWidth } : undefined,
        tabBarStyle: isDesktopWeb
          ? {
              position: 'absolute',
              left: 0,
              top: 0,
              bottom: 0,
              width: sidebarWidth,
              height: '100%',
              borderTopWidth: 0,
              elevation: 0,
            }
          : undefined,
      }}
    >
      <Tab.Screen name="Buscar" component={BuscarStack} options={{ tabBarLabel: 'Buscar' }} />
      <Tab.Screen name="Favoritos" component={FavoritosScreen} />
      <Tab.Screen name="Histórico" component={HistoricoScreen} />
      <Tab.Screen name="Mensagens" component={MensagensStack} />
      <Tab.Screen name="Perfil" component={ProfileScreen} />
    </Tab.Navigator>
  );
}
