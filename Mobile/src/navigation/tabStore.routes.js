import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import ProductsScreen from '../screens/store/ProductsScreen';
import StoreScreen from '../screens/store/StoreScreen';
import MensagensStack from './mensagensStack.routes';
import ProfileScreen from '../screens/user/ProfileScreen';
import TabShell from './TabShell';
import { useChatBadge } from '../context/ChatBadgeContext';

const Tab = createBottomTabNavigator();

export default function StoreTabs() {
  const { badgeCount } = useChatBadge();

  return (
    <TabShell>
      {(renderTabBar) => (
        <Tab.Navigator
          tabBar={(props) => renderTabBar({ ...props, badgeCount })}
          screenOptions={{ headerShown: false }}
        >
          <Tab.Screen name="Produtos" component={ProductsScreen} />
          <Tab.Screen name="Loja" component={StoreScreen} />
          <Tab.Screen name="Mensagens" component={MensagensStack} options={{ tabBarLabel: 'Chat' }} />
          <Tab.Screen name="Conta" component={ProfileScreen} />
        </Tab.Navigator>
      )}
    </TabShell>
  );
}
