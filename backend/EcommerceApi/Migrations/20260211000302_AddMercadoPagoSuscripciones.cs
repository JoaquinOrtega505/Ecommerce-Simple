using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EcommerceApi.Migrations
{
    /// <inheritdoc />
    public partial class AddMercadoPagoSuscripciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EstadoSuscripcion",
                table: "Tiendas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaFinTrial",
                table: "Tiendas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaInicioTrial",
                table: "Tiendas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MercadoPagoSuscripcionId",
                table: "Tiendas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReintentosPago",
                table: "Tiendas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "MercadoPagoPlanId",
                table: "PlanesSuscripcion",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MercadoPagoSyncDate",
                table: "PlanesSuscripcion",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ConfiguracionSuscripciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DiasPrueba = table.Column<int>(type: "integer", nullable: false),
                    MaxReintentosPago = table.Column<int>(type: "integer", nullable: false),
                    DiasGraciaSuspension = table.Column<int>(type: "integer", nullable: false),
                    DiasAvisoFinTrial = table.Column<int>(type: "integer", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionSuscripciones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MercadoPagoCredenciales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccessToken = table.Column<string>(type: "text", nullable: true),
                    RefreshToken = table.Column<string>(type: "text", nullable: true),
                    PublicKey = table.Column<string>(type: "text", nullable: true),
                    MercadoPagoUserId = table.Column<string>(type: "text", nullable: true),
                    MercadoPagoEmail = table.Column<string>(type: "text", nullable: true),
                    TokenExpiracion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Conectado = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaConexion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaDesconexion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EsProduccion = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MercadoPagoCredenciales", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ConfiguracionSuscripciones",
                columns: new[] { "Id", "Activo", "DiasAvisoFinTrial", "DiasGraciaSuspension", "DiasPrueba", "FechaCreacion", "FechaModificacion", "MaxReintentosPago" },
                values: new object[] { 1, true, 2, 3, 7, new DateTime(2026, 2, 11, 0, 2, 57, 118, DateTimeKind.Utc).AddTicks(5312), null, 3 });

            migrationBuilder.InsertData(
                table: "MercadoPagoCredenciales",
                columns: new[] { "Id", "AccessToken", "Conectado", "EsProduccion", "FechaConexion", "FechaCreacion", "FechaDesconexion", "MercadoPagoEmail", "MercadoPagoUserId", "PublicKey", "RefreshToken", "TokenExpiracion" },
                values: new object[] { 1, null, false, false, null, new DateTime(2026, 2, 11, 0, 2, 57, 118, DateTimeKind.Utc).AddTicks(8750), null, null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "PlanesSuscripcion",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "FechaCreacion", "MercadoPagoPlanId", "MercadoPagoSyncDate" },
                values: new object[] { new DateTime(2026, 2, 11, 0, 2, 56, 691, DateTimeKind.Utc).AddTicks(873), null, null });

            migrationBuilder.UpdateData(
                table: "PlanesSuscripcion",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "FechaCreacion", "MercadoPagoPlanId", "MercadoPagoSyncDate" },
                values: new object[] { new DateTime(2026, 2, 11, 0, 2, 56, 691, DateTimeKind.Utc).AddTicks(1408), null, null });

            migrationBuilder.UpdateData(
                table: "PlanesSuscripcion",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "FechaCreacion", "MercadoPagoPlanId", "MercadoPagoSyncDate" },
                values: new object[] { new DateTime(2026, 2, 11, 0, 2, 56, 691, DateTimeKind.Utc).AddTicks(1414), null, null });

            migrationBuilder.UpdateData(
                table: "PlanesSuscripcion",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "FechaCreacion", "MercadoPagoPlanId", "MercadoPagoSyncDate" },
                values: new object[] { new DateTime(2026, 2, 11, 0, 2, 56, 691, DateTimeKind.Utc).AddTicks(1419), null, null });

            migrationBuilder.UpdateData(
                table: "Tiendas",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "EstadoSuscripcion", "FechaCreacion", "FechaFinTrial", "FechaInicioTrial", "MercadoPagoSuscripcionId", "ReintentosPago" },
                values: new object[] { null, new DateTime(2026, 2, 11, 0, 2, 56, 692, DateTimeKind.Utc).AddTicks(9437), null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "FechaCreacion", "PasswordHash" },
                values: new object[] { new DateTime(2026, 2, 11, 0, 2, 57, 117, DateTimeKind.Utc).AddTicks(8808), "$2a$11$kMas8M/Dfcg7krK3Gr/8GeMjfeAXu1uXAyB/sle3yVxRH3ebfnijK" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguracionSuscripciones");

            migrationBuilder.DropTable(
                name: "MercadoPagoCredenciales");

            migrationBuilder.DropColumn(
                name: "EstadoSuscripcion",
                table: "Tiendas");

            migrationBuilder.DropColumn(
                name: "FechaFinTrial",
                table: "Tiendas");

            migrationBuilder.DropColumn(
                name: "FechaInicioTrial",
                table: "Tiendas");

            migrationBuilder.DropColumn(
                name: "MercadoPagoSuscripcionId",
                table: "Tiendas");

            migrationBuilder.DropColumn(
                name: "ReintentosPago",
                table: "Tiendas");

            migrationBuilder.DropColumn(
                name: "MercadoPagoPlanId",
                table: "PlanesSuscripcion");

            migrationBuilder.DropColumn(
                name: "MercadoPagoSyncDate",
                table: "PlanesSuscripcion");

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
    }
}
