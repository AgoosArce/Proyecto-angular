using GestionAcademica.Application.Interfaces;
using GestionAcademica.Domain.Constants;
using GestionAcademica.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GestionAcademica.Infrastructure.Persistence;

/// <summary>
/// Crea (o promueve) un administrador de desarrollo a partir de la configuración
/// SeedAdmin:Email / SeedAdmin:Password / SeedAdmin:Nombre (user-secrets o variables de entorno).
/// Es el único camino para obtener el rol Admin: el registro público siempre crea "Usuario".
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAdminAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        var email = config["SeedAdmin:Email"]?.Trim().ToLowerInvariant();
        var password = config["SeedAdmin:Password"];
        var nombre = config["SeedAdmin:Nombre"] ?? "Administrador";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation("SeedAdmin no configurado: no se crea administrador de desarrollo.");
            return;
        }

        try
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
            if (usuario is null)
            {
                db.Usuarios.Add(new Usuario
                {
                    Nombre = nombre,
                    Email = email,
                    PasswordHash = hasher.Hash(password),
                    Rol = Roles.Admin
                });
                logger.LogInformation("Administrador de desarrollo creado: {Email}", email);
            }
            else if (usuario.Rol != Roles.Admin)
            {
                usuario.Rol = Roles.Admin;
                logger.LogInformation("Usuario {Email} promovido a Admin.", email);
            }

            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // No impide arrancar la API (por ejemplo, si falta aplicar la migración AddUsuarioRol).
            logger.LogWarning(ex, "No se pudo ejecutar el seeding del administrador. ¿Se aplicaron las migraciones?");
        }
    }
}
