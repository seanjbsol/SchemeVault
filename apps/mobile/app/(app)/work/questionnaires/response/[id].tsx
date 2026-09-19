import { useCallback, useState } from 'react';
import { ScrollView, StyleSheet, Text } from 'react-native';
import { useFocusEffect, useLocalSearchParams } from 'expo-router';
import { Card, ErrorBanner, PrimaryButton, Screen } from '@/components/ui';
import { ApiError } from '@/lib/api';
import { endpoints } from '@/lib/endpoints';
import { colours, formatDate } from '@/lib/theme';
import type { QuestionnaireResponse } from '@/lib/types';

export default function QuestionnaireResultScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const [item, setItem] = useState<QuestionnaireResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [downloading, setDownloading] = useState(false);

  const load = useCallback(async () => {
    if (!id) {
      return;
    }
    setError(null);
    try {
      setItem(await endpoints.questionnaireResponse(String(id)));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load that pack.');
    }
  }, [id]);

  useFocusEffect(
    useCallback(() => {
      void load();
    }, [load]),
  );

  async function download() {
    if (!item) {
      return;
    }
    setDownloading(true);
    setError(null);
    try {
      await endpoints.downloadQuestionnairePdf(item.id, `${item.schemeCode.toLowerCase()}-pack-draft.pdf`);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not download the PDF.');
    } finally {
      setDownloading(false);
    }
  }

  return (
    <Screen>
      <ScrollView contentContainerStyle={styles.content}>
        <ErrorBanner message={error} />
        {item ? (
          <>
            <Text style={styles.title}>{item.schemeName} pack draft</Text>
            <Text style={styles.meta}>
              {item.status} · {formatDate(item.updatedAt)}
            </Text>
            <PrimaryButton title="Download PDF" onPress={() => void download()} loading={downloading} />
            <Card>
              <Text style={styles.markdown}>{item.generatedMarkdown ?? 'No markdown was generated.'}</Text>
            </Card>
          </>
        ) : null}
      </ScrollView>
    </Screen>
  );
}

const styles = StyleSheet.create({
  content: { padding: 16, paddingBottom: 40 },
  title: { fontWeight: '800', color: colours.navy, fontSize: 20 },
  meta: { color: colours.muted, marginVertical: 8 },
  markdown: { color: colours.ink, lineHeight: 22, fontSize: 14 },
});
