// Backend base URL. For a device/simulator, point this at your machine's LAN
// IP (e.g. http://192.168.1.20:5068/api); localhost only works on web / the
// same host. Override via the EXPO_PUBLIC_API_URL env var.
export const API_BASE_URL =
  process.env.EXPO_PUBLIC_API_URL ?? 'http://localhost:5068/api';

// Fixed-fractional risk per trade (1R = 1% of bankroll). Matches the backend default.
export const RISK_FRACTION = 0.01;

export const STARTING_BANKROLL = 10_000;
