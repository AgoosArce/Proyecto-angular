using GestionAcademica.Application.Interfaces;
using GestionAcademica.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GestionAcademica.Application;

public static class DependencyInjection
{
    /// <summary>Registra los casos de uso. Lo invoca Program.cs (punto de composición).</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAlumnoService, AlumnoService>();
        services.AddScoped<IMateriaService, MateriaService>();
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}
