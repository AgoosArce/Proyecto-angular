using GestionAcademica.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace GestionAcademica.Api.ExceptionHandling;

/// <summary>
/// Captura cualquier excepción no controlada y la convierte en un ProblemDetails (RFC 7807).
/// Reemplaza los try/catch repetidos en cada controller.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(IProblemDetailsService problemDetails, ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetails = problemDetails;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, title, code, detail) = exception switch
        {
            ValidationException e => (StatusCodes.Status400BadRequest, "Datos inválidos", e.Code, e.Message),
            UnauthorizedException e => (StatusCodes.Status401Unauthorized, "No autenticado", e.Code, e.Message),
            NotFoundException e => (StatusCodes.Status404NotFound, "Recurso no encontrado", e.Code, e.Message),
            ConflictException e => (StatusCodes.Status409Conflict, "Conflicto con el estado actual", e.Code, e.Message),
            BadHttpRequestException e => (e.StatusCode, "Solicitud inválida", ErrorCodes.Validation, "La solicitud no pudo procesarse."),
            OperationCanceledException when context.RequestAborted.IsCancellationRequested =>
                (499, "Solicitud cancelada", "REQUEST_CANCELLED", "El cliente canceló la solicitud."),
            // Cualquier otra cosa: 500 sin filtrar mensaje ni stack trace al cliente.
            _ => (StatusCodes.Status500InternalServerError, "Error interno del servidor", ErrorCodes.Internal,
                  "Ocurrió un error inesperado. Informe el traceId al administrador.")
        };

        if (status >= 500)
            _logger.LogError(exception, "Excepción no controlada. traceId={TraceId}", context.TraceIdentifier);
        else
            _logger.LogWarning("Excepción de negocio {Code} ({Status}): {Message}. traceId={TraceId}",
                code, status, exception.Message, context.TraceIdentifier);

        context.Response.StatusCode = status;

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Type = $"https://httpstatuses.com/{status}"
        };
        problem.Extensions["code"] = code;

        if (exception is ValidationException validation)
            problem.Extensions["errors"] = validation.Errors;

        // traceId e instance se agregan en CustomizeProblemDetails (Program.cs).
        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problem,
            Exception = exception
        });
    }
}

public static class ErrorCodes
{
    public const string Validation = "VALIDATION_ERROR";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string NotFound = "RESOURCE_NOT_FOUND";
    public const string Conflict = "RESOURCE_CONFLICT";
    public const string TooManyRequests = "TOO_MANY_REQUESTS";
    public const string Internal = "INTERNAL_ERROR";

    /// <summary>Código por defecto cuando la respuesta no viene de una excepción (ej. 401/403 de JWT).</summary>
    public static string ForStatus(int status) => status switch
    {
        400 => Validation,
        401 => Unauthorized,
        403 => Forbidden,
        404 => NotFound,
        409 => Conflict,
        429 => TooManyRequests,
        _ => Internal
    };

    public static string TitleForStatus(int status) => status switch
    {
        400 => "Datos inválidos",
        401 => "No autenticado",
        403 => "Acceso denegado",
        404 => "Recurso no encontrado",
        405 => "Método no permitido",
        409 => "Conflicto con el estado actual",
        429 => "Demasiadas solicitudes",
        _ => "Error interno del servidor"
    };
}
