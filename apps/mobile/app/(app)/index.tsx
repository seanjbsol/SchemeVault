import { useCallback, useState } from 'react';
import { Pressable, RefreshControl, ScrollView, StyleSheet, Text, View } from 'react-native';
import { router, useFocusEffect } from 'expo-router';
import { Card, EmptyState, ErrorBanner, Screen, Title, TrafficDot } from '@/components/ui';
import { ApiError } from '@/lib/api';
import { endpoints } from '@/lib/endpoints';
import { useAuth } from '@/lib/auth';
import { colours, daysCopy, formatDate, trafficLabel } from '@/lib/theme';
import type { Dashboard } from '@/lib/types';

export default function HomeScreen() {
  const { user } = useAuth();
  const [data, setData] = useState<Dashboard | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [refreshing, setRefreshing] = useState(false);

  const load = useCallback(async () => {
    setError(null);
    try {
      setData(await endpoints.dashboard());
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load the dashboard.');
    }
  }, []);

  useFocusEffect(
    useCallback(() => {
      void load();
    }, [load]),
  );

  async function onRefresh() {
    setRefreshing(true);
    await load();
    setRefreshing(false);
  }

  const counts = data?.counts;

  return (
    <Screen>
      <ScrollView
        contentContainerStyle={styles.content}
        refreshControl={<RefreshControl refreshing={refreshing} onRefresh={onRefresh} />}>
        <Text style={styles.hello}>Hello{user?.fullName ? `, ${user.fullName.split(' ')[0]}` : ''}</Text>
        <Title>{data?.tenantName ?? user?.tenantName ?? 'Your organisation'}</Title>
        <Text style={styles.lede}>Renewal traffic lights for the next 60 days, plus gaps that still need evidence.</Text>
        <Pressable onPress={() => router.push('/work')}>
          <Text style={styles.link}>H&S tools (questionnaires, accidents, equipment) →</Text>
        </Pressable>
        <ErrorBanner message={error} />

        <View style={styles.grid}>
          <Stat label="Due soon" value={counts?.upcoming ?? '—'} tone={colours.amber} />
          <Stat label="Expired" value={counts?.expired ?? '—'} tone={colours.red} />
          <Stat label="Missing evidence" value={counts?.missingEvidence ?? '—'} tone={colours.navyMid} />
          <Stat label="Active schemes" value={counts?.activeSchemes ?? '—'} tone={colours.green} />
          <Stat label="Open incidents" value={counts?.openAccidents ?? '—'} tone={colours.navy} />
          <Stat label="Overdue kit" value={counts?.overdueEquipment ?? '—'} tone={colours.red} />
        </View>

        <Text style={styles.section}>Upcoming renewals</Text>
        {data?.upcomingRenewals.length ? (
          data.upcomingRenewals.map((item) => (
            <Card key={item.id}>
              <Row light={item.trafficLight} title={item.schemeName} meta={daysCopy(item.daysUntilExpiry)} />
              <Text style={styles.meta}>Expires {formatDate(item.expiresOn)} · {trafficLabel(item.trafficLight)}</Text>
            </Card>
          ))
        ) : (
          <EmptyState title="Nothing due in the next 60 days" body="Expired items and schemes with no date still need a look." />
        )}

        <Text style={styles.section}>Expired</Text>
        {data?.expiredRenewals.length ? (
          data.expiredRenewals.map((item) => (
            <Card key={item.id}>
              <Row light={item.trafficLight} title={item.schemeName} meta={item.status} />
              <Text style={styles.meta}>Expired {formatDate(item.expiresOn)}</Text>
            </Card>
          ))
        ) : (
          <EmptyState title="No expired accreditations" body="When a scheme lapses it lands here in red." />
        )}

        <Text style={styles.section}>Missing evidence</Text>
        {data?.missingEvidence.length ? (
          data.missingEvidence.map((item) => (
            <Card key={item.id}>
              <Text style={styles.itemTitle}>{item.title}</Text>
              <Text style={styles.meta}>{item.description}</Text>
            </Card>
          ))
        ) : (
          <EmptyState title="Gap list is clear" body="Map vault items to scheme checklist rows from the scheme screen." />
        )}
      </ScrollView>
    </Screen>
  );
}

function Stat({ label, value, tone }: { label: string; value: number | string; tone: string }) {
  return (
    <View style={styles.stat}>
      <Text style={[styles.statValue, { color: tone }]}>{value}</Text>
      <Text style={styles.statLabel}>{label}</Text>
    </View>
  );
}

function Row({ light, title, meta }: { light: string; title: string; meta: string }) {
  return (
    <View style={styles.row}>
      <TrafficDot light={light} />
      <Text style={styles.itemTitle}>{title}</Text>
      <Text style={styles.pill}>{meta}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  content: { padding: 16, paddingBottom: 32 },
  hello: { color: colours.muted, fontWeight: '600' },
  lede: { color: colours.muted, marginBottom: 16, lineHeight: 20 },
  link: { color: colours.navyMid, fontWeight: '700', marginBottom: 16 },
  grid: { flexDirection: 'row', flexWrap: 'wrap', gap: 8, marginBottom: 8 },
  stat: {
    width: '48%',
    flexGrow: 1,
    backgroundColor: colours.card,
    borderRadius: 12,
    padding: 12,
    borderWidth: 1,
    borderColor: colours.line,
  },
  statValue: { fontSize: 28, fontWeight: '800' },
  statLabel: { color: colours.muted, marginTop: 2, fontWeight: '600' },
  section: { marginTop: 18, marginBottom: 8, fontWeight: '800', color: colours.navy, fontSize: 16 },
  row: { flexDirection: 'row', alignItems: 'center', gap: 8 },
  itemTitle: { flex: 1, fontWeight: '700', color: colours.navy },
  pill: { color: colours.muted, fontSize: 12, fontWeight: '600' },
  meta: { color: colours.muted, marginTop: 6, fontSize: 13 },
});
