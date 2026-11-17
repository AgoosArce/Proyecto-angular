# Gestión Académica — .NET 8 + Angular 20

Aplicación full-stack para administrar **Alumnos** y **Materias** (relación uno-a-muchos), con registro, inicio de sesión y permisos por rol.

Este documento explica **qué se implementó, cómo funciona cada parte y cómo ejecutarla**, siguiendo el plan de [docs/README_PLAN_EVOLUCION.md](docs/README_PLAN_EVOLUCION.md).

| Requisito del trabajo práctico | Dónde está |
|---|---|
| API REST propia en ASP.NET Core 8 | `CLASE_ORM/GestionAcademica.Api` |
| ORM Entity Framework Core + SQL Server, **Code First** | `GestionAcademica.Infrastructure/Persistence` (migraciones incluidas) |
| **Clean Architecture** | 4 proyectos: Domain, Application, Infrastructure, Api |
| JWT + **autorización por roles** | `Admin` administra, `Usuario` solo consulta |
| **Paginación y filtrado** en el servidor | `GET /api/Alumno?pageNumber=&pageSize=&nombre=&materiaId=` |
| **Middleware personalizado** | `CorrelationIdMiddleware` (header `X-Correlation-ID` + logs) |
| **Manejo global de excepciones** | `GlobalExceptionHandler` (`IExceptionHandler` + `ProblemDetails`) |
| Angular 20 standalone + Angular Material | `TestAngular/` (`MatTable`, `MatPaginator`, filtros, diálogos) |
| Pruebas automatizadas | `GestionAcademica.Tests` — 33 pruebas (unitarias + integración) |

---

## Índice

