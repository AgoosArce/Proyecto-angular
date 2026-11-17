using System.Text;
using GestionAcademica.Application.Interfaces;
using GestionAcademica.Infrastructure.Persistence;
using GestionAcademica.Infrastructure.Repositories;
using GestionAcademica.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GestionAcademica.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // La cadena se resuelve al crear el DbContext (no al registrar), así los tests
        // y los distintos entornos pueden reemplazar la configuración sin tocar este código.
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var connection = sp.GetRequiredService<IConfiguration>().GetConnectionString("Con")
                ?? throw new InvalidOperationException("Falta la cadena de conexión 'ConnectionStrings:Con'.");
            options.UseSqlServer(connection);
        });

        services.AddScoped<IAlumnoRepository, AlumnoRepository>();
        services.AddScoped<IMateriaRepository, MateriaRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();

        services.AddOptions<JwtSettings>()
            .BindConfiguration(JwtSettings.SectionName)
            .Validate(s => Encoding.UTF8.GetByteCount(s.Key) >= 32,
                "Jwt:Key debe tener al menos 32 bytes. Configúrela con 'dotnet user-secrets set \"Jwt:Key\" \"...\"' o la variable de entorno Jwt__Key.")
            .Validate(s => !string.IsNullOrWhiteSpace(s.Issuer) && !string.IsNullOrWhiteSpace(s.Audience),
                "Jwt:Issuer y Jwt:Audience son obligatorios.")
            .ValidateOnStart();

        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}
