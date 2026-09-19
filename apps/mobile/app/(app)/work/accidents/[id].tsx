import { useCallback, useState } from 'react';
import { Pressable, ScrollView, StyleSheet, Text } from 'react-native';
import { router, useFocusEffect, useLocalSearchParams } from 'expo-router';
import { Card, ErrorBanner, Field, PrimaryButton, Screen } from '@/components/ui';
import { PhotoStrip } from '@/components/photos';
import { ApiError } from '@/lib/api';
import { endpoints } from '@/lib/endpoints';
import { colours, formatDate } from '@/lib/theme';
import type { Accident } from '@/lib/types';

export default function AccidentDetailScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const [item, setItem] = useState<Accident | null>(null);
  const [hours, setHours] = useState('');
  const [reason, setReason] = useState('Additional lost time');
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const load = useCallback(async () => {
    if (!id) {
      return;
    }
    setError(null);
    try {
      setItem(await endpoints.accident(String(id)));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load that incident.');
    }
  }, [id]);

  useFocusEffect(
    useCallback(() => {
      void load();
    }, [load]),
  );

  async function addHours() {
    if (!item) {
      return;
    }
    const value = Number(hours);
    if (!value || value <= 0) {
      setError('Enter the hours lost.');
      return;
    }
    setSaving(true);
    setError(null);
    try {
      await endpoints.addLostHours({
        occurredOn: item.occurredOn,
        hours: value,
        reason: reason.trim() || 'Lost hours',
        accidentId: item.id,
      });
      setHours('');
      await load();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not add lost hours.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <Screen>
      <ScrollView contentContainerStyle={styles.content}>
        <ErrorBanner message={error} />
        {item ? (
          <>
            <Text style={styles.title}>{item.location}</Text>
            <Text style={styles.meta}>
              {formatDate(item.occurredOn)} · {item.severity} · {item.status}
            </Text>
            <Card>
              <Text style={styles.body}>{item.description}</Text>
              {item.injuredPerson ? <Text style={styles.meta}>Person: {item.injuredPerson}</Text> : null}
              {item.immediateAction ? <Text style={styles.meta}>Action: {item.immediateAction}</Text> : null}
              <Text style={styles.stat}>{item.lostHours} lost hours on this record</Text>
            </Card>
            <PhotoStrip kind="accidents" ownerId={item.id} onError={setError} />
            <Card>
              <Text style={styles.section}>Add lost hours</Text>
              <Field label="Hours" value={hours} onChangeText={setHours} keyboardType="decimal-pad" />
              <Field label="Reason" value={reason} onChangeText={setReason} autoCapitalize="sentences" />
              <PrimaryButton title="Add to log" onPress={() => void addHours()} loading={saving} />
            </Card>
            <Pressable
              onPress={async () => {
                try {
                  await endpoints.deleteAccident(item.id);
                  router.replace('/work/accidents');
                } catch (err) {
                  setError(err instanceof ApiError ? err.message : 'Could not delete that incident.');
                }
              }}>
              <Text style={styles.delete}>Remove incident</Text>
            </Pressable>
          </>
        ) : null}
      </ScrollView>
    </Screen>
  );
}

const styles = StyleSheet.create({
  content: { padding: 16, paddingBottom: 40 },
  title: { fontWeight: '800', color: colours.navy, fontSize: 20 },
  meta: { color: colours.muted, marginTop: 6, lineHeight: 20 },
  body: { color: colours.ink, lineHeight: 20 },
  stat: { marginTop: 12, fontWeight: '800', color: colours.navy },
  section: { fontWeight: '800', color: colours.navy, marginBottom: 8 },
  delete: { color: colours.red, fontWeight: '700', marginTop: 16 },
});
