import { createNativeStackNavigator } from '@react-navigation/native-stack';
import ConversasScreen from '../screens/user/ConversasScreen';
import ChatScreen from '../screens/user/ChatScreen';

const Stack = createNativeStackNavigator();

export default function MensagensStack() {
  return (
    <Stack.Navigator screenOptions={{ headerShown: false }}>
      <Stack.Screen name="ConversasLista" component={ConversasScreen} />
      <Stack.Screen name="Chat" component={ChatScreen} />
    </Stack.Navigator>
  );
}
