import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthResponse, Rol } from '../interfaces/AuthResponse';
import { LoginDto } from '../interfaces/LoginDto';
import { RegisterDto } from '../interfaces/RegisterDto';

interface JwtPayload {
  sub?: string;
  email?: string;
  nombre?: string;
  role?: Rol;
  exp?: number;
}

/**
 * Sesión del usuario.
 * Nota de seguridad: el token se guarda en localStorage (aceptable para la entrega, pero
 * vulnerable a robo por XSS). El rol se lee del propio JWT solo para ajustar la interfaz:
 * la autorización real la hace SIEMPRE el backend con [Authorize(Roles = "Admin")].
 */
@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly apiUrl = `${environment.apiUrl}/auth`;
  private readonly keys = { token: 'token', email: 'userEmail', nombre: 'userNombre' } as const;

  constructor(private http: HttpClient) {}

  login(dto: LoginDto): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, dto).pipe(tap((r) => this.setSession(r)));
  }

  register(dto: RegisterDto): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/register`, dto).pipe(tap((r) => this.setSession(r)));
  }

  logout(): void {
    Object.values(this.keys).forEach((k) => localStorage.removeItem(k));
    localStorage.removeItem('userRol'); // clave de versiones anteriores
  }

  getToken(): string | null {
    return localStorage.getItem(this.keys.token);
  }

  /** Hay token y no está vencido. */
  isLoggedIn(): boolean {
    const payload = this.getPayload();
    return !!payload && (!payload.exp || payload.exp * 1000 > Date.now());
  }

  getNombre(): string | null {
    return localStorage.getItem(this.keys.nombre);
  }

  getRol(): Rol | null {
    return this.isLoggedIn() ? (this.getPayload()?.role ?? null) : null;
  }

  isAdmin(): boolean {
    return this.getRol() === 'Admin';
  }

  hasRole(roles: string[]): boolean {
    const rol = this.getRol();
    return !!rol && roles.includes(rol);
  }

  private setSession(response: AuthResponse): void {
    localStorage.setItem(this.keys.token, response.token);
    localStorage.setItem(this.keys.email, response.email);
    localStorage.setItem(this.keys.nombre, response.nombre);
  }

  /** Decodifica (sin verificar) la parte central del JWT. La firma la valida el servidor. */
  private getPayload(): JwtPayload | null {
    const token = this.getToken();
    if (!token) return null;
    try {
      const base64 = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
      const bytes = Uint8Array.from(atob(base64), (c) => c.charCodeAt(0));
      return JSON.parse(new TextDecoder().decode(bytes)) as JwtPayload;
    } catch {
      return null;
    }
  }
}
