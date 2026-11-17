using System.Text;
using System.Threading.RateLimiting;
using GestionAcademica.Api.ExceptionHandling;
using GestionAcademica.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace GestionAcademica.Api.Configuration;

public static class RateLimitPolicies
{
    public const string Auth = "auth";
}

public static class ApiServiceExtensions
{
    public const string CorsPolicy = "AngularClient";

    /// <summary>ProblemDetails homogéneo para TODAS las respuestas de error (excepciones, validación, 401/403/404...).</summary>
    public static IServiceCollection AddApiErrorHandling(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = ctx =>
            {
                var problem = ctx.ProblemDetails;
                var status = problem.Status ?? ctx.HttpContext.Response.StatusCode;

                problem.Instance ??= ctx.HttpContext.Request.Path;
                problem.Type = $"https://httpstatuses.com/{status}";
                // TraceIdentifier = X-Correlation-ID (lo asigna CorrelationIdMiddleware).
                problem.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;

                // Respuestas que no vienen del GlobalExceptionHandler: completar code y título en español.
                if (!problem.Extensions.ContainsKey("code"))
                {
                    problem.Extensions["code"] = ErrorCodes.ForStatus(status);
                    problem.Title = ErrorCodes.TitleForStatus(status);
                }
            };
        });

        services.AddExceptionHandler<GlobalExceptionHandler>();
        return services;
    }

    public static IServiceCollection AddApiControllers(this IServiceCollection services)
    {
        services.AddControllers()
            .ConfigureApiBehaviorOptions(options =>
            {
                // DTO inválido (DataAnnotations) o query mal formada → 400 con el mismo formato.
                options.InvalidModelStateResponseFactory = context =>
                {
                    var problem = new ValidationProblemDetails(context.ModelState)
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Datos inválidos",
                        Detail = "Uno o más datos enviados no son válidos."
                    };
                    problem.Extensions["code"] = ErrorCodes.Validation;

                    var pdOptions = context.HttpContext.RequestServices.GetRequiredService<IOptions<ProblemDetailsOptions>>().Value;
                    pdOptions.CustomizeProblemDetails?.Invoke(new ProblemDetailsContext
                    {
                        HttpContext = context.HttpContext,
                        ProblemDetails = problem
                    });

                    return new BadRequestObjectResult(problem)
                    {
                        ContentTypes = { "application/problem+json" }
                    };
                };
            });
        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Se configura de forma diferida para leer JwtSettings ya validado (Key >= 32 bytes).
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtSettings>>((options, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;

                // Sin mapeo a URIs largas: el claim "role" del token se usa tal cual.
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    RoleClaimType = JwtClaimNames.Role,   // hace funcionar [Authorize(Roles = "Admin")]
                    NameClaimType = JwtClaimNames.Nombre,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };

                // 401 y 403 no son excepciones: se escriben aquí con el mismo formato ProblemDetails.
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async ctx =>
                    {
                        ctx.HandleResponse();
                        var (code, detail) = ctx.AuthenticateFailure switch
                        {
                            SecurityTokenExpiredException => ("TOKEN_EXPIRED", "El token expiró. Inicie sesión nuevamente."),
                            not null => ("INVALID_TOKEN", "El token enviado no es válido."),
                            _ => (ErrorCodes.Unauthorized, "Debe iniciar sesión para acceder a este recurso.")
                        };
                        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        ctx.Response.Headers.WWWAuthenticate = "Bearer";
                        await WriteProblemAsync(ctx.HttpContext, 401, "No autenticado", detail, code);
                    },
                    OnForbidden = async ctx =>
                    {
                        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await WriteProblemAsync(ctx.HttpContext, 403, "Acceso denegado",
                            "Su rol no tiene permiso para realizar esta operación.", ErrorCodes.Forbidden);
                    }
                };
            });

        services.AddAuthorization();
        return services;
    }

    /// <summary>Límite por IP para /api/Auth/* (protección básica contra fuerza bruta).</summary>
    public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(RateLimitPolicies.Auth, httpContext =>
            {
                var config = httpContext.RequestServices.GetRequiredService<IConfiguration>();
                var permitLimit = config.GetValue("RateLimiting:AuthPermitLimitPerMinute", 10);

                return RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    });
            });
            options.OnRejected = async (ctx, _) =>
            {
                await WriteProblemAsync(ctx.HttpContext, 429, "Demasiadas solicitudes",
                    "Superó el límite de intentos. Espere un minuto e intente nuevamente.", ErrorCodes.TooManyRequests);
            };
        });
        return services;
    }

    public static IServiceCollection AddAngularCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:4200" };
        services.AddCors(options => options.AddPolicy(CorsPolicy, policy => policy
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders(Middleware.CorrelationIdMiddleware.HeaderName)));
        return services;
    }

    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Gestión Académica API", Version = "v1" });

            var scheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Pegue solo el token JWT (sin la palabra Bearer).",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            };
            c.AddSecurityDefinition("Bearer", scheme);
            c.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = Array.Empty<string>() });

            var xml = Path.Combine(AppContext.BaseDirectory, "GestionAcademica.Api.xml");
            if (File.Exists(xml)) c.IncludeXmlComments(xml);
        });
        return services;
    }

    private static async Task WriteProblemAsync(HttpContext http, int status, string title, string detail, string code)
    {
        var problem = new ProblemDetails { Status = status, Title = title, Detail = detail };
        problem.Extensions["code"] = code;

        var service = http.RequestServices.GetRequiredService<IProblemDetailsService>();
        await service.TryWriteAsync(new ProblemDetailsContext { HttpContext = http, ProblemDetails = problem });
    }
}
