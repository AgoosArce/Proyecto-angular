using GestionAcademica.Application.DTOs.Alumno;
using GestionAcademica.Application.DTOs.Auth;
using GestionAcademica.Application.Exceptions;
using GestionAcademica.Application.Services;
using GestionAcademica.Domain.Constants;
using GestionAcademica.Domain.Entities;

namespace GestionAcademica.Tests.Unit;

public class AlumnoServiceTests
{
    private readonly FakeMateriaRepository _materias = new();
    private readonly FakeAlumnoRepository _alumnos;
    private readonly AlumnoService _service;

    public AlumnoServiceTests()
    {
        _alumnos = new FakeAlumnoRepository(_materias);
        _materias.Materias.Add(new Materia { Id = 1, Nombre = "Matemática" });
        _materias.Materias.Add(new Materia { Id = 2, Nombre = "Historia" });
        _service = new AlumnoService(_alumnos, _materias);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task GetPaged_ConPaginacionInvalida_LanzaValidationException(int page, int size)
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.GetPagedAsync(new AlumnoFiltroDto { PageNumber = page, PageSize = size }));
        Assert.Equal("VALIDATION_ERROR", ex.Code);
    }

    [Fact]
    public async Task GetPaged_SinRegistros_DevuelveItemsVacios()
    {
        var result = await _service.GetPagedAsync(new AlumnoFiltroDto());
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public async Task GetPaged_CalculaTotalPagesYFiltraCombinado()
    {
        for (var i = 1; i <= 12; i++)
            await _service.CreateAsync(new CrearAlumnoDto { Nombre = $"Ana {i}", Email = $"ana{i}@test.com", MateriaId = i % 2 + 1 });

        var pagina3 = await _service.GetPagedAsync(new AlumnoFiltroDto { PageNumber = 3, PageSize = 5 });
        Assert.Equal(12, pagina3.TotalCount);
        Assert.Equal(3, pagina3.TotalPages);
        Assert.Equal(2, pagina3.Items.Count);

        var filtrado = await _service.GetPagedAsync(new AlumnoFiltroDto { Nombre = "ana 1", MateriaId = 2 });
        // "Ana 1", "Ana 11" (impares → materia 2); "Ana 10", "Ana 12" son materia 1.
        Assert.All(filtrado.Items, a => Assert.Equal(2, a.MateriaId));
        Assert.Equal(2, filtrado.TotalCount);
    }

    [Fact]
    public async Task Create_ConMateriaInexistente_LanzaValidationException()
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.CreateAsync(new CrearAlumnoDto { Nombre = "Ana", Email = "ana@test.com", MateriaId = 99 }));
        Assert.True(ex.Errors.ContainsKey("materiaId"));
    }

    [Fact]
    public async Task Create_ConEmailDuplicado_LanzaConflictException()
    {
        await _service.CreateAsync(new CrearAlumnoDto { Nombre = "Ana", Email = "ana@test.com", MateriaId = 1 });

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            _service.CreateAsync(new CrearAlumnoDto { Nombre = "Otra", Email = " ANA@test.com ", MateriaId = 1 }));
        Assert.Equal("ALUMNO_EMAIL_DUPLICADO", ex.Code);
    }

    [Fact]
    public async Task GetById_Inexistente_LanzaNotFound()
    {
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(99));
        Assert.Equal("ALUMNO_NOT_FOUND", ex.Code);
        Assert.Equal("No existe un alumno con ID 99.", ex.Message);
    }
}

public class MateriaServiceTests
{
    [Fact]
    public async Task Delete_ConAlumnosAsociados_LanzaConflict()
    {
        var materias = new FakeMateriaRepository();
        var alumnos = new FakeAlumnoRepository(materias);
        materias.Materias.Add(new Materia { Id = 1, Nombre = "Física" });
        alumnos.Alumnos.Add(new Alumno { Id = 1, Nombre = "Ana", Email = "a@a.com", MateriaId = 1 });

        var ex = await Assert.ThrowsAsync<ConflictException>(() => new MateriaService(materias).DeleteAsync(1));
        Assert.Equal("MATERIA_CON_ALUMNOS", ex.Code);
        Assert.Single(materias.Materias);
    }
}

public class AuthServiceTests
{
    private readonly FakeUsuarioRepository _usuarios = new();
    private readonly AuthService _service;

    public AuthServiceTests() => _service = new AuthService(_usuarios, new FakeHasher(), new FakeTokens());

    [Fact]
    public async Task Register_SiempreCreaRolUsuario()
    {
        var result = await _service.RegisterAsync(new RegisterDto { Nombre = "Ana", Email = "Ana@Test.com", Password = "123456" });

        Assert.Equal(Roles.Usuario, result.Rol);
        Assert.Equal("ana@test.com", _usuarios.Usuarios.Single().Email);
        Assert.NotEqual("123456", _usuarios.Usuarios.Single().PasswordHash);
    }

    [Fact]
    public async Task Register_EmailDuplicado_LanzaConflict()
    {
        await _service.RegisterAsync(new RegisterDto { Nombre = "Ana", Email = "ana@test.com", Password = "123456" });
        await Assert.ThrowsAsync<ConflictException>(() =>
            _service.RegisterAsync(new RegisterDto { Nombre = "Ana", Email = "ana@test.com", Password = "123456" }));
    }

    [Fact]
    public async Task Login_PasswordIncorrecta_LanzaUnauthorized()
    {
        await _service.RegisterAsync(new RegisterDto { Nombre = "Ana", Email = "ana@test.com", Password = "123456" });
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _service.LoginAsync(new LoginDto { Email = "ana@test.com", Password = "otra" }));
        Assert.Equal("INVALID_CREDENTIALS", ex.Code);
    }

    [Fact]
    public async Task Login_DevuelveRolPersistido()
    {
        _usuarios.Usuarios.Add(new Usuario { Id = 1, Nombre = "Admin", Email = "admin@test.com", PasswordHash = "hash:secreto", Rol = Roles.Admin });
        var result = await _service.LoginAsync(new LoginDto { Email = "admin@test.com", Password = "secreto" });
        Assert.Equal(Roles.Admin, result.Rol);
    }
}
