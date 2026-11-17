# Gestión de Alumnos y Materias — Plan de evolución del proyecto

> **Estado:** hoja de ruta de implementación (no significa que todas las funcionalidades estén desarrolladas).  
> **Punto de partida:** README de `.NET-ANGULAR-main`, con backend `CLASE_ORM`, frontend `TestAngular` y utilidad `SeedUser`.

## 1. Objetivo

Evolucionar la aplicación existente de gestión de alumnos y materias para cumplir los requisitos del trabajo práctico:

- API REST propia en ASP.NET Core 8.
- ORM Entity Framework Core y base de datos SQL Server mediante **Code First**.
- Frontend Angular 20 con componentes standalone y Angular Material.
- **Clean Architecture** en el backend.
- Autenticación JWT y **autorización por roles**.
- **Paginación y filtrado** del lado del servidor.
- **Middleware personalizado** para trazabilidad de solicitudes.
- **Manejo global de excepciones** y respuestas JSON estructuradas.

**Alcance funcional:** mantener el CRUD de `Alumno` y `Materia`, su relación uno-a-muchos, el registro e inicio de sesión. `Profesor` ya existe como entidad, pero su CRUD es **opcional**: no hace falta crearlo para cumplir los requisitos enumerados.

## 2. Diagnóstico: qué ya existe y qué falta

| Requisito | Estado según el README original | Acción |
|---|---|---|
| API REST | ✅ ASP.NET Core 8; controllers para Auth, Alumno y Materia | Mantener y refactorizar |
| ORM | ✅ Entity Framework Core 8 | Conservar EF Core en Infrastructure |
| Code First | ✅ Migraciones `init`, `AddProfesor`, `AddEmail`, `AddUsuarioAuth` | Mantener el historial y agregar migraciones nuevas |
| Angular | ✅ Angular 20 standalone | Mantener y organizar por funcionalidades |
| Angular Material | ✅ Tablas y diálogos de CRUD | Agregar paginator, filtros y mensajes de error |
| Clean Architecture | ❌ Arquitectura por carpetas dentro de `CLASE_ORM` | Separar proyectos y dependencias |
| Roles | ❌ Login JWT sin claim de rol documentado; endpoints CRUD públicos | Crear roles y políticas de acceso |
| Paginación y filtrado | ❌ No documentados | Crear queries paginadas y `MatPaginator` |
| Middleware propio | ❌ No documentado | Agregar middleware de correlación/logging |
| Excepciones globales | ❌ `ExepctionNotFound` vacía, sin uso | Agregar excepciones tipadas y `IExceptionHandler` |

### Problemas que conviene corregir durante la migración

1. `AlumnoController` y `MateriaController` no usan `[Authorize]`.
2. La clave JWT está en `appsettings.json`: mover el secreto fuera del repositorio.
3. El frontend declara `apellido` en `CrearAlumnoDto`, pero el backend necesita `materiaId`.
4. `materia-service.ts` usa `environment.development` en vez del import habitual `environment`.
5. `SeedUser` duplica la cadena de conexión y crea `usuario.prueba@test.com` con contraseña `123456`: usarlo solo en desarrollo y actualizar el seeding para roles.
6. Excluir `bin/`, `obj/`, `.vs/`, `TestAngular/dist/` y los secretos del control de versiones.

## 3. Arquitectura objetivo

**No borrar la aplicación existente.** Crear una rama Git de trabajo y refactorizar por etapas. Se recomienda convertir `CLASE_ORM` en una solución con cuatro proyectos:

```text
.NET-ANGULAR-main/
├── CLASE_ORM/                        # Backend (solución .NET)
│   ├── GestionAcademica.sln
│   ├── GestionAcademica.Domain/
│   │   ├── Entities/                # Alumno, Materia, Profesor, Usuario
│   │   └── Enums/                   # RolUsuario (si se usa enum)
│   ├── GestionAcademica.Application/
│   │   ├── DTOs/
│   │   ├── Interfaces/              # Contratos de servicios y repositorios
│   │   ├── Services/                # Casos de uso y reglas de aplicación
│   │   ├── Exceptions/              # NotFoundException, ConflictException...
│   │   └── Common/                  # PagedResult, filtros
│   ├── GestionAcademica.Infrastructure/
│   │   ├── Persistence/             # AppDbContext y configuraciones EF
│   │   ├── Migrations/
│   │   ├── Repositories/
│   │   └── Security/                # JWT, BCrypt
│   └── GestionAcademica.Api/
│       ├── Controllers/             # Auth, Alumno, Materia
│       ├── Middleware/              # CorrelationIdMiddleware
│       ├── ExceptionHandling/       # GlobalExceptionHandler
│       └── Program.cs
├── TestAngular/                      # Frontend ya existente
│   └── src/app/
│       ├── auth/
│       ├── alumnos/
│       ├── materia/
│       ├── services/
│       ├── guards/
│       ├── interceptors/
│       └── shared/
├── SeedUser/                         # Solo desarrollo; adaptar o reemplazar
└── README.md
```

