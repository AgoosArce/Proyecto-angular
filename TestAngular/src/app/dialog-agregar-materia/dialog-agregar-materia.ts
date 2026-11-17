import { Component, Inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Materias } from '../interfaces/Materias';
import { CrearMateriaDto } from '../interfaces/CrearMateriaDto';

@Component({
  selector: 'app-dialog-agregar-materia',
  standalone: true,
  imports: [
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    FormsModule,
  ],
  templateUrl: './dialog-agregar-materia.html',
  styleUrl: './dialog-agregar-materia.scss',
})
export class DialogAgregarMateria {
  nombre = '';

  constructor(
    private dialogRef: MatDialogRef<DialogAgregarMateria, CrearMateriaDto>,
    @Inject(MAT_DIALOG_DATA) public data: { materia?: Materias }
  ) {
    // Al editar, precargar el nombre actual.
    this.nombre = data.materia?.nombre ?? '';
  }

  confirmar(): void {
    this.dialogRef.close({ nombre: this.nombre.trim() });
  }

  cancelar(): void {
    this.dialogRef.close();
  }
}
