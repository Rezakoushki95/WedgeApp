import { inject } from '@angular/core';
import { CanActivateFn } from '@angular/router';
import { Router } from '@angular/router';

export const authGuard: CanActivateFn = (route, state) => {
  const isAuthenticated = !!localStorage.getItem('UserId'); // Check if logged in

  if (isAuthenticated) {
    return true; // Allow access
  }

  // Inject the Router service to create UrlTree
  return inject(Router).createUrlTree(['/login']);
};
