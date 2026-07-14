import React, { useEffect, useMemo, useState } from 'react';
import { View, Text, TouchableOpacity, StyleSheet, useWindowDimensions, ActivityIndicator } from 'react-native';
import { useRoute, type RouteProp } from '@react-navigation/native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { api } from '@/api/client';
import { CandleChart } from '@/components/CandleChart';
import { DealtChart, ExitReason, TradeDirection } from '@/api/types';
import { computeR, openR, stopHit } from '@/lib/priceAction';
import { isValidStopSide, revalidatePendingStop } from '@/lib/stopPlacement';
import { RISK_FRACTION } from '@/config';
import type { RootStackParamList } from '@/navigation';

type Position = {
  direction: TradeDirection;
  entryBarIndex: number;
  entryPrice: number;
  originalStop: number;
  liveStop: number;
};

// One chart, revealed bar-by-bar (no rewind). Open one position at a time;
// the original stop defines R; the live stop is enforced intra-bar.
export function TradeScreen() {
  const route = useRoute<RouteProp<RootStackParamList, 'Trade'>>();
  const { journeyId } = route.params;
  const { width } = useWindowDimensions();
  const insets = useSafeAreaInsets();
  // The chart fills whatever space is left between the stats bar and the
  // controls, measured via onLayout. Hardcoding a height clipped the bottom
  // controls off-screen on shorter devices (e.g. iOS Display Zoom).
  const [chartH, setChartH] = useState(360);

  const [chart, setChart] = useState<DealtChart | null>(null);
  const [revealed, setRevealed] = useState(1); // bars shown (never decreases)
  const [position, setPosition] = useState<Position | null>(null);
  const [showMagnets, setShowMagnets] = useState(false);
  const [lastR, setLastR] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);
  // Arm-then-place: pick a direction, then tap the chart to place the stop.
  // ENTER only exists once a valid stop is placed — entry without a stop is
  // impossible by construction.
  const [arming, setArming] = useState<TradeDirection | null>(null);
  const [pendingStop, setPendingStop] = useState<number | null>(null);
  const [hint, setHint] = useState<string | null>(null);

  const hintTimer = React.useRef<ReturnType<typeof setTimeout> | null>(null);
  const flashHint = (msg: string) => {
    setHint(msg);
    if (hintTimer.current) clearTimeout(hintTimer.current);
    hintTimer.current = setTimeout(() => setHint(null), 2500);
  };
  useEffect(() => () => { if (hintTimer.current) clearTimeout(hintTimer.current); }, []);

  const deal = async () => {
    setError(null);
    setPosition(null);
    setLastR(null);
    setArming(null);
    setPendingStop(null);
    setHint(null);
    try {
      const c = await api.dealChart();
      setChart(c);
      setRevealed(1);
    } catch (e) {
      setError((e as Error).message);
    }
  };

  useEffect(() => { deal(); }, []);

  const bars = chart?.bars ?? [];
  const visible = useMemo(() => bars.slice(0, revealed), [bars, revealed]);
  const currentBar = visible[visible.length - 1];
  const price = currentBar?.close ?? 0;
  const atLastBar = revealed >= bars.length;

  // Advance one bar; enforce the live stop intra-bar on the newly revealed bar.
  const nextBar = () => {
    if (atLastBar || !chart) return;
    const nextIdx = revealed; // index of the bar about to be revealed
    const nb = bars[nextIdx];
    if (position && stopHit(position.direction, position.liveStop, nb)) {
      closeTrade(position.liveStop, nextIdx, ExitReason.Stop);
    }
    if (arming != null && pendingStop != null) {
      const kept = revalidatePendingStop(arming, pendingStop, nb.close);
      if (kept == null) {
        setPendingStop(null);
        flashHint('Price crossed your stop level — place it again');
      }
    }
    setRevealed((r) => r + 1);
  };

  const arm = (direction: TradeDirection) => {
    if (position) return;
    setArming(direction);
    setPendingStop(null);
  };

  const cancelArming = () => {
    setArming(null);
    setPendingStop(null);
    setHint(null);
  };

  const onPriceTap = (tapped: number) => {
    if (arming == null || position || !currentBar) return;
    if (isValidStopSide(arming, tapped, price)) {
      setPendingStop(tapped);
    } else {
      flashHint(arming === TradeDirection.Long
        ? 'Long stop must be below price'
        : 'Short stop must be above price');
    }
  };

  const confirmEntry = () => {
    if (arming == null || pendingStop == null || position || !currentBar) return;
    setPosition({
      direction: arming,
      entryBarIndex: revealed - 1,
      entryPrice: price,
      originalStop: pendingStop,
      liveStop: pendingStop,
    });
    setArming(null);
    setPendingStop(null);
    setHint(null);
  };

  const moveStopToBreakeven = () => {
    if (!position) return;
    setPosition({ ...position, liveStop: position.entryPrice });
  };

  // Nudge the live stop toward (tighter) or away from (looser) price. Never
  // changes R — that's anchored to the original stop.
  const nudgeLiveStop = (tighter: boolean) => {
    if (!position) return;
    const step = position.entryPrice * 0.001;
    const sign = position.direction === TradeDirection.Long ? 1 : -1;
    setPosition({ ...position, liveStop: position.liveStop + sign * (tighter ? step : -step) });
  };

  const closeTrade = async (exitPrice: number, exitBarIndex: number, reason: ExitReason) => {
    if (!position) return;
    const r = computeR(position.direction, position.entryPrice, position.originalStop, exitPrice);
    setLastR(r);
    const pos = position;
    setPosition(null);
    try {
      const res = await api.submitTrade({
        journeyId,
        chartId: chart!.chartId,
        direction: pos.direction,
        entryBarIndex: pos.entryBarIndex,
        entryPrice: pos.entryPrice,
        originalStop: pos.originalStop,
        liveStop: pos.liveStop,
        exitBarIndex,
        exitPrice,
        exitReason: reason,
        riskFraction: RISK_FRACTION,
      });
      setLastR(res.resultR); // server-authoritative
    } catch (e) {
      setError((e as Error).message);
    }
  };

  if (!chart) {
    return (
      <View style={styles.center}>
        {error ? <Text style={styles.error}>{error}</Text> : <ActivityIndicator color="#888" />}
      </View>
    );
  }

  const live = position ? openR(position.direction, position.entryPrice, position.originalStop, price) : null;

  return (
    <View style={styles.container}>
      <View style={styles.statsBar}>
        <Text style={styles.stat}>Bar {revealed}/{bars.length}</Text>
        {live != null && (
          <Text style={[styles.stat, { color: live >= 0 ? '#26a69a' : '#ef5350' }]}>
            Open {live >= 0 ? '+' : ''}{live.toFixed(2)}R
          </Text>
        )}
        {lastR != null && !position && (
          <Text style={[styles.stat, { color: lastR >= 0 ? '#26a69a' : '#ef5350' }]}>
            Closed {lastR >= 0 ? '+' : ''}{lastR.toFixed(2)}R
          </Text>
        )}
        <TouchableOpacity onPress={() => setShowMagnets((s) => !s)}>
          <Text style={[styles.stat, { color: showMagnets ? '#e0b30a' : '#789' }]}>Magnets</Text>
        </TouchableOpacity>
      </View>

      <View
        style={styles.chartArea}
        onLayout={(e) => setChartH(e.nativeEvent.layout.height)}
      >
        <CandleChart
          bars={visible}
          width={width}
          height={chartH}
          showMagnets={showMagnets}
          magnets={chart.magnets}
          entryPrice={position?.entryPrice ?? null}
          liveStop={position?.liveStop ?? null}
          pendingStop={pendingStop}
          onPriceTap={onPriceTap}
        />
      </View>

      <View style={[styles.controls, { paddingBottom: 12 + insets.bottom }]}>
        {!position ? (
          arming == null ? (
            <View style={styles.row}>
              <Btn label="LONG" color="#26a69a" disabled={atLastBar} onPress={() => arm(TradeDirection.Long)} />
              <Btn label="SHORT" color="#ef5350" disabled={atLastBar} onPress={() => arm(TradeDirection.Short)} />
            </View>
          ) : (
            <>
              <Text style={styles.hint}>
                {hint ?? (pendingStop == null
                  ? 'Tap the chart to place your stop'
                  : `Stop ${pendingStop.toFixed(2)} · ${(Math.abs(price - pendingStop) / price * 100).toFixed(2)}% ${arming === TradeDirection.Long ? 'below' : 'above'}`)}
              </Text>
              <View style={styles.row}>
                {pendingStop != null && (
                  <Btn
                    label={arming === TradeDirection.Long ? 'ENTER LONG' : 'ENTER SHORT'}
                    color={arming === TradeDirection.Long ? '#26a69a' : '#ef5350'}
                    onPress={confirmEntry}
                  />
                )}
                <Btn label="CANCEL" color="#455a64" onPress={cancelArming} />
              </View>
            </>
          )
        ) : (
          <>
            <View style={styles.stopRow}>
              <Btn label="STOP –" color="#263238" onPress={() => nudgeLiveStop(true)} />
              <Btn label="B/E" color="#455a64" onPress={moveStopToBreakeven} />
              <Btn label="STOP +" color="#263238" onPress={() => nudgeLiveStop(false)} />
            </View>
            <View style={styles.row}>
              <Btn label="EXIT" color="#1f6feb" onPress={() => closeTrade(price, revealed - 1, ExitReason.Manual)} wide />
            </View>
          </>
        )}
        <View style={styles.row}>
          <Btn
            label={atLastBar ? 'NEW CHART' : 'NEXT BAR ▸'}
            color="#263238"
            onPress={atLastBar ? deal : nextBar}
            wide
          />
        </View>
      </View>
    </View>
  );
}

