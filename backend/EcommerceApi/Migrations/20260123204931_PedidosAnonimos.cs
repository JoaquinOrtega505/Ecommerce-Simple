using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceApi.Migrations
{
    /// <inheritdoc />
    public partial class PedidosAnonimos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "PlanesSuscripcion",
                keyColumn: "Id",
                keyValue: 1,
                column: "FechaCreacion",
                value: new DateTime(2026, 1, 22, 22, 10, 20, 64, DateTimeKind.Utc).AddTicks(4655));

            migrationBuilder.UpdateData(
                table: "PlanesSuscripcion",
                keyColumn: "Id",
                keyValue: 2,
                column: "FechaCreacion",
                value: new DateTime(2026, 1, 22, 22, 10, 20, 64, DateTimeKind.Utc).AddTicks(5227));

            migrationBuilder.UpdateData(
                table: "PlanesSuscripcion",
                keyColumn: "Id",
                keyValue: 3,
                column: "FechaCreacion",
                value: new DateTime(2026, 1, 22, 22, 10, 20, 64, DateTimeKind.Utc).AddTicks(5231));

            migrationBuilder.UpdateData(
                table: "PlanesSuscripcion",
                keyColumn: "Id",
                keyValue: 4,
                column: "FechaCreacion",
                value: new DateTime(2026, 1, 22, 22, 10, 20, 64, DateTimeKind.Utc).AddTicks(5235));

            migrationBuilder.UpdateData(
                table: "Tiendas",
                keyColumn: "Id",
                keyValue: 1,
                column: "FechaCreacion",
                value: new DateTime(2026, 1, 22, 22, 10, 20, 66, DateTimeKind.Utc).AddTicks(3670));

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "FechaCreacion", "PasswordHash" },
                values: new object[] { new DateTime(2026, 1, 22, 22, 10, 20, 780, DateTimeKind.Utc).AddTicks(2911), "$2a$11$Lx4KomlMj9Vp1lC7dol/d.T6dl2UA6ty6YXPLv7pHtsoN4Y7zNAWu" });
        }
    }
}
