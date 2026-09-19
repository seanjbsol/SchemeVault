import { useCallback, useState } from 'react';
import { Pressable, ScrollView, StyleSheet, Text } from 'react-native';
import { router, useFocusEffect, useLocalSearchParams } from 'expo-router';
import { Card, ErrorBanner, Screen, TrafficDot } from '@/components/ui';
import { PhotoStrip } from '@/components/photos';
import { ApiError } from '@/lib/api';
import { endpoints } from '@/lib/endpoints';
import { colours, formatDate, trafficLabel } from '@/lib/theme';
import type { Evidence } from '@/lib/types';

export default function EvidenceDetailScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const [item, setItem] = useState<Evidence | null>(null);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    if (!id) {
      return;
    }
    setError(null);
    try {
      setItem(await endpoints.evidenceItem(String(id)));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load that vault item.');
    }
  }, [id]);

  useFocusEffect(
    useCallback(() => {
      void load();
    }, [load]),
  );

  return (
    <Screen>
      <ScrollView contentContainerStyle={styles.content}>
        <ErrorBanner message={error} />
        {item ? (
          <>
            <Text style={styles.title}>{item.title}</Text>
            <Card>
              <TrafficDot light={item.trafficLight} />
              <Text style={styles.meta}>
                {item.category} · {trafficLabel(item.trafficLight)}
              </Text>
              <Text style={styles.meta}>{item.expiresOn ? `Expires ${formatDate(item.expiresOn)}` : 'No expiry'}</Text>
              {item.hasFile ? <Text style={styles.meta}>Document: {item.originalFileName}</Text> : null}
              {item.notes ? <Text style={styles.body}>{item.notes}</Text> : null}
            </Card>
            <PhotoStrip kind="evidence" ownerId={item.id} onError={setError} />
            <Pressable
              onPress={async () => {
                try {
                  await endpoints.deleteEvidence(item.id);
                  router.replace('/evidence');
                } catch (err) {
                  setError(err instanceof ApiError ? err.message : 'Could not delete that item.');
                }
              }}>
              <Text style={styles.delete}>Remove from vault</Text>
            </Pressable>
          </>
        ) : null}
      </ScrollView>
    </Screen>
  );
}

const styles = StyleSheet.create({
  content: { padding: 16, paddingBottom: 40 },
  title: { fontWeight: '800', color: colours.navy, fontSize: 20, marginBottom: 8 },
  meta: { color: colours.muted, marginTop: 6 },
  body: { color: colours.ink, marginTop: 10, lineHeight: 20 },
  delete: { color: colours.red, fontWeight: '700', marginTop: 16 },
});
