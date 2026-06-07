import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import BuscarStack from './buscarStack.routes';
import FavoritosScreen from '../screens/user/FavoritosScreen';
import HistoricoScreen from '../screens/user/HistoricoScreen';
import UserScreen from '../screens/user/UserScreen';

const Tab = createBottomTabNavigator();

export default function UserTabs() {
  return (
    <Tab.Navigator screenOptions={{ headerShown: false }}>
      <Tab.Screen name="Buscar" component={BuscarStack} />
      <Tab.Screen name="Favoritos" component={FavoritosScreen} />
      <Tab.Screen name="Histórico" component={HistoricoScreen} />
      <Tab.Screen name="Perfil" component={UserScreen} />
    </Tab.Navigator>
  );
}
