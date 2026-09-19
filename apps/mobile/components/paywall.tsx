import { router } from 'expo-router';
import { StyleSheet, Text } from 'react-native';
import { Card, PrimaryButton } from '@/components/ui';
import { useAuth } from '@/lib/auth';
import { colours } from '@/lib/theme';

export function Paywall({ feature }: { feature: string }) {
  const { entitlements } = useAuth();
  const plan = entitlements?.plan ?? 'Starter';

  return (
    <Card>
      <Text style={styles.title}>SchemeVault Pro is required</Text>
      <Text style={styles.body}>
        {feature} sits on the Pro plan. Your organisation is currently on {plan}. Starter still covers the evidence vault,
        schemes and renewals. Pro adds guided questionnaires, multi-scheme pack export, accident reporting and the
        equipment register.
      </Text>
      <Text style={styles.note}>
        This is a product entitlement, not a legal certification. Upgrade from Settings (Owner or Admin) via the Qck
        billing portal — SchemeVault does not take card details itself.
      </Text>
      <PrimaryButton title="Go to Settings to upgrade" onPress={() => router.push('/(app)/settings')} />
    </Card>
  );
}

const styles = StyleSheet.create({
  title: { fontWeight: '800', color: colours.navy, fontSize: 16, marginBottom: 8 },
  body: { color: colours.ink, lineHeight: 20 },
  note: { color: colours.muted, lineHeight: 20, marginTop: 10, marginBottom: 4 },
});
