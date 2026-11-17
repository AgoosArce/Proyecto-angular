using CLASE_ORM.Context;
using CLASE_ORM.Entities.DTO.Auth;
using CLASE_ORM.Interfaces;
using CLASE_ORM.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>
    (options => options.UseSqlServer(builder.Configuration.GetConnectionString("Con"))
);

builder.Services.AddScoped<IAlumnoService, AlumnoService>();
builder.Services.AddScoped<IMateriaService, MateriaService>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? string.Empty))
    };
});

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular",
        policy =>
        {
            policy.WithOrigins("http://localhost:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


//Para empezar a usar el ORM EntityFrameworkCore.SqlServer, tienen que instalarlo desde el administrador de paquetes Nuget
// esta en Herramientas => Administrador de paquetes Nuget => Administrar paquetes Nuget.
//Son dos cosas es EntityFrameworkCore.SqlServer y EntityFrameworkCore.Tools.
//tiene que coincidir con la versi�n de .NET que eligieron, no instalen la 10 o superior si est�n usando en su propia soluci�n la version 8.


app.UseHttpsRedirection();

app.UseCors("AllowAngular");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
