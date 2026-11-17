import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Alumno } from '../interfaces/Alumno';
import { CrearAlumnoDto } from '../interfaces/CrearAlumnoDto';
import { AlumnoFiltro, PagedResult } from '../interfaces/PagedResult';

@Injectable({
  providedIn: 'root',
})
export class alumnosService {
  private readonly ApiUrl = `${environment.apiUrl}/Alumno`;

  constructor(private http: HttpClient) {}

  /** GET /api/Alumno?pageNumber=1&pageSize=10&nombre=ana&materiaId=2 (paginación en el servidor). */
  getPaged(filtro: AlumnoFiltro): Observable<PagedResult<Alumno>> {
    let params = new HttpParams()
      .set('pageNumber', filtro.pageNumber)
      .set('pageSize', filtro.pageSize);

    const nombre = filtro.nombre?.trim();
    if (nombre) params = params.set('nombre', nombre);
    if (filtro.materiaId) params = params.set('materiaId', filtro.materiaId);

    return this.http.get<PagedResult<Alumno>>(this.ApiUrl, { params });
  }

  getById(id: number): Observable<Alumno> {
    return this.http.get<Alumno>(`${this.ApiUrl}/${id}`);
  }

  create(alumno: CrearAlumnoDto): Observable<Alumno> {
    return this.http.post<Alumno>(this.ApiUrl, alumno);
  }

  update(id: number, alumno: CrearAlumnoDto): Observable<void> {
    return this.http.put<void>(`${this.ApiUrl}/${id}`, alumno);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.ApiUrl}/${id}`);
  }
}
