import { createNativeStackNavigator } from '@react-navigation/native-stack';
import SearchScreen from '../screens/catalog/SearchScreen';
import ProductDetailScreen from '../screens/catalog/ProductDetailScreen';

const Stack = createNativeStackNavigator();

export default function BuscarStack() {
  return (
    <Stack.Navigator screenOptions={{ headerShown: false }}>
      <Stack.Screen name="Search" component={SearchScreen} />
      <Stack.Screen name="ProductDetail" component={ProductDetailScreen} />
    </Stack.Navigator>
  );
}
