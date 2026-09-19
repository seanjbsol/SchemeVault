import { useCallback, useMemo, useState } from 'react';
import { RefreshControl, ScrollView, StyleSheet, Text, View } from 'react-native';
import { useFocusEffect } from 'expo-router';
import { Card, EmptyState, ErrorBanner, Field, PrimaryButton, Screen, TrafficDot } from '@/components/ui';
import { ApiError } from '@/lib/api';
import { endpoints } from '@/lib/endpoints';
import { colours, daysCopy, formatDate, trafficLabel } from '@/lib/theme';
import type { Renewal, SchemeSummary } from '@/lib/types';

export default function RenewalsScreen() {
  const [items, setItems] = useState<Renewal[]>([]);
  const [schemes, setSchemes] = useState<SchemeSummary[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [refreshing, setRefreshing] = useState(false);
  const [schemeId, setSchemeId] = useState('');
  const [membershipNumber, setMembershipNumber] = useState('');
  const [expiresOn, setExpiresOn] = useState('');
  const [saving, setSaving] = useState(false);

  const load = useCallback(async () => {
    setError(null);
    try {
      const [renewals, catalogue] = await Promise.all([endpoints.renewals(), endpoints.schemes()]);
      setItems(renewals);
      setSchemes(catalogue);
      setSchemeId((current) => current || catalogue.find((s) => !s.accreditation)?.id || '');
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load renewals.');
    }
  }, []);

  useFocusEffect(
    useCallback(() => {
      void load();
    }, [load]),
  );

  const openSchemes = useMemo(() => schemes.filter((s) => !s.accreditation), [schemes]);

  async function addRenewal() {
    setError(null);
    if (!schemeId) {
      setError('Choose a scheme that does not already have a record.');
      return;
    }
    setSaving(true);
    try {
      await endpoints.createRenewal({
        schemeId,
        status: 'Active',
        membershipNumber: membershipNumber.trim() || undefined,
        expiresOn: expiresOn.trim() ? new Date(expiresOn.trim()).toISOString() : null,
      });
      setMembershipNumber('');
      setExpiresOn('');
      await load();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not save the renewal.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <Screen>
      <ScrollView
        contentContainerStyle={styles.content}
        refreshControl={
          <RefreshControl
            refreshing={refreshing}
            onRefresh={async () => {
              setRefreshing(true);
              await load();
              setRefreshing(false);
            }}
          />
        }>
        <Text style={styles.lede}>
          One accreditation record per scheme. Traffic lights go amber inside 60 days and red once expired.
        </Text>
        <ErrorBanner message={error} />
        {items.length === 0 ? (
          <EmptyState title="No renewals yet" body="Add CHAS or Constructionline with an expiry date to light the dashboard." />
        ) : (
          items.map((item) => (
            <Card key={item.id}>
              <View style={styles.row}>
                <TrafficDot light={item.trafficLight} />
                <View style={{ flex: 1 }}>
                  <Text style={styles.title}>{item.schemeName}</Text>
                  <Text style={styles.meta}>
                    {item.status} · {trafficLabel(item.trafficLight)}
                  </Text>
                  <Text style={styles.meta}>
                    {formatDate(item.expiresOn)}
                    {item.daysUntilExpiry !== null && item.daysUntilExpiry !== undefined
                      ? ` · ${daysCopy(item.daysUntilExpiry)}`
                      : ''}
                  </Text>
                  {item.membershipNumber ? <Text style={styles.meta}>{item.membershipNumber}</Text> : null}
                </View>
              </View>
            </Card>
          ))
        )}

        {openSchemes.length > 0 ? (
          <Card>
            <Text style={styles.formTitle}>Add accreditation</Text>
            <Text style={styles.meta}>Scheme id (paste from the list below)</Text>
            {openSchemes.map((s) => (
              <Text
                key={s.id}
                onPress={() => setSchemeId(s.id)}
                style={[styles.choice, schemeId === s.id && styles.choiceOn]}>
                {s.name}
              </Text>
            ))}
            <Field label="Membership number" value={membershipNumber} onChangeText={setMembershipNumber} />
            <Field label="Expiry (YYYY-MM-DD)" value={expiresOn} onChangeText={setExpiresOn} placeholder="2027-01-31" />
            <PrimaryButton title="Save renewal" onPress={addRenewal} loading={saving} />
          </Card>
        ) : null}
      </ScrollView>
    </Screen>
  );
}

const styles = StyleSheet.create({
  content: { padding: 16, paddingBottom: 32 },
  lede: { color: colours.muted, marginBottom: 12, lineHeight: 20 },
  row: { flexDirection: 'row', gap: 10, alignItems: 'flex-start' },
  title: { fontWeight: '800', color: colours.navy },
  meta: { color: colours.muted, marginTop: 4, fontSize: 13 },
  formTitle: { fontWeight: '800', color: colours.navy, marginBottom: 8 },
  choice: {
    paddingVertical: 8,
    color: colours.navyMid,
    fontWeight: '600',
  },
  choiceOn: {
    color: colours.navy,
    fontWeight: '800',
  },
});
