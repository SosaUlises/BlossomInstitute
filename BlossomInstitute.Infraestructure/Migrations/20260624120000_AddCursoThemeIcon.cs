using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlossomInstitute.Infraestructure.Migrations
{
    /// <inheritdoc />
    [Migration("20260624120000_AddCursoThemeIcon")]
    public partial class AddCursoThemeIcon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ThemeIcon",
                table: "Cursos",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ThemeIcon",
                table: "Cursos");
        }
    }
}
