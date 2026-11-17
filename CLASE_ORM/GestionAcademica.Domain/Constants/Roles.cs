namespace GestionAcademica.Domain.Constants;

/// <summary>
/// Roles válidos del sistema. Se usan como string controlado (no enum) para
/// que coincidan tal cual con el claim "role" del JWT y con [Authorize(Roles = ...)].
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Usuario = "Usuario";

    public static readonly IReadOnlyCollection<string> Todos = new[] { Admin, Usuario };

    public static bool EsValido(string? rol) => rol is not null && Todos.Contains(rol);
}
