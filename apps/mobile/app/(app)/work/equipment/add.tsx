import { router } from 'expo-router';
import { useState } from 'react';
import { KeyboardAvoidingView, Platform, ScrollView, StyleSheet, Text } from 'react-native';
import { ErrorBanner, Field, PrimaryButton, Screen } from '@/components/ui';
import { ApiError } from '@/lib/api';
import { endpoints } from '@/lib/endpoints';
import { colours } from '@/lib/theme';

export default function AddEquipmentScreen() {
  const [name, setName] = useState('');
  const [category, setCategory] = useState('Plant');
  const [serialNumber, setSerialNumber] = useState('');
  const [calibrationDueOn, setCalibrationDueOn] = useState('');
  const [serviceDueOn, setServiceDueOn] = useState('');
  const [notes, setNotes] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  async function submit() {
    setError(null);
    if (name.trim().length < 2) {
      setError('Give this item a name.');
      return;
    }
    setSaving(true);
    try {
      const created = await endpoints.createEquipment({
        name: name.trim(),
        category: category.trim() || 'Plant',
        serialNumber: serialNumber.trim() || undefined,
        calibrationDueOn: calibrationDueOn.trim() ? new Date(calibrationDueOn.trim()).toISOString() : null,
        serviceDueOn: serviceDueOn.trim() ? new Date(serviceDueOn.trim()).toISOString() : null,
        notes: notes.trim() || undefined,
      });
      router.replace(`/work/equipment/${created.id}`);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not save equipment.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <Screen>
      <KeyboardAvoidingView behavior={Platform.OS === 'ios' ? 'padding' : undefined} style={{ flex: 1 }}>
        <ScrollView contentContainerStyle={styles.content} keyboardShouldPersistTaps="handled">
          <Text style={styles.lede}>Due dates are calendar dates. Leave a field blank if it does not apply.</Text>
          <ErrorBanner message={error} />
          <Field label="Name" value={name} onChangeText={setName} autoCapitalize="sentences" />
          <Field
            label="Category"
            value={category}
            onChangeText={setCategory}
            placeholder="Plant, MEWP, Lifting, Electrical, PPE, FirstAid"
            autoCapitalize="words"
          />
          <Field label="Serial number" value={serialNumber} onChangeText={setSerialNumber} autoCapitalize="characters" />
          <Field label="Calibration due (YYYY-MM-DD)" value={calibrationDueOn} onChangeText={setCalibrationDueOn} />
          <Field label="Service due (YYYY-MM-DD)" value={serviceDueOn} onChangeText={setServiceDueOn} />
          <Field label="Notes" value={notes} onChangeText={setNotes} multiline autoCapitalize="sentences" />
          <PrimaryButton title="Save to register" onPress={() => void submit()} loading={saving} />
        </ScrollView>
      </KeyboardAvoidingView>
    </Screen>
  );
}

const styles = StyleSheet.create({
  content: { padding: 16, paddingBottom: 40 },
  lede: { color: colours.muted, marginBottom: 12, lineHeight: 20 },
});
