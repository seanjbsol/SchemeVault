import { Pressable, ScrollView, StyleSheet, Text } from 'react-native';
import { router } from 'expo-router';
import { Card, Screen } from '@/components/ui';
import { Paywall } from '@/components/paywall';
import { useAuth } from '@/lib/auth';
import { colours } from '@/lib/theme';

export default function WorkHubScreen() {
  const { isPro, entitlements } = useAuth();

  return (
    <Screen>
      <ScrollView contentContainerStyle={styles.content}>
        <Text style={styles.lede}>
          Guided questionnaires, accident reporting and the plant register. These are SchemeVault Pro tools. They produce
          working drafts and records for your organisation — not official scheme certificates.
        </Text>
        {!isPro ? <Paywall feature="H&S tools" /> : null}
        <Tile
          title="Guided questionnaires"
          body="Answer plain-English questions and generate a CHAS / Constructionline / SafeContractor / Avetta-style pack draft (markdown and PDF)."
          onPress={() => router.push('/work/questionnaires')}
        />
        <Tile
          title="Accidents and lost hours"
          body="Log incidents for this organisation and keep a lost-hours total. This is an internal record, not a RIDDOR filing service."
          onPress={() => router.push('/work/accidents')}
        />
        <Tile
          title="Equipment register"
          body="Calibration and service due dates with an overdue flag. Add, remove, and attach photos."
          onPress={() => router.push('/work/equipment')}
        />
        <Text style={styles.plan}>Current plan: {entitlements?.plan ?? 'Loading…'}</Text>
      </ScrollView>
    </Screen>
  );
}

function Tile({ title, body, onPress }: { title: string; body: string; onPress: () => void }) {
  return (
    <Pressable onPress={onPress}>
      <Card>
        <Text style={styles.title}>{title}</Text>
        <Text style={styles.body}>{body}</Text>
        <Text style={styles.link}>Open</Text>
      </Card>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  content: { padding: 16, paddingBottom: 40 },
  lede: { color: colours.muted, marginBottom: 16, lineHeight: 20 },
  title: { fontWeight: '800', color: colours.navy, fontSize: 16 },
  body: { color: colours.muted, marginTop: 6, lineHeight: 20 },
  link: { color: colours.navyMid, fontWeight: '700', marginTop: 10 },
  plan: { color: colours.muted, marginTop: 8 },
});
