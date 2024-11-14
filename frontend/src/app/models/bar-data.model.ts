import { UTCTimestamp } from "lightweight-charts";

export interface BarData {
  time: UTCTimestamp; // Ensure time is of type UTCTimestamp
  open: number;
  high: number;
  low: number;
  close: number;
}