### Regla de dependencias

```text
               API (controllers, middleware)
                 |                  |
                 v                  v
            Application <----- Infrastructure
                 |
                 v
               Domain

Infrastructure ------> Domain
API es el punto de composición: registra implementaciones de Infrastructure.
```

- **Domain:** entidades y reglas de negocio; sin referencias a EF Core ni ASP.NET.
- **Application:** casos de uso, contratos, validaciones, DTOs y respuestas de paginación; depende solo de Domain.
- **Infrastructure:** `DbContext`, SQL Server, repositorios, BCrypt e implementación de JWT; depende de Application y Domain.
- **API:** endpoints HTTP, configuración, autorización y manejo de errores; enlaza Application con Infrastructure.

**Importante:** un servicio de Application no debería inyectar `AppDbContext` directamente. Debe usar interfaces (`IAlumnoRepository`, `IMateriaRepository`, etc.) implementadas en Infrastructure. Tampoco debe recibir ni devolver tipos HTTP como `IActionResult`.

### Comandos iniciales de solución (ejemplo)

Ejecutar dentro de `CLASE_ORM` una vez decidido cómo conservar el proyecto existente:

```bash
dotnet new sln -n GestionAcademica
dotnet new classlib -n GestionAcademica.Domain -f net8.0
dotnet new classlib -n GestionAcademica.Application -f net8.0
dotnet new classlib -n GestionAcademica.Infrastructure -f net8.0
dotnet new webapi -n GestionAcademica.Api -f net8.0

dotnet sln add GestionAcademica.Domain/GestionAcademica.Domain.csproj
dotnet sln add GestionAcademica.Application/GestionAcademica.Application.csproj
dotnet sln add GestionAcademica.Infrastructure/GestionAcademica.Infrastructure.csproj
dotnet sln add GestionAcademica.Api/GestionAcademica.Api.csproj

dotnet add GestionAcademica.Application reference GestionAcademica.Domain
dotnet add GestionAcademica.Infrastructure reference GestionAcademica.Application
dotnet add GestionAcademica.Infrastructure reference GestionAcademica.Domain
dotnet add GestionAcademica.Api reference GestionAcademica.Application
dotnet add GestionAcademica.Api reference GestionAcademica.Infrastructure
```

Estos comandos son una **plantilla para los proyectos nuevos**, no deben ejecutarse indiscriminadamente sobre archivos preexistentes: primero respaldar/mover el código de `CLASE_ORM`, identificar el `.csproj` real y evitar crear dos APIs activas por accidente. Conservar funcionando la API anterior hasta que el nuevo arranque esté verificado.

## 4. Fase 0 — Asegurar un punto de partida

- [ ] Crear rama: `git checkout -b feature/arquitectura-avanzada`.
- [ ] Confirmar `dotnet restore`, arranque API y Swagger en `http://localhost:5201/swagger`.
- [ ] Confirmar `npm install` y `npm start` en `TestAngular` (`http://localhost:4200`).
- [ ] Hacer copia de seguridad de la base `testprueba` con datos de prueba que se quieran conservar.
- [ ] Verificar endpoints actuales de Auth, Alumno y Materia con Swagger.
- [ ] Revisar el `.gitignore` y mover la clave JWT a `dotnet user-secrets` / variables de entorno.
- [ ] Registrar contratos HTTP actuales para evitar romper el frontend innecesariamente.

**Resultado esperado:** proyecto original funcional y una rama aislada de desarrollo.

## 5. Fase 1 — Migrar a Clean Architecture

1. Mover `Alumno`, `Materia`, `Profesor` y `Usuario` a **Domain**; verificar su relación actual.
2. Mover DTOs a **Application**; mantener nombres y JSON esperados cuando sea posible.
3. Definir interfaces de servicios y repositorios en **Application**.
4. Mover implementación de servicios a **Application** y reemplazar consultas directas al `DbContext` por los contratos.
5. Mover `AppDbContext`, repositorios EF Core y las configuraciones de persistencia a **Infrastructure**.
6. Llevar controladores, configuración Swagger, autenticación y CORS a **API**.
7. Registrar dependencias (`AddScoped`) en el punto de composición `Program.cs`.
8. Compilar con `dotnet build` y probar nuevamente los CRUD.

