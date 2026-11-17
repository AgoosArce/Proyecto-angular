import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { AuthService } from '../services/auth.service';

/** Panel administrativo: protegido por roleGuard con data.roles = ['Admin']. */
@Component({
  selector: 'app-dashboard-c',
  standalone: true,
  imports: [RouterModule],
  templateUrl: './dashboard-c.html',
  styleUrl: './dashboard-c.scss',
})
export class DashboardC {
  constructor(public auth: AuthService) {}
}
