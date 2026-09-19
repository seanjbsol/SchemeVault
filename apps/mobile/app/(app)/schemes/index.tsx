import { useCallback, useState } from 'react';
import { Pressable, RefreshControl, ScrollView, StyleSheet, Text, View } from 'react-native';
import { router, useFocusEffect } from 'expo-router';
import { Card, EmptyState, ErrorBanner, Screen, TrafficDot } from '@/components/ui';
import { ApiError } from '@/lib/api';
import { endpoints } from '@/lib/endpoints';
import { colours, daysCopy, formatDate, trafficLabel } from '@/lib/theme';
import type { SchemeSummary } from '@/lib/types';

export default function SchemesScreen() {
  const [items, setItems] = useState<SchemeSummary[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [refreshing, setRefreshing] = useState(false);

  const load = useCallback(async () => {
    setError(null);
    try {
      setItems(await endpoints.schemes());
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load schemes.');
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
          Shared UK catalogue — CHAS, Constructionline, SafeContractor, Avetta and SMAS. Accreditation records are
          yours alone.
        </Text>
        <ErrorBanner message={error} />
        {items.length === 0 ? (
          <EmptyState title="No schemes" body="The API seed should include five schemes." />
        ) : (
          items.map((scheme) => {
            const light = scheme.accreditation?.trafficLight ?? 'Grey';
            return (
              <Pressable key={scheme.id} onPress={() => router.push(`/schemes/${scheme.id}`)}>
                <Card>
                  <View style={styles.row}>
                    <TrafficDot light={light} />
                    <View style={{ flex: 1 }}>
                      <Text style={styles.name}>{scheme.name}</Text>
                      <Text style={styles.meta}>{scheme.provider}</Text>
                    </View>
                    {scheme.isSsipStyle ? <Text style={styles.ssip}>SSIP</Text> : null}
                  </View>
                  <Text style={styles.meta}>
                    {scheme.accreditation
                      ? `${trafficLabel(light)} · ${formatDate(scheme.accreditation.expiresOn)} · ${daysCopy(scheme.accreditation.daysUntilExpiry)}`
                      : 'No accreditation record yet'}
                  </Text>
                  <Text style={styles.gaps}>
                    {scheme.completeGaps} complete · {scheme.missingGaps} missing
                  </Text>
                </Card>
              </Pressable>
            );
          })
        )}
      </ScrollView>
    </Screen>
  );
}

const styles = StyleSheet.create({
  content: { padding: 16, paddingBottom: 32 },
  lede: { color: colours.muted, marginBottom: 12, lineHeight: 20 },
  row: { flexDirection: 'row', alignItems: 'center', gap: 10 },
  name: { fontWeight: '800', color: colours.navy, fontSize: 16 },
  meta: { color: colours.muted, marginTop: 6, fontSize: 13 },
  gaps: { marginTop: 8, color: colours.navyMid, fontWeight: '600' },
  ssip: {
    backgroundColor: colours.navy,
    color: colours.white,
    fontSize: 11,
    fontWeight: '800',
    paddingHorizontal: 8,
    paddingVertical: 3,
    borderRadius: 6,
    overflow: 'hidden',
  },
});
