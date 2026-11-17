# .NET + Angular — Gestión de Alumnos y Materias

Aplicación full-stack de práctica compuesta por:

- **Backend:** API REST en **ASP.NET Core 8** con **Entity Framework Core** y **SQL Server (LocalDB)**, con autenticación **JWT**.
- **Frontend:** SPA en **Angular 20** (componentes standalone) con **Angular Material** y **SweetAlert2**.
- **Utilidad:** consola `SeedUser` para crear un usuario de prueba en la base de datos.

Permite registrar usuarios, iniciar sesión y hacer CRUD de **Alumnos** y **Materias** (cada alumno pertenece a una materia).

---

## Índice

1. [Estructura del repositorio](#estructura-del-repositorio)
2. [Tecnologías](#tecnologías)
3. [Requisitos previos](#requisitos-previos)
4. [Puesta en marcha](#puesta-en-marcha)
5. [Backend (CLASE_ORM)](#backend-clase_orm)
6. [Frontend (TestAngular)](#frontend-testangular)
7. [SeedUser](#seeduser)
8. [Flujo de autenticación](#flujo-de-autenticación)
9. [Observaciones y mejoras pendientes](#observaciones-y-mejoras-pendientes)

---

## Estructura del repositorio

```
.NET-ANGULAR-main/
├── CLASE_ORM/          # API REST ASP.NET Core 8 (versión actual, con autenticación JWT)
├── TestAngular/        # Frontend Angular 20
├── SeedUser/           # Consola .NET que inserta un usuario de prueba
├── .NET-ANGULAR/       # Copia anterior del repo (sin autenticación) — histórico
├── docs/               # Esta documentación
└── README.md
```

> La carpeta `.NET-ANGULAR/` es una versión previa del proyecto (antes de agregar login/registro). La versión vigente es la de la raíz.

---

## Tecnologías

| Capa | Tecnología | Versión |
|------|------------|---------|
| API | ASP.NET Core Web API | .NET 8 |
| ORM | Entity Framework Core (SqlServer + Tools) | 8.0.0 |
| Base de datos | SQL Server LocalDB (`(localdb)\MSSQLLocalDB`) | — |
| Autenticación | JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`) | 8.0.0 |
| Hash de contraseñas | BCrypt.Net-Next | 4.0.2 |
| Documentación API | Swashbuckle (Swagger) | 6.6.2 |
| Frontend | Angular (standalone components) | 20 |
| UI | Angular Material / CDK | 20.2 |
| Alertas | SweetAlert2 | 11 |
| Tests frontend | Karma + Jasmine | — |

---

## Requisitos previos

- [.NET SDK 8](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server LocalDB (incluido con Visual Studio) u otra instancia de SQL Server
- [Node.js](https://nodejs.org/) 20+ y npm
- Angular CLI: `npm install -g @angular/cli`
- (Opcional) herramienta EF: `dotnet tool install --global dotnet-ef`

---

## Puesta en marcha

### 1. Base de datos y API

```bash
cd CLASE_ORM
dotnet restore
dotnet ef database update      # crea la BD "testprueba" y aplica las migraciones
dotnet run --launch-profile http
```

En Visual Studio, el equivalente desde la *Consola del Administrador de paquetes* es `Update-Database`.

La API queda en **http://localhost:5201** y Swagger en **http://localhost:5201/swagger**.

### 2. (Opcional) Usuario de prueba

```bash
cd SeedUser
dotnet run
```

Crea `usuario.prueba@test.com` / `123456`.

### 3. Frontend

```bash
cd TestAngular
npm install
npm start          # ng serve
```

Abrir **http://localhost:4200**.

> El frontend apunta a `http://localhost:5201/api` (ver `src/environments/`). El backend solo acepta CORS desde `http://localhost:4200`.

---

## Backend (CLASE_ORM)

### Arquitectura

Arquitectura en capas sencilla:

```
Controllers  →  Interfaces (IXxxService)  →  Services  →  AppDbContext (EF Core)  →  SQL Server
                        ↑
                      DTOs
```

| Carpeta | Contenido |
|---------|-----------|
| `Controllers/` | `AlumnoController`, `MateriaController`, `AuthController` |
| `Interfaces/` | `IAlumnoService`, `IMateriaService`, `IAuthService` |
| `Services/` | Implementaciones con la lógica y el acceso a datos |
| `Entities/` | Modelos de dominio: `Alumno`, `Materia`, `Profesor`, `Usuario` |
| `Entities/DTO/` | DTOs de entrada/salida agrupados por `Alumno`, `Materia`, `Auth` |
| `Context/` | `AppDbContext` con los `DbSet` |
| `Migrations/` | Migraciones de EF Core |
| `Exception/` | `ExepctionNotFound` (clase vacía, sin uso todavía) |

Los servicios se registran con inyección de dependencias como `Scoped` en `Program.cs`.

### Modelo de datos

```
Materia (1) ──────< (N) Alumno
  Id                     Id
  Nombre                 Nombre
                         Email
                         MateriaId (FK)

Profesor                Usuario
  Id                      Id
  Nombre                  Nombre
                          Email
                          PasswordHash (BCrypt)
```

- `Alumno.MateriaId` es FK hacia `Materia` (relación uno a muchos por convención de EF).
- `Profesor` existe en la BD pero no tiene endpoints.
- `Usuario` se usa solo para autenticación.

### Migraciones

| Migración | Fecha | Cambio |
|-----------|-------|--------|
| `init` | 28/04/2026 | Tablas `Alumnos` y `Materias` |
| `AddProfesor` | 28/04/2026 | Tabla `Profesores` |
| `AddEmail` | 05/05/2026 | Columna `Email` en `Alumnos` |
| `AddUsuarioAuth` | 11/08/2026 | Tabla `Usuarios` |

Comandos útiles (Package Manager Console / dotnet-ef):

| PMC | dotnet CLI |
|-----|------------|
| `Add-Migration Nombre` | `dotnet ef migrations add Nombre` |
| `Update-Database` | `dotnet ef database update` |
| `Remove-Migration` | `dotnet ef migrations remove` |
| `Drop-Database` | `dotnet ef database drop` |

### Endpoints

Base: `http://localhost:5201/api`

#### Auth — `/api/Auth`

| Método | Ruta | Body | Respuesta |
|--------|------|------|-----------|
| POST | `/register` | `{ nombre, email, password }` | `200` `AuthResponseDto` · `400` si el email ya existe |
| POST | `/login` | `{ email, password }` | `200` `AuthResponseDto` · `401` si las credenciales son inválidas |

`AuthResponseDto`:

```json
{
  "token": "eyJhbGciOi...",
  "expiration": "2026-11-08T21:00:00Z",
  "email": "usuario.prueba@test.com",
  "nombre": "Usuario Prueba"
}
```

#### Alumnos — `/api/Alumno`

| Método | Ruta | Body | Respuesta |
|--------|------|------|-----------|
| GET | `/` | — | Lista de `AlumnoDto` `{ id, nombre, email, materia }` (materia = nombre) |
| GET | `/{id}` | — | `AlumnoDto` o `404` |
| POST | `/` | `{ nombre, email, materiaId }` | Alumno creado |
| PUT | `/{id}` | `{ nombre, email, materiaId }` | `204` o `404` |
| DELETE | `/{id}` | — | `204` o `404` |

#### Materias — `/api/Materia`

| Método | Ruta | Body | Respuesta |
|--------|------|------|-----------|
| GET | `/` | — | Lista de `{ id, nombre }` |
| GET | `/{id}` | — | Materia o `404` |
| POST | `/` | `{ nombre }` | Materia creada |
| PUT | `/{id}` | `{ nombre }` | `204` o `404` |
| DELETE | `/{id}` | — | `204` o `404` |

El archivo `CLASE_ORM.http` permite probar requests desde Visual Studio / VS Code.

### Configuración (`appsettings.json`)

```json
{
  "ConnectionStrings": {
    "Con": "Server=(localdb)\\MSSQLLocalDB;Database=testprueba;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "...",
    "Issuer": "CLASE_ORM_API",
    "Audience": "CLASE_ORM_Cliente",
    "ExpiresInMinutes": 60
  }
}
```

Perfiles de ejecución (`Properties/launchSettings.json`):

| Perfil | URL |
|--------|-----|
| `http` | http://localhost:5201 |
| `https` | https://localhost:7197 y http://localhost:5201 |
| IIS Express | http://localhost:22761 (SSL 44382) |

---

## Frontend (TestAngular)

### Estructura

```
src/app/
├── app.ts / app.html        # Layout: <app-header> + <router-outlet> + <app-footer>
├── app.routes.ts            # Rutas con lazy loading (loadComponent)
├── app.config.ts            # Router, HttpClient e interceptor JWT
├── auth/                    # Login y Registro
├── header/ · footer/        # Navegación y pie
├── home/                    # Página inicial con botones de navegación
├── dashboard-a/             # Contiene <app-alumnos>
├── dashboard-b/             # Contiene <app-materia>
├── dashboard-c/             # Página de ejemplo (placeholder)
├── alumnos/                 # Tabla Material de alumnos + acciones CRUD
├── materia/                 # Tabla Material de materias + acciones CRUD
├── dialog-agregar-alumno/   # Diálogo para crear/editar alumno (select de materias)
├── dialog-agregar-materia/  # Diálogo para crear materia
├── interfaces/              # Tipos TS: Alumno, Materias, DTOs, AuthResponse...
└── services/
    ├── AlumnoService.ts     # CRUD /api/Alumno
    ├── materia-service.ts   # CRUD /api/Materia
    ├── auth.service.ts      # login/register/logout + sesión en localStorage
    └── auth.interceptor.ts  # Agrega "Authorization: Bearer <token>"
```

### Rutas

| Ruta | Componente | Descripción |
|------|------------|-------------|
| `/` y `/home` | `Home` | Inicio |
| `/dashboardA` | `DashboardA` | Gestión de alumnos |
| `/dashboardB` | `DashboardB` | Gestión de materias |
| `/dashboardC` | `DashboardC` | Ejemplo |
| `/auth/login` | `Login` | Inicio de sesión |
| `/auth/register` | `Register` | Registro |

### Scripts

| Comando | Acción |
|---------|--------|
| `npm start` | Servidor de desarrollo en `localhost:4200` |
| `npm run build` | Build de producción en `dist/TestAngular` |
| `npm run watch` | Build en modo watch (development) |
| `npm test` | Tests unitarios con Karma |

---

## SeedUser

Aplicación de consola (.NET 8) que se conecta directamente a la BD `testprueba` con `Microsoft.Data.SqlClient` y, si no existe, inserta:

| Campo | Valor |
|-------|-------|
| Email | `usuario.prueba@test.com` |
| Password | `123456` (guardada con hash BCrypt) |
| Nombre | `Usuario Prueba` |

Requiere que antes se hayan aplicado las migraciones (tabla `Usuarios`).

---

## Flujo de autenticación

1. El usuario se registra o inicia sesión desde `/auth/register` o `/auth/login`.
2. `AuthService` (backend) valida/crea el usuario, hashea la contraseña con BCrypt y genera un JWT (HMAC-SHA256) con los claims `sub`, `email` y `nombre`, válido por 60 minutos.
3. El frontend guarda `token`, `userEmail` y `userNombre` en `localStorage`.
4. `AuthInterceptor` agrega `Authorization: Bearer <token>` a todas las peticiones HTTP.
5. El header muestra "Hola {nombre}" y el botón **Salir**, que limpia el `localStorage`.

---

## Observaciones y mejoras pendientes

- **Los endpoints de Alumno y Materia no están protegidos:** ningún controlador tiene `[Authorize]`, así que se pueden usar sin token. Para protegerlos, agregar `[Authorize]` a `AlumnoController` y `MateriaController`, y un guard de rutas en Angular.
- **Clave JWT en `appsettings.json`:** para algo más que práctica, moverla a *User Secrets* o variables de entorno.
- La cadena de conexión está duplicada en `SeedUser/Program.cs`.
- `CrearAlumnoDto` en el frontend declara `apellido`, pero el backend espera `materiaId`.
- `materia-service.ts` importa `environment.development` en lugar de `environment`.
- `ExepctionNotFound` está vacía y sin uso; la entidad `Profesor` no tiene endpoints.
- El build `TestAngular/dist/` y las carpetas `.vs/`, `bin/` y `obj/` no deberían versionarse (agregar al `.gitignore`).
