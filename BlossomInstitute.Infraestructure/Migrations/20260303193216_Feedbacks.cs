using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlossomInstitute.Infraestructure.Migrations
{
    /// <inheritdoc />
    public partial class Feedbacks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EntregaFeedbacks_EntregaId",
                table: "EntregaFeedbacks");

            migrationBuilder.AddColumn<bool>(
                name: "EsVigente",
                table: "EntregaFeedbacks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_EntregaFeedbacks_EntregaId",
                table: "EntregaFeedbacks",
                column: "EntregaId",
                unique: true,
                filter: "\"EsVigente\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_EntregaFeedbacks_EntregaId_EsVigente",
                table: "EntregaFeedbacks",
                columns: new[] { "EntregaId", "EsVigente" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EntregaFeedbacks_EntregaId",
                table: "EntregaFeedbacks");

            migrationBuilder.DropIndex(
                name: "IX_EntregaFeedbacks_EntregaId_EsVigente",
                table: "EntregaFeedbacks");

            migrationBuilder.DropColumn(
                name: "EsVigente",
                table: "EntregaFeedbacks");

            migrationBuilder.CreateIndex(
                name: "IX_EntregaFeedbacks_EntregaId",
                table: "EntregaFeedbacks",
                column: "EntregaId",
                unique: true);
        }
    }
}
