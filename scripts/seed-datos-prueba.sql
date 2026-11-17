/* =====================================================================
   DATOS DE PRUEBA — Gestión Académica (SOLO DESARROLLO)
   ---------------------------------------------------------------------
   Carga usuarios (Admin y Usuario), materias y alumnos en la base.
   Es idempotente: se puede ejecutar todas las veces que se quiera
   sin duplicar registros.

   Requisito: haber aplicado las migraciones (incluida AddUsuarioRol).

   Credenciales que quedan creadas:
     Admin   -> admin@test.com        / Admin123!
     Admin   -> secretaria@test.com   / Admin123!
     Usuario -> docente@test.com      / Usuario123!
     Usuario -> preceptor@test.com    / Usuario123!
     Usuario -> invitado@test.com     / Usuario123!

   Las contraseñas se guardan como hash BCrypt (el mismo algoritmo que
   usa la API), nunca en texto plano.
   ===================================================================== */
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF COL_LENGTH('dbo.Usuarios', 'Rol') IS NULL
BEGIN
    RAISERROR(N'Falta la columna Usuarios.Rol. Ejecute primero: dotnet ef database update (ver README).', 16, 1);
    RETURN;
END;

BEGIN TRANSACTION;

/* ───────────── 1. Usuarios y roles ───────────── */
DECLARE @HashAdmin   nvarchar(200) = N'$2a$11$0bBP4dcJ0Sgqb/t0XaVcZ.HbinKzF5PLMSAiS1jgor9qXuad/JztK'; -- Admin123!
DECLARE @HashUsuario nvarchar(200) = N'$2a$11$P662OllsgNjEYmjT8JWWWuepA8rHAkxNLnoLEEtfTs1GAeryPNOfi'; -- Usuario123!

DECLARE @usuarios TABLE (Nombre nvarchar(100), Email nvarchar(150), Rol nvarchar(20), Hash nvarchar(200));
INSERT INTO @usuarios VALUES
 (N'Admin Principal',       N'admin@test.com',      N'Admin',   @HashAdmin),
 (N'Secretaría Académica',  N'secretaria@test.com', N'Admin',   @HashAdmin),
 (N'Laura Docente',         N'docente@test.com',    N'Usuario', @HashUsuario),
 (N'Martín Preceptor',      N'preceptor@test.com',  N'Usuario', @HashUsuario),
 (N'Invitado Consulta',     N'invitado@test.com',   N'Usuario', @HashUsuario);

-- Si ya existen: se restablecen rol y contraseña a los valores de prueba.
UPDATE u SET u.Rol = s.Rol, u.PasswordHash = s.Hash, u.Nombre = s.Nombre
FROM Usuarios u JOIN @usuarios s ON u.Email = s.Email;

INSERT INTO Usuarios (Nombre, Email, PasswordHash, Rol)
SELECT s.Nombre, s.Email, s.Hash, s.Rol FROM @usuarios s
WHERE NOT EXISTS (SELECT 1 FROM Usuarios u WHERE u.Email = s.Email);

/* ───────────── 2. Materias ───────────── */
DECLARE @materias TABLE (Nombre nvarchar(100));
INSERT INTO @materias VALUES
 (N'Matemática'), (N'Historia'), (N'Física'), (N'Química'),
 (N'Programación'), (N'Inglés'), (N'Biología'),
 (N'Filosofía');   -- queda SIN alumnos: sirve para probar que se puede eliminar

INSERT INTO Materias (Nombre)
SELECT m.Nombre FROM @materias m
WHERE NOT EXISTS (SELECT 1 FROM Materias x WHERE x.Nombre = m.Nombre);

