import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class TradingSessionService {
  private apiUrl = 'http://localhost:5068/api/TradingSession';

  constructor(private http: HttpClient) {}

  // Fetch the active trading session for a user
  getSession(userId: number, instrument: string = 'S&P 500'): Observable<any> {
    const url = `${this.apiUrl}/get-session?userId=${userId}&instrument=${instrument}`;
    return this.http.get<any>(url); // Replace `any` with a session model if available
  }

  // Update the session with state and stats
  updateSession(sessionId: number, data: { 
    currentProfitLoss?: number; 
    totalProfitLoss?: number; 
    totalOrders?: number; 
    hasOpenOrder?: boolean; 
    entryPrice?: number | null;
    currentBarIndex?: number; // Added currentBarIndex
  }) {
    const url = `${this.apiUrl}/update-session`;
    return this.http.put(url, { sessionId, ...data });
  }
  
  // Complete the current trading day
  completeDay(sessionId: number) {
    const url = `${this.apiUrl}/${sessionId}/complete-day`;
    return this.http.post(url, null);
  }
}
