using GestionAcademica.Application.DTOs.Alumno;
using GestionAcademica.Application.Interfaces;
using GestionAcademica.Domain.Entities;

namespace GestionAcademica.Tests.Unit;

// Repositorios en memoria: permiten probar Application sin EF Core ni SQL Server.

internal class FakeMateriaRepository : IMateriaRepository
{
    public List<Materia> Materias { get; } = new();
    public List<Alumno> Alumnos { get; set; } = new();

    public Task<IReadOnlyList<Materia>> GetAllAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Materia>>(Materias.ToList());
    public Task<Materia?> GetByIdAsync(int id, CancellationToken ct) => Task.FromResult(Materias.FirstOrDefault(m => m.Id == id));
    public Task<bool> ExistsAsync(int id, CancellationToken ct) => Task.FromResult(Materias.Any(m => m.Id == id));
    public Task<bool> NombreExistsAsync(string nombre, int? excluirId, CancellationToken ct) =>
        Task.FromResult(Materias.Any(m => string.Equals(m.Nombre, nombre, StringComparison.OrdinalIgnoreCase) && m.Id != excluirId));
    public Task<bool> TieneAlumnosAsync(int id, CancellationToken ct) => Task.FromResult(Alumnos.Any(a => a.MateriaId == id));
    public Task AddAsync(Materia materia, CancellationToken ct) { materia.Id = Materias.Count + 1; Materias.Add(materia); return Task.CompletedTask; }
    public void Remove(Materia materia) => Materias.Remove(materia);
    public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
}

internal class FakeAlumnoRepository : IAlumnoRepository
{
    private readonly FakeMateriaRepository _materias;
    public List<Alumno> Alumnos { get; } = new();

    public FakeAlumnoRepository(FakeMateriaRepository materias)
    {
        _materias = materias;
        _materias.Alumnos = Alumnos;
    }

    public Task<(IReadOnlyList<AlumnoDto> Items, int TotalCount)> GetPagedAsync(string? nombre, int? materiaId, int pageNumber, int pageSize, CancellationToken ct)
    {
        var q = Alumnos.AsEnumerable();
        if (nombre is not null) q = q.Where(a => a.Nombre.Contains(nombre, StringComparison.OrdinalIgnoreCase));
        if (materiaId is not null) q = q.Where(a => a.MateriaId == materiaId);
        var list = q.OrderBy(a => a.Id).ToList();
        IReadOnlyList<AlumnoDto> page = list.Skip((pageNumber - 1) * pageSize).Take(pageSize).Select(ToDto).ToList();
        return Task.FromResult((page, list.Count));
    }

    public Task<AlumnoDto?> GetDtoByIdAsync(int id, CancellationToken ct) =>
        Task.FromResult(Alumnos.Where(a => a.Id == id).Select(ToDto).FirstOrDefault());
    public Task<Alumno?> GetByIdAsync(int id, CancellationToken ct) => Task.FromResult(Alumnos.FirstOrDefault(a => a.Id == id));
    public Task<bool> EmailExistsAsync(string email, int? excluirId, CancellationToken ct) =>
        Task.FromResult(Alumnos.Any(a => a.Email == email && a.Id != excluirId));
    public Task AddAsync(Alumno alumno, CancellationToken ct) { alumno.Id = Alumnos.Count + 1; Alumnos.Add(alumno); return Task.CompletedTask; }
    public void Remove(Alumno alumno) => Alumnos.Remove(alumno);
    public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;

    private AlumnoDto ToDto(Alumno a) => new()
    {
        Id = a.Id, Nombre = a.Nombre, Email = a.Email, MateriaId = a.MateriaId,
        Materia = _materias.Materias.FirstOrDefault(m => m.Id == a.MateriaId)?.Nombre ?? ""
    };
}

internal class FakeUsuarioRepository : IUsuarioRepository
{
    public List<Usuario> Usuarios { get; } = new();

    public Task<Usuario?> GetByEmailAsync(string email, CancellationToken ct) => Task.FromResult(Usuarios.FirstOrDefault(u => u.Email == email));
    public Task<bool> EmailExistsAsync(string email, CancellationToken ct) => Task.FromResult(Usuarios.Any(u => u.Email == email));
    public Task AddAsync(Usuario usuario, CancellationToken ct) { usuario.Id = Usuarios.Count + 1; Usuarios.Add(usuario); return Task.CompletedTask; }
    public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
}

internal class FakeHasher : IPasswordHasher
{
    public string Hash(string password) => "hash:" + password;
    public bool Verify(string password, string hash) => hash == "hash:" + password;
}

internal class FakeTokens : IJwtTokenGenerator
{
    public (string Token, DateTime Expiration) Generate(Usuario usuario) => ($"token-{usuario.Rol}", DateTime.UtcNow.AddHours(1));
}