**Ejemplo de contrato en Application:**

```csharp
public interface IAlumnoRepository
{
    Task<Alumno?> GetByIdAsync(int id, CancellationToken ct);
    Task AddAsync(Alumno alumno, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
    // Agregar actualización, eliminación y búsqueda paginada.
}
```

**Criterio de finalización:** Domain y Application compilan sin depender de EF Core, SQL Server ni ASP.NET Core.

## 6. Fase 2 — Code First y rol de usuario

### Estado actual

Ya existen `DbSet` y las migraciones `init`, `AddProfesor`, `AddEmail` y `AddUsuarioAuth`, que construyen la base `testprueba`. **No es necesario volver a crear la base desde cero.**

### Modificación propuesta

Para el trabajo práctico, usar inicialmente dos roles:

| Rol | Permisos propuestos |
|---|---|
| `Admin` | Consultar, crear, editar y eliminar alumnos y materias |
| `Usuario` | Solo consultar alumnos y materias |

Agregar la propiedad `Rol` a `Usuario` (opción sencilla con string controlado o enum) y establecer `Usuario` como valor por defecto para nuevos registros. Los administradores deben ser designados mediante un proceso interno o seeding confiable, **no** mediante el formulario público de registro.

**Ejemplo conceptual:**

```csharp
public class Usuario
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Rol { get; set; } = "Usuario";
}
```

No reemplazar sin comparar las propiedades reales de `Usuario` del proyecto: este fragmento muestra únicamente la modificación prevista.

**Migración al cambiar el modelo:**

```bash
# Ejecutar desde CLASE_ORM una vez creados los proyectos.
dotnet ef migrations add AddUsuarioRol \
  --project GestionAcademica.Infrastructure \
  --startup-project GestionAcademica.Api \
  --context AppDbContext

dotnet ef database update \
  --project GestionAcademica.Infrastructure \
  --startup-project GestionAcademica.Api \
  --context AppDbContext
```

Instalar `Microsoft.EntityFrameworkCore.SqlServer` y `Microsoft.EntityFrameworkCore.Design` en los proyectos apropiados con versión compatible con EF Core 8. Puede requerirse una fábrica de diseño (`IDesignTimeDbContextFactory`) para localizar `AppDbContext` al ejecutar migraciones. Revisar el SQL generado antes de aplicar cambios sobre datos existentes; asignar el rol `Usuario` a los usuarios anteriores y crear un administrador de desarrollo seguro. **No reiniciar el historial de migraciones como si la base no existiera.**

**Criterio de finalización:** la columna de rol queda persistida y la base se actualiza mediante migraciones, sin pérdida de registros.

## 7. Fase 3 — Autenticación JWT y autorización por roles

El login ya utiliza BCrypt y emite JWT, pero el README original documenta solo los claims `sub`, `email` y `nombre`. Agregar un claim de rol emitido desde un rol **persistido y validado en servidor**.

```csharp
new Claim(ClaimTypes.Role, usuario.Rol)
```

Configurar JWT Bearer en `Program.cs` para validar firma, issuer, audience y expiración. Configurar `TokenValidationParameters.RoleClaimType = ClaimTypes.Role` si corresponde a la generación del token; revisar el mapeo de claims para que `[Authorize(Roles = "Admin")]` funcione realmente.

Aplicar el siguiente esquema de permisos:

```csharp
[Authorize] // a nivel de controlador
public class AlumnoController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() { /* ... */ }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create() { /* ... */ }
}
```

Repetir para Materia, protegiendo `POST`, `PUT` y `DELETE` como Admin. Mantener `/api/Auth/login` y `/api/Auth/register` públicos (con validación y controles contra abuso).

**Regla importante:** ocultar botones en Angular mejora la UX, pero **no protege datos**. Las restricciones obligatorias deben verificarse siempre en el backend.

**Pruebas mínimas:**

- [ ] Sin token, `GET /api/Alumno` devuelve `401`.
- [ ] Un `Usuario` autenticado puede consultar.
- [ ] Un `Usuario` autenticado no puede crear/modificar/eliminar: `403`.
- [ ] Un `Admin` puede realizar CRUD.
- [ ] Token inválido o expirado devuelve `401`.
- [ ] El usuario no puede registrarse a sí mismo como Admin cambiando el JSON.

