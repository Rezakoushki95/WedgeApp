import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { TradingSession } from '../models/trading-session.model';


@Injectable({
  providedIn: 'root'
})
export class TradingSessionService {

  private apiUrl = 'http://localhost:5068/api/TradingSession';

  constructor(private http: HttpClient) { }

  getSession(userId: number, instrument: string = 'S&P 500'): Observable<TradingSession> {
    const url = `${this.apiUrl}/get-session?userId=${userId}&instrument=${instrument}`;
    return this.http.get<TradingSession | null>(url).pipe(
      map((session) => this.handleResponse(session, 'Session not found')),
      catchError((error) => this.handleError(error))
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
  }): Observable<TradingSession> {
    const url = `${this.apiUrl}/update-session`;
    return this.http.put<TradingSession | null>(url, { sessionId, ...data }).pipe(
      map((updatedSession) => this.handleResponse(updatedSession, 'Failed to update session')),
      catchError((error) => this.handleError(error))
    );
  }




  completeDay(sessionId: number): Observable<void> {
    const url = `${this.apiUrl}/complete-day?sessionId=${sessionId}`;
    return this.http.post<void>(url, null).pipe(
      catchError((error) => this.handleError(error))
    );
  }


  getBarsForSession(sessionId: number): Observable<any[]> {
    const url = `${this.apiUrl}/${sessionId}/get-bars`;
    return this.http.get<any[] | null>(url).pipe(
      map((bars) => this.handleResponse(bars, 'No bars found for the session')),
      catchError((error) => this.handleError(error))
    );
  }


  private handleResponse<T>(response: T | null, errorMessage: string): T {
    if (!response) {
      throw new Error(errorMessage);
    }
    return response;
  }

  private handleError(error: any): Observable<never> {
    console.error('API Error:', error);
    return throwError(() => new Error(error.message || 'An unexpected error occurred.'));
  }


}

