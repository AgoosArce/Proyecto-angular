using System.ComponentModel.DataAnnotations;
using GestionAcademica.Application.Common;

namespace GestionAcademica.Application.DTOs.Alumno;

public class AlumnoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int MateriaId { get; set; }
    public string Materia { get; set; } = string.Empty;
}

public class CrearAlumnoDto
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El email no tiene un formato válido.")]
    [StringLength(150, ErrorMessage = "El email no puede superar los 150 caracteres.")]
    public string Email { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar una materia válida.")]
    public int MateriaId { get; set; }
}

/// <summary>Mismas reglas que la creación.</summary>
public class ActualizarAlumnoDto : CrearAlumnoDto
{
}

/// <summary>Parámetros de query: ?pageNumber=1&amp;pageSize=10&amp;nombre=ana&amp;materiaId=2</summary>
public class AlumnoFiltroDto
{
    /// <summary>Página solicitada, empieza en 1.</summary>
    public int PageNumber { get; set; } = PaginationRules.DefaultPageNumber;

    /// <summary>Cantidad de registros por página (1 a 100).</summary>
    public int PageSize { get; set; } = PaginationRules.DefaultPageSize;

    /// <summary>Coincidencia parcial sobre el nombre.</summary>
    public string? Nombre { get; set; }

    /// <summary>Filtra por la materia asociada.</summary>
    public int? MateriaId { get; set; }
}