## 8. Fase 4 — Paginación y filtrado de alumnos

Implementar paginación **real en el backend**: filtrar y ordenar en SQL antes de ejecutar `Skip` y `Take`; no descargar toda la tabla para paginarla en el navegador.

### Endpoint propuesto

`GET /api/Alumno?pageNumber=1&pageSize=10&nombre=ana&materiaId=2`

**Ejemplo de respuesta nueva:**

```json
{
  "items": [
    { "id": 3, "nombre": "Ana García", "email": "ana@example.com", "materia": "Matemática" }
  ],
  "pageNumber": 1,
  "pageSize": 10,
  "totalCount": 24,
  "totalPages": 3
}
```

### Reglas

- `pageNumber` inicia en **1**; por defecto `1`.
- `pageSize` por defecto `10`; máximo `100`.
- `nombre` filtra por coincidencia parcial, sin asumir sensibilidad a mayúsculas (depende de la collation SQL).
- `materiaId` filtra por materia asociada.
- Ordenar por `Id` de forma estable antes de paginar (o agregar orden configurable con lista permitida de campos).
- Devolver `items: []` cuando no haya registros.

**Pseudocódigo con EF Core:**

```csharp
var query = db.Alumnos.AsNoTracking().AsQueryable();
if (!string.IsNullOrWhiteSpace(nombre))
    query = query.Where(a => a.Nombre.Contains(nombre));
if (materiaId.HasValue)
    query = query.Where(a => a.MateriaId == materiaId.Value);

var total = await query.CountAsync(ct);
var items = await query
    .OrderBy(a => a.Id)
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .Select(a => new AlumnoDto { /* mapear propiedades reales */ })
    .ToListAsync(ct);
```

Este código pertenece a **Infrastructure** (EF Core), expuesto por una interfaz de Application. Se debe validar el rango de paginación antes del cálculo de `Skip`.

**Nota de compatibilidad:** actualmente `GET /api/Alumno` devuelve una lista simple. La propuesta cambia su respuesta a un objeto paginado: actualizar el servicio y tabla de Angular en la misma fase, o versionar el endpoint (`/api/Alumno/paged`) mientras se migra el frontend.

## 9. Fase 5 — Angular Material, filtros y guardas

Conservar Angular 20 standalone, las tablas Material, los diálogos y el interceptor JWT existentes.

### Cambios en alumnos

- Agregar `MatPaginator` con tamaños `5, 10, 25`.
- Agregar un `mat-form-field` con `matInput` para buscar por nombre.
- Agregar `mat-select` para filtrar por materia.
- Al cambiar filtro: regresar a la página 1 y solicitar datos al backend.
- Al cambiar página: volver a consultar el endpoint con `pageNumber = event.pageIndex + 1`.
- Mostrar indicador de carga, estado vacío y errores.
- Ocultar botones de agregar, editar y eliminar a usuarios sin rol `Admin`.

**Interfaz TypeScript propuesta:**

```typescript
export interface PagedResult<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}
```

**Ejemplo en HTML:**

```html
<mat-paginator
  [length]="totalCount"
  [pageIndex]="pageNumber - 1"
  [pageSize]="pageSize"
  [pageSizeOptions]="[5, 10, 25]"
  (page)="onPageChange($event)">
</mat-paginator>
```

### Sesión y permisos

- Crear `authGuard` para impedir entrar a dashboards sin sesión.
- Crear `roleGuard` para proteger vistas exclusivamente administrativas.
- Mantener interceptor que agrega `Authorization: Bearer ...` solo a solicitudes destinadas a la API propia.
- Interpretar respuestas `401` (sesión inválida/expirada), `403` (sin permiso), `404` y errores de validación.
- El frontend actual usa `localStorage`: para la entrega puede conservarse, pero documentar el riesgo de robo de token mediante XSS y evitar guardar otros datos sensibles. Como mejora posterior, evaluar estrategia de cookies seguras o token en memoria.

**Criterio de finalización:** un usuario común ve listados paginados, un Admin administra y los errores del backend son visibles y comprensibles.

## 10. Fase 6 — Middleware personalizado y manejo global de excepciones

Implementar **dos mecanismos complementarios**:

