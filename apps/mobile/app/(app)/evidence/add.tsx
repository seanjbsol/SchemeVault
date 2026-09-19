import { router } from 'expo-router';
import { useState } from 'react';
import { KeyboardAvoidingView, Platform, ScrollView, StyleSheet, Text } from 'react-native';
import { ErrorBanner, Field, PrimaryButton, Screen } from '@/components/ui';
import { ApiError } from '@/lib/api';
import { endpoints } from '@/lib/endpoints';
import { colours } from '@/lib/theme';

export default function AddEvidenceScreen() {
  const [title, setTitle] = useState('');
  const [category, setCategory] = useState('Insurance');
  const [notes, setNotes] = useState('');
  const [expiresOn, setExpiresOn] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function onSubmit() {
    setError(null);
    if (title.trim().length < 2) {
      setError('Give this item a title.');
      return;
    }
    setLoading(true);
    try {
      await endpoints.createEvidence({
        title: title.trim(),
        category: category.trim() || 'General',
        notes: notes.trim() || undefined,
        expiresOn: expiresOn.trim() ? new Date(expiresOn.trim()).toISOString() : null,
      });
      router.back();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not save evidence.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <Screen>
      <KeyboardAvoidingView behavior={Platform.OS === 'ios' ? 'padding' : undefined} style={{ flex: 1 }}>
        <ScrollView contentContainerStyle={styles.content} keyboardShouldPersistTaps="handled">
          <Text style={styles.lede}>
            Metadata is stored against your organisation only. File upload lives on POST /api/evidence/&#123;id&#125;/file
            (local disk stub on the API).
          </Text>
          <ErrorBanner message={error} />
          <Field label="Title" value={title} onChangeText={setTitle} autoCapitalize="sentences" />
          <Field
            label="Category"
            value={category}
            onChangeText={setCategory}
            autoCapitalize="words"
            placeholder="Insurance, Policy, RAMS, Training"
          />
          <Field
            label="Expiry (YYYY-MM-DD)"
            value={expiresOn}
            onChangeText={setExpiresOn}
            placeholder="2027-03-31"
          />
          <Field label="Notes" value={notes} onChangeText={setNotes} multiline autoCapitalize="sentences" />
          <PrimaryButton title="Save to vault" onPress={onSubmit} loading={loading} />
        </ScrollView>
      </KeyboardAvoidingView>
    </Screen>
  );
}

const styles = StyleSheet.create({
  content: { padding: 16, paddingBottom: 40 },
  lede: { color: colours.muted, marginBottom: 16, lineHeight: 20 },
});
