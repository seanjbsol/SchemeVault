import { useCallback, useState } from 'react';
import { Pressable, RefreshControl, ScrollView, StyleSheet, Text } from 'react-native';
import { router, useFocusEffect } from 'expo-router';
import { Card, EmptyState, ErrorBanner, PrimaryButton, Screen } from '@/components/ui';
import { Paywall } from '@/components/paywall';
import { ApiError } from '@/lib/api';
import { useAuth } from '@/lib/auth';
import { endpoints } from '@/lib/endpoints';
import { colours, formatDate } from '@/lib/theme';
import type { QuestionnaireTemplate } from '@/lib/types';

export default function QuestionnairesScreen() {
  const { isPro } = useAuth();
  const [items, setItems] = useState<QuestionnaireTemplate[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [refreshing, setRefreshing] = useState(false);

  const load = useCallback(async () => {
    if (!isPro) {
      return;
    }
    setError(null);
    try {
      setItems(await endpoints.questionnaires());
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load questionnaires.');
    }
  }, [isPro]);

  useFocusEffect(
    useCallback(() => {
      void load();
    }, [load]),
  );

  async function exportPack() {
    setError(null);
    try {
      await endpoints.downloadExportPack();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not export the pack.');
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
          These questionnaires help a layperson assemble a scheme-style narrative. The output is a draft for you to
          review. It is not submitted to CHAS, Constructionline, SafeContractor or Avetta.
        </Text>
        {!isPro ? <Paywall feature="guided questionnaires" /> : null}
        <ErrorBanner message={error} />
        {isPro ? <PrimaryButton title="Export multi-scheme PDF pack" onPress={() => void exportPack()} /> : null}
        {isPro && items.length === 0 ? (
          <EmptyState title="No questionnaires" body="The scheme catalogue should appear once the API is reachable." />
        ) : null}
        {items.map((item) => (
          <Pressable key={item.schemeCode} onPress={() => router.push(`/work/questionnaires/${item.schemeCode}`)}>
            <Card>
              <Text style={styles.title}>{item.title}</Text>
              <Text style={styles.meta}>
                {item.questionCount} questions
                {item.latestGeneratedAt ? ` · last draft ${formatDate(item.latestGeneratedAt)}` : ' · no draft yet'}
              </Text>
              <Text style={styles.body}>{item.introduction}</Text>
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
  title: { fontWeight: '800', color: colours.navy },
  meta: { color: colours.muted, marginTop: 4, fontSize: 13 },
  body: { color: colours.ink, marginTop: 8, lineHeight: 20 },
});
