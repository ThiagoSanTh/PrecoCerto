import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { useCallback } from 'react';
import { useFocusEffect } from '@react-navigation/native';
import ConversasScreen from '../screens/user/ConversasScreen';
import ChatScreen from '../screens/user/ChatScreen';
import { useChatBadge } from '../context/ChatBadgeContext';
import { useAuth } from '../context/AuthContext';

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

function ConversasListaGate(props) {
  const { session, loading } = useAuth();
  if (loading || !session) return null;
  return <ConversasScreen {...props} />;
}

export default function MensagensStack() {
  return (
    <>
      <MensagensFocusEffect />
      <Stack.Navigator screenOptions={{ headerShown: false }}>
        <Stack.Screen name="ConversasLista" component={ConversasListaGate} />
        <Stack.Screen name="Chat" component={ChatScreen} />
      </Stack.Navigator>
    </>
  );
}
