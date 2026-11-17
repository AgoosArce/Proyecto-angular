import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { getErrorMessage } from '../shared/error-message';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.scss',
})
export class Register {
  nombre = '';
  email = '';
  password = '';
  error = '';
  enviando = false;

  constructor(private auth: AuthService, private router: Router) {}

  submit(): void {
    if (this.enviando) return;
    this.error = '';
    this.enviando = true;
    // No se envía rol: el backend siempre registra con rol "Usuario".
    this.auth.register({ nombre: this.nombre, email: this.email, password: this.password }).subscribe({
      next: () => this.router.navigate(['/home']),
      error: (err) => {
        this.enviando = false;
        this.error = getErrorMessage(err, 'Error al registrarse.');
      },
    });
  }
}
