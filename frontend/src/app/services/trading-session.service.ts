import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class TradingSessionService {
  private apiUrl = 'http://localhost:5068/api/TradingSession';  

  constructor(private http: HttpClient) {}

  // Method to update session data on the backend
  updateSession(currentProfitLoss: number, totalProfitLoss: number, totalOrders: number) {
    const url = `${this.apiUrl}/update-session`;
    const body = {
      currentProfitLoss,
      totalProfitLoss,
      totalOrders
    };

    return this.http.put(url, body);  // Sends a PUT request to update session data
  }
}
