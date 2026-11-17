using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionAcademica.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUsuarioRol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Rol",
                table: "Usuarios",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Usuario");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rol",
                table: "Usuarios");
        }
    }
}
