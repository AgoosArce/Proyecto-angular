using GestionAcademica.Domain.Constants;

namespace GestionAcademica.Domain.Entities;

public class Usuario
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    // Rol persistido en la base. Todo registro público nace como "Usuario".
    public string Rol { get; set; } = Roles.Usuario;
}
