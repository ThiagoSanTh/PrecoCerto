import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';

import LoginScreen from '../screens/LoginScreen';
import RegisterScreen from '../screens/RegisterScreen';
import ForgotPasswordScreen from '../screens/ForgotPasswordScreen';
import CreateStoreScreen from '../screens/CreateStoreScreen';
import CreateProductScreen from '../screens/CreateProductScreen';
import ProductDetailScreen from '../screens/ProductDetailScreen';
import CreateOfertaScreen from '../screens/CreateOfertaScreen';

import AppRoutes from './AppRoutes';

const Stack = createNativeStackNavigator();

export default function Routes() {
  return (
    <NavigationContainer>
      <Stack.Navigator initialRouteName="Login">
        <Stack.Screen name="Login" component={LoginScreen} options={{ headerShown: false }} />
        <Stack.Screen name="Cadastro" component={RegisterScreen} options={{ headerShown: false }} />
        <Stack.Screen name="EsqueciSenha" component={ForgotPasswordScreen} options={{ headerShown: false }} />
        <Stack.Screen name="CreateStore" component={CreateStoreScreen} options={{ headerShown: false }} />
        <Stack.Screen name="CreateProduct" component={CreateProductScreen} options={{ headerShown: false }} />
        <Stack.Screen name="ProductDetail" component={ProductDetailScreen} options={{ headerShown: false }} />
        <Stack.Screen name="CreateOferta" component={CreateOfertaScreen} options={{ headerShown: false }} />
        <Stack.Screen name="Home" component={AppRoutes} options={{ headerShown: false }} />
      </Stack.Navigator>
    </NavigationContainer>
  );
}
