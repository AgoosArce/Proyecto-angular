namespace GestionAcademica.Domain.Entities;

public class Alumno
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // FK por convención (relación uno-a-muchos: una Materia tiene muchos Alumnos).
    public int MateriaId { get; set; }

    // Propiedad de navegación.
    public Materia? Materia { get; set; }
}
