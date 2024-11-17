import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class TradingSessionService {
  private apiUrl = 'http://localhost:5068/api/TradingSession';

  constructor(private http: HttpClient) {}

  // Update session with aggregated stats and state
  updateSession(data: {
    currentProfitLoss: number;
    totalProfitLoss: number;
    totalOrders: number;
    hasOpenOrder: boolean;
    entryPrice: number | null;
  }) {
    const url = `${this.apiUrl}/update-session`;
    return this.http.put(url, data); // Sends a PUT request to update session data
  }

  getActiveSession(userId: number = 2, instrument: string = 'S&P 500') {
    const url = `${this.apiUrl}/active?userId=${userId}&instrument=${instrument}`;
    return this.http.get<any>(url); // Replace `any` with a session model if you have it
  }
  
}
