/** Respuesta paginada del backend (GET /api/Alumno). */
export interface PagedResult<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

/** Parámetros de consulta aceptados por GET /api/Alumno. */
export interface AlumnoFiltro {
  pageNumber: number;
  pageSize: number;
  nombre?: string | null;
  materiaId?: number | null;
}
