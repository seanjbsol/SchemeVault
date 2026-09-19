import { useCallback, useEffect, useState } from 'react';
import { Image, Platform, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import * as ImagePicker from 'expo-image-picker';
import { GhostButton } from '@/components/ui';
import { apiBaseUrl, ApiError } from '@/lib/api';
import { endpoints } from '@/lib/endpoints';
import { getToken } from '@/lib/tokenStorage';
import { colours } from '@/lib/theme';
import type { Photo } from '@/lib/types';

type Kind = 'evidence' | 'accidents' | 'equipment';

export function PhotoStrip({
  kind,
  ownerId,
  onError,
}: {
  kind: Kind;
  ownerId: string;
  onError: (message: string) => void;
}) {
  const [photos, setPhotos] = useState<Photo[]>([]);
  const [busy, setBusy] = useState(false);

  const load = useCallback(async () => {
    try {
      const list =
        kind === 'evidence'
          ? await endpoints.evidencePhotos(ownerId)
          : kind === 'accidents'
            ? await endpoints.accidentPhotos(ownerId)
            : await endpoints.equipmentPhotos(ownerId);
      setPhotos(list);
    } catch (err) {
      onError(err instanceof ApiError ? err.message : 'Could not load photos.');
    }
  }, [kind, ownerId, onError]);

  useEffect(() => {
    void load();
  }, [load]);

  async function addPhoto() {
    setBusy(true);
    try {
      const picked = await ImagePicker.launchImageLibraryAsync({
        mediaTypes: ['images'],
        quality: 0.8,
      });
      if (picked.canceled || !picked.assets[0]) {
        return;
      }
      const asset = picked.assets[0];
      const file = {
        uri: asset.uri,
        name: asset.fileName ?? `photo-${Date.now()}.jpg`,
        type: asset.mimeType ?? 'image/jpeg',
      };
      if (kind === 'evidence') {
        await endpoints.uploadEvidencePhoto(ownerId, file);
      } else if (kind === 'accidents') {
        await endpoints.uploadAccidentPhoto(ownerId, file);
      } else {
        await endpoints.uploadEquipmentPhoto(ownerId, file);
      }
      await load();
    } catch (err) {
      onError(err instanceof ApiError ? err.message : 'Could not upload that photo.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <View>
      <Text style={styles.label}>Photos</Text>
      <ScrollView horizontal contentContainerStyle={styles.row}>
        {photos.map((photo) => (
          <View key={photo.id} style={styles.item}>
            <AuthImage photoId={photo.id} />
            <Pressable
              onPress={async () => {
                try {
                  await endpoints.deletePhoto(photo.id);
                  await load();
                } catch (err) {
                  onError(err instanceof ApiError ? err.message : 'Could not remove that photo.');
                }
              }}>
              <Text style={styles.remove}>Remove</Text>
            </Pressable>
          </View>
        ))}
        {photos.length === 0 ? <Text style={styles.empty}>No photos yet.</Text> : null}
      </ScrollView>
      <GhostButton title={busy ? 'Uploading…' : 'Add photo'} onPress={() => void addPhoto()} />
    </View>
  );
}

function AuthImage({ photoId }: { photoId: string }) {
  const [uri, setUri] = useState<string | null>(null);
  const [token, setToken] = useState<string | null>(null);

  useEffect(() => {
    void getToken().then(setToken);
  }, []);

  useEffect(() => {
    if (Platform.OS !== 'web' || !token) {
      return;
    }
    let objectUrl: string | null = null;
    let cancelled = false;
    (async () => {
      const response = await fetch(`${apiBaseUrl}/api/photos/${photoId}/file`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      if (!response.ok || cancelled) {
        return;
      }
      const blob = await response.blob();
      objectUrl = URL.createObjectURL(blob);
      if (!cancelled) {
        setUri(objectUrl);
      }
    })();
    return () => {
      cancelled = true;
      if (objectUrl) {
        URL.revokeObjectURL(objectUrl);
      }
    };
  }, [photoId, token]);

  if (Platform.OS === 'web') {
    if (!uri) {
      return <View style={styles.ph} />;
    }
    return <Image source={{ uri }} style={styles.img} />;
  }

  if (!token) {
    return <View style={styles.ph} />;
  }

  return (
    <Image
      source={{ uri: `${apiBaseUrl}/api/photos/${photoId}/file`, headers: { Authorization: `Bearer ${token}` } }}
      style={styles.img}
    />
  );
}

const styles = StyleSheet.create({
  label: { fontWeight: '800', color: colours.navy, marginBottom: 8, marginTop: 8 },
  row: { gap: 10, paddingBottom: 8, alignItems: 'flex-start' },
  item: { width: 110 },
  img: { width: 110, height: 110, borderRadius: 10, backgroundColor: colours.line },
  ph: { width: 110, height: 110, borderRadius: 10, backgroundColor: colours.line },
  remove: { color: colours.red, fontWeight: '700', marginTop: 6, fontSize: 12 },
  empty: { color: colours.muted, paddingVertical: 12 },
});
