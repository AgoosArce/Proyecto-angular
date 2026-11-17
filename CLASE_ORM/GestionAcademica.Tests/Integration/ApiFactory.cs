using System.Net.Http.Headers;
using System.Net.Http.Json;
using GestionAcademica.Application.DTOs.Auth;
using GestionAcademica.Application.Interfaces;
using GestionAcademica.Domain.Constants;
using GestionAcademica.Domain.Entities;
using GestionAcademica.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GestionAcademica.Tests.Integration;

/// <summary>
/// Levanta la API real en memoria (TestServer) con una base EF InMemory aislada por instancia.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@test.com";
    public const string UserEmail = "usuario@test.com";
    public const string Password = "Secreta123";

    private readonly string _dbName = "tests-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Jwt:Key", "clave-de-pruebas-de-integracion-con-mas-de-32-bytes");
        builder.UseSetting("RateLimiting:AuthPermitLimitPerMinute", "1000");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(_dbName));
        });
    }

    public async Task SeedAsync(int cantidadAlumnos = 3)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        if (await db.Usuarios.AnyAsync()) return;

        db.Usuarios.AddRange(
            new Usuario { Nombre = "Admin", Email = AdminEmail, PasswordHash = hasher.Hash(Password), Rol = Roles.Admin },
            new Usuario { Nombre = "Usuario", Email = UserEmail, PasswordHash = hasher.Hash(Password), Rol = Roles.Usuario });

        var mate = new Materia { Nombre = "Matemática" };
        var historia = new Materia { Nombre = "Historia" };
        db.Materias.AddRange(mate, historia);

        for (var i = 1; i <= cantidadAlumnos; i++)
            db.Alumnos.Add(new Alumno { Nombre = $"Alumno {i}", Email = $"alumno{i}@test.com", Materia = i % 2 == 0 ? historia : mate });

        await db.SaveChangesAsync();
    }

    public async Task<HttpClient> CreateClientAsAsync(string email)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/Auth/login", new LoginDto { Email = email, Password = Password });
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        return client;
    }
}
