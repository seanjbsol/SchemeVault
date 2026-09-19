import { useCallback, useState } from 'react';
import { Pressable, ScrollView, StyleSheet, Text } from 'react-native';
import { router, useFocusEffect, useLocalSearchParams } from 'expo-router';
import { Card, ErrorBanner, Screen, TrafficDot } from '@/components/ui';
import { PhotoStrip } from '@/components/photos';
import { ApiError } from '@/lib/api';
import { endpoints } from '@/lib/endpoints';
import { colours, formatDate } from '@/lib/theme';
import type { Equipment } from '@/lib/types';

export default function EquipmentDetailScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const [item, setItem] = useState<Equipment | null>(null);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    if (!id) {
      return;
    }
    setError(null);
    try {
      setItem(await endpoints.equipmentItem(String(id)));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load that item.');
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
            <Text style={styles.title}>{item.name}</Text>
            <Card>
              <TrafficDot light={item.trafficLight} />
              <Text style={styles.meta}>
                {item.category}
                {item.isOverdue ? ' · OVERDUE — do not send to site until it is in date' : ''}
              </Text>
              {item.serialNumber ? <Text style={styles.meta}>Serial {item.serialNumber}</Text> : null}
              <Text style={styles.meta}>Calibration due {formatDate(item.calibrationDueOn)}</Text>
              <Text style={styles.meta}>Service due {formatDate(item.serviceDueOn)}</Text>
              {item.notes ? <Text style={styles.body}>{item.notes}</Text> : null}
            </Card>
            <PhotoStrip kind="equipment" ownerId={item.id} onError={setError} />
            <Pressable
              onPress={async () => {
                try {
                  await endpoints.deleteEquipment(item.id);
                  router.replace('/work/equipment');
                } catch (err) {
                  setError(err instanceof ApiError ? err.message : 'Could not remove that item.');
                }
              }}>
              <Text style={styles.delete}>Remove from register</Text>
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
