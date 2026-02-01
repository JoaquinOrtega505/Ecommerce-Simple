using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceApi.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCamposPedidosAnonimos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "UsuarioId",
                table: "Pedidos",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "CompradorEmail",
                table: "Pedidos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompradorNombre",
                table: "Pedidos",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "PlanesSuscripcion",
                keyColumn: "Id",
                keyValue: 1,
                column: "FechaCreacion",
                value: new DateTime(2026, 1, 23, 21, 26, 1, 627, DateTimeKind.Utc).AddTicks(2144));

            migrationBuilder.UpdateData(
                table: "PlanesSuscripcion",
                keyColumn: "Id",
                keyValue: 2,
                column: "FechaCreacion",
                value: new DateTime(2026, 1, 23, 21, 26, 1, 627, DateTimeKind.Utc).AddTicks(2669));

            migrationBuilder.UpdateData(
                table: "PlanesSuscripcion",
                keyColumn: "Id",
                keyValue: 3,
                column: "FechaCreacion",
                value: new DateTime(2026, 1, 23, 21, 26, 1, 627, DateTimeKind.Utc).AddTicks(2674));

            migrationBuilder.UpdateData(
                table: "PlanesSuscripcion",
                keyColumn: "Id",
                keyValue: 4,
                column: "FechaCreacion",
                value: new DateTime(2026, 1, 23, 21, 26, 1, 627, DateTimeKind.Utc).AddTicks(2677));

            migrationBuilder.UpdateData(
                table: "Tiendas",
                keyColumn: "Id",
                keyValue: 1,
                column: "FechaCreacion",
                value: new DateTime(2026, 1, 23, 21, 26, 1, 629, DateTimeKind.Utc).AddTicks(755));

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "FechaCreacion", "PasswordHash" },
                values: new object[] { new DateTime(2026, 1, 23, 21, 26, 2, 119, DateTimeKind.Utc).AddTicks(313), "$2a$11$lMAf3JF2vZjxdIc9x6f5iuezdrTmM6CdufvAy2fNUJCzd7emd/jJC" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompradorEmail",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "CompradorNombre",
                table: "Pedidos");

            migrationBuilder.AlterColumn<int>(
                name: "UsuarioId",
                table: "Pedidos",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "PlanesSuscripcion",
                keyColumn: "Id",
                keyValue: 1,
                column: "FechaCreacion",
                value: new DateTime(2026, 1, 23, 20, 49, 25, 104, DateTimeKind.Utc).AddTicks(6583));

            migrationBuilder.UpdateData(
                table: "PlanesSuscripcion",
                keyColumn: "Id",
                keyValue: 2,
                column: "FechaCreacion",
                value: new DateTime(2026, 1, 23, 20, 49, 25, 104, DateTimeKind.Utc).AddTicks(7376));

            migrationBuilder.UpdateData(
                table: "PlanesSuscripcion",
                keyColumn: "Id",
                keyValue: 3,
                column: "FechaCreacion",
                value: new DateTime(2026, 1, 23, 20, 49, 25, 104, DateTimeKind.Utc).AddTicks(7383));

            migrationBuilder.UpdateData(
                table: "PlanesSuscripcion",
                keyColumn: "Id",
                keyValue: 4,
                column: "FechaCreacion",
                value: new DateTime(2026, 1, 23, 20, 49, 25, 104, DateTimeKind.Utc).AddTicks(7388));

            migrationBuilder.UpdateData(
                table: "Tiendas",
                keyColumn: "Id",
                keyValue: 1,
                column: "FechaCreacion",
                value: new DateTime(2026, 1, 23, 20, 49, 25, 107, DateTimeKind.Utc).AddTicks(5721));

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "FechaCreacion", "PasswordHash" },
                values: new object[] { new DateTime(2026, 1, 23, 20, 49, 26, 227, DateTimeKind.Utc).AddTicks(7035), "$2a$11$JzNq6o13ERxsrYmFmcsFru7mfnNOhZKzdD0KMUD5xqqEyq7eiBIke" });
        }
    }
}