1. [Estructura del repositorio](#1-estructura-del-repositorio)
2. [Puesta en marcha](#2-puesta-en-marcha)
3. [Clean Architecture](#3-clean-architecture)
4. [Code First y rol de usuario](#4-code-first-y-rol-de-usuario)
5. [Autenticación JWT y autorización por roles](#5-autenticación-jwt-y-autorización-por-roles)
6. [Paginación y filtrado](#6-paginación-y-filtrado)
7. [Middleware personalizado: CorrelationIdMiddleware](#7-middleware-personalizado-correlationidmiddleware)
8. [Manejo global de excepciones](#8-manejo-global-de-excepciones)
9. [Reglas de negocio y validaciones](#9-reglas-de-negocio-y-validaciones)
10. [Frontend Angular](#10-frontend-angular)
11. [Endpoints](#11-endpoints)
12. [Pruebas](#12-pruebas)
13. [Guion de demostración](#13-guion-de-demostración)
14. [Cambios respecto de la versión anterior](#14-cambios-respecto-de-la-versión-anterior)
15. [Pendientes y limitaciones conocidas](#15-pendientes-y-limitaciones-conocidas)

---

## 1. Estructura del repositorio

```text
.NET-ANGULAR-main/
├── CLASE_ORM/                          # Backend: solución GestionAcademica.sln
│   ├── GestionAcademica.Domain/        # Entidades y constantes de rol (sin dependencias)
│   ├── GestionAcademica.Application/   # Casos de uso, DTOs, contratos, excepciones, paginación
│   ├── GestionAcademica.Infrastructure/# EF Core, migraciones, repositorios, BCrypt, JWT
│   ├── GestionAcademica.Api/           # Controllers, middleware, manejo de errores, Program.cs
│   └── GestionAcademica.Tests/         # xUnit: pruebas unitarias y de integración
├── TestAngular/                        # Frontend Angular 20
├── SeedUser/                           # Consola de desarrollo para crear usuarios con rol
├── legacy/CLASE_ORM_original/          # Copia intacta de la API anterior (un solo proyecto)
├── docs/                               # Plan de evolución y documentación de la versión anterior
└── README.md                           # Este documento
```

> `legacy/CLASE_ORM_original` y `.NET-ANGULAR/` son copias de versiones anteriores, solo como referencia. **No forman parte de la solución activa.**

---

## 2. Puesta en marcha

### Requisitos

- .NET SDK 8 o superior (los proyectos apuntan a `net8.0`) y el **runtime ASP.NET Core 8**.
- SQL Server LocalDB (`(localdb)\MSSQLLocalDB`).
- Node.js 20+ y npm.
- Herramienta EF: `dotnet tool install --global dotnet-ef --version 8.*`

### 2.1 Configurar secretos (una sola vez)

La clave JWT **ya no está en `appsettings.json`**. Se guarda con *user-secrets* (fuera del repositorio):

```bash
cd CLASE_ORM/GestionAcademica.Api
dotnet user-secrets set "Jwt:Key" "<clave aleatoria de al menos 32 caracteres>"

# Administrador de desarrollo (se crea solo al arrancar la API)
dotnet user-secrets set "SeedAdmin:Email" "admin@gestion.local"
dotnet user-secrets set "SeedAdmin:Password" "<contraseña>"
dotnet user-secrets set "SeedAdmin:Nombre" "Administrador"
```

Si `Jwt:Key` falta o es corta, la API **no arranca** y muestra un mensaje que explica cómo configurarla. En producción se usan variables de entorno (`Jwt__Key`, `SeedAdmin__Email`, …).

### 2.2 Base de datos

```bash
cd CLASE_ORM
dotnet ef database update \
  --project GestionAcademica.Infrastructure \
  --startup-project GestionAcademica.Api \
  --context AppDbContext
```

Sobre una base `testprueba` existente solo aplica la migración nueva `AddUsuarioRol`.

### 2.3 API

```bash
cd CLASE_ORM
dotnet run --project GestionAcademica.Api --launch-profile http
```

Swagger: <http://localhost:5201/swagger>. Para probar endpoints protegidos: `POST /api/Auth/login` → copiar `token` → botón **Authorize** → pegar el token (sin la palabra `Bearer`).

### 2.4 Frontend

```bash
cd TestAngular
npm install
npm start          # http://localhost:4200
```

### 2.5 Cargar datos de prueba (usuarios, roles, materias y alumnos)

```powershell
powershell -ExecutionPolicy Bypass -File scripts\cargar-datos-prueba.ps1
```

Ejecuta [scripts/seed-datos-prueba.sql](scripts/seed-datos-prueba.sql), que se puede correr varias veces sin duplicar datos. Crea 2 admins (`admin@test.com` / `Admin123!`), 3 usuarios de consulta (`docente@test.com` / `Usuario123!`, …), 8 materias y 30 alumnos. Requiere haber aplicado las migraciones (paso 2.2). Sin `sqlcmd`, abra el `.sql` en SSMS o Visual Studio y ejecútelo sobre `testprueba`.

### 2.6 Crear usuarios adicionales (opcional, solo desarrollo)

```bash
cd SeedUser
dotnet run -- otro.admin@test.com MiClave123 "Otro Admin" Admin
dotnet run -- usuario.prueba@test.com MiClave123 "Usuario" Usuario   # si el email existe, solo cambia el rol
```

---

## 3. Clean Architecture

El proyecto `CLASE_ORM` era una sola aplicación organizada por carpetas. Ahora es una solución de **cuatro capas** con dependencias en una sola dirección:

```text
           ┌──────────────────────────────┐
           │  Api (controllers, middleware,│  ← punto de composición (Program.cs)
           │  manejo de errores, JWT)      │
           └──────┬───────────────┬────────┘
                  │               │
                  ▼               ▼
        ┌──────────────┐   ┌──────────────────┐
        │ Application  │◄──│  Infrastructure  │  (EF Core, SQL Server, BCrypt, JWT)
        │ casos de uso │   └────────┬─────────┘
        └──────┬───────┘            │
               ▼                    ▼
           ┌──────────────────────────┐
           │  Domain (entidades)      │
           └──────────────────────────┘
```

| Capa | Contiene | Depende de |
|---|---|---|
| **Domain** | `Alumno`, `Materia`, `Profesor`, `Usuario`, `Roles` | Nada |
| **Application** | DTOs, `PagedResult<T>`, interfaces (`IAlumnoRepository`, `IPasswordHasher`, …), servicios (`AlumnoService`, `MateriaService`, `AuthService`), excepciones de negocio | Domain |
| **Infrastructure** | `AppDbContext`, migraciones, repositorios EF Core, `BCryptPasswordHasher`, `JwtTokenGenerator`, `DbSeeder` | Application, Domain |
| **Api** | Controllers, `CorrelationIdMiddleware`, `GlobalExceptionHandler`, configuración de JWT, CORS, Swagger y rate limiting | Application, Infrastructure |

**Por qué importa:**

- Los servicios de Application **no conocen EF Core ni HTTP**. Reciben interfaces (`IAlumnoRepository`) y lanzan excepciones como `NotFoundException`; nunca devuelven `IActionResult`. Eso permite probarlos con repositorios falsos en memoria (ver `Tests/Unit/Fakes.cs`).
- Cambiar SQL Server por otra base solo afecta a Infrastructure.
- Cada capa registra sus servicios con un método de extensión (`AddApplication()`, `AddInfrastructure()`), y `Program.cs` los une.

**Ejemplo de flujo** `POST /api/Alumno`:

1. `AlumnoController.Create` recibe `CrearAlumnoDto` (ASP.NET ya validó las DataAnnotations).
2. Llama a `IAlumnoService.CreateAsync` (Application).
3. El servicio verifica que la materia exista y que el email no esté repetido, usando `IMateriaRepository` / `IAlumnoRepository`.
4. `AlumnoRepository` (Infrastructure) guarda con EF Core.
5. El controller responde `201 Created` con el `AlumnoDto` y el header `Location`.

---

## 4. Code First y rol de usuario

- Se **conservó el historial de migraciones** (`init`, `AddProfesor`, `AddEmail`, `AddUsuarioAuth`) con sus IDs originales. Así EF reconoce la base `testprueba` existente y no la recrea.
- Se agregó `Usuario.Rol` y la migración **`AddUsuarioRol`**:

```csharp
migrationBuilder.AddColumn<string>(
    name: "Rol", table: "Usuarios", type: "nvarchar(20)",
    maxLength: 20, nullable: false, defaultValue: "Usuario");
```

  El `defaultValue` asigna `Usuario` a todos los usuarios que ya existían: **no se pierde ningún registro**.
- Los roles válidos están en `Domain/Constants/Roles.cs` (`Admin`, `Usuario`) como strings controlados, para que coincidan exactamente con el claim del JWT y con `[Authorize(Roles = Roles.Admin)]`.
- `DesignTimeDbContextFactory` permite ejecutar `dotnet ef` sin arrancar toda la API.

**¿Cómo se obtiene un Admin?** Nunca desde el registro público. Solo mediante:

1. `DbSeeder` (al arrancar la API, si `SeedAdmin:Email` y `SeedAdmin:Password` están configurados): crea el usuario o lo promueve a Admin.
2. La consola `SeedUser` (solo desarrollo).

---

## 5. Autenticación JWT y autorización por roles

### Emisión del token (`Infrastructure/Security/Security.cs`)

`JwtTokenGenerator` firma con HMAC-SHA256 e incluye los claims:

| Claim | Valor |
|---|---|
| `sub` | Id del usuario |
| `email` | Email |
| `nombre` | Nombre |
| `role` | Rol **leído de la base de datos** |
| `jti` | Identificador único del token |
| `exp`, `iss`, `aud` | Expiración (60 min), emisor y audiencia |

### Validación (`Api/Configuration/ApiServiceExtensions.cs` → `AddJwtAuthentication`)

- Valida firma, emisor, audiencia y expiración (`ClockSkew` de 30 s).
- `MapInboundClaims = false` y `RoleClaimType = "role"`: así `[Authorize(Roles = "Admin")]` lee directamente el claim `role` del token.
- Los eventos `OnChallenge` (401) y `OnForbidden` (403) escriben un cuerpo JSON `ProblemDetails`, igual que el resto de los errores. El 401 distingue `UNAUTHORIZED` (sin token), `INVALID_TOKEN` y `TOKEN_EXPIRED`.

### Permisos

```csharp
[Authorize]                       // a nivel de controller: hay que estar logueado
public class AlumnoController : ControllerBase
{
    [HttpGet] ...                 // Usuario y Admin
    [HttpPost, Authorize(Roles = Roles.Admin)] ...   // solo Admin
    [HttpPut("{id:int}"), Authorize(Roles = Roles.Admin)] ...
    [HttpDelete("{id:int}"), Authorize(Roles = Roles.Admin)] ...
}
```

`MateriaController` usa el mismo esquema. `AuthController` es público (`[AllowAnonymous]`).

| Rol | GET | POST / PUT / DELETE |
|---|---|---|
| Sin token | 401 | 401 |
| `Usuario` | ✅ 200 | ❌ 403 |
| `Admin` | ✅ 200 | ✅ 201 / 204 |

### Protecciones adicionales

- **Registro siempre como `Usuario`:** `RegisterDto` no tiene propiedad `Rol`; si el cliente envía `"rol": "Admin"`, se ignora (hay una prueba de integración que lo verifica).
- **Mensaje genérico en el login:** "Email o contraseña inválidos" tanto si el email no existe como si la contraseña es incorrecta, para no revelar qué cuentas existen.
- **Rate limiting:** `/api/Auth/*` admite 10 solicitudes por minuto por IP (configurable en `RateLimiting:AuthPermitLimitPerMinute`). Al superarlo responde `429 TOO_MANY_REQUESTS`.
- Contraseñas con **BCrypt**; el hash nunca sale en ningún DTO.

> **Regla importante:** ocultar botones en Angular mejora la experiencia, pero **no protege datos**. Quien decide siempre es el backend.

---

## 6. Paginación y filtrado

`GET /api/Alumno?pageNumber=1&pageSize=10&nombre=ana&materiaId=2`

| Parámetro | Regla |
|---|---|
| `pageNumber` | Empieza en 1. Por defecto 1. Menor que 1 → 400 |
| `pageSize` | Por defecto 10. Rango 1–100. Fuera de rango → 400 |
| `nombre` | Coincidencia parcial (`LIKE '%ana%'`). La distinción de mayúsculas depende de la collation de SQL Server (por defecto no la hace) |
| `materiaId` | Filtra por la materia asociada. Se combina con `nombre` |

**Respuesta:**

```json
{
  "items": [
    { "id": 3, "nombre": "Ana García", "email": "ana@example.com", "materiaId": 2, "materia": "Matemática" }
  ],
  "pageNumber": 1,
  "pageSize": 10,
  "totalCount": 24,
  "totalPages": 3
}
```

**Cómo funciona** (`Infrastructure/Repositories/Repositories.cs` → `AlumnoRepository.GetPagedAsync`): toda la consulta se traduce a SQL. Primero aplica los `WHERE`, después `COUNT` para el total, después `ORDER BY Id` (orden estable) y por último `OFFSET/FETCH` (`Skip`/`Take`). **Nunca se descarga la tabla completa.** La proyección a `AlumnoDto` se hace en el mismo `SELECT`, con un `JOIN` a `Materias`.

La validación del rango (`AlumnoService.ValidarPaginacion`) ocurre **antes** de calcular `Skip`, para no generar desplazamientos negativos ni páginas enormes. Si no hay resultados, devuelve `items: []` con `totalPages: 0`.

`GET /api/Materia` sigue devolviendo la lista completa, porque el combo de materias necesita todas las opciones.

---

## 7. Middleware personalizado: CorrelationIdMiddleware

Archivo: `GestionAcademica.Api/Middleware/CorrelationIdMiddleware.cs`.

**Qué hace en cada solicitud:**

1. Lee el header `X-Correlation-ID`. Si el cliente lo envía y es seguro (1 a 64 caracteres `A-Z a-z 0-9 - _ .`), lo reutiliza. Si no, genera un GUID. Así se evita la inyección de texto en los logs (por ejemplo, saltos de línea).
2. Lo guarda en `HttpContext.TraceIdentifier`, que es el valor que aparece como `traceId` en todas las respuestas de error.
3. Lo agrega a la respuesta con `Response.OnStarting` (header `X-Correlation-ID`). Se usa `OnStarting` porque el manejador de excepciones limpia los headers: así el ID sobrevive incluso en respuestas 500.
4. Abre un *scope* de logging con `CorrelationId`. Todo log de esa solicitud (incluido el de una excepción) queda asociado a ese ID.
5. Al terminar, registra una línea con método, ruta, código y duración:

```text
info: GestionAcademica.Api.Middleware.CorrelationIdMiddleware[0]
      => CorrelationId:demo-123
      HTTP GET /api/Alumno/99 -> 404 (20 ms) [correlationId=demo-123]
```

**No registra** query strings, headers, cuerpos, tokens ni contraseñas.

**Por qué es el primer middleware del pipeline:** si fuera después de `UseExceptionHandler`, los errores se procesarían fuera de su scope y perderían la correlación.

CORS expone el header (`WithExposedHeaders("X-Correlation-ID")`) para que Angular también pueda leerlo.

**Uso práctico:** un usuario reporta un error y muestra el `traceId` que vio en pantalla. Se busca ese valor en los logs del servidor y aparece la solicitud completa con su excepción.

---

## 8. Manejo global de excepciones

### Excepciones de negocio (`Application/Exceptions/AppExceptions.cs`)

Todas heredan de `AppException`, que tiene un `Code` estable (pensado para que lo lean programas). No conocen HTTP:

| Excepción | Uso | HTTP |
|---|---|---|
| `ValidationException` | Paginación inválida, materia inexistente en el body (incluye `errors` por campo) | 400 |
| `UnauthorizedException` | Credenciales inválidas | 401 |
| `NotFoundException` | Alumno o materia inexistente (`NotFoundException.For("Alumno", 99)`) | 404 |
| `ConflictException` | Email duplicado, materia repetida, materia con alumnos | 409 |
| Cualquier otra | Error inesperado | 500 |

### GlobalExceptionHandler (`Api/ExceptionHandling/GlobalExceptionHandler.cs`)

Implementa `IExceptionHandler` (.NET 8) y se activa con `app.UseExceptionHandler()`. Gracias a esto, **los controllers no tienen ningún `try/catch`** y quedan en una o dos líneas.

1. Traduce el tipo de excepción a código HTTP, título y `code`.
2. Registra el error: `LogWarning` para errores de negocio, `LogError` con stack trace para los 500.
3. Escribe la respuesta con `IProblemDetailsService` en formato **ProblemDetails (RFC 7807)**.
4. En los 500 **no expone** el mensaje ni el stack trace al cliente; solo un texto genérico y el `traceId`.

### Respuesta homogénea para todos los errores

Todos los errores tienen la misma forma y `Content-Type: application/problem+json`, vengan de donde vengan:

| Origen | Cómo se logra |
|---|---|
| Excepciones | `GlobalExceptionHandler` |
| DTO inválido / query mal formada | `InvalidModelStateResponseFactory` personalizado |
| 401 / 403 de JWT | Eventos `OnChallenge` / `OnForbidden` |
| 429 | `RateLimiter.OnRejected` |
| Ruta inexistente (404), 405… | `UseStatusCodePages()` + `AddProblemDetails()` |

`CustomizeProblemDetails` completa siempre `type`, `instance`, `traceId` y, si falta, `code` y un título en español.

**Ejemplo real:**

```json
{
  "type": "https://httpstatuses.com/404",
  "title": "Recurso no encontrado",
  "status": 404,
  "detail": "No existe un alumno con ID 99.",
  "instance": "/api/Alumno/99",
  "code": "ALUMNO_NOT_FOUND",
  "traceId": "demo-123"
}
```

**Ejemplo de validación (400):**

```json
{
  "title": "Datos inválidos",
  "status": 400,
  "code": "VALIDATION_ERROR",
  "errors": {
    "pageNumber": ["pageNumber debe ser mayor o igual a 1."],
    "pageSize": ["pageSize debe estar entre 1 y 100."]
  },
  "traceId": "1158bdd0103c47dfb31d5bdced987829"
}
```

### Tabla de códigos `code`

| Código | HTTP | Cuándo |
|---|---:|---|
| `VALIDATION_ERROR` | 400 | DTO o paginación inválidos |
| `UNAUTHORIZED` / `INVALID_TOKEN` / `TOKEN_EXPIRED` | 401 | Sin token / token inválido / vencido |
| `INVALID_CREDENTIALS` | 401 | Login fallido |
| `FORBIDDEN` | 403 | Rol insuficiente |
| `ALUMNO_NOT_FOUND`, `MATERIA_NOT_FOUND`, `RESOURCE_NOT_FOUND` | 404 | Recurso o ruta inexistente |
| `EMAIL_DUPLICADO`, `ALUMNO_EMAIL_DUPLICADO`, `MATERIA_DUPLICADA`, `MATERIA_CON_ALUMNOS` | 409 | Conflictos de negocio |
| `TOO_MANY_REQUESTS` | 429 | Límite de intentos en Auth |
| `INTERNAL_ERROR` | 500 | Error inesperado |

---

## 9. Reglas de negocio y validaciones

- **DataAnnotations** en los DTOs: nombre obligatorio (2–100 caracteres), email válido (máx. 150), `materiaId ≥ 1`, contraseña de al menos 6 caracteres.
- **Emails normalizados** (sin espacios y en minúsculas) en alumnos y usuarios.
- **Email de alumno único** → 409 `ALUMNO_EMAIL_DUPLICADO`.
- **Nombre de materia único** → 409 `MATERIA_DUPLICADA`.
- **No se puede borrar una materia con alumnos** → 409 `MATERIA_CON_ALUMNOS`. La FK está en cascada, así que antes se borraban todos sus alumnos sin aviso. Ahora se bloquea.
- **Crear o editar un alumno con una materia inexistente** → 400, con el error en el campo `materiaId`.

---

## 10. Frontend Angular

### Archivos nuevos

| Archivo | Función |
|---|---|
| `guards/auth.guard.ts` → `authGuard` | Sin sesión válida redirige a `/auth/login?returnUrl=...` |
| `guards/auth.guard.ts` → `roleGuard` | Lee `data.roles` de la ruta; si el rol no coincide, muestra "Acceso denegado" y vuelve a Home |
| `interceptors/auth.interceptor.ts` | Agrega `Authorization: Bearer` **solo** a solicitudes dirigidas a `environment.apiUrl` (el token no se envía a terceros) |
| `interceptors/error.interceptor.ts` | Ante un 401 de la API (fuera de `/auth`) cierra la sesión y redirige al login con el aviso "Su sesión expiró" |
| `shared/error-message.ts` → `getErrorMessage()` | Convierte un `ProblemDetails` en un texto legible: errores por campo → `detail` → mensaje por código. En los 500 agrega el `traceId` |
| `interfaces/PagedResult.ts`, `ProblemDetails.ts` | Tipos de los contratos nuevos de la API |

### Rutas

| Ruta | Protección | Contenido |
|---|---|---|
| `/home`, `/auth/login`, `/auth/register` | Pública | |
| `/dashboardA` | `authGuard` | Alumnos |
| `/dashboardB` | `authGuard` | Materias |
| `/dashboardC` | `roleGuard` con `roles: ['Admin']` | Panel de administración |

### Tabla de alumnos (`alumnos/alumnos.ts`)

- **`MatPaginator`** con tamaños 5, 10 y 25, conectado a la paginación del servidor. Al cambiar de página se consulta `pageNumber = event.pageIndex + 1`, porque el paginador cuenta desde 0 y la API desde 1.
- **Búsqueda por nombre** (`matInput`) con *debounce* de 400 ms, para no enviar una solicitud por cada letra.
- **Filtro por materia** (`mat-select`) combinable con el nombre. Al cambiar cualquier filtro vuelve a la página 1.
- Barra de carga, mensaje de error con botón **Reintentar** y estado vacío ("No hay alumnos que coincidan…").
- Si se borra el último registro de la última página, retrocede una página automáticamente.
- Los botones **Nuevo / Editar / Eliminar** y la columna *Acciones* solo aparecen para `Admin`.

### Sesión (`services/auth.service.ts`)

- `isLoggedIn()` comprueba además que el token no esté **vencido** (lee `exp`).
- `getRol()` / `isAdmin()` / `hasRole()` leen el claim `role` del propio JWT. Solo sirven para la interfaz: la autorización real la hace el servidor.
- El login redirige al `returnUrl` (solo si es una ruta interna, para evitar redirecciones a sitios externos).

### Correcciones del plan

- `CrearAlumnoDto` ahora tiene `nombre`, `email` y `materiaId` (antes tenía `apellido`).
- `materia-service.ts` importa `environment` en lugar de `environment.development`.
- El diálogo de alumno precarga la materia al editar (el backend ahora devuelve `materiaId`) y el de materia precarga el nombre.
- `materia.ts` importaba `MatButtonModule` de forma incompleta y el diálogo tipaba `materia` con el componente en lugar de la interfaz.

> **Riesgo documentado:** el token se guarda en `localStorage`, que es vulnerable a robo mediante XSS. Para esta entrega se mantiene; como mejora, se podría usar una cookie `HttpOnly` o guardar el token en memoria.

---

## 11. Endpoints

Base: `http://localhost:5201/api`

| Método | Endpoint | Acceso | Respuesta |
|---|---|---|---|
| POST | `/Auth/register` | Público (rate limit) | 200 `AuthResponseDto` · 400 · 409 |
| POST | `/Auth/login` | Público (rate limit) | 200 `AuthResponseDto` (incluye `rol`) · 401 |
| GET | `/Alumno?pageNumber&pageSize&nombre&materiaId` | Usuario / Admin | 200 `PagedResult<AlumnoDto>` · 400 |
| GET | `/Alumno/{id}` | Usuario / Admin | 200 · 404 |
| POST | `/Alumno` | Admin | 201 + `Location` · 400 · 403 · 409 |
| PUT | `/Alumno/{id}` | Admin | 204 · 400 · 403 · 404 · 409 |
| DELETE | `/Alumno/{id}` | Admin | 204 · 403 · 404 |
| GET | `/Materia` | Usuario / Admin | 200 lista completa |
| GET | `/Materia/{id}` | Usuario / Admin | 200 · 404 |
| POST | `/Materia` | Admin | 201 · 400 · 403 · 409 |
| PUT | `/Materia/{id}` | Admin | 204 · 400 · 403 · 404 · 409 |
| DELETE | `/Materia/{id}` | Admin | 204 · 403 · 404 · 409 (tiene alumnos) |

Todos los endpoints, salvo Auth, devuelven 401 sin token.

**Cambios de contrato que afectan a clientes:**

- `GET /Alumno` devuelve un objeto paginado en lugar de una lista.
- `POST` responde 201 en lugar de 200.
- `AlumnoDto` incluye `materiaId`.
- `AuthResponseDto` incluye `rol`.
- Los errores tienen formato ProblemDetails (antes: `{ "message": "..." }`).

El frontend ya está adaptado a todos estos cambios.

---

## 12. Pruebas

```bash
cd CLASE_ORM
dotnet test GestionAcademica.sln
```

**33 pruebas, todas pasando.**

**Unitarias** (`Tests/Unit`): prueban Application sin base de datos, con repositorios falsos.

- Paginación inválida (0, 101…), resultado vacío y cálculo de `totalPages` con 12 registros; filtros combinados.
- Materia inexistente → `ValidationException`; email duplicado (con distinto formato) → `ConflictException`; ID inexistente → `NotFoundException`.
- No se puede borrar una materia con alumnos.
- El registro siempre crea el rol `Usuario`; una contraseña incorrecta produce `UnauthorizedException`; el login devuelve el rol guardado.

**Integración** (`Tests/Integration`): levantan la API real con `WebApplicationFactory` y EF InMemory.

- 401 sin token y con token inválido; un `Usuario` consulta (200) pero recibe 403 en 5 operaciones de escritura; un `Admin` crea (201).
- Registrarse con `"rol":"Admin"` produce un token con `role = Usuario`, y ese usuario no puede crear.
- Paginación (página 2 de 5, orden estable), filtros combinados, resultado vacío y paginación inválida (incluido `pageNumber=abc`) → 400.
- 404 con `code`, `instance` y `traceId` igual al `X-Correlation-ID` enviado; un ID de correlación inseguro se descarta.
- DTO inválido → 400 con errores por campo; email duplicado → 409.

**Frontend:** `npm run build` compila sin errores ni warnings.

---

## 13. Guion de demostración

1. **Swagger:** `GET /api/Alumno` sin token → 401 con JSON. `POST /api/Auth/login` → copiar el token → **Authorize**.
2. **Roles:** con un usuario recién registrado, `POST /api/Materia` → 403. Con el admin → 201.
3. **Code First:** mostrar `Usuario.Rol`, la migración `AddUsuarioRol` y la columna en la tabla `Usuarios`.
4. **Clean Architecture:** abrir `GestionAcademica.sln` y mostrar las referencias entre proyectos (Domain no referencia nada).
5. **Paginación:** en Angular (Alumnos), cambiar de página, buscar por nombre y elegir una materia. En las herramientas de desarrollo del navegador (F12 → Red) mostrar `?pageNumber=2&pageSize=5&nombre=...`.
6. **Middleware:** en Swagger o con curl, enviar `X-Correlation-ID: demo-123`, ver el header en la respuesta y la línea `[correlationId=demo-123]` en la consola de la API.
7. **Excepciones:** `GET /api/Alumno/99` → 404 con `code` y `traceId`. Borrar una materia con alumnos → 409 `MATERIA_CON_ALUMNOS`.
8. **Calidad:** ejecutar `dotnet test` y `npm run build`.

---

## 14. Cambios respecto de la versión anterior

| Antes | Ahora |
|---|---|
| Un solo proyecto `CLASE_ORM.csproj` | Solución con 4 capas + proyecto de pruebas |
| Endpoints de Alumno y Materia públicos | `[Authorize]` + `Roles = Admin` en escritura |
| Token sin rol | Claim `role` leído de la base |
| Clave JWT en `appsettings.json` | user-secrets / variables de entorno, validada al arrancar |
| `GET /Alumno` devolvía toda la tabla | Paginación y filtros en SQL |
| `ExepctionNotFound` vacía, controllers con `if (null) NotFound()` | Excepciones tipadas + `GlobalExceptionHandler` |
| Errores con formatos distintos | ProblemDetails en todas las respuestas |
| Sin trazabilidad | `CorrelationIdMiddleware` |
| Borrar una materia eliminaba sus alumnos en cascada | Se bloquea con 409 |
| `SeedUser` con credenciales fijas | Recibe argumentos y rol |
| Paquetes JWT/SqlClient 8.0.0 (con avisos de vulnerabilidad) | EF Core / JwtBearer 8.0.11 |

---

## 15. Pendientes y limitaciones conocidas

- **Runtime de .NET en este equipo:** está instalado ASP.NET Core 8.0.33 pero falta `Microsoft.NETCore.App 8.0.33`. Por eso, cualquier API .NET 8 falla al arrancar con *"You must install or update .NET"*. La solución es reinstalar o reparar el **.NET 8 Runtime** (por ejemplo, instalar el *.NET 8.0 Hosting Bundle* o el SDK 8 más reciente desde <https://dotnet.microsoft.com/download/dotnet/8.0>).
- El CRUD de `Profesor` no se implementó (es opcional según el plan); la entidad y la tabla se mantienen.
- Los `*.spec.ts` que generó Angular CLI (por ejemplo, `app.spec.ts`, que busca un `<h1>Hello…`) no se actualizaron; `ng test` necesita Chrome.
- La copia `.NET-ANGULAR/` y `legacy/` pueden eliminarse cuando ya no se necesiten como referencia.
- `docs/README.md` describe la versión **anterior** a esta refactorización.
