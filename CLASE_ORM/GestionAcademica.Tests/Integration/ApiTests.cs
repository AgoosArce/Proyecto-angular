using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GestionAcademica.Application.Common;
using GestionAcademica.Application.DTOs.Alumno;
using GestionAcademica.Application.DTOs.Auth;

namespace GestionAcademica.Tests.Integration;

public class ApiTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory _factory;

    public ApiTests(ApiFactory factory) => _factory = factory;

    public Task InitializeAsync() => _factory.SeedAsync(cantidadAlumnos: 12);
    public Task DisposeAsync() => Task.CompletedTask;

    private static async Task<JsonElement> ReadProblem(HttpResponseMessage response)
    {
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    // ───────────── Autenticación / autorización ─────────────

    [Fact]
    public async Task SinToken_Devuelve401ConProblemDetails()
    {
        var response = await _factory.CreateClient().GetAsync("/api/Alumno");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await ReadProblem(response);
        Assert.Equal("UNAUTHORIZED", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task TokenInvalido_Devuelve401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "abc.def.ghi");

        var response = await client.GetAsync("/api/Alumno");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("INVALID_TOKEN", (await ReadProblem(response)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task Usuario_PuedeConsultar()
    {
        var client = await _factory.CreateClientAsAsync(ApiFactory.UserEmail);
        var response = await client.GetAsync("/api/Materia");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("POST", "/api/Alumno")]
    [InlineData("PUT", "/api/Alumno/1")]
    [InlineData("DELETE", "/api/Alumno/1")]
    [InlineData("POST", "/api/Materia")]
    [InlineData("DELETE", "/api/Materia/1")]
    public async Task Usuario_NoPuedeModificar_Devuelve403(string method, string url)
    {
        var client = await _factory.CreateClientAsAsync(ApiFactory.UserEmail);
        var request = new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = JsonContent.Create(new { nombre = "Nuevo", email = "nuevo@test.com", materiaId = 1 })
        };

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("FORBIDDEN", (await ReadProblem(response)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task Admin_PuedeCrearAlumno_201()
    {
        var client = await _factory.CreateClientAsAsync(ApiFactory.AdminEmail);

        var response = await client.PostAsJsonAsync("/api/Alumno",
            new CrearAlumnoDto { Nombre = "Nueva Alumna", Email = "nueva@test.com", MateriaId = 1 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<AlumnoDto>();
        Assert.Equal("Matemática", dto!.Materia);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task Register_NoPermiteAutoasignarseAdmin()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/Auth/register",
            new { nombre = "Hacker", email = "hacker@test.com", password = "123456", rol = "Admin" });

        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.Equal("Usuario", auth!.Rol);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(auth.Token);
        Assert.Equal("Usuario", jwt.Claims.Single(c => c.Type == "role").Value);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        var create = await client.PostAsJsonAsync("/api/Materia", new { nombre = "Hackeo" });
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
    }

    [Fact]
    public async Task Register_EmailDuplicado_Devuelve409()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/Auth/register",
            new RegisterDto { Nombre = "Otro", Email = ApiFactory.UserEmail, Password = "123456" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("EMAIL_DUPLICADO", (await ReadProblem(response)).GetProperty("code").GetString());
    }

    // ───────────── Paginación y filtros ─────────────

    [Fact]
    public async Task Paginacion_DevuelvePaginaCorrecta()
    {
        var client = await _factory.CreateClientAsAsync(ApiFactory.UserEmail);

        var page = await client.GetFromJsonAsync<PagedResult<AlumnoDto>>("/api/Alumno?pageNumber=2&pageSize=5");

        Assert.NotNull(page);
        Assert.Equal(2, page!.PageNumber);
        Assert.Equal(5, page.Items.Count);
        Assert.True(page.TotalCount >= 12);
        Assert.Equal((int)Math.Ceiling(page.TotalCount / 5.0), page.TotalPages);
        Assert.True(page.Items.Select(a => a.Id).SequenceEqual(page.Items.Select(a => a.Id).Order()));
    }

    [Fact]
    public async Task Filtro_NombreYMateriaCombinados()
    {
        var client = await _factory.CreateClientAsAsync(ApiFactory.UserEmail);
        var materias = await client.GetFromJsonAsync<List<JsonElement>>("/api/Materia");
        var historiaId = materias!.First(m => m.GetProperty("nombre").GetString() == "Historia").GetProperty("id").GetInt32();

        var page = await client.GetFromJsonAsync<PagedResult<AlumnoDto>>($"/api/Alumno?nombre=Alumno 1&materiaId={historiaId}");

        // Alumno 10 y Alumno 12 son de Historia (pares) y contienen "Alumno 1".
        Assert.All(page!.Items, a => Assert.Equal("Historia", a.Materia));
        Assert.Equal(2, page.TotalCount);
    }

    [Fact]
    public async Task Filtro_SinCoincidencias_DevuelveItemsVacios()
    {
        var client = await _factory.CreateClientAsAsync(ApiFactory.UserEmail);
        var page = await client.GetFromJsonAsync<PagedResult<AlumnoDto>>("/api/Alumno?nombre=zzzz");
        Assert.Empty(page!.Items);
        Assert.Equal(0, page.TotalPages);
    }

    [Theory]
    [InlineData("pageNumber=0")]
    [InlineData("pageSize=101")]
    [InlineData("pageNumber=abc")]
    public async Task PaginacionInvalida_Devuelve400(string query)
    {
        var client = await _factory.CreateClientAsAsync(ApiFactory.UserEmail);
        var response = await client.GetAsync($"/api/Alumno?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await ReadProblem(response);
        Assert.Equal("VALIDATION_ERROR", problem.GetProperty("code").GetString());
        Assert.True(problem.TryGetProperty("errors", out _));
    }

    // ───────────── Errores y middleware ─────────────

    [Fact]
    public async Task AlumnoInexistente_Devuelve404_ConCodeYTraceIdIgualAlCorrelationId()
    {
        var client = await _factory.CreateClientAsAsync(ApiFactory.UserEmail);
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/Alumno/9999");
        request.Headers.Add("X-Correlation-ID", "prueba-404");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("prueba-404", response.Headers.GetValues("X-Correlation-ID").Single());
        var problem = await ReadProblem(response);
        Assert.Equal("ALUMNO_NOT_FOUND", problem.GetProperty("code").GetString());
        Assert.Equal("prueba-404", problem.GetProperty("traceId").GetString());
        Assert.Equal("/api/Alumno/9999", problem.GetProperty("instance").GetString());
    }

    [Fact]
    public async Task CorrelationId_SeGeneraSiNoSeEnvia_YSeRechazaSiEsInseguro()
    {
        var client = _factory.CreateClient();

        var sinHeader = await client.GetAsync("/api/Materia");
        Assert.Matches("^[0-9a-f]{32}$", sinHeader.Headers.GetValues("X-Correlation-ID").Single());

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/Materia");
        request.Headers.TryAddWithoutValidation("X-Correlation-ID", "malo\r\ninyeccion");
        var conHeaderMalo = await client.SendAsync(request);
        Assert.Matches("^[0-9a-f]{32}$", conHeaderMalo.Headers.GetValues("X-Correlation-ID").Single());
    }

    [Fact]
    public async Task DtoInvalido_Devuelve400ConErroresPorCampo()
    {
        var client = await _factory.CreateClientAsAsync(ApiFactory.AdminEmail);
        var response = await client.PostAsJsonAsync("/api/Alumno", new { nombre = "", email = "no-es-email", materiaId = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await ReadProblem(response)).GetProperty("errors");
        Assert.True(errors.TryGetProperty("Email", out _));
        Assert.True(errors.TryGetProperty("MateriaId", out _));
    }
}
