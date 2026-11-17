import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthService } from '../services/auth.service';

/**
 * Si la API responde 401 (token vencido/inválido) fuera de /auth, cierra la sesión
 * y redirige al login conservando la URL actual. El error se propaga igual para que
 * cada componente pueda mostrar su mensaje.
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      const esApi = req.url.startsWith(environment.apiUrl);
      const esAuth = req.url.toLowerCase().includes('/auth/');

      if (err.status === 401 && esApi && !esAuth) {
        auth.logout();
        router.navigate(['/auth/login'], { queryParams: { returnUrl: router.url, expired: 1 } });
      }
      return throwError(() => err);
    })
  );
};
