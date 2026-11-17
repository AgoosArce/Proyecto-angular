import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Subject, Subscription, debounceTime, distinctUntilChanged, finalize } from 'rxjs';
import Swal from 'sweetalert2';

import { Alumno } from '../interfaces/Alumno';
import { Materias } from '../interfaces/Materias';
import { alumnosService } from '../services/AlumnoService';
import { MateriaService } from '../services/materia-service';
import { AuthService } from '../services/auth.service';
import { getErrorMessage } from '../shared/error-message';
import { AlumnoDialogComponent } from '../dialog-agregar-alumno/dialog-agregar-alumno';

@Component({
  selector: 'app-alumnos',
  standalone: true,
  imports: [
    FormsModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule,
    MatPaginatorModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatProgressBarModule,
  ],
  templateUrl: './alumnos.html',
  styleUrl: './alumnos.scss',
})
export class AlumnosComponent implements OnInit, OnDestroy {
  alumnos: Alumno[] = [];
  materias: Materias[] = [];
  displayedColumns: string[] = [];

  // Estado de paginación (pageNumber empieza en 1, como en el backend).
  pageNumber = 1;
  pageSize = 10;
  totalCount = 0;
  readonly pageSizeOptions = [5, 10, 25];

  // Filtros
  filtroNombre = '';
  filtroMateriaId: number | null = null;

  cargando = false;
  error = '';
  esAdmin = false;

  private readonly busqueda$ = new Subject<string>();
  private busquedaSub?: Subscription;

  constructor(
    private alumnosService: alumnosService,
    private materiaService: MateriaService,
    private auth: AuthService,
    private dialog: MatDialog
  ) {}

  ngOnInit(): void {
    this.esAdmin = this.auth.isAdmin();
    this.displayedColumns = ['id', 'nombre', 'email', 'materia', ...(this.esAdmin ? ['acciones'] : [])];

    // Espera 400 ms sin teclear antes de consultar: evita una request por cada letra.
    this.busquedaSub = this.busqueda$
      .pipe(debounceTime(400), distinctUntilChanged())
      .subscribe(() => this.irAPrimeraPagina());

    this.materiaService.getAll().subscribe({
      next: (data) => (this.materias = data),
      error: (err) => console.error('Error al cargar materias', err),
    });

    this.cargarAlumnos();
  }

  ngOnDestroy(): void {
    this.busquedaSub?.unsubscribe();
  }

  cargarAlumnos(): void {
    this.cargando = true;
    this.error = '';

    this.alumnosService
      .getPaged({
        pageNumber: this.pageNumber,
        pageSize: this.pageSize,
        nombre: this.filtroNombre,
        materiaId: this.filtroMateriaId,
      })
      .pipe(finalize(() => (this.cargando = false)))
      .subscribe({
        next: (page) => {
          // Si se eliminó el último registro de la última página, retroceder una página.
          if (page.items.length === 0 && page.pageNumber > 1 && page.totalCount > 0) {
            this.pageNumber = page.totalPages;
            this.cargarAlumnos();
            return;
          }
          this.alumnos = page.items;
          this.totalCount = page.totalCount;
        },
        error: (err) => {
          this.alumnos = [];
          this.totalCount = 0;
          this.error = getErrorMessage(err, 'No se pudieron cargar los alumnos.');
        },
      });
  }

  // ───────────── Paginación y filtros ─────────────

  onPageChange(event: PageEvent): void {
    this.pageNumber = event.pageIndex + 1; // MatPaginator es base 0; la API, base 1
    this.pageSize = event.pageSize;
    this.cargarAlumnos();
  }

  onNombreChange(valor: string): void {
    this.busqueda$.next(valor.trim());
  }

  onMateriaChange(): void {
    this.irAPrimeraPagina();
  }

  limpiarFiltros(): void {
    this.filtroNombre = '';
    this.filtroMateriaId = null;
    this.busqueda$.next('');
    this.irAPrimeraPagina();
  }

  get hayFiltros(): boolean {
    return !!this.filtroNombre.trim() || this.filtroMateriaId !== null;
  }

  private irAPrimeraPagina(): void {
    this.pageNumber = 1;
    this.cargarAlumnos();
  }

  // ───────────── CRUD (solo Admin; el backend también lo exige) ─────────────

  agregarAlumno(): void {
    this.dialog.open(AlumnoDialogComponent, { data: {} }).afterClosed().subscribe((result) => {
      if (!result) return;

      this.alumnosService.create(result).subscribe({
        next: () => {
          Swal.fire('Listo', 'Alumno agregado correctamente.', 'success');
          this.cargarAlumnos();
        },
        error: (err) => Swal.fire('Error', getErrorMessage(err, 'No se pudo agregar el alumno.'), 'error'),
      });
    });
  }

  editarAlumno(alumno: Alumno): void {
    this.dialog.open(AlumnoDialogComponent, { data: { alumno } }).afterClosed().subscribe((result) => {
      if (!result) return;

      this.alumnosService.update(alumno.id, result).subscribe({
        next: () => {
          Swal.fire('Guardado', 'Alumno actualizado correctamente.', 'success');
          this.cargarAlumnos();
        },
        error: (err) => Swal.fire('Error', getErrorMessage(err, 'No se pudo actualizar el alumno.'), 'error'),
      });
    });
  }

  eliminar(alumno: Alumno): void {
    Swal.fire({
      title: '¿Eliminar alumno?',
      text: `Vas a eliminar a ${alumno.nombre}. Esta acción no se puede deshacer.`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      confirmButtonText: 'Sí, eliminar',
      cancelButtonText: 'Cancelar',
    }).then((result) => {
      if (!result.isConfirmed) return;

      this.alumnosService.delete(alumno.id).subscribe({
        next: () => {
          Swal.fire('Eliminado', 'El alumno fue eliminado.', 'success');
          this.cargarAlumnos();
        },
        error: (err) => Swal.fire('Error', getErrorMessage(err, 'No se pudo eliminar el alumno.'), 'error'),
      });
    });
  }
}
