import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { Ionicons } from '@expo/vector-icons';
import BuscarStack from './buscarStack.routes';
import FavoritosScreen from '../screens/user/FavoritosScreen';
import HistoricoScreen from '../screens/user/HistoricoScreen';
import CartScreen from '../screens/user/CartScreen';
import UserScreen from '../screens/user/UserScreen';
import { useCarrinho } from '../context/CarrinhoContext';
import { colors } from '../theme';

const Tab = createBottomTabNavigator();

const ICONS = {
  Buscar: 'search',
  Favoritos: 'heart',
  Histórico: 'time',
  Carrinho: 'cart',
  Perfil: 'person',
};

export default function UserTabs() {
  const { quantidadeItens } = useCarrinho();

  return (
    <Tab.Navigator
      screenOptions={({ route }) => ({
        headerShown: false,
        tabBarActiveTintColor: colors.primary,
        tabBarInactiveTintColor: '#94A3B8',
        tabBarIcon: ({ color, size, focused }) => {
          const base = ICONS[route.name] || 'ellipse';
          const name = focused ? base : `${base}-outline`;
          return <Ionicons name={name} size={size} color={color} />;
        },
      })}
    >
      <Tab.Screen name="Buscar" component={BuscarStack} />
      <Tab.Screen name="Favoritos" component={FavoritosScreen} />
      <Tab.Screen name="Histórico" component={HistoricoScreen} />
      <Tab.Screen
        name="Carrinho"
        component={CartScreen}
        options={{ tabBarBadge: quantidadeItens > 0 ? quantidadeItens : undefined }}
      />
      <Tab.Screen name="Perfil" component={UserScreen} />
    </Tab.Navigator>
  );
}
