import { Link, Stack } from 'expo-router';
import { StyleSheet, Text, View } from 'react-native';
import { colours } from '@/lib/theme';

export default function NotFoundScreen() {
  return (
    <>
      <Stack.Screen options={{ title: 'Not found' }} />
      <View style={styles.container}>
        <Text style={styles.title}>That screen is not in SchemeVault.</Text>
        <Link href="/" style={styles.link}>
          <Text style={styles.linkText}>Back to home</Text>
        </Link>
      </View>
    </>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, alignItems: 'center', justifyContent: 'center', padding: 20, backgroundColor: colours.paper },
  title: { fontSize: 18, fontWeight: '700', color: colours.navy, textAlign: 'center' },
  link: { marginTop: 16 },
  linkText: { color: colours.navyMid, fontWeight: '700' },
});
