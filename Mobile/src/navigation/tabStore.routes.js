import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { useEffect, useState } from 'react';
import ProductsScreen from '../screens/store/ProductsScreen';
import StoreScreen from '../screens/store/StoreScreen';
import MensagensStack from './mensagensStack.routes';
import ProfileScreen from '../screens/user/ProfileScreen';
import CustomTabBar from '../components/CustomTabBar';
import { contarNaoLidas } from '../services/chatService';

const Tab = createBottomTabNavigator();

export default function StoreTabs() {
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
      <Tab.Screen name="Produtos" component={ProductsScreen} />
      <Tab.Screen name="Loja" component={StoreScreen} />
      <Tab.Screen name="Mensagens" component={MensagensStack} options={{ tabBarLabel: 'Chat' }} />
      <Tab.Screen name="Conta" component={ProfileScreen} />
    </Tab.Navigator>
  );
}
