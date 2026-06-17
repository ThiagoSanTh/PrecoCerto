import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { useCallback } from 'react';
import { useFocusEffect } from '@react-navigation/native';
import ConversasScreen from '../screens/user/ConversasScreen';
import ChatScreen from '../screens/user/ChatScreen';
import { useChatBadge } from '../context/ChatBadgeContext';

const Stack = createNativeStackNavigator();

function MensagensFocusEffect() {
  const { setMensagensTabAtiva } = useChatBadge();

  useFocusEffect(
    useCallback(() => {
      setMensagensTabAtiva(true);
      return () => setMensagensTabAtiva(false);
    }, [setMensagensTabAtiva])
  );

  return null;
}

export default function MensagensStack() {
  return (
    <>
      <MensagensFocusEffect />
      <Stack.Navigator screenOptions={{ headerShown: false }}>
        <Stack.Screen name="ConversasLista" component={ConversasScreen} />
        <Stack.Screen name="Chat" component={ChatScreen} />
      </Stack.Navigator>
    </>
  );
}
