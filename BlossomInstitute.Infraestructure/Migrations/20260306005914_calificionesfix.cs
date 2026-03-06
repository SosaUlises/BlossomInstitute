using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlossomInstitute.Infraestructure.Migrations
{
    /// <inheritdoc />
    public partial class calificionesfix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NombreEvaluacion",
                table: "Calificaciones",
                newName: "Titulo");

            migrationBuilder.RenameColumn(
                name: "Archivada",
                table: "Calificaciones",
                newName: "Archivado");

            migrationBuilder.AddColumn<string>(
                name: "Descripcion",
                table: "Calificaciones",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EntregaId",
                table: "Calificaciones",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TareaId",
                table: "Calificaciones",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Tipo",
                table: "Calificaciones",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Calificaciones_EntregaId",
                table: "Calificaciones",
                column: "EntregaId");

            migrationBuilder.CreateIndex(
                name: "IX_Calificaciones_TareaId",
                table: "Calificaciones",
                column: "TareaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Calificaciones_Entregas_EntregaId",
                table: "Calificaciones",
                column: "EntregaId",
                principalTable: "Entregas",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Calificaciones_Tareas_TareaId",
                table: "Calificaciones",
                column: "TareaId",
                principalTable: "Tareas",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Calificaciones_Entregas_EntregaId",
                table: "Calificaciones");

            migrationBuilder.DropForeignKey(
                name: "FK_Calificaciones_Tareas_TareaId",
                table: "Calificaciones");

            migrationBuilder.DropIndex(
                name: "IX_Calificaciones_EntregaId",
                table: "Calificaciones");

            migrationBuilder.DropIndex(
                name: "IX_Calificaciones_TareaId",
                table: "Calificaciones");

            migrationBuilder.DropColumn(
                name: "Descripcion",
                table: "Calificaciones");

            migrationBuilder.DropColumn(
                name: "EntregaId",
                table: "Calificaciones");

            migrationBuilder.DropColumn(
                name: "TareaId",
                table: "Calificaciones");

            migrationBuilder.DropColumn(
                name: "Tipo",
                table: "Calificaciones");

            migrationBuilder.RenameColumn(
                name: "Titulo",
                table: "Calificaciones",
                newName: "NombreEvaluacion");

            migrationBuilder.RenameColumn(
                name: "Archivado",
                table: "Calificaciones",
                newName: "Archivada");
        }
    }
}
