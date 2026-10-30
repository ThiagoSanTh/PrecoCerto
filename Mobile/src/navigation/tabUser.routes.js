import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import BuscarStack from './buscarStack.routes';
import AiChatScreen from '../screens/user/AiChatScreen';
import FavoritosScreen from '../screens/user/FavoritosScreen';
import HistoricoScreen from '../screens/user/HistoricoScreen';
import MensagensStack from './mensagensStack.routes';
import ProfileScreen from '../screens/user/ProfileScreen';
import TabShell from './TabShell';
import { useChatBadge } from '../context/ChatBadgeContext';

const Tab = createBottomTabNavigator();

export default function UserTabs() {
  const { badgeCount } = useChatBadge();

  return (
    <TabShell>
      {(renderTabBar) => (
        <Tab.Navigator
          tabBar={(props) => renderTabBar({ ...props, badgeCount })}
          screenOptions={{ headerShown: false }}
        >
          <Tab.Screen name="Buscar" component={BuscarStack} options={{ tabBarLabel: 'Buscar' }} />
          <Tab.Screen name="IA" component={AiChatScreen} options={{ tabBarLabel: 'Assistente IA' }} />
          <Tab.Screen name="Favoritos" component={FavoritosScreen} options={{ tabBarLabel: 'Favoritos' }} />
          <Tab.Screen name="Histórico" component={HistoricoScreen} options={{ tabBarLabel: 'Histórico' }} />
          <Tab.Screen name="Mensagens" component={MensagensStack} options={{ tabBarLabel: 'Mensagens' }} />
          <Tab.Screen name="Perfil" component={ProfileScreen} options={{ tabBarLabel: 'Perfil' }} />
        </Tab.Navigator>
      )}
    </TabShell>
  );
}
