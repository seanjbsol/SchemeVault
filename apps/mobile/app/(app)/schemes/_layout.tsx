import { Stack } from 'expo-router';
import { colours } from '@/lib/theme';

export default function SchemesStack() {
  return (
    <Stack
      screenOptions={{
        headerStyle: { backgroundColor: colours.navy },
        headerTintColor: colours.white,
        headerTitleStyle: { fontWeight: '700' },
      }}>
      <Stack.Screen name="index" options={{ title: 'Schemes' }} />
      <Stack.Screen name="[id]" options={{ title: 'Scheme' }} />
    </Stack>
  );
}
