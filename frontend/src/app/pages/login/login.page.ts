import { Component, OnInit } from '@angular/core';
import { AuthService } from '../../services/auth.service';
import { Router } from '@angular/router';
import { TradingSessionService } from '../../services/trading-session.service'; // Adjust the path if needed

@Component({
  selector: 'app-login',
  templateUrl: './login.page.html',
  styleUrls: ['./login.page.scss'],
})
export class LoginPage implements OnInit {
  username = '';
  password = '';
  errorMessage = '';

  constructor(
    private authService: AuthService,
    private tradingSessionService: TradingSessionService, // Inject TradingSessionService
    private router: Router
  ) { }

  ngOnInit() { }

  onLogin() {
    if (!this.username || !this.password) {
      this.errorMessage = 'Username and password are required';
      return;
    }

    this.authService.login(this.username, this.password).subscribe({
      next: (response) => {
        console.log('User logged in:', response);
        localStorage.setItem('UserId', response.userId.toString()); // Store UserId
        this.router.navigate(['/home']); // Redirect to home
      },
      error: (error) => {
        console.error('Login failed:', error);
        this.errorMessage = error.message; // Display error message
      },
    });
  }


  onLogout() {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
