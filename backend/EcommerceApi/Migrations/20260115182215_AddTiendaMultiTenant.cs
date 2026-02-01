using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EcommerceApi.Migrations
{
    /// <inheritdoc />
    public partial class AddTiendaMultiTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Create Tiendas table first
            migrationBuilder.CreateTable(
                name: "Tiendas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Subdominio = table.Column<string>(type: "text", nullable: false),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    BannerUrl = table.Column<string>(type: "text", nullable: true),
                    Descripcion = table.Column<string>(type: "text", nullable: true),
                    MercadoPagoPublicKey = table.Column<string>(type: "text", nullable: true),
                    MercadoPagoAccessToken = table.Column<string>(type: "text", nullable: true),
                    EnvioHabilitado = table.Column<bool>(type: "boolean", nullable: false),
                    ApiEnvioProveedor = table.Column<string>(type: "text", nullable: true),
                    ApiEnvioCredenciales = table.Column<string>(type: "text", nullable: true),
                    MaxProductos = table.Column<int>(type: "integer", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tiendas", x => x.Id);
                });

            // Step 2: Insert default Tienda
            migrationBuilder.InsertData(
                table: "Tiendas",
                columns: new[] { "Id", "Activo", "ApiEnvioCredenciales", "ApiEnvioProveedor", "BannerUrl", "Descripcion", "EnvioHabilitado", "FechaCreacion", "FechaModificacion", "LogoUrl", "MaxProductos", "MercadoPagoAccessToken", "MercadoPagoPublicKey", "Nombre", "Subdominio" },
                values: new object[] { 1, true, null, null, null, null, false, new DateTime(2026, 1, 15, 18, 22, 13, 197, DateTimeKind.Utc).AddTicks(3796), null, null, 100, null, null, "Tienda Demo", "demo" });

            // Step 3: Add TiendaId columns to existing tables
            migrationBuilder.AddColumn<int>(
                name: "TiendaId",
                table: "Usuarios",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TiendaId",
                table: "Productos",
                type: "integer",
                nullable: false,
                defaultValue: 1); // Set default to 1 instead of 0

            migrationBuilder.AddColumn<int>(
                name: "TiendaId",
                table: "Pedidos",
                type: "integer",
                nullable: false,
                defaultValue: 1); // Set default to 1 instead of 0

            migrationBuilder.AddColumn<int>(
                name: "TiendaId",
                table: "Categorias",
                type: "integer",
                nullable: false,
                defaultValue: 1); // Set default to 1 instead of 0

            // Step 4: Update seed data
            migrationBuilder.UpdateData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 1,
                column: "TiendaId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 2,
                column: "TiendaId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 3,
                column: "TiendaId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "FechaCreacion", "Nombre", "PasswordHash", "Rol", "TiendaId" },
                values: new object[] { new DateTime(2026, 1, 15, 18, 22, 13, 653, DateTimeKind.Utc).AddTicks(6284), "Super Administrador", "$2a$11$UM4lkjDEUjV9EWXrcQwG7OUrbpFBeqWdGlFxFg4CLVxfparIYl00W", "SuperAdmin", null });

            // Step 5: Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_Tiendas_Subdominio",
                table: "Tiendas",
                column: "Subdominio",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_TiendaId",
                table: "Usuarios",
                column: "TiendaId");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_TiendaId",
                table: "Productos",
                column: "TiendaId");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_TiendaId",
                table: "Pedidos",
                column: "TiendaId");

            migrationBuilder.CreateIndex(
                name: "IX_Categorias_TiendaId",
                table: "Categorias",
                column: "TiendaId");

            // Step 6: Add foreign key constraints
            migrationBuilder.AddForeignKey(
                name: "FK_Categorias_Tiendas_TiendaId",
                table: "Categorias",
                column: "TiendaId",
                principalTable: "Tiendas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Pedidos_Tiendas_TiendaId",
                table: "Pedidos",
                column: "TiendaId",
                principalTable: "Tiendas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_Tiendas_TiendaId",
                table: "Productos",
                column: "TiendaId",
                principalTable: "Tiendas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Tiendas_TiendaId",
                table: "Usuarios",
                column: "TiendaId",
                principalTable: "Tiendas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categorias_Tiendas_TiendaId",
                table: "Categorias");

            migrationBuilder.DropForeignKey(
                name: "FK_Pedidos_Tiendas_TiendaId",
                table: "Pedidos");

            migrationBuilder.DropForeignKey(
                name: "FK_Productos_Tiendas_TiendaId",
                table: "Productos");

            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Tiendas_TiendaId",
                table: "Usuarios");

            migrationBuilder.DropTable(
                name: "Tiendas");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_TiendaId",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Productos_TiendaId",
                table: "Productos");

            migrationBuilder.DropIndex(
                name: "IX_Pedidos_TiendaId",
                table: "Pedidos");

            migrationBuilder.DropIndex(
                name: "IX_Categorias_TiendaId",
                table: "Categorias");

            migrationBuilder.DropColumn(
                name: "TiendaId",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "TiendaId",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "TiendaId",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "TiendaId",
                table: "Categorias");

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "FechaCreacion", "Nombre", "PasswordHash", "Rol" },
                values: new object[] { new DateTime(2026, 1, 7, 23, 18, 9, 386, DateTimeKind.Utc).AddTicks(9345), "Administrador", "$2a$11$f9mG5reM/Gio3NFt1HppAuDgYc3P8dI7P7m/WNTqDhTDjobcm/uMG", "Admin" });
        }
    }
}
