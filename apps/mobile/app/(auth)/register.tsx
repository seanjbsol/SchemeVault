import { useState } from 'react';
import { KeyboardAvoidingView, Platform, ScrollView, StyleSheet, Text } from 'react-native';
import { ErrorBanner, Field, PrimaryButton, Screen } from '@/components/ui';
import { ApiError } from '@/lib/api';
import { useAuth } from '@/lib/auth';
import { colours } from '@/lib/theme';

export default function RegisterScreen() {
  const { register } = useAuth();
  const [organisationName, setOrganisationName] = useState('');
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function onSubmit() {
    setError(null);
    if (!organisationName.trim() || !fullName.trim() || !email.trim() || password.length < 8) {
      setError('Enter organisation name, your name, email, and a password of at least 8 characters.');
      return;
    }
    setLoading(true);
    try {
      await register({ organisationName, fullName, email, password });
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not create the organisation.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <Screen>
      <KeyboardAvoidingView behavior={Platform.OS === 'ios' ? 'padding' : undefined} style={{ flex: 1 }}>
        <ScrollView contentContainerStyle={styles.content} keyboardShouldPersistTaps="handled">
          <Text style={styles.heading}>Set up your vault</Text>
          <Text style={styles.lede}>
            Your organisation becomes a tenant. You are the Owner. Scheme catalogues are shared; evidence and
            renewals are not.
          </Text>
          <ErrorBanner message={error} />
          <Field
            label="Organisation name"
            value={organisationName}
            onChangeText={setOrganisationName}
            autoCapitalize="words"
            placeholder="e.g. Humber Civils Ltd"
          />
          <Field label="Your name" value={fullName} onChangeText={setFullName} autoCapitalize="words" />
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
            autoComplete="password-new"
          />
          <PrimaryButton title="Create organisation" onPress={onSubmit} loading={loading} />
        </ScrollView>
      </KeyboardAvoidingView>
    </Screen>
  );
}

const styles = StyleSheet.create({
  content: { padding: 20, paddingBottom: 40 },
  heading: { fontSize: 24, fontWeight: '800', color: colours.navy, marginBottom: 8 },
  lede: { color: colours.muted, marginBottom: 20, lineHeight: 20 },
});
