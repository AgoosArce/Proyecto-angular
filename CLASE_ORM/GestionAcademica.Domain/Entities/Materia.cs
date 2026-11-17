namespace GestionAcademica.Domain.Entities;

public class Materia
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;

    // Navegación inversa.
    public List<Alumno> Alumnos { get; set; } = new();
}
