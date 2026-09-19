import { Redirect, Stack } from 'expo-router';
import { useAuth } from '@/lib/auth';
import { colours } from '@/lib/theme';

export default function AuthLayout() {
  const { token } = useAuth();
  if (token) {
    return <Redirect href="/(app)" />;
  }

  return (
    <Stack
      screenOptions={{
        headerStyle: { backgroundColor: colours.navy },
        headerTintColor: colours.white,
        headerTitleStyle: { fontWeight: '700' },
        contentStyle: { backgroundColor: colours.paper },
      }}>
      <Stack.Screen name="login" options={{ title: 'SchemeVault' }} />
      <Stack.Screen name="register" options={{ title: 'Create organisation' }} />
    </Stack>
  );
}