1. `CorrelationIdMiddleware`: agrega o propaga un identificador por solicitud; lo devuelve en `X-Correlation-ID` y lo incluye en logs. No registrar tokens ni contraseñas.
2. `GlobalExceptionHandler` con `IExceptionHandler`: captura excepciones no controladas y las convierte en respuestas JSON consistentes. Se integra con `UseExceptionHandler()`; en .NET 8 es preferible a repetir `try/catch` en cada controller.

### Contrato de error recomendado

Usar `ProblemDetails` (estándar RFC 7807) con extensiones para `traceId`, `code` y, cuando corresponda, `errors`:

```json
{
  "type": "https://httpstatuses.com/404",
  "title": "Recurso no encontrado",
  "status": 404,
  "detail": "No existe un alumno con ID 99.",
  "instance": "/api/Alumno/99",
  "traceId": "0HNEXAMPLE",
  "code": "ALUMNO_NOT_FOUND"
}
```

### Mapeo de excepciones

| Situación | Código HTTP | Ejemplo de `code` |
|---|---:|---|
| DTO inválido / paginación inválida | 400 | `VALIDATION_ERROR` |
| No autenticado | 401 | `UNAUTHORIZED` |
| Sin permisos | 403 | `FORBIDDEN` |
| Alumno o materia inexistente | 404 | `RESOURCE_NOT_FOUND` |
| Email duplicado o conflicto de negocio | 409 | `RESOURCE_CONFLICT` |
| Error inesperado | 500 | `INTERNAL_ERROR` |

**Importante:** `401` y `403` generados directamente por JWT/Authorization no siempre son excepciones. Si se quiere el **mismo cuerpo JSON** para ellos, configurar además los eventos de JWT Bearer / resultados del sistema de autorización, sin convertir artificialmente todo en excepción.

### Ejemplo de registro de servicios y pipeline

```csharp
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// ... registro de DbContext, repositorios, casos de uso, Auth, CORS...

var app = builder.Build();
app.UseExceptionHandler();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseCors("AngularClient");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

El ejemplo supone que se crearán `GlobalExceptionHandler` y `CorrelationIdMiddleware` y que se registrará la política de CORS llamada `AngularClient`. **No es código listo para pegar sin definir dichas clases y servicios.** El handler debe escribir el cuerpo usando `IProblemDetailsService` o `Results.Problem`, mapear excepciones conocidas y nunca filtrar stack traces al cliente en producción. Registrar internamente la excepción con `traceId`.

**Casos de prueba:** buscar ID inexistente, registrar email repetido, enviar paginación inválida, crear sin token, crear con rol insuficiente y forzar un error inesperado en entorno de prueba. Verificar código HTTP, JSON estable y correlación en logs.

## 11. Endpoints de la versión objetivo

**Base local:** `http://localhost:5201/api` (conservar si es posible al reorganizar el proyecto).

| Método | Endpoint | Acceso | Resultado esperado |
|---|---|---|---|
| POST | `/Auth/register` | Público | Registra `Usuario` (nunca Admin) |
| POST | `/Auth/login` | Público | JWT con claim de rol |
| GET | `/Alumno` | Usuario / Admin | Lista paginada y filtrada |
| GET | `/Alumno/{id}` | Usuario / Admin | Detalle o `404` |
| POST | `/Alumno` | Admin | Crea alumno |
| PUT | `/Alumno/{id}` | Admin | Actualiza alumno |
| DELETE | `/Alumno/{id}` | Admin | Elimina alumno |
| GET | `/Materia` | Usuario / Admin | Lista (paginación opcional) |
| GET | `/Materia/{id}` | Usuario / Admin | Detalle o `404` |
| POST | `/Materia` | Admin | Crea materia |
| PUT | `/Materia/{id}` | Admin | Actualiza materia |
| DELETE | `/Materia/{id}` | Admin | Elimina materia |

No hace falta cambiar todas las rutas del proyecto original. Si se agrega paginación a `Materia`, seguir el mismo contrato `PagedResult<T>` y adaptar el combo de selección de materias para que cargue todas las opciones válidas y no solo la primera página.

## 12. Pruebas y criterios de aceptación

### Backend

- [ ] Build de los cuatro proyectos sin dependencias invertidas.
- [ ] Migración EF Core aplicada sobre una base existente de prueba.
- [ ] CRUD de Alumno y Materia funcionando después de la refactorización.
- [ ] Contraseñas verificadas mediante BCrypt, sin exponer el hash en DTOs.
- [ ] JWT firmado/validado y rol obtenido del servidor.
- [ ] `401`, `403`, `404`, `409`, `400` y `500` comprobados.
- [ ] Paginación con 0, 1, 10 y más de 10 registros; filtros combinados y orden estable.
- [ ] Error JSON con `traceId`; middleware de correlación visible en la respuesta.
- [ ] Swagger actualizado con seguridad Bearer.

