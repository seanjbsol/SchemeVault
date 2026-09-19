import { useCallback, useState } from 'react';
import { Pressable, RefreshControl, ScrollView, StyleSheet, Text, View } from 'react-native';
import { router, useFocusEffect } from 'expo-router';
import { Card, EmptyState, ErrorBanner, PrimaryButton, Screen, TrafficDot } from '@/components/ui';
import { ApiError } from '@/lib/api';
import { endpoints } from '@/lib/endpoints';
import { colours, formatDate, trafficLabel } from '@/lib/theme';
import type { Evidence } from '@/lib/types';

export default function EvidenceListScreen() {
  const [items, setItems] = useState<Evidence[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [refreshing, setRefreshing] = useState(false);

  const load = useCallback(async () => {
    setError(null);
    try {
      setItems(await endpoints.evidence());
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load the vault.');
    }
  }, []);

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
          Store insurance, policies, RAMS and training once, then reuse across schemes. Files can be attached via the
          API; this screen records the metadata.
        </Text>
        <PrimaryButton title="Add evidence" onPress={() => router.push('/evidence/add')} />
        <View style={{ height: 12 }} />
        <ErrorBanner message={error} />
        {items.length === 0 ? (
          <EmptyState title="Vault is empty" body="Add a policy or certificate to start closing scheme gaps." />
        ) : (
          items.map((item) => (
            <Card key={item.id}>
              <View style={styles.row}>
                <TrafficDot light={item.trafficLight} />
                <View style={{ flex: 1 }}>
                  <Text style={styles.title}>{item.title}</Text>
                  <Text style={styles.meta}>
                    {item.category} · {trafficLabel(item.trafficLight)}
                  </Text>
                  <Text style={styles.meta}>
                    {item.expiresOn ? `Expires ${formatDate(item.expiresOn)}` : 'No expiry'}
                    {item.hasFile ? ` · ${item.originalFileName}` : ' · no file yet'}
                  </Text>
                </View>
              </View>
              {item.notes ? <Text style={styles.notes}>{item.notes}</Text> : null}
              <Pressable
                onPress={async () => {
                  try {
                    await endpoints.deleteEvidence(item.id);
                    await load();
                  } catch (err) {
                    setError(err instanceof ApiError ? err.message : 'Could not delete that item.');
                  }
                }}>
                <Text style={styles.delete}>Remove</Text>
              </Pressable>
            </Card>
          ))
        )}
      </ScrollView>
    </Screen>
  );
}

const styles = StyleSheet.create({
  content: { padding: 16, paddingBottom: 32 },
  lede: { color: colours.muted, marginBottom: 8, lineHeight: 20 },
  row: { flexDirection: 'row', gap: 10, alignItems: 'flex-start' },
  title: { fontWeight: '800', color: colours.navy },
  meta: { color: colours.muted, marginTop: 4, fontSize: 13 },
  notes: { marginTop: 8, color: colours.ink, fontSize: 14 },
  delete: { marginTop: 10, color: colours.red, fontWeight: '700' },
});
