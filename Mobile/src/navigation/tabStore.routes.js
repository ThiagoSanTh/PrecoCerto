import { View } from 'react-native';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { useEffect, useState } from 'react';
import ProductsScreen from '../screens/store/ProductsScreen';
import StoreScreen from '../screens/store/StoreScreen';
import MensagensStack from './mensagensStack.routes';
import ProfileScreen from '../screens/user/ProfileScreen';
import CustomTabBar from '../components/CustomTabBar';
import { contarNaoLidas } from '../services/chatService';
import { useLayoutProfile } from '../hooks/useLayoutProfile';

const Tab = createBottomTabNavigator();

function desktopTabBarStyle(sidebarWidth) {
  return {
    position: 'absolute',
    left: 0,
    top: 0,
    bottom: 0,
    width: sidebarWidth,
    height: '100%',
    borderTopWidth: 0,
    borderRightWidth: 1,
    elevation: 0,
    zIndex: 10,
  };
}

function desktopSceneStyle(sidebarWidth) {
  return {
    flex: 1,
    marginLeft: sidebarWidth,
    minHeight: 0,
  };
}

export default function StoreTabs() {
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
    <View style={{ flex: 1, minHeight: 0 }}>
      <Tab.Navigator
        tabBar={(props) => <CustomTabBar {...props} badgeCount={badge} />}
        screenOptions={{
          headerShown: false,
          sceneStyle: isDesktopWeb ? desktopSceneStyle(sidebarWidth) : { flex: 1 },
          tabBarStyle: isDesktopWeb ? desktopTabBarStyle(sidebarWidth) : undefined,
        }}
      >
        <Tab.Screen name="Produtos" component={ProductsScreen} />
        <Tab.Screen name="Loja" component={StoreScreen} />
        <Tab.Screen name="Mensagens" component={MensagensStack} options={{ tabBarLabel: 'Chat' }} />
        <Tab.Screen name="Conta" component={ProfileScreen} />
      </Tab.Navigator>
    </View>
  );
}
