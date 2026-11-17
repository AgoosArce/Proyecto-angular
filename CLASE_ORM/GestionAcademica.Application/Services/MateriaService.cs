using GestionAcademica.Application.DTOs.Materia;
using GestionAcademica.Application.Exceptions;
using GestionAcademica.Application.Interfaces;
using GestionAcademica.Domain.Entities;

namespace GestionAcademica.Application.Services;

public class MateriaService : IMateriaService
{
    private readonly IMateriaRepository _materias;

    public MateriaService(IMateriaRepository materias)
    {
        _materias = materias;
    }

    public async Task<IReadOnlyList<MateriaDto>> GetAllAsync(CancellationToken ct = default)
    {
        var materias = await _materias.GetAllAsync(ct);
        return materias.Select(ToDto).ToList();
    }

    public async Task<MateriaDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var materia = await _materias.GetByIdAsync(id, ct) ?? throw NotFoundException.For("Materia", id);
        return ToDto(materia);
    }

    public async Task<MateriaDto> CreateAsync(CrearMateriaDto dto, CancellationToken ct = default)
    {
        var nombre = dto.Nombre.Trim();
        await ValidarNombreUnicoAsync(nombre, null, ct);

        var materia = new Materia { Nombre = nombre };
        await _materias.AddAsync(materia, ct);
        await _materias.SaveChangesAsync(ct);
        return ToDto(materia);
    }

    public async Task UpdateAsync(int id, CrearMateriaDto dto, CancellationToken ct = default)
    {
        var materia = await _materias.GetByIdAsync(id, ct) ?? throw NotFoundException.For("Materia", id);
        var nombre = dto.Nombre.Trim();
        await ValidarNombreUnicoAsync(nombre, id, ct);

        materia.Nombre = nombre;
        await _materias.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var materia = await _materias.GetByIdAsync(id, ct) ?? throw NotFoundException.For("Materia", id);

        // La FK Alumno→Materia está en cascada: borrar una materia con alumnos los eliminaría a todos.
        // Se bloquea explícitamente para no perder datos por accidente.
        if (await _materias.TieneAlumnosAsync(id, ct))
            throw new ConflictException("MATERIA_CON_ALUMNOS",
                $"La materia \"{materia.Nombre}\" tiene alumnos asociados. Reasígnelos o elimínelos antes de borrarla.");

        _materias.Remove(materia);
        await _materias.SaveChangesAsync(ct);
    }

    private async Task ValidarNombreUnicoAsync(string nombre, int? excluirId, CancellationToken ct)
    {
        if (await _materias.NombreExistsAsync(nombre, excluirId, ct))
            throw new ConflictException("MATERIA_DUPLICADA", $"Ya existe una materia llamada \"{nombre}\".");
    }

    private static MateriaDto ToDto(Materia m) => new() { Id = m.Id, Nombre = m.Nombre };
}
