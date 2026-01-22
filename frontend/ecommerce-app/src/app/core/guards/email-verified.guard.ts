import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const emailVerifiedGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const currentUser = authService.currentUser();

  // Si no hay usuario autenticado, permitir acceso (el authGuard se encargará)
  if (!currentUser) {
    return true;
  }

  // Si el email no está verificado, redirigir a verificación
  if (!currentUser.emailVerificado) {
    router.navigate(['/verify-email'], {
      queryParams: { email: currentUser.email }
    });
    return false;
  }

  return true;
};
