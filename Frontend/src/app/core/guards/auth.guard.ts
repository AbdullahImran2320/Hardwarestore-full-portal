import { inject } from '@angular/core';
import { CanActivateChildFn, CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.getToken()) return true;

  router.navigate(['/login']);
  return false;
};

// Pages that only an Admin may open (Users, Backup).
export const adminGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAdmin()) return true;

  return router.createUrlTree(['/dashboard']);
};

// Accounts that still use a seeded or admin-set password must choose a new one first.
export const mustChangePasswordGuard: CanActivateChildFn = (_route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const mustChange = authService.currentUser()?.mustChangePassword === true;
  if (mustChange && !state.url.startsWith('/account')) {
    return router.createUrlTree(['/account']);
  }
  return true;
};
