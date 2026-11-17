import { HttpErrorResponse } from '@angular/common/http';
import { ProblemDetails } from '../interfaces/ProblemDetails';

/**
 * Convierte un error HTTP de la API (ProblemDetails) en un mensaje legible para el usuario.
 * Prioridad: errores de validación por campo → detail → mensaje genérico por código HTTP.
 */
export function getErrorMessage(err: unknown, fallback = 'Ocurrió un error inesperado.'): string {
  if (!(err instanceof HttpErrorResponse)) return fallback;

  if (err.status === 0) {
    return 'No se pudo conectar con el servidor. Verifique que la API esté en ejecución.';
  }

  const problem = (typeof err.error === 'object' ? err.error : null) as ProblemDetails | null;

  if (problem?.errors) {
    const mensajes = Object.values(problem.errors).flat();
    if (mensajes.length) return mensajes.join('\n');
  }

  if (problem?.detail) {
    return err.status >= 500 && problem.traceId
      ? `${problem.detail} (traceId: ${problem.traceId})`
      : problem.detail;
  }

  switch (err.status) {
    case 400: return 'Los datos enviados no son válidos.';
    case 401: return 'Su sesión no es válida o expiró. Inicie sesión nuevamente.';
    case 403: return 'No tiene permisos para realizar esta operación.';
    case 404: return 'El recurso solicitado no existe.';
    case 409: return 'La operación entra en conflicto con datos existentes.';
    case 429: return 'Demasiados intentos. Espere un minuto.';
    default:  return problem?.title ?? fallback;
  }
}
