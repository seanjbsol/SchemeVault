import { useCallback, useState } from 'react';
import { RefreshControl, ScrollView, StyleSheet, Text, View } from 'react-native';
import { useFocusEffect, useLocalSearchParams } from 'expo-router';
import { Card, EmptyState, ErrorBanner, Screen, TrafficDot } from '@/components/ui';
import { ApiError } from '@/lib/api';
import { endpoints } from '@/lib/endpoints';
import { colours, formatDate, trafficLabel } from '@/lib/theme';
import type { SchemeDetail } from '@/lib/types';

export default function SchemeDetailScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const [scheme, setScheme] = useState<SchemeDetail | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [refreshing, setRefreshing] = useState(false);

  const load = useCallback(async () => {
    if (!id) {
      return;
    }
    setError(null);
    try {
      setScheme(await endpoints.scheme(id));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load this scheme.');
    }
  }, [id]);

  useFocusEffect(
    useCallback(() => {
      void load();
    }, [load]),
  );

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
        <ErrorBanner message={error} />
        {scheme ? (
          <>
            <Text style={styles.kicker}>{scheme.provider}</Text>
            <Text style={styles.title}>{scheme.name}</Text>
            <Text style={styles.body}>{scheme.description}</Text>
            <Card>
              {scheme.accreditation ? (
                <View style={styles.row}>
                  <TrafficDot light={scheme.accreditation.trafficLight} size={14} />
                  <View style={{ flex: 1 }}>
                    <Text style={styles.cardTitle}>{trafficLabel(scheme.accreditation.trafficLight)}</Text>
                    <Text style={styles.meta}>
                      {scheme.accreditation.status}
                      {scheme.accreditation.membershipNumber ? ` · ${scheme.accreditation.membershipNumber}` : ''}
                    </Text>
                    <Text style={styles.meta}>Expires {formatDate(scheme.accreditation.expiresOn)}</Text>
                  </View>
                </View>
              ) : (
                <Text style={styles.meta}>No accreditation on file. Add one from Renewals.</Text>
              )}
            </Card>
            <Text style={styles.section}>Gap checklist</Text>
            {scheme.gaps.length === 0 ? (
              <EmptyState
                title="No gap items"
                body="New organisations receive checklist rows from the scheme templates."
              />
            ) : (
              scheme.gaps.map((gap) => (
                <Card key={gap.id}>
                  <Text style={styles.cardTitle}>{gap.title}</Text>
                  <Text style={styles.meta}>{gap.description}</Text>
                  <Text style={styles.status}>
                    {gap.status}
                    {gap.evidenceTitle ? ` · linked: ${gap.evidenceTitle}` : ' · no evidence linked'}
                  </Text>
                </Card>
              ))
            )}
          </>
        ) : null}
      </ScrollView>
    </Screen>
  );
}

const styles = StyleSheet.create({
  content: { padding: 16, paddingBottom: 32 },
  kicker: { color: colours.amber, fontWeight: '800' },
  title: { fontSize: 24, fontWeight: '800', color: colours.navy, marginVertical: 6 },
  body: { color: colours.muted, lineHeight: 20, marginBottom: 16 },
  section: { marginTop: 8, marginBottom: 8, fontWeight: '800', color: colours.navy, fontSize: 16 },
  row: { flexDirection: 'row', gap: 10, alignItems: 'flex-start' },
  cardTitle: { fontWeight: '700', color: colours.navy },
  meta: { color: colours.muted, marginTop: 4, fontSize: 13, lineHeight: 18 },
  status: { marginTop: 8, color: colours.navyMid, fontWeight: '600', fontSize: 13 },
});
