using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlossomInstitute.Infraestructure.Migrations
{
    /// <inheritdoc />
    public partial class cloudinary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "TareaRecursos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "TareaRecursos",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<long>(
                name: "SizeBytes",
                table: "TareaRecursos",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StorageKey",
                table: "TareaRecursos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StorageProvider",
                table: "TareaRecursos",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "TareaRecursos");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "TareaRecursos");

            migrationBuilder.DropColumn(
                name: "SizeBytes",
                table: "TareaRecursos");

            migrationBuilder.DropColumn(
                name: "StorageKey",
                table: "TareaRecursos");

            migrationBuilder.DropColumn(
                name: "StorageProvider",
                table: "TareaRecursos");
        }
    }
}
