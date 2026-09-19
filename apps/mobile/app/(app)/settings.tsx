import { useState } from 'react';
import { ScrollView, StyleSheet, Text } from 'react-native';
import { Card, ErrorBanner, Field, GhostButton, PrimaryButton, Screen } from '@/components/ui';
import { apiBaseUrl, ApiError } from '@/lib/api';
import { endpoints } from '@/lib/endpoints';
import { useAuth } from '@/lib/auth';
import { colours } from '@/lib/theme';

export default function SettingsScreen() {
  const { user, signOut, setUser } = useAuth();
  const [name, setName] = useState(user?.tenantName ?? '');
  const [error, setError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);
  const [saving, setSaving] = useState(false);
  const canRename = user?.role === 'Owner' || user?.role === 'Admin';

  async function save() {
    setError(null);
    setSaved(false);
    if (!canRename) {
      setError('Only an Owner or Admin can rename the organisation.');
      return;
    }
    setSaving(true);
    try {
      const tenant = await endpoints.updateTenant(name.trim());
      if (user) {
        setUser({ ...user, tenantName: tenant.name });
      }
      setSaved(true);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not update the organisation.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <Screen>
      <ScrollView contentContainerStyle={styles.content}>
        <Text style={styles.lede}>
          You are signed in as {user?.fullName} ({user?.role}) for this organisation only. The JWT carries tenant_id
          and role; the API never returns another tenant's vault.
        </Text>
        <ErrorBanner message={error} />
        {saved ? <Text style={styles.ok}>Organisation name saved.</Text> : null}
        <Card>
          <Field label="Organisation name" value={name} onChangeText={setName} autoCapitalize="words" editable={canRename} />
          {canRename ? <PrimaryButton title="Save name" onPress={save} loading={saving} /> : null}
        </Card>
        <Card>
          <Text style={styles.rowLabel}>Email</Text>
          <Text style={styles.rowValue}>{user?.email}</Text>
          <Text style={styles.rowLabel}>Role</Text>
          <Text style={styles.rowValue}>{user?.role}</Text>
          <Text style={styles.rowLabel}>API</Text>
          <Text style={styles.rowValue}>{apiBaseUrl}</Text>
        </Card>
        <GhostButton title="Sign out" onPress={() => void signOut()} />
      </ScrollView>
    </Screen>
  );
}

const styles = StyleSheet.create({
  content: { padding: 16, paddingBottom: 40 },
  lede: { color: colours.muted, marginBottom: 16, lineHeight: 20 },
  ok: { color: colours.green, fontWeight: '700', marginBottom: 12 },
  rowLabel: { color: colours.muted, fontSize: 12, fontWeight: '700', marginTop: 8 },
  rowValue: { color: colours.navy, fontSize: 15, fontWeight: '600' },
});
