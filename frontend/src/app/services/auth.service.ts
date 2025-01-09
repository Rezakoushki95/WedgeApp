import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private apiUrl = 'http://192.168.1.11:5068/api/user'; // Replace with your backend URL

  constructor(private http: HttpClient) { }

  login(username: string, password: string): Observable<{ userId: number }> {
    return this.http.post<{ userId: number }>(`${this.apiUrl}/login`, { username, password }).pipe(
      catchError((error) => {
        // Handle errors gracefully
        console.error('Login error:', error);
        return throwError(() => new Error('Failed to log in. Please try again.'));
      })
    );
  }

  logout(): void {
    localStorage.removeItem('UserId');
  }
}
