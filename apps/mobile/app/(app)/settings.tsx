import { useCallback, useEffect, useState } from 'react';
import { Linking, ScrollView, StyleSheet, Text } from 'react-native';
import { Card, ErrorBanner, Field, GhostButton, PrimaryButton, Screen } from '@/components/ui';
import { apiBaseUrl, ApiError } from '@/lib/api';
import { endpoints } from '@/lib/endpoints';
import { useAuth } from '@/lib/auth';
import { colours } from '@/lib/theme';
import type { Entitlements } from '@/lib/types';

export default function SettingsScreen() {
  const { user, signOut, setUser } = useAuth();
  const [name, setName] = useState(user?.tenantName ?? '');
  const [error, setError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);
  const [saving, setSaving] = useState(false);
  const [entitlements, setEntitlements] = useState<Entitlements | null>(null);
  const [billingBusy, setBillingBusy] = useState(false);
  const canRename = user?.role === 'Owner' || user?.role === 'Admin';
  const canBill = canRename;

  const loadEntitlements = useCallback(async () => {
    try {
      setEntitlements(await endpoints.entitlements());
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load billing status.');
    }
  }, []);

  useEffect(() => {
    void loadEntitlements();
  }, [loadEntitlements]);

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

  async function openBilling() {
    if (!canBill) {
      setError('Only an Owner or Admin can manage billing.');
      return;
    }
    setError(null);
    setBillingBusy(true);
    try {
      const returnUrl = 'https://schemevault.app/settings';
      const session = entitlements?.isActive
        ? await endpoints.billingPortal({ returnUrl })
        : await endpoints.billingCheckout({ successUrl: returnUrl, cancelUrl: returnUrl });
      const opened = await Linking.openURL(session.url);
      if (!opened) {
        setError('Could not open the billing page. Copy the URL from support if this continues.');
      }
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not start billing.');
    } finally {
      setBillingBusy(false);
    }
  }

  const planLabel = entitlements?.plan ?? 'Unknown';
  const statusLabel = entitlements?.status
    ? entitlements.status.charAt(0).toUpperCase() + entitlements.status.slice(1)
    : 'Loading…';
  const billingTitle = entitlements?.isActive ? 'Manage billing' : 'Upgrade';

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
          <Text style={styles.rowLabel}>Plan</Text>
          <Text style={styles.rowValue}>
            {planLabel}
            {entitlements?.isPro ? ' (Pro features on)' : ' (Starter)'}
          </Text>
          <Text style={styles.rowLabel}>Subscription</Text>
          <Text style={styles.rowValue}>{statusLabel}</Text>
          <Text style={styles.hint}>
            Pro unlocks guided questionnaires, multi-scheme pack export, accident reporting and the equipment register.
            In local stub mode set SubscriptionApi:ForcePro or StubPlan=Pro (already on in Development).
          </Text>
          {canBill ? (
            <PrimaryButton title={billingTitle} onPress={() => void openBilling()} loading={billingBusy} />
          ) : (
            <Text style={styles.hint}>Ask an Owner or Admin to manage billing for this organisation.</Text>
          )}
        </Card>
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
  hint: { color: colours.muted, marginTop: 12, lineHeight: 20 },
});
