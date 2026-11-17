using GestionAcademica.Application.Common;
using GestionAcademica.Application.DTOs.Alumno;
using GestionAcademica.Application.DTOs.Auth;
using GestionAcademica.Application.DTOs.Materia;
using GestionAcademica.Domain.Entities;

namespace GestionAcademica.Application.Interfaces;

// ───────────── Repositorios (implementados en Infrastructure con EF Core) ─────────────

public interface IAlumnoRepository
{
    /// <summary>Filtra, ordena y pagina en SQL. Devuelve la página y el total filtrado.</summary>
    Task<(IReadOnlyList<AlumnoDto> Items, int TotalCount)> GetPagedAsync(
        string? nombre, int? materiaId, int pageNumber, int pageSize, CancellationToken ct);
    Task<AlumnoDto?> GetDtoByIdAsync(int id, CancellationToken ct);
    Task<Alumno?> GetByIdAsync(int id, CancellationToken ct);
    Task<bool> EmailExistsAsync(string email, int? excluirId, CancellationToken ct);
    Task AddAsync(Alumno alumno, CancellationToken ct);
    void Remove(Alumno alumno);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IMateriaRepository
{
    Task<IReadOnlyList<Materia>> GetAllAsync(CancellationToken ct);
    Task<Materia?> GetByIdAsync(int id, CancellationToken ct);
    Task<bool> ExistsAsync(int id, CancellationToken ct);
    Task<bool> NombreExistsAsync(string nombre, int? excluirId, CancellationToken ct);
    Task<bool> TieneAlumnosAsync(int id, CancellationToken ct);
    Task AddAsync(Materia materia, CancellationToken ct);
    void Remove(Materia materia);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IUsuarioRepository
{
    Task<Usuario?> GetByEmailAsync(string email, CancellationToken ct);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct);
    Task AddAsync(Usuario usuario, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

// ───────────── Seguridad (implementada en Infrastructure: BCrypt y JWT) ─────────────

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface IJwtTokenGenerator
{
    (string Token, DateTime Expiration) Generate(Usuario usuario);
}

// ───────────── Casos de uso (implementados en Application/Services) ─────────────

public interface IAlumnoService
{
    Task<PagedResult<AlumnoDto>> GetPagedAsync(AlumnoFiltroDto filtro, CancellationToken ct = default);
    Task<AlumnoDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<AlumnoDto> CreateAsync(CrearAlumnoDto dto, CancellationToken ct = default);
    Task UpdateAsync(int id, ActualizarAlumnoDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public interface IMateriaService
{
    Task<IReadOnlyList<MateriaDto>> GetAllAsync(CancellationToken ct = default);
    Task<MateriaDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<MateriaDto> CreateAsync(CrearMateriaDto dto, CancellationToken ct = default);
    Task UpdateAsync(int id, CrearMateriaDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default);
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken ct = default);
}
