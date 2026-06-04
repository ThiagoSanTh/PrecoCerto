import { View, ActivityIndicator } from 'react-native';
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { useAuth } from '../context/AuthContext';

import LoginScreen from '../screens/LoginScreen';
import RegisterScreen from '../screens/RegisterScreen';
import ForgotPasswordScreen from '../screens/ForgotPasswordScreen';
import CreateStoreScreen from '../screens/CreateStoreScreen';
import CreateProductScreen from '../screens/CreateProductScreen';
import EditProductScreen from '../screens/EditProductScreen';
import CreateOfertaScreen from '../screens/CreateOfertaScreen';

import AppRoutes from './AppRoutes';

const Stack = createNativeStackNavigator();

function RootNavigator() {
  const { session, loading } = useAuth();

  if (loading) {
    return (
      <View style={{ flex: 1, justifyContent: 'center', alignItems: 'center' }}>
        <ActivityIndicator size="large" />
      </View>
    );
  }

  return (
    <Stack.Navigator screenOptions={{ headerShown: false }}>
      {session ? (
        <>
          <Stack.Screen name="Home" component={AppRoutes} />
          <Stack.Screen name="CreateStore" component={CreateStoreScreen} />
          <Stack.Screen name="CreateProduct" component={CreateProductScreen} />
          <Stack.Screen name="EditProduct" component={EditProductScreen} />
          <Stack.Screen name="CreateOferta" component={CreateOfertaScreen} />
        </>
      ) : (
        <>
          <Stack.Screen name="Login" component={LoginScreen} />
          <Stack.Screen name="Cadastro" component={RegisterScreen} />
          <Stack.Screen name="EsqueciSenha" component={ForgotPasswordScreen} />
        </>
      )}
    </Stack.Navigator>
  );
}

export default function Routes() {
  return (
    <NavigationContainer>
      <RootNavigator />
    </NavigationContainer>
  );
}
