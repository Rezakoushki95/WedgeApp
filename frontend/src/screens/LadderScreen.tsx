import React, { useCallback, useState } from 'react';
import { View, Text, FlatList, StyleSheet, RefreshControl } from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import { api } from '@/api/client';
import { EdgeState, JourneyStats } from '@/api/types';

// Ranks the user's journeys by Edge and gives an honest keep/cut verdict —
// the strategy lab's whole point: which patterns you can actually read.
const VERDICT: Record<EdgeState, { text: string; color: string }> = {
  [EdgeState.Calibrating]: { text: 'needs more trades', color: '#789' },
  [EdgeState.Noise]: { text: 'CUT — no edge', color: '#ef5350' },
  [EdgeState.Promising]: { text: 'KEEP — promising', color: '#26a69a' },
  [EdgeState.Established]: { text: 'KEEP — established', color: '#26a69a' },
};

export function LadderScreen() {
  const [rows, setRows] = useState<JourneyStats[]>([]);
  const [loading, setLoading] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const journeys = await api.listJourneys(true);
      // Matured edges first (desc), then calibrating ones.
      journeys.sort((a, b) => {
        const am = a.edgeState !== EdgeState.Calibrating;
        const bm = b.edgeState !== EdgeState.Calibrating;
        if (am !== bm) return am ? -1 : 1;
        return b.edge - a.edge;
      });
      setRows(journeys);
    } finally {
      setLoading(false);
    }
  }, []);

  useFocusEffect(useCallback(() => { load(); }, [load]));

  return (
    <View style={styles.container}>
      <Text style={styles.subtitle}>Your strategies ranked by Edge</Text>
      <FlatList
        data={rows}
        keyExtractor={(j) => String(j.journeyId)}
        refreshControl={<RefreshControl refreshing={loading} onRefresh={load} tintColor="#888" />}
        ListEmptyComponent={!loading ? <Text style={styles.empty}>No journeys yet.</Text> : null}
        renderItem={({ item, index }) => {
          const v = VERDICT[item.edgeState];
          return (
            <View style={styles.row}>
              <Text style={styles.rank}>{index + 1}</Text>
              <View style={styles.mid}>
                <Text style={styles.name}>{item.name}</Text>
                <Text style={styles.meta}>
                  {item.edge >= 0 ? '+' : ''}{item.edge.toFixed(2)}R edge · {item.tradeCount} trades
                  {item.patternTag ? ` · ${item.patternTag}` : ''}
                </Text>
              </View>
              <Text style={[styles.verdict, { color: v.color }]}>{v.text}</Text>
            </View>
          );
        }}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#0B0E11', padding: 16 },
  subtitle: { color: '#789', fontSize: 14, marginBottom: 12 },
  empty: { color: '#789', marginTop: 40, textAlign: 'center' },
  row: { flexDirection: 'row', alignItems: 'center', backgroundColor: '#151A1F', borderRadius: 12, padding: 14, marginBottom: 10 },
  rank: { color: '#cfd8dc', fontSize: 18, fontWeight: '700', width: 28 },
  mid: { flex: 1 },
  name: { color: '#fff', fontSize: 16, fontWeight: '600' },
  meta: { color: '#789', fontSize: 12, marginTop: 4 },
  verdict: { fontSize: 12, fontWeight: '700', maxWidth: 110, textAlign: 'right' },
});