function Btn({ label, color, onPress, disabled, wide }: {
  label: string; color: string; onPress: () => void; disabled?: boolean; wide?: boolean;
}) {
  return (
    <TouchableOpacity
      style={[styles.btn, { backgroundColor: color, opacity: disabled ? 0.4 : 1, flex: wide ? 1 : 0.5 }]}
      onPress={onPress}
      disabled={disabled}
    >
      <Text style={styles.btnText}>{label}</Text>
    </TouchableOpacity>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#0B0E11' },
  center: { flex: 1, backgroundColor: '#0B0E11', alignItems: 'center', justifyContent: 'center' },
  error: { color: '#ef5350', padding: 16, textAlign: 'center' },
  statsBar: { flexDirection: 'row', justifyContent: 'space-between', padding: 12, alignItems: 'center' },
  chartArea: { flex: 1, overflow: 'hidden' },
  stat: { color: '#cfd8dc', fontSize: 13, fontWeight: '600' },
  controls: { padding: 12, gap: 10 },
  row: { flexDirection: 'row', gap: 10 },
  stopRow: { flexDirection: 'row', gap: 10, alignItems: 'center', justifyContent: 'center' },
  hint: { color: '#e0b30a', fontSize: 13, fontWeight: '600', textAlign: 'center' },
  btn: { paddingVertical: 16, borderRadius: 12, alignItems: 'center' },
  btnText: { color: '#fff', fontSize: 15, fontWeight: '700' },
});
