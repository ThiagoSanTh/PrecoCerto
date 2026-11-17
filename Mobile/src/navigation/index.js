import { useCallback, useEffect, useMemo } from 'react';
import { Platform } from 'react-native';
import { DefaultTheme, NavigationContainer, createNavigationContainerRef } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { useAuth } from '../context/AuthContext';
import { useTheme } from '../context/ThemeContext';
import { linkingScreens, PUBLIC_ROUTE_NAMES } from './linking';

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
import AiChatScreen from '../screens/user/AiChatScreen';
import NotFoundScreen from '../screens/auth/NotFoundScreen';

import AppRoutes from './AppRoutes';

const Stack = createNativeStackNavigator();
const navigationRef = createNavigationContainerRef();

const linking = {
  prefixes: [typeof window !== 'undefined' ? window.location.origin : 'preco-certo://'],
  config: { screens: linkingScreens },
};

const hiddenHeader = { headerShown: false };

function withSession(Screen) {
  return function SessionScreen(props) {
    const { session, loading } = useAuth();
    if (loading || !session) return null;
    return <Screen {...props} />;
  };
}

const GuardedCreateStore = withSession(CreateStoreScreen);
const GuardedCreateProduct = withSession(CreateProductScreen);
const GuardedProductDetail = withSession(ProductDetailScreen);
const GuardedStoreCatalog = withSession(StoreCatalogScreen);
const GuardedCreateOferta = withSession(CreateOfertaScreen);
const GuardedVendedores = withSession(VendedoresScreen);
const GuardedChangePassword = withSession(ChangePasswordScreen);
const GuardedEditProfile = withSession(EditProfileScreen);
const GuardedEditEmail = withSession(EditEmailScreen);
const GuardedChat = withSession(ChatScreen);
const GuardedAiChat = withSession(AiChatScreen);
const GuardedHome = withSession(AppRoutes);

export default function Routes() {
  const { session, loading } = useAuth();
  const { colors, isDark } = useTheme();
  const navigationTheme = useMemo(() => ({
    ...DefaultTheme,
    dark: isDark,
    colors: {
      ...DefaultTheme.colors,
      primary: colors.primary,
      background: colors.background,
      card: colors.surface,
      text: colors.text,
      border: colors.border,
      notification: colors.destructive,
    },
  }), [
    isDark,
    colors.primary,
    colors.background,
    colors.surface,
    colors.text,
    colors.border,
    colors.destructive,
  ]);

  const guard = useCallback(() => {
    if (loading || !navigationRef.isReady()) return;
    const name = navigationRef.getCurrentRoute()?.name;
    if (name && !session && !PUBLIC_ROUTE_NAMES.has(name)) {
      navigationRef.reset({ index: 0, routes: [{ name: 'Login' }] });
    }
  }, [loading, session]);

  useEffect(() => {
    guard();
  }, [guard]);

  return (
    <NavigationContainer
      ref={navigationRef}
      theme={navigationTheme}
      linking={Platform.OS === 'web' ? linking : undefined}
      onReady={guard}
      onStateChange={guard}
    >
      <Stack.Navigator initialRouteName="Login">
        <Stack.Screen name="Login" component={LoginScreen} options={hiddenHeader} />
        <Stack.Screen name="Cadastro" component={RegisterScreen} options={hiddenHeader} />
        <Stack.Screen name="EsqueciSenha" component={ForgotPasswordScreen} options={hiddenHeader} />
        <Stack.Screen name="NotFound" component={NotFoundScreen} options={hiddenHeader} />
        <Stack.Screen name="CreateStore" component={GuardedCreateStore} options={hiddenHeader} />
        <Stack.Screen name="CreateProduct" component={GuardedCreateProduct} options={hiddenHeader} />
        <Stack.Screen name="ProductDetail" component={GuardedProductDetail} options={hiddenHeader} />
        <Stack.Screen name="StoreCatalog" component={GuardedStoreCatalog} options={hiddenHeader} />
        <Stack.Screen name="CreateOferta" component={GuardedCreateOferta} options={hiddenHeader} />
        <Stack.Screen name="Vendedores" component={GuardedVendedores} options={hiddenHeader} />
        <Stack.Screen name="ChangePassword" component={GuardedChangePassword} options={hiddenHeader} />
        <Stack.Screen name="EditProfile" component={GuardedEditProfile} options={hiddenHeader} />
        <Stack.Screen name="EditEmail" component={GuardedEditEmail} options={hiddenHeader} />
        <Stack.Screen name="Chat" component={GuardedChat} options={hiddenHeader} />
        <Stack.Screen name="AiChat" component={GuardedAiChat} options={hiddenHeader} />
        <Stack.Screen name="Home" component={GuardedHome} options={hiddenHeader} />
      </Stack.Navigator>
    </NavigationContainer>
  );
}
