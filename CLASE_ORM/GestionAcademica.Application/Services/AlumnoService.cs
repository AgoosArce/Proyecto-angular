using GestionAcademica.Application.Common;
using GestionAcademica.Application.DTOs.Alumno;
using GestionAcademica.Application.Exceptions;
using GestionAcademica.Application.Interfaces;
using GestionAcademica.Domain.Entities;

namespace GestionAcademica.Application.Services;

public class AlumnoService : IAlumnoService
{
    private readonly IAlumnoRepository _alumnos;
    private readonly IMateriaRepository _materias;

    public AlumnoService(IAlumnoRepository alumnos, IMateriaRepository materias)
    {
        _alumnos = alumnos;
        _materias = materias;
    }

    public async Task<PagedResult<AlumnoDto>> GetPagedAsync(AlumnoFiltroDto filtro, CancellationToken ct = default)
    {
        ValidarPaginacion(filtro.PageNumber, filtro.PageSize);

        var nombre = string.IsNullOrWhiteSpace(filtro.Nombre) ? null : filtro.Nombre.Trim();
        var (items, total) = await _alumnos.GetPagedAsync(nombre, filtro.MateriaId, filtro.PageNumber, filtro.PageSize, ct);

        return new PagedResult<AlumnoDto>(items, total, filtro.PageNumber, filtro.PageSize);
    }

    public async Task<AlumnoDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _alumnos.GetDtoByIdAsync(id, ct) ?? throw NotFoundException.For("Alumno", id);
    }

    public async Task<AlumnoDto> CreateAsync(CrearAlumnoDto dto, CancellationToken ct = default)
    {
        var email = NormalizarEmail(dto.Email);
        await ValidarMateriaAsync(dto.MateriaId, ct);
        await ValidarEmailUnicoAsync(email, null, ct);

        var alumno = new Alumno { Nombre = dto.Nombre.Trim(), Email = email, MateriaId = dto.MateriaId };
        await _alumnos.AddAsync(alumno, ct);
        await _alumnos.SaveChangesAsync(ct);

        return await GetByIdAsync(alumno.Id, ct);
    }

    public async Task UpdateAsync(int id, ActualizarAlumnoDto dto, CancellationToken ct = default)
    {
        var alumno = await _alumnos.GetByIdAsync(id, ct) ?? throw NotFoundException.For("Alumno", id);
        var email = NormalizarEmail(dto.Email);

        await ValidarMateriaAsync(dto.MateriaId, ct);
        await ValidarEmailUnicoAsync(email, id, ct);

        alumno.Nombre = dto.Nombre.Trim();
        alumno.Email = email;
        alumno.MateriaId = dto.MateriaId;
        await _alumnos.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var alumno = await _alumnos.GetByIdAsync(id, ct) ?? throw NotFoundException.For("Alumno", id);
        _alumnos.Remove(alumno);
        await _alumnos.SaveChangesAsync(ct);
    }

    /// <summary>Se valida antes de calcular Skip para no generar offsets negativos ni páginas gigantes.</summary>
    private static void ValidarPaginacion(int pageNumber, int pageSize)
    {
        var errores = new Dictionary<string, string[]>();
        if (pageNumber < 1)
            errores["pageNumber"] = new[] { "pageNumber debe ser mayor o igual a 1." };
        if (pageSize < 1 || pageSize > PaginationRules.MaxPageSize)
            errores["pageSize"] = new[] { $"pageSize debe estar entre 1 y {PaginationRules.MaxPageSize}." };

        if (errores.Count > 0) throw new ValidationException(errores);
    }

    private async Task ValidarMateriaAsync(int materiaId, CancellationToken ct)
    {
        if (!await _materias.ExistsAsync(materiaId, ct))
            throw new ValidationException("materiaId", $"No existe una materia con ID {materiaId}.");
    }

    private async Task ValidarEmailUnicoAsync(string email, int? excluirId, CancellationToken ct)
    {
        if (await _alumnos.EmailExistsAsync(email, excluirId, ct))
            throw new ConflictException("ALUMNO_EMAIL_DUPLICADO", $"Ya existe un alumno con el email {email}.");
    }

    private static string NormalizarEmail(string email) => email.Trim().ToLowerInvariant();
}
