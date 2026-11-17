using GestionAcademica.Application.DTOs.Alumno;
using GestionAcademica.Application.Interfaces;
using GestionAcademica.Domain.Entities;
using GestionAcademica.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestionAcademica.Infrastructure.Repositories;

public class AlumnoRepository : IAlumnoRepository
{
    private readonly AppDbContext _db;

    public AlumnoRepository(AppDbContext db) => _db = db;

    public async Task<(IReadOnlyList<AlumnoDto> Items, int TotalCount)> GetPagedAsync(
        string? nombre, int? materiaId, int pageNumber, int pageSize, CancellationToken ct)
    {
        // Todo se traduce a SQL: WHERE → COUNT → ORDER BY → OFFSET/FETCH.
        // Nunca se descarga la tabla completa para paginar en memoria.
        var query = _db.Alumnos.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(nombre))
            query = query.Where(a => a.Nombre.Contains(nombre));   // LIKE '%nombre%' (sensibilidad según collation)

        if (materiaId.HasValue)
            query = query.Where(a => a.MateriaId == materiaId.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(a => a.Id)                                    // orden estable antes de paginar
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task<AlumnoDto?> GetDtoByIdAsync(int id, CancellationToken ct) =>
        _db.Alumnos.AsNoTracking().Where(a => a.Id == id).Select(ToDto).FirstOrDefaultAsync(ct);

    public Task<Alumno?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.Alumnos.FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<bool> EmailExistsAsync(string email, int? excluirId, CancellationToken ct) =>
        _db.Alumnos.AnyAsync(a => a.Email == email && (excluirId == null || a.Id != excluirId), ct);

    public async Task AddAsync(Alumno alumno, CancellationToken ct) => await _db.Alumnos.AddAsync(alumno, ct);

    public void Remove(Alumno alumno) => _db.Alumnos.Remove(alumno);

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);

    // Proyección traducible a SQL (JOIN con Materias).
    private static readonly System.Linq.Expressions.Expression<Func<Alumno, AlumnoDto>> ToDto = a => new AlumnoDto
    {
        Id = a.Id,
        Nombre = a.Nombre,
        Email = a.Email,
        MateriaId = a.MateriaId,
        Materia = a.Materia != null ? a.Materia.Nombre : string.Empty
    };
}

public class MateriaRepository : IMateriaRepository
{
    private readonly AppDbContext _db;

    public MateriaRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<Materia>> GetAllAsync(CancellationToken ct) =>
        await _db.Materias.AsNoTracking().OrderBy(m => m.Nombre).ToListAsync(ct);

    public Task<Materia?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.Materias.FirstOrDefaultAsync(m => m.Id == id, ct);

    public Task<bool> ExistsAsync(int id, CancellationToken ct) =>
        _db.Materias.AnyAsync(m => m.Id == id, ct);

    public Task<bool> NombreExistsAsync(string nombre, int? excluirId, CancellationToken ct) =>
        _db.Materias.AnyAsync(m => m.Nombre == nombre && (excluirId == null || m.Id != excluirId), ct);

    public Task<bool> TieneAlumnosAsync(int id, CancellationToken ct) =>
        _db.Alumnos.AnyAsync(a => a.MateriaId == id, ct);

    public async Task AddAsync(Materia materia, CancellationToken ct) => await _db.Materias.AddAsync(materia, ct);

    public void Remove(Materia materia) => _db.Materias.Remove(materia);

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}

public class UsuarioRepository : IUsuarioRepository
{
    private readonly AppDbContext _db;

    public UsuarioRepository(AppDbContext db) => _db = db;

    public Task<Usuario?> GetByEmailAsync(string email, CancellationToken ct) =>
        _db.Usuarios.FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct) =>
        _db.Usuarios.AnyAsync(u => u.Email == email, ct);

    public async Task AddAsync(Usuario usuario, CancellationToken ct) => await _db.Usuarios.AddAsync(usuario, ct);

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
