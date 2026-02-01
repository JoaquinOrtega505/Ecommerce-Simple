using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceApi.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCamposTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NumeroSeguimiento",
                table: "Pedidos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServicioEnvio",
                table: "Pedidos",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "FechaCreacion", "PasswordHash" },
                values: new object[] { new DateTime(2026, 1, 7, 23, 18, 9, 386, DateTimeKind.Utc).AddTicks(9345), "$2a$11$f9mG5reM/Gio3NFt1HppAuDgYc3P8dI7P7m/WNTqDhTDjobcm/uMG" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NumeroSeguimiento",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "ServicioEnvio",
                table: "Pedidos");

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "FechaCreacion", "PasswordHash" },
                values: new object[] { new DateTime(2026, 1, 7, 22, 39, 1, 898, DateTimeKind.Utc).AddTicks(6356), "$2a$11$7lnUfklHaYpOcEdutH3.Q.cLqI0dfH0STeDQz.R.PPCamtw6mkQ1O" });
        }
    }
}
