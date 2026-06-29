import React, { useMemo } from 'react';
import { View } from 'react-native';
import { Canvas, Path, Rect, Line, vec } from '@shopify/react-native-skia';
import { Bar, MagnetLevels } from '@/api/types';
import { ema } from '@/lib/priceAction';

const CANDLE = '#FFFFFF'; // black & white candles: solid = up, hollow = down
const EMA_COLOR = '#e0b30a';
const MAGNET = 'rgba(120,144,156,0.45)';
const STOP_COLOR = '#ef5350';
const ENTRY_COLOR = '#90caf9';

interface Props {
  bars: Bar[]; // revealed bars only (caller slices to currentIndex)
  width: number;
  height: number;
  emaPeriod?: number;
  showMagnets?: boolean;
  magnets?: MagnetLevels | null;
  entryPrice?: number | null;
  liveStop?: number | null;
  proposedStops?: number[]; // faint candidate stop lines shown before entry
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
  proposedStops = [],
}: Props) {
  const layout = useMemo(() => {
    if (bars.length === 0) return null;

    const magnetLevels = showMagnets && magnets ? collectMagnets(magnets) : [];
    const extra = [...magnetLevels, ...proposedStops];
    let hi = Math.max(...bars.map((b) => b.high), ...extra);
    let lo = Math.min(...bars.map((b) => b.low), ...extra);
    if (entryPrice != null) { hi = Math.max(hi, entryPrice); lo = Math.min(lo, entryPrice); }
    if (liveStop != null) { hi = Math.max(hi, liveStop); lo = Math.min(lo, liveStop); }

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
  }, [bars, width, height, showMagnets, magnets, entryPrice, liveStop, proposedStops]);

  if (!layout) return <View style={{ width, height }} />;

  // Build the EMA as an SVG path string. Passing a string to <Path path=...>
  // lets Skia construct it internally (works on web without touching the Skia
  // global before CanvasKit is ready).
  const emaVals = ema(bars, emaPeriod);
  const emaPath = emaVals
    .map((v, i) => `${i === 0 ? 'M' : 'L'}${layout.xCenter(i).toFixed(2)},${layout.y(v).toFixed(2)}`)
    .join(' ');

  return (
    <Canvas style={{ width, height }}>
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

      {/* candles — black & white: up = solid white, down = hollow outline */}
      {bars.map((b, i) => {
        const up = b.close >= b.open;
        const cx = layout.xCenter(i);
        const top = layout.y(Math.max(b.open, b.close));
        const bot = layout.y(Math.min(b.open, b.close));
        return (
          <React.Fragment key={i}>
            <Line p1={vec(cx, layout.y(b.high))} p2={vec(cx, layout.y(b.low))} color={CANDLE} strokeWidth={1} />
            <Rect
              x={cx - layout.bodyW / 2}
              y={top}
              width={layout.bodyW}
              height={Math.max(bot - top, 1)}
              color={CANDLE}
              style={up ? 'fill' : 'stroke'}
              strokeWidth={1.5}
            />
          </React.Fragment>
        );
      })}

      {/* EMA */}
      <Path path={emaPath} color={EMA_COLOR} style="stroke" strokeWidth={1.5} />

      {/* proposed (pre-entry) stop candidates */}
      {proposedStops.map((s, i) => (
        <Line key={`ps${i}`} p1={vec(0, layout.y(s))} p2={vec(width, layout.y(s))} color="rgba(239,83,80,0.35)" strokeWidth={1} />
      ))}

      {/* entry + live stop */}
      {entryPrice != null && (
        <Line p1={vec(0, layout.y(entryPrice))} p2={vec(width, layout.y(entryPrice))} color={ENTRY_COLOR} strokeWidth={1} />
      )}
      {liveStop != null && (
        <Line p1={vec(0, layout.y(liveStop))} p2={vec(width, layout.y(liveStop))} color={STOP_COLOR} strokeWidth={1} />
      )}
    </Canvas>
  );
}

function collectMagnets(m: MagnetLevels): number[] {
  return [
    m.prevDayOpen, m.prevDayHigh, m.prevDayLow, m.prevDayClose,
    m.prevWeekHigh, m.prevWeekLow,
    m.prevMonthHigh, m.prevMonthLow,
  ].filter((v): v is number => v != null);
}
