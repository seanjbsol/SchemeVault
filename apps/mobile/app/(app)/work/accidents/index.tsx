import { useCallback, useState } from 'react';
import { Pressable, RefreshControl, ScrollView, StyleSheet, Text } from 'react-native';
import { router, useFocusEffect } from 'expo-router';
import { Card, EmptyState, ErrorBanner, PrimaryButton, Screen } from '@/components/ui';
import { Paywall } from '@/components/paywall';
import { ApiError } from '@/lib/api';
import { useAuth } from '@/lib/auth';
import { endpoints } from '@/lib/endpoints';
import { colours, formatDate } from '@/lib/theme';
import type { Accident, LostHoursSummary } from '@/lib/types';

export default function AccidentsScreen() {
  const { isPro } = useAuth();
  const [items, setItems] = useState<Accident[]>([]);
  const [hours, setHours] = useState<LostHoursSummary | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [refreshing, setRefreshing] = useState(false);

  const load = useCallback(async () => {
    if (!isPro) {
      return;
    }
    setError(null);
    try {
      const [list, log] = await Promise.all([endpoints.accidents(), endpoints.lostHours()]);
      setItems(list);
      setHours(log);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load accidents.');
    }
  }, [isPro]);

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
        <Text style={styles.lede}>
          Internal incident log for this organisation. It is not a RIDDOR submission and does not notify the HSE.
        </Text>
        {!isPro ? <Paywall feature="accident reporting" /> : null}
        <ErrorBanner message={error} />
        {isPro ? (
          <>
            <Card>
              <Text style={styles.stat}>{hours?.totalHours ?? 0} hours</Text>
              <Text style={styles.meta}>Lost hours recorded for this tenant</Text>
            </Card>
            <PrimaryButton title="Log an incident" onPress={() => router.push('/work/accidents/add')} />
          </>
        ) : null}
        {isPro && items.length === 0 ? (
          <EmptyState title="No incidents yet" body="Log a near miss or injury when it happens on site or in the yard." />
        ) : null}
        {items.map((item) => (
          <Pressable key={item.id} onPress={() => router.push(`/work/accidents/${item.id}`)}>
            <Card>
              <Text style={styles.title}>{item.location}</Text>
              <Text style={styles.meta}>
                {formatDate(item.occurredOn)} · {item.severity} · {item.status}
                {item.lostHours ? ` · ${item.lostHours} lost hours` : ''}
              </Text>
              <Text style={styles.body} numberOfLines={3}>
                {item.description}
              </Text>
            </Card>
          </Pressable>
        ))}
      </ScrollView>
    </Screen>
  );
}

const styles = StyleSheet.create({
  content: { padding: 16, paddingBottom: 40 },
  lede: { color: colours.muted, marginBottom: 12, lineHeight: 20 },
  stat: { fontSize: 28, fontWeight: '800', color: colours.navy },
  title: { fontWeight: '800', color: colours.navy },
  meta: { color: colours.muted, marginTop: 4, fontSize: 13 },
  body: { color: colours.ink, marginTop: 8, lineHeight: 20 },
});
