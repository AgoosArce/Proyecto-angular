namespace GestionAcademica.Application.Exceptions;

/// <summary>
/// Base de las excepciones de negocio. No conoce HTTP: la API decide el código de estado
/// según el tipo concreto (ver GlobalExceptionHandler en el proyecto Api).
/// </summary>
public abstract class AppException : Exception
{
    /// <summary>Código estable y legible por máquinas, ej. ALUMNO_NOT_FOUND.</summary>
    public string Code { get; }

    protected AppException(string code, string message) : base(message)
    {
        Code = code;
    }
}

/// <summary>Recurso inexistente (→ 404).</summary>
public class NotFoundException : AppException
{
    public NotFoundException(string code, string message) : base(code, message) { }

    /// <summary>Ej.: For("Alumno", 99) → ALUMNO_NOT_FOUND, "No existe un alumno con ID 99."</summary>
    public static NotFoundException For(string recurso, int id)
    {
        var articulo = recurso.EndsWith('a') ? "una" : "un";
        return new NotFoundException(
            $"{recurso.ToUpperInvariant()}_NOT_FOUND",
            $"No existe {articulo} {recurso.ToLowerInvariant()} con ID {id}.");
    }
}

/// <summary>Conflicto con el estado actual, ej. email duplicado (→ 409).</summary>
public class ConflictException : AppException
{
    public ConflictException(string code, string message) : base(code, message) { }
}

/// <summary>Datos de entrada inválidos (→ 400). Incluye errores por campo.</summary>
public class ValidationException : AppException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(string campo, string mensaje)
        : this(new Dictionary<string, string[]> { [campo] = new[] { mensaje } }) { }

    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("VALIDATION_ERROR", "Uno o más datos enviados no son válidos.")
    {
        Errors = errors;
    }
}

/// <summary>Credenciales inválidas (→ 401).</summary>
public class UnauthorizedException : AppException
{
    public UnauthorizedException(string code, string message) : base(code, message) { }
}
