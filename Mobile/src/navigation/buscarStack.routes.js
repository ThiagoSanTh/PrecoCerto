import { createNativeStackNavigator } from '@react-navigation/native-stack';
import SearchScreen from '../screens/catalog/SearchScreen';
import ProductDetailScreen from '../screens/catalog/ProductDetailScreen';
import StoreCatalogScreen from '../screens/catalog/StoreCatalogScreen';

const Stack = createNativeStackNavigator();

export default function BuscarStack() {
  return (
    <Stack.Navigator screenOptions={{ headerShown: false }}>
      <Stack.Screen name="Search" component={SearchScreen} />
      <Stack.Screen name="ProductDetail" component={ProductDetailScreen} />
      <Stack.Screen name="StoreCatalog" component={StoreCatalogScreen} />
    </Stack.Navigator>
  );
}
