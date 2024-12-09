import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { TradingSession } from '../models/trading-session.model';


@Injectable({
  providedIn: 'root'
})
export class TradingSessionService {

  private apiUrl = 'http://localhost:5068/api/TradingSession';

  constructor(private http: HttpClient) {}
  
  getSession(userId: number, instrument: string = 'S&P 500'): Observable<TradingSession> {
    const url = `${this.apiUrl}/get-session?userId=${userId}&instrument=${instrument}`;
    return this.http.get<TradingSession>(url).pipe(
      catchError((error): Observable<never> => {
        console.error('Error fetching session:', error);
        return throwError(() => new Error(error.message || 'An unexpected error occurred.'));
      })
    );
  }
  
  // Update the session with state and stats
  updateSession(sessionId: number, data: { 
    currentProfitLoss?: number; 
    totalProfitLoss?: number; 
    totalOrders?: number; 
    hasOpenOrder?: boolean; 
    entryPrice?: number | null;
    currentBarIndex?: number; 
  }) {
    const url = `${this.apiUrl}/update-session`;
    return this.http.put(url, { sessionId, ...data }).pipe(
      catchError((error): Observable<never> => {
        console.error('Error updating session:', error);
        return throwError(() => new Error(error.message || 'Failed to update session.'));
      })
    );
  }
  
  
  completeDay(sessionId: number) {
    const url = `${this.apiUrl}/complete-day?sessionId=${sessionId}`;
    return this.http.post(url, null).pipe(
      catchError((error): Observable<never> => {
        console.error('Error completing trading day:', error);
        return throwError(() => new Error(error.message || 'Failed to complete trading day.'));
      })
    );
  }
  
  


  getBarsForSession(sessionId: number): Observable<any> {
    const url = `${this.apiUrl}/${sessionId}/bars`;
    return this.http.get<any>(url).pipe(
      catchError((error): Observable<never> => {
        console.error('Error fetching bars for session:', error);
        return throwError(() => new Error(error.message || 'Failed to fetch bars for session.'));
      })
    );
  }
  
}

