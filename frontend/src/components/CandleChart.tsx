import React, { useMemo } from 'react';
import { View, Pressable } from 'react-native';
import { Canvas, Path, Rect, Line, vec } from '@shopify/react-native-skia';
import { Bar, MagnetLevels } from '@/api/types';
import { ema } from '@/lib/priceAction';

// Classic black & white candles on a white chart: up = white body, down =
// black body, every candle has a black outline.
const BG = '#FFFFFF';
const UP_FILL = '#FFFFFF';
const DOWN_FILL = '#000000';
const BORDER = '#000000';
const WICK = '#000000';
const EMA_COLOR = '#1f6feb';
const MAGNET = 'rgba(0,0,0,0.22)';
const STOP_COLOR = '#e53935';
const ENTRY_COLOR = '#1565c0';

interface Props {
  bars: Bar[]; // revealed bars only (caller slices to currentIndex)
  width: number;
  height: number;
  emaPeriod?: number;
  showMagnets?: boolean;
  magnets?: MagnetLevels | null;
  entryPrice?: number | null;
  liveStop?: number | null;
  pendingStop?: number | null; // placed-but-unentered stop (Armed/Placed state)
  onPriceTap?: (price: number) => void; // tap on the chart -> price at that y
}

// A naked candlestick chart (candles + optional EMA + magnet levels), rendered
// with Skia. Reveal/no-rewind is the caller's job — pass only revealed bars.
export function CandleChart({
  bars,
  width,
  height,
  emaPeriod = 9,
  showMagnets = false,
  magnets,
  entryPrice,
  liveStop,
  pendingStop,
  onPriceTap,
}: Props) {
  const layout = useMemo(() => {
    if (bars.length === 0) return null;

    const magnetLevels = showMagnets && magnets ? collectMagnets(magnets) : [];
    let hi = Math.max(...bars.map((b) => b.high), ...magnetLevels);
    let lo = Math.min(...bars.map((b) => b.low), ...magnetLevels);
    if (entryPrice != null) { hi = Math.max(hi, entryPrice); lo = Math.min(lo, entryPrice); }
    if (liveStop != null) { hi = Math.max(hi, liveStop); lo = Math.min(lo, liveStop); }
    if (pendingStop != null) { hi = Math.max(hi, pendingStop); lo = Math.min(lo, pendingStop); }

    const pad = (hi - lo) * 0.08 || 1;
    hi += pad;
    lo -= pad;
    const range = hi - lo || 1;

    const padX = 8;
    const innerW = width - padX * 2;
    const slot = innerW / Math.max(bars.length, 1);
    const bodyW = Math.max(slot * 0.6, 1);

    const y = (price: number) => ((hi - price) / range) * height;
    const xCenter = (i: number) => padX + slot * i + slot / 2;

    return { hi, lo, range, slot, bodyW, y, xCenter, magnetLevels };
  }, [bars, width, height, showMagnets, magnets, entryPrice, liveStop, pendingStop]);

  if (!layout) return <View style={{ width, height }} />;

  // Build the EMA as an SVG path string. Passing a string to <Path path=...>
  // lets Skia construct it internally (works on web without touching the Skia
  // global before CanvasKit is ready).
  const emaVals = ema(bars, emaPeriod);
  const emaPath = emaVals
    .map((v, i) => `${i === 0 ? 'M' : 'L'}${layout.xCenter(i).toFixed(2)},${layout.y(v).toFixed(2)}`)
    .join(' ');

  const handlePress = (e: { nativeEvent: { locationY: number } }) => {
    if (!onPriceTap || !layout) return;
    const { hi, range } = layout;
    onPriceTap(hi - (e.nativeEvent.locationY / height) * range);
  };

  return (
    <Pressable onPress={handlePress} disabled={!onPriceTap}>
      <Canvas style={{ width, height }}>
        {/* white chart background */}
        <Rect x={0} y={0} width={width} height={height} color={BG} />

        {/* magnet levels */}
        {layout.magnetLevels.map((lvl, i) => (
          <Line
            key={`m${i}`}
            p1={vec(0, layout.y(lvl))}
            p2={vec(width, layout.y(lvl))}
            color={MAGNET}
            strokeWidth={1}
          />
        ))}

        {/* candles — up = white body, down = black body, black outline on each */}
        {bars.map((b, i) => {
          const up = b.close >= b.open;
          const cx = layout.xCenter(i);
          const top = layout.y(Math.max(b.open, b.close));
          const bot = layout.y(Math.min(b.open, b.close));
          const h = Math.max(bot - top, 1);
          const x = cx - layout.bodyW / 2;
          return (
            <React.Fragment key={i}>
              <Line p1={vec(cx, layout.y(b.high))} p2={vec(cx, layout.y(b.low))} color={WICK} strokeWidth={1} />
              <Rect x={x} y={top} width={layout.bodyW} height={h} color={up ? UP_FILL : DOWN_FILL} />
              <Rect x={x} y={top} width={layout.bodyW} height={h} color={BORDER} style="stroke" strokeWidth={1} />
            </React.Fragment>
          );
        })}

        {/* EMA */}
        <Path path={emaPath} color={EMA_COLOR} style="stroke" strokeWidth={1.5} />

        {/* placed-but-unentered stop (Armed/Placed) */}
        {pendingStop != null && (
          <Line p1={vec(0, layout.y(pendingStop))} p2={vec(width, layout.y(pendingStop))} color={STOP_COLOR} strokeWidth={1} />
        )}

        {/* entry + live stop */}
        {entryPrice != null && (
          <Line p1={vec(0, layout.y(entryPrice))} p2={vec(width, layout.y(entryPrice))} color={ENTRY_COLOR} strokeWidth={1} />
        )}
        {liveStop != null && (
          <Line p1={vec(0, layout.y(liveStop))} p2={vec(width, layout.y(liveStop))} color={STOP_COLOR} strokeWidth={1} />
        )}
      </Canvas>
    </Pressable>
  );
}

function collectMagnets(m: MagnetLevels): number[] {
  return [
    m.prevDayOpen, m.prevDayHigh, m.prevDayLow, m.prevDayClose,
    m.prevWeekHigh, m.prevWeekLow,
    m.prevMonthHigh, m.prevMonthLow,
  ].filter((v): v is number => v != null);
}
