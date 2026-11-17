import { Routes } from '@angular/router';
import { authGuard, roleGuard } from './guards/auth.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'home' },
  {
    path: 'home',
    loadComponent: () => import('./home/home').then(m => m.Home)
  },
  {
    // Alumnos: requiere sesión (Usuario consulta, Admin administra).
    path: 'dashboardA',
    canActivate: [authGuard],
    loadComponent: () => import('./dashboard-a/dashboard-a').then(m => m.DashboardA)
  },
  {
    // Materias: requiere sesión.
    path: 'dashboardB',
    canActivate: [authGuard],
    loadComponent: () => import('./dashboard-b/dashboard-b').then(m => m.DashboardB)
  },
  {
    // Panel administrativo: solo rol Admin.
    path: 'dashboardC',
    canActivate: [roleGuard],
    data: { roles: ['Admin'] },
    loadComponent: () => import('./dashboard-c/dashboard-c').then(m => m.DashboardC)
  },
  {
    path: 'auth/login',
    loadComponent: () => import('./auth/login').then(m => m.Login)
  },
  {
    path: 'auth/register',
    loadComponent: () => import('./auth/register').then(m => m.Register)
  },
  { path: '**', redirectTo: 'home' }
];
