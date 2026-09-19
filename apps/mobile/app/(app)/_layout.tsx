import { Redirect, Tabs } from 'expo-router';
import { Text } from 'react-native';
import { useAuth } from '@/lib/auth';
import { colours } from '@/lib/theme';

function TabGlyph({ glyph, focused }: { glyph: string; focused: boolean }) {
  return <Text style={{ color: focused ? colours.amber : '#C5D0DC', fontSize: 16 }}>{glyph}</Text>;
}

export default function AppLayout() {
  const { token } = useAuth();
  if (!token) {
    return <Redirect href="/(auth)/login" />;
  }

  return (
    <Tabs
      screenOptions={{
        headerStyle: { backgroundColor: colours.navy },
        headerTintColor: colours.white,
        headerTitleStyle: { fontWeight: '700' },
        tabBarActiveTintColor: colours.amber,
        tabBarInactiveTintColor: '#C5D0DC',
        tabBarStyle: { backgroundColor: colours.navy, borderTopColor: colours.navyMid },
        tabBarLabelStyle: { fontSize: 11, fontWeight: '700' },
      }}>
      <Tabs.Screen name="index" options={{ title: 'Home', tabBarIcon: ({ focused }) => <TabGlyph glyph="●" focused={focused} /> }} />
      <Tabs.Screen
        name="schemes"
        options={{
          title: 'Schemes',
          headerShown: false,
          tabBarIcon: ({ focused }) => <TabGlyph glyph="▣" focused={focused} />,
        }}
      />
      <Tabs.Screen
        name="evidence"
        options={{
          title: 'Vault',
          headerShown: false,
          tabBarIcon: ({ focused }) => <TabGlyph glyph="▤" focused={focused} />,
        }}
      />
      <Tabs.Screen name="renewals" options={{ title: 'Renewals', tabBarIcon: ({ focused }) => <TabGlyph glyph="↻" focused={focused} /> }} />
      <Tabs.Screen
        name="work"
        options={{
          title: 'H&S',
          headerShown: false,
          tabBarIcon: ({ focused }) => <TabGlyph glyph="◆" focused={focused} />,
        }}
      />
      <Tabs.Screen name="settings" options={{ title: 'Settings', tabBarIcon: ({ focused }) => <TabGlyph glyph="⚙" focused={focused} /> }} />
    </Tabs>
  );
}
