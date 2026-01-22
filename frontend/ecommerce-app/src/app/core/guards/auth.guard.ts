import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated) {
    return true;
  }

  router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
  return false;
};

export const adminGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const currentUser = authService.currentUserValue;

  if (!authService.isAuthenticated || !authService.isAdmin) {
    router.navigate(['/']);
    return false;
  }

  // Si el Admin no tiene tienda y no está en la ruta de onboarding, redirigir
  if (currentUser && !currentUser.tiendaId && !state.url.includes('/onboarding')) {
    router.navigate(['/onboarding']);
    return false;
  }

  return true;
};

export const depositoGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const currentUser = authService.currentUserValue;
  if (authService.isAuthenticated && currentUser && (currentUser.rol === 'Deposito' || currentUser.rol === 'Admin')) {
    return true;
  }

  router.navigate(['/']);
  return false;
};

export const superAdminGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const currentUser = authService.currentUserValue;
  if (authService.isAuthenticated && currentUser && currentUser.rol === 'SuperAdmin') {
    return true;
  }

  router.navigate(['/']);
  return false;
};
