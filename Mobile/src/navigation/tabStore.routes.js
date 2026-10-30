import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import MerchantDashboardScreen from '../screens/store/MerchantDashboardScreen';
import ProductsScreen from '../screens/store/ProductsScreen';
import StoreScreen from '../screens/store/StoreScreen';
import AiChatScreen from '../screens/user/AiChatScreen';
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
          <Tab.Screen name="Painel" component={MerchantDashboardScreen} options={{ tabBarLabel: 'Painel BI' }} />
          <Tab.Screen name="Produtos" component={ProductsScreen} options={{ tabBarLabel: 'Meus produtos' }} />
          <Tab.Screen name="Loja" component={StoreScreen} options={{ tabBarLabel: 'Minha loja' }} />
          <Tab.Screen name="IA" component={AiChatScreen} options={{ tabBarLabel: 'Assistente IA' }} />
          <Tab.Screen name="Mensagens" component={MensagensStack} options={{ tabBarLabel: 'Mensagens' }} />
          <Tab.Screen name="Conta" component={ProfileScreen} options={{ tabBarLabel: 'Perfil' }} />
        </Tab.Navigator>
      )}
    </TabShell>
  );
}
