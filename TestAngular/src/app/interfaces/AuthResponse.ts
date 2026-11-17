export type Rol = 'Admin' | 'Usuario';

export interface AuthResponse {
  token: string;
  expiration: string;
  email: string;
  nombre: string;
  rol: Rol;
}
