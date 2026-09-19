import { Link } from 'expo-router';
import { useState } from 'react';
import { KeyboardAvoidingView, Platform, ScrollView, StyleSheet, Text, View } from 'react-native';
import { ErrorBanner, Field, PrimaryButton, Screen } from '@/components/ui';
import { ApiError } from '@/lib/api';
import { useAuth } from '@/lib/auth';
import { colours } from '@/lib/theme';

export default function LoginScreen() {
  const { signIn } = useAuth();
  const [email, setEmail] = useState('demo@schemevault.test');
  const [password, setPassword] = useState('DemoPassw0rd!');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function onSubmit() {
    setError(null);
    setLoading(true);
    try {
      await signIn(email, password);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not sign in.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <Screen>
      <KeyboardAvoidingView behavior={Platform.OS === 'ios' ? 'padding' : undefined} style={{ flex: 1 }}>
        <ScrollView contentContainerStyle={styles.content} keyboardShouldPersistTaps="handled">
          <Text style={styles.kicker}>Multi-scheme compliance vault</Text>
          <Text style={styles.heading}>Keep CHAS, Constructionline and the rest in one place.</Text>
          <Text style={styles.lede}>
            Sign in to your organisation. Evidence, gap lists and renewal traffic lights stay inside your tenant.
          </Text>
          <ErrorBanner message={error} />
          <Field
            label="Email"
            value={email}
            onChangeText={setEmail}
            keyboardType="email-address"
            autoComplete="email"
          />
          <Field
            label="Password"
            value={password}
            onChangeText={setPassword}
            secureTextEntry
            autoComplete="password"
          />
          <PrimaryButton title="Sign in" onPress={onSubmit} loading={loading} />
          <Link href="/(auth)/register" style={styles.link}>
            <Text style={styles.linkText}>Create an organisation</Text>
          </Link>
          <View style={styles.hint}>
            <Text style={styles.hintText}>
              Demo tenant is pre-filled: Northern Plant Hire Ltd. Register a second organisation to confirm tenant
              isolation.
            </Text>
          </View>
        </ScrollView>
      </KeyboardAvoidingView>
    </Screen>
  );
}

const styles = StyleSheet.create({
  content: { padding: 20, paddingBottom: 40 },
  kicker: { color: colours.amber, fontWeight: '800', letterSpacing: 0.4, marginBottom: 8 },
  heading: { fontSize: 26, fontWeight: '800', color: colours.navy, marginBottom: 10 },
  lede: { color: colours.muted, marginBottom: 20, lineHeight: 20 },
  link: { alignSelf: 'center', marginTop: 16 },
  linkText: { color: colours.navyMid, fontWeight: '700' },
  hint: { marginTop: 24, padding: 12, backgroundColor: '#E8EEF6', borderRadius: 10 },
  hintText: { color: colours.navyMid, fontSize: 13, lineHeight: 18 },
});
