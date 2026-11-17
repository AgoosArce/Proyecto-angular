using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GestionAcademica.Infrastructure.Persistence;

/// <summary>
/// Usada solo por las herramientas de EF (dotnet ef migrations / database update).
/// Permite generar migraciones sin arrancar la API (que exige la clave JWT, etc.).
/// La cadena se toma de la variable de entorno ConnectionStrings__Con si existe.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string DefaultConnection =
        "Server=(localdb)\\MSSQLLocalDB;Database=testprueba;Trusted_Connection=True;TrustServerCertificate=True;";

    public AppDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Con") ?? DefaultConnection;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connection)
            .Options;
        return new AppDbContext(options);
    }
}
