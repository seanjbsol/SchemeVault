import { Stack } from 'expo-router';
import { colours } from '@/lib/theme';

export default function WorkStack() {
  return (
    <Stack
      screenOptions={{
        headerStyle: { backgroundColor: colours.navy },
        headerTintColor: colours.white,
        headerTitleStyle: { fontWeight: '700' },
      }}>
      <Stack.Screen name="index" options={{ title: 'H&S tools' }} />
      <Stack.Screen name="questionnaires/index" options={{ title: 'Questionnaires' }} />
      <Stack.Screen name="questionnaires/[code]" options={{ title: 'Questionnaire' }} />
      <Stack.Screen name="questionnaires/response/[id]" options={{ title: 'Generated pack' }} />
      <Stack.Screen name="accidents/index" options={{ title: 'Accidents' }} />
      <Stack.Screen name="accidents/add" options={{ title: 'Log incident' }} />
      <Stack.Screen name="accidents/[id]" options={{ title: 'Incident' }} />
      <Stack.Screen name="equipment/index" options={{ title: 'Equipment' }} />
      <Stack.Screen name="equipment/add" options={{ title: 'Add equipment' }} />
      <Stack.Screen name="equipment/[id]" options={{ title: 'Equipment' }} />
    </Stack>
  );
}
