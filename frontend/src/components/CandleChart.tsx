import React, { useMemo } from 'react';
import { View } from 'react-native';
import { Canvas, Path, Rect, Line, Skia, vec } from '@shopify/react-native-skia';
import { Bar, MagnetLevels } from '@/api/types';
import { ema } from '@/lib/priceAction';

const UP = '#26a69a';
const DOWN = '#ef5350';
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
}: Props) {
  const layout = useMemo(() => {
    if (bars.length === 0) return null;

    const magnetLevels = showMagnets && magnets ? collectMagnets(magnets) : [];
    let hi = Math.max(...bars.map((b) => b.high), ...magnetLevels);
    let lo = Math.min(...bars.map((b) => b.low), ...magnetLevels);
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
  }, [bars, width, height, showMagnets, magnets, entryPrice, liveStop]);

  if (!layout) return <View style={{ width, height }} />;

  const emaVals = ema(bars, emaPeriod);
  const emaPath = Skia.Path.Make();
  emaVals.forEach((v, i) => {
    const px = layout.xCenter(i);
    const py = layout.y(v);
    if (i === 0) emaPath.moveTo(px, py);
    else emaPath.lineTo(px, py);
  });

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

      {/* candles */}
      {bars.map((b, i) => {
        const color = b.close >= b.open ? UP : DOWN;
        const cx = layout.xCenter(i);
        const top = layout.y(Math.max(b.open, b.close));
        const bot = layout.y(Math.min(b.open, b.close));
        return (
          <React.Fragment key={i}>
            <Line p1={vec(cx, layout.y(b.high))} p2={vec(cx, layout.y(b.low))} color={color} strokeWidth={1} />
            <Rect x={cx - layout.bodyW / 2} y={top} width={layout.bodyW} height={Math.max(bot - top, 1)} color={color} />
          </React.Fragment>
        );
      })}

      {/* EMA */}
      <Path path={emaPath} color={EMA_COLOR} style="stroke" strokeWidth={1.5} />

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
