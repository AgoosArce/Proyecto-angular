using Microsoft.Data.SqlClient;

// SOLO DESARROLLO. Crea un usuario (o actualiza su rol) en la tabla Usuarios.
//
// Uso:
//   dotnet run -- <email> <password> [nombre] [rol]
//   dotnet run -- admin2@test.com MiClave123 "Otro Admin" Admin
//
// La cadena de conexión se toma de la variable de entorno ConnectionStrings__Con
// (la misma que usa la API); si no existe, se usa LocalDB/testprueba.
// Requiere que la migración AddUsuarioRol esté aplicada.

if (args.Length < 2)
{
    Console.WriteLine("Uso: dotnet run -- <email> <password> [nombre] [rol: Usuario|Admin]");
    return 1;
}

var email = args[0].Trim().ToLowerInvariant();
var password = args[1];
var nombre = args.Length > 2 ? args[2] : "Usuario Prueba";
var rol = args.Length > 3 ? args[3] : "Usuario";

if (rol is not ("Usuario" or "Admin"))
{
    Console.WriteLine("Rol inválido. Valores permitidos: Usuario, Admin.");
    return 1;
}

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Con")
    ?? "Server=(localdb)\\MSSQLLocalDB;Database=testprueba;Trusted_Connection=True;TrustServerCertificate=True;";

using var connection = new SqlConnection(connectionString);
connection.Open();

var checkCmd = new SqlCommand("SELECT COUNT(*) FROM Usuarios WHERE Email = @Email", connection);
checkCmd.Parameters.AddWithValue("@Email", email);
var exists = (int)checkCmd.ExecuteScalar()! > 0;

if (exists)
{
    var update = new SqlCommand("UPDATE Usuarios SET Rol = @Rol WHERE Email = @Email", connection);
    update.Parameters.AddWithValue("@Rol", rol);
    update.Parameters.AddWithValue("@Email", email);
    update.ExecuteNonQuery();
    Console.WriteLine($"El usuario {email} ya existía: rol actualizado a {rol} (la contraseña no se modificó).");
    return 0;
}

var insertCmd = new SqlCommand(
    "INSERT INTO Usuarios (Nombre, Email, PasswordHash, Rol) VALUES (@Nombre, @Email, @PasswordHash, @Rol)",
    connection);
insertCmd.Parameters.AddWithValue("@Nombre", nombre);
insertCmd.Parameters.AddWithValue("@Email", email);
insertCmd.Parameters.AddWithValue("@PasswordHash", BCrypt.Net.BCrypt.HashPassword(password));
insertCmd.Parameters.AddWithValue("@Rol", rol);
insertCmd.ExecuteNonQuery();

Console.WriteLine($"Usuario creado: {email} (rol {rol}).");
return 0;
