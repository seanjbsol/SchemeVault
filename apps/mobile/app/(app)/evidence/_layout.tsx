import { Stack } from 'expo-router';
import { colours } from '@/lib/theme';

export default function EvidenceStack() {
  return (
    <Stack
      screenOptions={{
        headerStyle: { backgroundColor: colours.navy },
        headerTintColor: colours.white,
        headerTitleStyle: { fontWeight: '700' },
      }}>
      <Stack.Screen name="index" options={{ title: 'Evidence vault' }} />
      <Stack.Screen name="add" options={{ title: 'Add evidence' }} />
      <Stack.Screen name="[id]" options={{ title: 'Vault item' }} />
    </Stack>
  );
}
