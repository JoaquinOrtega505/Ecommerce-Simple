using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceApi.Migrations
{
    /// <inheritdoc />
    public partial class AddPagoFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaDespacho",
                table: "Pedidos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaPago",
                table: "Pedidos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetodoPago",
                table: "Pedidos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransaccionId",
                table: "Pedidos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsuarioId1",
                table: "CarritoItems",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "FechaCreacion", "PasswordHash" },
                values: new object[] { new DateTime(2026, 1, 7, 22, 39, 1, 898, DateTimeKind.Utc).AddTicks(6356), "$2a$11$7lnUfklHaYpOcEdutH3.Q.cLqI0dfH0STeDQz.R.PPCamtw6mkQ1O" });

            migrationBuilder.CreateIndex(
                name: "IX_CarritoItems_UsuarioId1",
                table: "CarritoItems",
                column: "UsuarioId1");

            migrationBuilder.AddForeignKey(
                name: "FK_CarritoItems_Usuarios_UsuarioId1",
                table: "CarritoItems",
                column: "UsuarioId1",
                principalTable: "Usuarios",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CarritoItems_Usuarios_UsuarioId1",
                table: "CarritoItems");

            migrationBuilder.DropIndex(
                name: "IX_CarritoItems_UsuarioId1",
                table: "CarritoItems");

            migrationBuilder.DropColumn(
                name: "FechaDespacho",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "FechaPago",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "MetodoPago",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "TransaccionId",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "UsuarioId1",
                table: "CarritoItems");

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "FechaCreacion", "PasswordHash" },
                values: new object[] { new DateTime(2025, 11, 25, 23, 9, 35, 520, DateTimeKind.Utc).AddTicks(9779), "$2a$11$Pnq3hW/dKpacRO1AERDLLuXvCOUD3MXWBTLVkLGzcGlMXHV9uZCea" });
        }
    }
}
