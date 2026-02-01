using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceApi.Migrations
{
    /// <inheritdoc />
    public partial class AddTiendaContactAndEstado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodigoVerificacion",
                table: "Usuarios",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailVerificado",
                table: "Usuarios",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaExpiracionCodigo",
                table: "Usuarios",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EstadoTienda",
                table: "Tiendas",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LinkInstagram",
                table: "Tiendas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TelefonoWhatsApp",
                table: "Tiendas",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "PlanesSuscripcion",
                keyColumn: "Id",
                keyValue: 1,
                column: "FechaCreacion",
                value: new DateTime(2026, 1, 20, 1, 21, 23, 687, DateTimeKind.Utc).AddTicks(9009));

            migrationBuilder.UpdateData(
                table: "PlanesSuscripcion",
                keyColumn: "Id",
                keyValue: 2,
                column: "FechaCreacion",
                value: new DateTime(2026, 1, 20, 1, 21, 23, 687, DateTimeKind.Utc).AddTicks(9674));

            migrationBuilder.UpdateData(
                table: "PlanesSuscripcion",
                keyColumn: "Id",
                keyValue: 3,
                column: "FechaCreacion",
                value: new DateTime(2026, 1, 20, 1, 21, 23, 687, DateTimeKind.Utc).AddTicks(9678));

            migrationBuilder.UpdateData(
                table: "PlanesSuscripcion",
                keyColumn: "Id",
                keyValue: 4,
                column: "FechaCreacion",
                value: new DateTime(2026, 1, 20, 1, 21, 23, 687, DateTimeKind.Utc).AddTicks(9681));

            migrationBuilder.UpdateData(
                table: "Tiendas",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "EstadoTienda", "FechaCreacion", "LinkInstagram", "TelefonoWhatsApp" },
                values: new object[] { "Borrador", new DateTime(2026, 1, 20, 1, 21, 23, 691, DateTimeKind.Utc).AddTicks(1482), null, null });

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CodigoVerificacion", "EmailVerificado", "FechaCreacion", "FechaExpiracionCodigo", "PasswordHash" },
                values: new object[] { null, false, new DateTime(2026, 1, 20, 1, 21, 24, 135, DateTimeKind.Utc).AddTicks(3632), null, "$2a$11$qatfjdSykrkFWeyG4nRLAOibIdUfz4vdYFsQizqY0XnIE7cYr.IWW" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodigoVerificacion",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "EmailVerificado",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "FechaExpiracionCodigo",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "EstadoTienda",
                table: "Tiendas");

            migrationBuilder.DropColumn(
                name: "LinkInstagram",
                table: "Tiendas");

            migrationBuilder.DropColumn(
                name: "TelefonoWhatsApp",
                table: "Tiendas");

            migrationBuilder.UpdateData(
                table: "PlanesSuscripcion",
                keyColumn: "Id",
                keyValue: 1,
                column: "FechaCreacion",
                value: new DateTime(2026, 1, 20, 0, 48, 55, 101, DateTimeKind.Utc).AddTicks(1304));

            migrationBuilder.UpdateData(
                table: "PlanesSuscripcion",
                keyColumn: "Id",
                keyValue: 2,
                column: "FechaCreacion",
                value: new DateTime(2026, 1, 20, 0, 48, 55, 101, DateTimeKind.Utc).AddTicks(1838));

            migrationBuilder.UpdateData(
                table: "PlanesSuscripcion",
                keyColumn: "Id",
                keyValue: 3,
                column: "FechaCreacion",
                value: new DateTime(2026, 1, 20, 0, 48, 55, 101, DateTimeKind.Utc).AddTicks(1843));

            migrationBuilder.UpdateData(
                table: "PlanesSuscripcion",
                keyColumn: "Id",
                keyValue: 4,
                column: "FechaCreacion",
                value: new DateTime(2026, 1, 20, 0, 48, 55, 101, DateTimeKind.Utc).AddTicks(1847));

            migrationBuilder.UpdateData(
                table: "Tiendas",
                keyColumn: "Id",
                keyValue: 1,
                column: "FechaCreacion",
                value: new DateTime(2026, 1, 20, 0, 48, 55, 103, DateTimeKind.Utc).AddTicks(92));

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "FechaCreacion", "PasswordHash" },
                values: new object[] { new DateTime(2026, 1, 20, 0, 48, 55, 510, DateTimeKind.Utc).AddTicks(4893), "$2a$11$8VSBihZjivUjwWrJc1ZqludJDyhQbeTjTUzW/gs0wGsrQU5W1ldd." });
        }
    }
}
