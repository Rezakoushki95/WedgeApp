import 'react-native-gesture-handler';
import React from 'react';
import { GestureHandlerRootView } from 'react-native-gesture-handler';
import { SafeAreaProvider, initialWindowMetrics } from 'react-native-safe-area-context';
import { StatusBar } from 'expo-status-bar';
import { NavigationContainer, DefaultTheme } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { HomeScreen } from '@/screens/HomeScreen';
import { TradeScreen } from '@/screens/TradeScreen';
import { LadderScreen } from '@/screens/LadderScreen';
import type { RootStackParamList } from '@/navigation';

const Stack = createNativeStackNavigator<RootStackParamList>();

const theme = {
  ...DefaultTheme,
  colors: {
    ...DefaultTheme.colors,
    background: '#0B0E11',
    card: '#0B0E11',
    text: '#ffffff',
    border: '#1f2733',
    primary: '#1f6feb',
  },
};

export default function App() {
  return (
    <GestureHandlerRootView style={{ flex: 1 }}>
      <SafeAreaProvider initialMetrics={initialWindowMetrics}>
        <NavigationContainer theme={theme}>
          <StatusBar style="light" />
          <Stack.Navigator
            screenOptions={{
              headerStyle: { backgroundColor: '#0B0E11' },
              headerTintColor: '#fff',
              contentStyle: { backgroundColor: '#0B0E11' },
            }}
          >
            <Stack.Screen name="Home" component={HomeScreen} options={{ title: 'Wedge' }} />
            <Stack.Screen name="Ladder" component={LadderScreen} options={{ title: 'Edge Ladder' }} />
            <Stack.Screen
              name="Trade"
              component={TradeScreen}
              options={({ route }) => ({ title: route.params.journeyName })}
            />
          </Stack.Navigator>
        </NavigationContainer>
      </SafeAreaProvider>
    </GestureHandlerRootView>
  );
}
