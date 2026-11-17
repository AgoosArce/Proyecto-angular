import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import Swal from 'sweetalert2';
import { AuthService } from '../services/auth.service';

/** Impide entrar a rutas privadas sin sesión válida. */
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return auth.isLoggedIn()
    ? true
    : router.createUrlTree(['/auth/login'], { queryParams: { returnUrl: state.url } });
};

/**
 * Protege vistas por rol. Uso en la ruta:  canActivate: [roleGuard], data: { roles: ['Admin'] }
 * Es solo UX: el backend vuelve a verificar el rol en cada endpoint.
 */
export const roleGuard: CanActivateFn = (route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const roles = (route.data['roles'] as string[] | undefined) ?? [];

  if (!auth.isLoggedIn()) {
    return router.createUrlTree(['/auth/login'], { queryParams: { returnUrl: state.url } });
  }
  if (roles.length === 0 || auth.hasRole(roles)) {
    return true;
  }

  Swal.fire('Acceso denegado', 'Esta sección es solo para administradores.', 'warning');
  return router.createUrlTree(['/home']);
};
