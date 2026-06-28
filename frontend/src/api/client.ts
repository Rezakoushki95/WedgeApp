import { API_BASE_URL } from '@/config';
import {
  CreateJourney,
  DealtChart,
  Journey,
  JourneyStats,
  SubmitTrade,
  TradeResult,
} from './types';

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${API_BASE_URL}${path}`, {
    headers: { 'Content-Type': 'application/json' },
    ...init,
  });
  if (!res.ok) {
    let message = `Request failed (${res.status})`;
    try {
      const body = await res.json();
      if (body?.message) message = body.message;
    } catch {
      // non-JSON error body; keep the status message
    }
    throw new Error(message);
  }
  return res.json() as Promise<T>;
}

export const api = {
  // Charts
  dealChart: () => request<DealtChart>('/Chart/deal'),
  getChart: (chartId: number) => request<DealtChart>(`/Chart/${chartId}`),

  // Journeys
  listJourneys: (includeArchived = false) =>
    request<JourneyStats[]>(`/Journey?includeArchived=${includeArchived}`),
  getStats: (journeyId: number) =>
    request<JourneyStats>(`/Journey/${journeyId}/stats`),
  createJourney: (dto: CreateJourney) =>
    request<Journey>('/Journey', { method: 'POST', body: JSON.stringify(dto) }),
  renameJourney: (journeyId: number, name: string) =>
    request<Journey>(`/Journey/${journeyId}/rename`, {
      method: 'PUT',
      body: JSON.stringify({ name }),
    }),
  archiveJourney: (journeyId: number, archived = true) =>
    request<Journey>(`/Journey/${journeyId}/archive?archived=${archived}`, {
      method: 'PUT',
    }),
  submitTrade: (dto: SubmitTrade) =>
    request<TradeResult>('/Journey/trade', {
      method: 'POST',
      body: JSON.stringify(dto),
    }),
};
