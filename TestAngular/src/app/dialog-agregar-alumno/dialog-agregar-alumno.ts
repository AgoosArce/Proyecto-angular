import { Component, Inject, OnInit } from '@angular/core';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { FormsModule } from '@angular/forms';
import { Alumno } from '../interfaces/Alumno';
import { Materias } from '../interfaces/Materias';
import { CrearAlumnoDto } from '../interfaces/CrearAlumnoDto';
import { MateriaService } from '../services/materia-service';

@Component({
  selector: 'app-alumno-dialog',
  standalone: true,
  imports: [
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    FormsModule,
    MatSelectModule,
  ],
  template: `
    <h2 mat-dialog-title>{{ data.alumno ? 'Editar alumno' : 'Nuevo alumno' }}</h2>

    <mat-dialog-content>
      <mat-form-field appearance="outline">
        <mat-label>Nombre</mat-label>
        <input matInput [(ngModel)]="nombre" placeholder="Nombre" required maxlength="100">
      </mat-form-field>

      <mat-form-field appearance="outline">
        <mat-label>Email</mat-label>
        <input matInput type="email" [(ngModel)]="email" placeholder="Email" required email #emailCtrl="ngModel">
        @if (emailCtrl.invalid && emailCtrl.touched) {
          <mat-error>Ingrese un email válido.</mat-error>
        }
      </mat-form-field>

      <mat-form-field appearance="outline">
        <mat-label>Materia</mat-label>
        <mat-select [(ngModel)]="materiaId" name="materia" required>
          @for (materia of materias; track materia.id) {
            <mat-option [value]="materia.id">{{ materia.nombre }}</mat-option>
          }
        </mat-select>
      </mat-form-field>
    </mat-dialog-content>

    <mat-dialog-actions align="end">
      <button mat-button type="button" (click)="cancelar()">Cancelar</button>
      <button mat-raised-button color="primary" type="button" (click)="confirmar()"
              [disabled]="!nombre.trim() || !email.trim() || emailCtrl.invalid || !materiaId">
        {{ data.alumno ? 'Guardar' : 'Agregar' }}
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    mat-dialog-content {
      display: flex;
      flex-direction: column;
      gap: 8px;
      min-width: 0; width: min(350px, 100%);
    }
  `]
})
export class AlumnoDialogComponent implements OnInit {
  nombre = '';
  email = '';
  materiaId: number | null = null;
  materias: Materias[] = [];

  constructor(
    private dialogRef: MatDialogRef<AlumnoDialogComponent, CrearAlumnoDto>,
    @Inject(MAT_DIALOG_DATA) public data: { alumno?: Alumno },
    private materiaService: MateriaService
  ) {
    if (data.alumno) {
      this.nombre = data.alumno.nombre;
      this.email = data.alumno.email;
      this.materiaId = data.alumno.materiaId; // ahora el backend devuelve materiaId
    }
  }

  ngOnInit(): void {
    this.materiaService.getAll().subscribe({
      next: (data) => (this.materias = data),
      error: (err) => console.error('Error al cargar materias', err),
    });
  }

  confirmar(): void {
    if (!this.materiaId) return;
    this.dialogRef.close({
      nombre: this.nombre.trim(),
      email: this.email.trim(),
      materiaId: this.materiaId,
    });
  }

  cancelar(): void {
    this.dialogRef.close();
  }
}