/* ───────────── 3. Alumnos (30, para probar paginación y filtros) ───────────── */
DECLARE @alumnos TABLE (Nombre nvarchar(100), Email nvarchar(150), Materia nvarchar(100));
INSERT INTO @alumnos VALUES
 (N'Ana García',        N'ana.garcia@test.com',        N'Matemática'),
 (N'Ana López',         N'ana.lopez@test.com',         N'Historia'),
 (N'Ana Martínez',      N'ana.martinez@test.com',      N'Programación'),
 (N'Juan Pérez',        N'juan.perez@test.com',        N'Matemática'),
 (N'Juan Gómez',        N'juan.gomez@test.com',        N'Física'),
 (N'Juan Rodríguez',    N'juan.rodriguez@test.com',    N'Programación'),
 (N'María Fernández',   N'maria.fernandez@test.com',   N'Química'),
 (N'María Sosa',        N'maria.sosa@test.com',        N'Matemática'),
 (N'Lucas Díaz',        N'lucas.diaz@test.com',        N'Historia'),
 (N'Lucía Romero',      N'lucia.romero@test.com',      N'Inglés'),
 (N'Sofía Álvarez',     N'sofia.alvarez@test.com',     N'Biología'),
 (N'Mateo Torres',      N'mateo.torres@test.com',      N'Programación'),
 (N'Valentina Ruiz',    N'valentina.ruiz@test.com',    N'Matemática'),
 (N'Benjamín Castro',   N'benjamin.castro@test.com',   N'Física'),
 (N'Camila Flores',     N'camila.flores@test.com',     N'Química'),
 (N'Tomás Herrera',     N'tomas.herrera@test.com',     N'Historia'),
 (N'Martina Medina',    N'martina.medina@test.com',    N'Inglés'),
 (N'Santiago Acosta',   N'santiago.acosta@test.com',   N'Programación'),
 (N'Isabella Rojas',    N'isabella.rojas@test.com',    N'Biología'),
 (N'Thiago Molina',     N'thiago.molina@test.com',     N'Matemática'),
 (N'Emilia Silva',      N'emilia.silva@test.com',      N'Física'),
 (N'Joaquín Vega',      N'joaquin.vega@test.com',      N'Programación'),
 (N'Catalina Núñez',    N'catalina.nunez@test.com',    N'Historia'),
 (N'Facundo Ríos',      N'facundo.rios@test.com',      N'Química'),
 (N'Agustina Ortiz',    N'agustina.ortiz@test.com',    N'Matemática'),
 (N'Nicolás Benítez',   N'nicolas.benitez@test.com',   N'Inglés'),
 (N'Paula Cabrera',     N'paula.cabrera@test.com',     N'Biología'),
 (N'Diego Morales',     N'diego.morales@test.com',     N'Programación'),
 (N'Julieta Suárez',    N'julieta.suarez@test.com',    N'Matemática'),
 (N'Federico Ramos',    N'federico.ramos@test.com',    N'Física');

INSERT INTO Alumnos (Nombre, Email, MateriaId)
SELECT a.Nombre, a.Email, m.Id
FROM @alumnos a
JOIN Materias m ON m.Nombre = a.Materia
WHERE NOT EXISTS (SELECT 1 FROM Alumnos x WHERE x.Email = a.Email);

COMMIT TRANSACTION;

/* ───────────── Resumen ───────────── */
PRINT N'';
PRINT N'== Usuarios de prueba ==';
SELECT Email, Rol FROM Usuarios WHERE Email IN (SELECT Email FROM @usuarios) ORDER BY Rol, Email;

PRINT N'== Alumnos por materia ==';
SELECT m.Nombre AS Materia, COUNT(a.Id) AS Alumnos
FROM Materias m LEFT JOIN Alumnos a ON a.MateriaId = m.Id
GROUP BY m.Nombre ORDER BY m.Nombre;

SELECT (SELECT COUNT(*) FROM Usuarios) AS TotalUsuarios,
       (SELECT COUNT(*) FROM Materias) AS TotalMaterias,
       (SELECT COUNT(*) FROM Alumnos)  AS TotalAlumnos;
