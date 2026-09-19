import { useCallback, useState } from 'react';
import { KeyboardAvoidingView, Platform, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { router, useLocalSearchParams } from 'expo-router';
import { useFocusEffect } from 'expo-router';
import { ErrorBanner, Field, PrimaryButton, Screen } from '@/components/ui';
import { Paywall } from '@/components/paywall';
import { ApiError } from '@/lib/api';
import { useAuth } from '@/lib/auth';
import { endpoints } from '@/lib/endpoints';
import { colours } from '@/lib/theme';
import type { QuestionnaireTemplate } from '@/lib/types';

export default function QuestionnaireFormScreen() {
  const { isPro } = useAuth();
  const { code } = useLocalSearchParams<{ code: string }>();
  const [template, setTemplate] = useState<QuestionnaireTemplate | null>(null);
  const [answers, setAnswers] = useState<Record<string, string>>({});
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const load = useCallback(async () => {
    if (!isPro || !code) {
      return;
    }
    setError(null);
    try {
      const data = await endpoints.questionnaire(String(code));
      setTemplate(data);
      setAnswers((current) => {
        const next = { ...current };
        for (const question of data.questions) {
          if (next[question.id] === undefined) {
            next[question.id] = '';
          }
        }
        return next;
      });
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load that questionnaire.');
    }
  }, [code, isPro]);

  useFocusEffect(
    useCallback(() => {
      void load();
    }, [load]),
  );

  async function submit() {
    if (!template) {
      return;
    }
    setError(null);
    setSaving(true);
    try {
      const payload: Record<string, string> = {};
      for (const [key, value] of Object.entries(answers)) {
        if (value.trim()) {
          payload[key] = value.trim();
        }
      }
      const saved = await endpoints.saveQuestionnaire(template.schemeCode, payload, true);
      router.replace(`/work/questionnaires/response/${saved.id}`);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not generate the pack.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <Screen>
      <KeyboardAvoidingView behavior={Platform.OS === 'ios' ? 'padding' : undefined} style={{ flex: 1 }}>
        <ScrollView contentContainerStyle={styles.content} keyboardShouldPersistTaps="handled">
          {!isPro ? <Paywall feature="guided questionnaires" /> : null}
          <ErrorBanner message={error} />
          {template ? (
            <>
              <Text style={styles.title}>{template.title}</Text>
              <Text style={styles.lede}>{template.introduction}</Text>
              {template.questions.map((question) => (
                <View key={question.id} style={styles.block}>
                  {question.kind === 'yesNo' ? (
                    <>
                      <Text style={styles.label}>
                        {question.prompt}
                        {question.required ? ' *' : ''}
                      </Text>
                      <Text style={styles.help}>{question.help}</Text>
                      <View style={styles.yesNo}>
                        {['Yes', 'No'].map((option) => {
                          const selected = answers[question.id]?.toLowerCase().startsWith(option.toLowerCase());
                          return (
                            <Pressable
                              key={option}
                              onPress={() => setAnswers((a) => ({ ...a, [question.id]: option }))}
                              style={[styles.chip, selected && styles.chipOn]}>
                              <Text style={[styles.chipText, selected && styles.chipTextOn]}>{option}</Text>
                            </Pressable>
                          );
                        })}
                      </View>
                    </>
                  ) : (
                    <Field
                      label={`${question.prompt}${question.required ? ' *' : ''}`}
                      value={answers[question.id] ?? ''}
                      onChangeText={(text) => setAnswers((a) => ({ ...a, [question.id]: text }))}
                      placeholder={question.help}
                      multiline={question.kind === 'longText'}
                      keyboardType={question.kind === 'number' ? 'numeric' : 'default'}
                      autoCapitalize="sentences"
                    />
                  )}
                </View>
              ))}
              <PrimaryButton title="Generate pack draft" onPress={() => void submit()} loading={saving} />
            </>
          ) : null}
        </ScrollView>
      </KeyboardAvoidingView>
    </Screen>
  );
}

const styles = StyleSheet.create({
  content: { padding: 16, paddingBottom: 48 },
  title: { fontWeight: '800', color: colours.navy, fontSize: 20, marginBottom: 8 },
  lede: { color: colours.muted, marginBottom: 16, lineHeight: 20 },
  block: { marginBottom: 8 },
  label: { fontSize: 13, fontWeight: '600', color: colours.navyMid, marginBottom: 6 },
  help: { color: colours.muted, fontSize: 13, marginBottom: 8, lineHeight: 18 },
  yesNo: { flexDirection: 'row', gap: 8, marginBottom: 12 },
  chip: {
    borderWidth: 1,
    borderColor: colours.line,
    borderRadius: 20,
    paddingHorizontal: 16,
    paddingVertical: 8,
    backgroundColor: colours.white,
  },
  chipOn: { backgroundColor: colours.navy, borderColor: colours.navy },
  chipText: { fontWeight: '700', color: colours.navy },
  chipTextOn: { color: colours.white },
});
