using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EcommerceApi.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanSuscripcion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaSuscripcion",
                table: "Tiendas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaVencimientoSuscripcion",
                table: "Tiendas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlanSuscripcionId",
                table: "Tiendas",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PlanesSuscripcion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Descripcion = table.Column<string>(type: "text", nullable: false),
                    MaxProductos = table.Column<int>(type: "integer", nullable: false),
                    PrecioMensual = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanesSuscripcion", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "PlanesSuscripcion",
                columns: new[] { "Id", "Activo", "Descripcion", "FechaCreacion", "MaxProductos", "Nombre", "PrecioMensual" },
                values: new object[,]
                {
                    { 1, true, "Ideal para emprendedores que están comenzando", new DateTime(2026, 1, 20, 0, 48, 55, 101, DateTimeKind.Utc).AddTicks(1304), 20, "Plan Básico", 2999.99m },
                    { 2, true, "Perfecto para negocios en crecimiento", new DateTime(2026, 1, 20, 0, 48, 55, 101, DateTimeKind.Utc).AddTicks(1838), 30, "Plan Estándar", 4999.99m },
                    { 3, true, "Para negocios establecidos con catálogo mediano", new DateTime(2026, 1, 20, 0, 48, 55, 101, DateTimeKind.Utc).AddTicks(1843), 50, "Plan Profesional", 7999.99m },
                    { 4, true, "Sin límites para grandes emprendimientos", new DateTime(2026, 1, 20, 0, 48, 55, 101, DateTimeKind.Utc).AddTicks(1847), 100, "Plan Premium", 12999.99m }
                });

            migrationBuilder.UpdateData(
                table: "Tiendas",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "FechaCreacion", "FechaSuscripcion", "FechaVencimientoSuscripcion", "PlanSuscripcionId" },
                values: new object[] { new DateTime(2026, 1, 20, 0, 48, 55, 103, DateTimeKind.Utc).AddTicks(92), null, null, 4 });

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "FechaCreacion", "PasswordHash" },
                values: new object[] { new DateTime(2026, 1, 20, 0, 48, 55, 510, DateTimeKind.Utc).AddTicks(4893), "$2a$11$8VSBihZjivUjwWrJc1ZqludJDyhQbeTjTUzW/gs0wGsrQU5W1ldd." });

            migrationBuilder.CreateIndex(
                name: "IX_Tiendas_PlanSuscripcionId",
                table: "Tiendas",
                column: "PlanSuscripcionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tiendas_PlanesSuscripcion_PlanSuscripcionId",
                table: "Tiendas",
                column: "PlanSuscripcionId",
                principalTable: "PlanesSuscripcion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tiendas_PlanesSuscripcion_PlanSuscripcionId",
                table: "Tiendas");

            migrationBuilder.DropTable(
                name: "PlanesSuscripcion");

            migrationBuilder.DropIndex(
                name: "IX_Tiendas_PlanSuscripcionId",
                table: "Tiendas");

            migrationBuilder.DropColumn(
                name: "FechaSuscripcion",
                table: "Tiendas");

            migrationBuilder.DropColumn(
                name: "FechaVencimientoSuscripcion",
                table: "Tiendas");

            migrationBuilder.DropColumn(
                name: "PlanSuscripcionId",
                table: "Tiendas");

            migrationBuilder.UpdateData(
                table: "Tiendas",
                keyColumn: "Id",
                keyValue: 1,
                column: "FechaCreacion",
                value: new DateTime(2026, 1, 15, 18, 22, 13, 197, DateTimeKind.Utc).AddTicks(3796));

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "FechaCreacion", "PasswordHash" },
                values: new object[] { new DateTime(2026, 1, 15, 18, 22, 13, 653, DateTimeKind.Utc).AddTicks(6284), "$2a$11$UM4lkjDEUjV9EWXrcQwG7OUrbpFBeqWdGlFxFg4CLVxfparIYl00W" });
        }
    }
}
