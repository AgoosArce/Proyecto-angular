using GestionAcademica.Domain.Constants;
using GestionAcademica.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionAcademica.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Alumno> Alumnos => Set<Alumno>();
    public DbSet<Materia> Materias => Set<Materia>();
    public DbSet<Profesor> Profesores => Set<Profesor>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Las columnas existentes se mantienen como estaban (nvarchar(max) NOT NULL) para que
        // la nueva arquitectura no genere cambios de esquema innecesarios sobre la base testprueba.

        modelBuilder.Entity<Alumno>(b =>
        {
            b.ToTable("Alumnos");
            b.Property(a => a.Nombre).IsRequired();
            b.Property(a => a.Email).IsRequired();
            b.HasOne(a => a.Materia)
             .WithMany(m => m.Alumnos)
             .HasForeignKey(a => a.MateriaId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Materia>(b =>
        {
            b.ToTable("Materias");
            b.Property(m => m.Nombre).IsRequired();
        });

        modelBuilder.Entity<Profesor>(b =>
        {
            b.ToTable("Profesores");
            b.Property(p => p.Nombre).IsRequired();
        });

        modelBuilder.Entity<Usuario>(b =>
        {
            b.ToTable("Usuarios");
            b.Property(u => u.Nombre).IsRequired();
            b.Property(u => u.Email).IsRequired();
            b.Property(u => u.PasswordHash).IsRequired();

            // Nueva columna (migración AddUsuarioRol). El valor por defecto en SQL
            // asigna "Usuario" a todos los registros que ya existían.
            b.Property(u => u.Rol)
             .IsRequired()
             .HasMaxLength(20)
             .HasDefaultValue(Roles.Usuario);
        });
    }
}
