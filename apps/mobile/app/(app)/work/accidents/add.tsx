import { router } from 'expo-router';
import { useState } from 'react';
import { KeyboardAvoidingView, Platform, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { ErrorBanner, Field, PrimaryButton, Screen } from '@/components/ui';
import { ApiError } from '@/lib/api';
import { endpoints } from '@/lib/endpoints';
import { colours } from '@/lib/theme';

const severities = ['NearMiss', 'MinorInjury', 'LostTime', 'MajorInjury', 'DangerousOccurrence', 'Other'];

export default function AddAccidentScreen() {
  const [occurredOn, setOccurredOn] = useState(new Date().toISOString().slice(0, 10));
  const [location, setLocation] = useState('');
  const [severity, setSeverity] = useState('NearMiss');
  const [description, setDescription] = useState('');
  const [injuredPerson, setInjuredPerson] = useState('');
  const [immediateAction, setImmediateAction] = useState('');
  const [lostHours, setLostHours] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  async function submit() {
    setError(null);
    if (location.trim().length < 2 || description.trim().length < 4) {
      setError('Add a location and a short description.');
      return;
    }
    setSaving(true);
    try {
      const created = await endpoints.createAccident({
        occurredOn: new Date(occurredOn).toISOString(),
        location: location.trim(),
        severity,
        status: 'Open',
        description: description.trim(),
        injuredPerson: injuredPerson.trim() || undefined,
        immediateAction: immediateAction.trim() || undefined,
        lostHours: lostHours.trim() ? Number(lostHours) : 0,
      });
      router.replace(`/work/accidents/${created.id}`);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not save the incident.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <Screen>
      <KeyboardAvoidingView behavior={Platform.OS === 'ios' ? 'padding' : undefined} style={{ flex: 1 }}>
        <ScrollView contentContainerStyle={styles.content} keyboardShouldPersistTaps="handled">
          <Text style={styles.lede}>
            Keep personal data to a minimum. Initials are enough. This record stays on your tenant; it is not filed with
            the HSE.
          </Text>
          <ErrorBanner message={error} />
          <Field label="When (YYYY-MM-DD)" value={occurredOn} onChangeText={setOccurredOn} />
          <Field label="Where" value={location} onChangeText={setLocation} autoCapitalize="sentences" />
          <Text style={styles.label}>Severity</Text>
          <View style={styles.chips}>
            {severities.map((option) => (
              <Pressable
                key={option}
                onPress={() => setSeverity(option)}
                style={[styles.chip, severity === option && styles.chipOn]}>
                <Text style={[styles.chipText, severity === option && styles.chipTextOn]}>{option}</Text>
              </Pressable>
            ))}
          </View>
          <Field label="What happened" value={description} onChangeText={setDescription} multiline autoCapitalize="sentences" />
          <Field
            label="Injured person (optional, initials)"
            value={injuredPerson}
            onChangeText={setInjuredPerson}
            autoCapitalize="characters"
          />
          <Field
            label="Immediate action"
            value={immediateAction}
            onChangeText={setImmediateAction}
            multiline
            autoCapitalize="sentences"
          />
          <Field label="Lost hours (optional)" value={lostHours} onChangeText={setLostHours} keyboardType="decimal-pad" />
          <PrimaryButton title="Save incident" onPress={() => void submit()} loading={saving} />
        </ScrollView>
      </KeyboardAvoidingView>
    </Screen>
  );
}

const styles = StyleSheet.create({
  content: { padding: 16, paddingBottom: 48 },
  lede: { color: colours.muted, marginBottom: 12, lineHeight: 20 },
  label: { fontSize: 13, fontWeight: '600', color: colours.navyMid, marginBottom: 6 },
  chips: { flexDirection: 'row', flexWrap: 'wrap', gap: 8, marginBottom: 12 },
  chip: {
    borderWidth: 1,
    borderColor: colours.line,
    borderRadius: 16,
    paddingHorizontal: 10,
    paddingVertical: 6,
  },
  chipOn: { backgroundColor: colours.navy, borderColor: colours.navy },
  chipText: { fontWeight: '700', color: colours.navy, fontSize: 12 },
  chipTextOn: { color: colours.white },
});
