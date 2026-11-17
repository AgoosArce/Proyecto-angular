import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { getErrorMessage } from '../shared/error-message';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login implements OnInit {
  email = '';
  password = '';
  error = '';
  aviso = '';
  enviando = false;
  private returnUrl = '/home';

  constructor(private auth: AuthService, private router: Router, private route: ActivatedRoute) {}

  ngOnInit(): void {
    const params = this.route.snapshot.queryParamMap;
    const url = params.get('returnUrl');
    // Solo rutas internas (evita redirecciones abiertas a otros sitios).
    if (url && url.startsWith('/') && !url.startsWith('//')) this.returnUrl = url;
    if (params.get('expired')) this.aviso = 'Su sesión expiró. Inicie sesión nuevamente.';
  }

  submit(): void {
    if (this.enviando) return;
    this.error = '';
    this.enviando = true;
    this.auth.login({ email: this.email, password: this.password }).subscribe({
      next: () => this.router.navigateByUrl(this.returnUrl),
      error: (err) => {
        this.enviando = false;
        this.error = getErrorMessage(err, 'Error al iniciar sesión.');
      },
    });
  }
}
