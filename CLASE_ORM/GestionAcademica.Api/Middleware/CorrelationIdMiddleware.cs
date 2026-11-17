using System.Diagnostics;
using System.Text.RegularExpressions;

namespace GestionAcademica.Api.Middleware;

/// <summary>
/// Middleware propio de trazabilidad:
/// 1. Toma el header X-Correlation-ID entrante (si es válido) o genera uno nuevo.
/// 2. Lo guarda en HttpContext.TraceIdentifier → aparece como "traceId" en los ProblemDetails.
/// 3. Lo devuelve en la respuesta (header X-Correlation-ID).
/// 4. Abre un scope de logging para que todos los logs de la solicitud lo incluyan.
/// 5. Registra método, ruta, código y duración. Nunca registra headers, body, tokens ni contraseñas.
/// </summary>
public partial class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ObtenerOGenerar(context);
        context.TraceIdentifier = correlationId;

        // OnStarting se ejecuta justo antes de enviar los headers, incluso si
        // el manejador de excepciones limpió la respuesta.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogInformation(
                    "HTTP {Method} {Path} -> {StatusCode} ({ElapsedMs} ms) [correlationId={CorrelationId}]",
                    context.Request.Method,
                    context.Request.Path.Value,   // solo la ruta: sin query string
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds,
                    correlationId);
            }
        }
    }

    private static string ObtenerOGenerar(HttpContext context)
    {
        var entrante = context.Request.Headers[HeaderName].ToString();

        // Se acepta el valor del cliente solo si es corto y seguro (evita inyección en logs).
        return !string.IsNullOrEmpty(entrante) && IdValido().IsMatch(entrante)
            ? entrante
            : Guid.NewGuid().ToString("N");
    }

    [GeneratedRegex("^[A-Za-z0-9\\-_.]{1,64}$")]
    private static partial Regex IdValido();
}
