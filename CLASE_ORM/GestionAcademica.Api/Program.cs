using GestionAcademica.Api.Configuration;
using GestionAcademica.Api.Middleware;
using GestionAcademica.Application;
using GestionAcademica.Infrastructure;
using GestionAcademica.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ───────────── Punto de composición: aquí se unen Application e Infrastructure ─────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

builder.Services.AddApiErrorHandling();       // ProblemDetails + GlobalExceptionHandler
builder.Services.AddApiControllers();         // controllers + respuesta 400 homogénea
builder.Services.AddJwtAuthentication();      // JWT Bearer + roles + 401/403 en JSON
builder.Services.AddAuthRateLimiting();       // límite de intentos en /api/Auth
builder.Services.AddAngularCors(builder.Configuration);
builder.Services.AddSwaggerWithJwt();

var app = builder.Build();

// ───────────── Pipeline HTTP (el orden importa) ─────────────
// 1. Correlación primero: así el traceId y el header existen incluso en respuestas de error.
app.UseMiddleware<CorrelationIdMiddleware>();
// 2. Captura de excepciones → GlobalExceptionHandler.
app.UseExceptionHandler();
// 3. Respuestas de error sin cuerpo (404 de ruta inexistente, 405...) → ProblemDetails.
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors(ApiServiceExtensions.CorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Administrador de desarrollo (solo si SeedAdmin:Email/Password están configurados).
await DbSeeder.SeedAdminAsync(app.Services);

app.Run();

// Necesario para WebApplicationFactory<Program> en las pruebas de integración.
public partial class Program { }
