-- Datos de prueba para la base testprueba (solo desarrollo).
-- Es idempotente: se puede ejecutar varias veces sin duplicar registros.
--   sqlcmd -S "(localdb)\MSSQLLocalDB" -d testprueba -i docs/datos-de-prueba.sql
SET NOCOUNT ON;

-- ───────────── Materias ─────────────
DECLARE @materias TABLE (Nombre nvarchar(100));
INSERT INTO @materias VALUES
 (N'Matemática'), (N'Historia'), (N'Física'), (N'Química'),
 (N'Programación'), (N'Inglés'), (N'Biología'),
 (N'Filosofía');   -- queda SIN alumnos: sirve para probar que se puede eliminar

INSERT INTO Materias (Nombre)
SELECT m.Nombre FROM @materias m
WHERE NOT EXISTS (SELECT 1 FROM Materias x WHERE x.Nombre = m.Nombre);

-- ───────────── Alumnos ─────────────
DECLARE @alumnos TABLE (Nombre nvarchar(100), Email nvarchar(150), Materia nvarchar(100));
INSERT INTO @alumnos VALUES
 (N'Ana García',        'ana.garcia@test.com',        N'Matemática'),
 (N'Ana López',         'ana.lopez@test.com',         N'Historia'),
 (N'Ana Martínez',      'ana.martinez@test.com',      N'Programación'),
 (N'Juan Pérez',        'juan.perez@test.com',        N'Matemática'),
 (N'Juan Gómez',        'juan.gomez@test.com',        N'Física'),
 (N'Juan Rodríguez',    'juan.rodriguez@test.com',    N'Programación'),
 (N'María Fernández',   'maria.fernandez@test.com',   N'Química'),
 (N'María Sosa',        'maria.sosa@test.com',        N'Matemática'),
 (N'Lucas Díaz',        'lucas.diaz@test.com',        N'Historia'),
 (N'Lucía Romero',      'lucia.romero@test.com',      N'Inglés'),
 (N'Sofía Álvarez',     'sofia.alvarez@test.com',     N'Biología'),
 (N'Mateo Torres',      'mateo.torres@test.com',      N'Programación'),
 (N'Valentina Ruiz',    'valentina.ruiz@test.com',    N'Matemática'),
 (N'Benjamín Castro',   'benjamin.castro@test.com',   N'Física'),
 (N'Camila Flores',     'camila.flores@test.com',     N'Química'),
 (N'Tomás Herrera',     'tomas.herrera@test.com',     N'Historia'),
 (N'Martina Medina',    'martina.medina@test.com',    N'Inglés'),
 (N'Santiago Acosta',   'santiago.acosta@test.com',   N'Programación'),
 (N'Isabella Rojas',    'isabella.rojas@test.com',    N'Biología'),
 (N'Thiago Molina',     'thiago.molina@test.com',     N'Matemática'),
 (N'Emilia Silva',      'emilia.silva@test.com',      N'Física'),
 (N'Joaquín Vega',      'joaquin.vega@test.com',      N'Programación'),
 (N'Catalina Núñez',    'catalina.nunez@test.com',    N'Historia'),
 (N'Facundo Ríos',      'facundo.rios@test.com',      N'Química'),
 (N'Agustina Ortiz',    'agustina.ortiz@test.com',    N'Matemática'),
 (N'Nicolás Benítez',   'nicolas.benitez@test.com',   N'Inglés'),
 (N'Paula Cabrera',     'paula.cabrera@test.com',     N'Biología'),
 (N'Diego Morales',     'diego.morales@test.com',     N'Programación'),
 (N'Julieta Suárez',    'julieta.suarez@test.com',    N'Matemática'),
 (N'Federico Ramos',    'federico.ramos@test.com',    N'Física');

INSERT INTO Alumnos (Nombre, Email, MateriaId)
SELECT a.Nombre, a.Email, m.Id
FROM @alumnos a
JOIN Materias m ON m.Nombre = a.Materia
WHERE NOT EXISTS (SELECT 1 FROM Alumnos x WHERE x.Email = a.Email);

-- Resumen
SELECT m.Nombre AS Materia, COUNT(a.Id) AS Alumnos
FROM Materias m LEFT JOIN Alumnos a ON a.MateriaId = m.Id
GROUP BY m.Nombre ORDER BY m.Nombre;
SELECT COUNT(*) AS TotalAlumnos FROM Alumnos;