### Frontend

- [ ] Login y registro funcionando.
- [ ] Rutas protegidas con guardas.
- [ ] Roles reflejados correctamente en la interfaz.
- [ ] Tabla `MatTable` conectada a paginación de servidor con `MatPaginator`.
- [ ] Filtro de nombre y materia combinado.
- [ ] Confirmaciones y mensajes de éxito/error con Angular Material o SweetAlert2.
- [ ] Corrección del DTO `CrearAlumnoDto` (`materiaId`).
- [ ] `npm run build` sin errores.

### Pruebas automatizadas sugeridas

- **xUnit** para servicios/casos de uso en Application.
- **Pruebas de integración** de API con `WebApplicationFactory` y base aislada de pruebas para autorización y respuestas HTTP.
- **Angular Jasmine/Karma** para servicios, guardas y componentes (según setup actual).

## 13. Orden recomendado de trabajo y entregables

| Etapa | Tarea | Entregable observable |
|---|---|---|
| 0 | Respaldo, Git, estado inicial | Rama y sistema corriendo |
| 1 | Clean Architecture | 4 proyectos y CRUD operativo |
| 2 | Rol en entidad + migración Code First | BD actualizada y usuarios con rol |
| 3 | JWT + `[Authorize]` | Admin edita; Usuario consulta |
| 4 | Paginación/filtros backend | Endpoint paginado demostrable en Swagger |
| 5 | Angular Material y guards | Tabla paginada y UI según permisos |
| 6 | Middleware + excepciones | JSON estructurado, logs con correlación |
| 7 | Pruebas, documentación y demo | README final, capturas y pruebas pasando |

### Secuencia de commits sugerida

```text
chore: respaldar estado actual y ordenar gitignore
refactor: separar domain application infrastructure api
feat: agregar rol a usuario y migracion code first
feat: proteger endpoints con jwt y autorizacion por roles
feat: agregar paginacion y filtros de alumnos
feat: conectar mat-paginator y filtros en angular
feat: agregar middleware de correlacion y manejo global de errores
test: agregar pruebas de servicios y autorizacion
docs: actualizar readme y documentar endpoints
```

## 14. Cómo ejecutar al finalizar la refactorización

**API** (ajustar ruta y perfil según el nuevo `launchSettings.json`):

```bash
cd CLASE_ORM
dotnet restore GestionAcademica.sln
dotnet build GestionAcademica.sln
dotnet run --project GestionAcademica.Api --launch-profile http
```

**Frontend** (en otra terminal desde la raíz del repositorio):

```bash
cd TestAngular
npm install
npm start
```

**Base de datos:** usar la cadena de conexión de desarrollo en configuración local/variables de entorno. Ejecutar `dotnet ef database update` con los flags de proyecto definidos en la Fase 2. Nunca ejecutar migraciones destructivas en la BD real sin copia y revisión previa.

## 15. Demostración para la entrega

Mostrar, en este orden:

1. **Swagger:** el login devuelve JWT; sin token el CRUD exige autenticación.
2. **Roles:** Usuario consulta pero recibe `403` al crear; Admin puede crear.
3. **Code First:** entidad `Usuario` con nuevo campo `Rol` y migración aplicada.
4. **Clean Architecture:** solución con cuatro proyectos y dependencias correctas.
5. **Paginación/filtros:** cambiar página y buscar alumnos desde Angular Material; verificar query HTTP.
6. **Middleware:** observar `X-Correlation-ID` en la respuesta y su valor en logs.
7. **Excepciones:** consultar alumno inexistente y mostrar `404` con `ProblemDetails`, `code` y `traceId`.
8. **Calidad:** ejecución de pruebas y build de Angular.

## 16. Definición de terminado

El proyecto estará listo cuando cada requisito pueda **demostrarse ejecutando la aplicación**, no solamente cuando figure en el README. Mantener la funcionalidad original (login, alumnos, materias), validar la base Code First y documentar cualquier cambio de contrato de API que requiera modificaciones en Angular.

---

**Nota de alcance:** este documento es un plan basado en el README aportado, no una auditoría del código fuente. Los ejemplos de nombres de clases, interfaces, estructura y nuevas respuestas HTTP son **propuestas**, y deberán ajustarse al repositorio real durante la implementación.
