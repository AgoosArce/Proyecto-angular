import { Component, OnInit } from '@angular/core';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { finalize } from 'rxjs';
import Swal from 'sweetalert2';

import { MateriaService } from '../services/materia-service';
import { AuthService } from '../services/auth.service';
import { Materias } from '../interfaces/Materias';
import { getErrorMessage } from '../shared/error-message';
import { DialogAgregarMateria } from '../dialog-agregar-materia/dialog-agregar-materia';

@Component({
  selector: 'app-materia',
  standalone: true,
  imports: [MatTableModule, MatButtonModule, MatDialogModule, MatProgressBarModule],
  templateUrl: './materia.html',
  styleUrl: './materia.scss'
})
export class Materia implements OnInit {
  materia: Materias[] = [];
  displayedColumns: string[] = [];
  esAdmin = false;
  cargando = false;
  error = '';

  constructor(
    private materiaService: MateriaService,
    private auth: AuthService,
    private dialog: MatDialog
  ) {}

  ngOnInit(): void {
    this.esAdmin = this.auth.isAdmin();
    this.displayedColumns = ['id', 'nombre', ...(this.esAdmin ? ['acciones'] : [])];
    this.cargarMaterias();
  }

  cargarMaterias(): void {
    this.cargando = true;
    this.error = '';
    this.materiaService.getAll()
      .pipe(finalize(() => (this.cargando = false)))
      .subscribe({
        next: (data) => (this.materia = data),
        error: (err) => (this.error = getErrorMessage(err, 'No se pudieron cargar las materias.')),
      });
  }

  agregarMateria(): void {
    this.dialog.open(DialogAgregarMateria, { data: {} }).afterClosed().subscribe((result) => {
      if (!result) return;
      this.materiaService.CrearMateria(result).subscribe({
        next: () => {
          Swal.fire('Listo', 'Materia agregada correctamente.', 'success');
          this.cargarMaterias();
        },
        error: (err) => Swal.fire('Error', getErrorMessage(err, 'No se pudo agregar la materia.'), 'error'),
      });
    });
  }

  editarMateria(materia: Materias): void {
    this.dialog.open(DialogAgregarMateria, { data: { materia } }).afterClosed().subscribe((result) => {
      if (!result) return;
      this.materiaService.update(materia.id, result).subscribe({
        next: () => {
          Swal.fire('Guardado', 'Materia actualizada correctamente.', 'success');
          this.cargarMaterias();
        },
        error: (err) => Swal.fire('Error', getErrorMessage(err, 'No se pudo actualizar la materia.'), 'error'),
      });
    });
  }

  eliminar(materia: Materias): void {
    Swal.fire({
      title: '¿Eliminar materia?',
      text: `Vas a eliminar ${materia.nombre}. Esta acción no se puede deshacer.`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      confirmButtonText: 'Sí, eliminar',
      cancelButtonText: 'Cancelar'
    }).then((result) => {
      if (!result.isConfirmed) return;
      this.materiaService.delete(materia.id).subscribe({
        next: () => {
          Swal.fire('Eliminada', 'La materia fue eliminada.', 'success');
          this.cargarMaterias();
        },
        // Ej.: 409 MATERIA_CON_ALUMNOS → muestra el detail del backend.
        error: (err) => Swal.fire('No se pudo eliminar', getErrorMessage(err, 'No se pudo eliminar la materia.'), 'error'),
      });
    });
  }
}
