import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { Ionicons } from '@expo/vector-icons';
import ProductsScreen from '../screens/store/ProductsScreen';
import StoreScreen from '../screens/store/StoreScreen';
import UserScreen from '../screens/user/UserScreen';
import { colors } from '../theme';

const Tab = createBottomTabNavigator();

const ICONS = {
  Produtos: 'pricetags',
  Loja: 'storefront',
  Conta: 'person',
};

export default function StoreTabs() {
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
      <Tab.Screen name="Produtos" component={ProductsScreen} />
      <Tab.Screen name="Loja" component={StoreScreen} />
      <Tab.Screen name="Conta" component={UserScreen} />
    </Tab.Navigator>
  );
}
