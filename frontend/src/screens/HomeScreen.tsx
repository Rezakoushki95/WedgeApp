import React, { useCallback, useState } from 'react';
import { View, Text, FlatList, TouchableOpacity, StyleSheet, RefreshControl } from 'react-native';
import { useFocusEffect, useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { api } from '@/api/client';
import { EdgeState, JourneyStats } from '@/api/types';
import type { RootStackParamList } from '@/navigation';

const EDGE_LABEL: Record<EdgeState, string> = {
  [EdgeState.Calibrating]: 'CALIBRATING',
  [EdgeState.Noise]: 'NOISE — could be luck',
  [EdgeState.Promising]: 'PROMISING',
  [EdgeState.Established]: 'ESTABLISHED EDGE',
};

export function HomeScreen() {
  const nav = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const [journeys, setJourneys] = useState<JourneyStats[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setJourneys(await api.listJourneys());
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setLoading(false);
    }
  }, []);

  useFocusEffect(useCallback(() => { load(); }, [load]));

  const createAndPlay = async () => {
    try {
      const j = await api.createJourney({ name: 'New Journey', patternTag: '' });
      nav.navigate('Trade', { journeyId: j.id, journeyName: j.name });
    } catch (e) {
      setError((e as Error).message);
    }
  };

  return (
    <View style={styles.container}>
      <Text style={styles.title}>Journeys</Text>
      {error && <Text style={styles.error}>{error}</Text>}
      <FlatList
        data={journeys}
        keyExtractor={(j) => String(j.journeyId)}
        refreshControl={<RefreshControl refreshing={loading} onRefresh={load} tintColor="#888" />}
        ListEmptyComponent={
          !loading ? <Text style={styles.empty}>No journeys yet. Start one below.</Text> : null
        }
        renderItem={({ item }) => (
          <TouchableOpacity
            style={styles.card}
            onPress={() => nav.navigate('Trade', { journeyId: item.journeyId, journeyName: item.name })}
          >
            <View style={styles.cardRow}>
              <Text style={styles.cardName}>{item.name}</Text>
              <Text style={styles.bankroll}>${item.bankroll.toLocaleString(undefined, { maximumFractionDigits: 0 })}</Text>
            </View>
            <View style={styles.cardRow}>
              <Text style={styles.edge}>{EDGE_LABEL[item.edgeState]}</Text>
              <Text style={styles.meta}>
                {item.expectancy >= 0 ? '+' : ''}{item.expectancy.toFixed(2)}R · {item.tradeCount} trades
              </Text>
            </View>
          </TouchableOpacity>
        )}
      />
      <TouchableOpacity style={styles.fab} onPress={createAndPlay}>
        <Text style={styles.fabText}>+ New Journey</Text>
      </TouchableOpacity>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#0B0E11', padding: 16 },
  title: { color: '#fff', fontSize: 28, fontWeight: '700', marginBottom: 12 },
  error: { color: '#ef5350', marginBottom: 8 },
  empty: { color: '#789', marginTop: 40, textAlign: 'center' },
  card: { backgroundColor: '#151A1F', borderRadius: 12, padding: 16, marginBottom: 12 },
  cardRow: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' },
  cardName: { color: '#fff', fontSize: 18, fontWeight: '600' },
  bankroll: { color: '#26a69a', fontSize: 18, fontWeight: '700' },
  edge: { color: '#e0b30a', fontSize: 12, fontWeight: '600', marginTop: 6 },
  meta: { color: '#789', fontSize: 12, marginTop: 6 },
  fab: { backgroundColor: '#1f6feb', borderRadius: 12, padding: 16, alignItems: 'center' },
  fabText: { color: '#fff', fontSize: 16, fontWeight: '700' },
});
