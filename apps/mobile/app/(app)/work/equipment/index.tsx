import { useCallback, useState } from 'react';
import { Pressable, RefreshControl, ScrollView, StyleSheet, Text, View } from 'react-native';
import { router, useFocusEffect } from 'expo-router';
import { Card, EmptyState, ErrorBanner, PrimaryButton, Screen, TrafficDot } from '@/components/ui';
import { Paywall } from '@/components/paywall';
import { ApiError } from '@/lib/api';
import { useAuth } from '@/lib/auth';
import { endpoints } from '@/lib/endpoints';
import { colours, formatDate } from '@/lib/theme';
import type { Equipment } from '@/lib/types';

export default function EquipmentScreen() {
  const { isPro } = useAuth();
  const [items, setItems] = useState<Equipment[]>([]);
  const [overdueOnly, setOverdueOnly] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [refreshing, setRefreshing] = useState(false);

  const load = useCallback(async () => {
    if (!isPro) {
      return;
    }
    setError(null);
    try {
      setItems(await endpoints.equipment(overdueOnly));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load the register.');
    }
  }, [isPro, overdueOnly]);

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
          Track calibration and service due dates for plant, lifting kit and first-aid equipment. Overdue items are
          flagged red so they are not sent to site.
        </Text>
        {!isPro ? <Paywall feature="the equipment register" /> : null}
        <ErrorBanner message={error} />
        {isPro ? (
          <>
            <PrimaryButton title="Add equipment" onPress={() => router.push('/work/equipment/add')} />
            <Pressable onPress={() => setOverdueOnly((value) => !value)}>
              <Text style={styles.filter}>{overdueOnly ? 'Show all' : 'Show overdue only'}</Text>
            </Pressable>
          </>
        ) : null}
        {isPro && items.length === 0 ? (
          <EmptyState title="Register is empty" body="Add a MEWP, harness or first-aid kit and set the next due date." />
        ) : null}
        {items.map((item) => (
          <Pressable key={item.id} onPress={() => router.push(`/work/equipment/${item.id}`)}>
            <Card>
              <View style={styles.row}>
                <TrafficDot light={item.trafficLight} />
                <View style={{ flex: 1 }}>
                  <Text style={styles.title}>{item.name}</Text>
                  <Text style={styles.meta}>
                    {item.category}
                    {item.isOverdue ? ' · OVERDUE' : ''}
                    {item.serialNumber ? ` · ${item.serialNumber}` : ''}
                  </Text>
                  <Text style={styles.meta}>
                    Cal {formatDate(item.calibrationDueOn)} · Service {formatDate(item.serviceDueOn)}
                  </Text>
                </View>
              </View>
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
  filter: { color: colours.navyMid, fontWeight: '700', marginVertical: 12 },
  row: { flexDirection: 'row', gap: 10, alignItems: 'flex-start' },
  title: { fontWeight: '800', color: colours.navy },
  meta: { color: colours.muted, marginTop: 4, fontSize: 13 },
});
