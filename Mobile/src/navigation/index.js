import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';

import LoginScreen from '../screens/auth/LoginScreen';
import RegisterScreen from '../screens/auth/RegisterScreen';
import ForgotPasswordScreen from '../screens/auth/ForgotPasswordScreen';
import CreateStoreScreen from '../screens/store/CreateStoreScreen';
import CreateProductScreen from '../screens/store/CreateProductScreen';
import ProductDetailScreen from '../screens/catalog/ProductDetailScreen';
import StoreCatalogScreen from '../screens/catalog/StoreCatalogScreen';
import CreateOfertaScreen from '../screens/store/CreateOfertaScreen';
import VendedoresScreen from '../screens/store/VendedoresScreen';
import ChangePasswordScreen from '../screens/user/ChangePasswordScreen';
import EditProfileScreen from '../screens/user/EditProfileScreen';
import EditEmailScreen from '../screens/user/EditEmailScreen';
import ChatScreen from '../screens/user/ChatScreen';

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
        <Stack.Screen name="StoreCatalog" component={StoreCatalogScreen} options={{ headerShown: false }} />
        <Stack.Screen name="CreateOferta" component={CreateOfertaScreen} options={{ headerShown: false }} />
        <Stack.Screen name="Vendedores" component={VendedoresScreen} options={{ headerShown: false }} />
        <Stack.Screen name="ChangePassword" component={ChangePasswordScreen} options={{ headerShown: false }} />
        <Stack.Screen name="EditProfile" component={EditProfileScreen} options={{ headerShown: false }} />
        <Stack.Screen name="EditEmail" component={EditEmailScreen} options={{ headerShown: false }} />
        <Stack.Screen name="Chat" component={ChatScreen} options={{ headerShown: false }} />
        <Stack.Screen name="Home" component={AppRoutes} options={{ headerShown: false }} />
      </Stack.Navigator>
    </NavigationContainer>
  );
}
