using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BlossomInstitute.Infraestructure.Migrations
{
    /// <inheritdoc />
    public partial class Entregas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Entregas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TareaId = table.Column<int>(type: "integer", nullable: false),
                    AlumnoId = table.Column<int>(type: "integer", nullable: false),
                    Texto = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    FechaEntregaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entregas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Entregas_Alumnos_AlumnoId",
                        column: x => x.AlumnoId,
                        principalTable: "Alumnos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Entregas_Tareas_TareaId",
                        column: x => x.TareaId,
                        principalTable: "Tareas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EntregaAdjuntos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EntregaId = table.Column<int>(type: "integer", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntregaAdjuntos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntregaAdjuntos_Entregas_EntregaId",
                        column: x => x.EntregaId,
                        principalTable: "Entregas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EntregaFeedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EntregaId = table.Column<int>(type: "integer", nullable: false),
                    Comentario = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    Nota = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    FechaCorreccionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ArchivoCorregidoUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ArchivoCorregidoNombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntregaFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntregaFeedbacks_Entregas_EntregaId",
                        column: x => x.EntregaId,
                        principalTable: "Entregas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EntregaAdjuntos_EntregaId_Tipo",
                table: "EntregaAdjuntos",
                columns: new[] { "EntregaId", "Tipo" });

            migrationBuilder.CreateIndex(
                name: "IX_EntregaFeedbacks_EntregaId",
                table: "EntregaFeedbacks",
                column: "EntregaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Entregas_AlumnoId",
                table: "Entregas",
                column: "AlumnoId");

            migrationBuilder.CreateIndex(
                name: "IX_Entregas_TareaId_AlumnoId",
                table: "Entregas",
                columns: new[] { "TareaId", "AlumnoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Entregas_TareaId_Estado",
                table: "Entregas",
                columns: new[] { "TareaId", "Estado" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntregaAdjuntos");

            migrationBuilder.DropTable(
                name: "EntregaFeedbacks");

            migrationBuilder.DropTable(
                name: "Entregas");
        }
    }
}
