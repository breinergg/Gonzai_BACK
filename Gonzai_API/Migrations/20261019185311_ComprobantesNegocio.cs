using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Gonzai_API.Migrations
{
    /// <inheritdoc />
    public partial class ComprobantesNegocio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Cliente",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ComprobanteEnlace",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Token = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    VentaClienteId = table.Column<int>(type: "integer", nullable: true),
                    PagoClienteId = table.Column<int>(type: "integer", nullable: true),
                    Formato = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ExpiraEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComprobanteEnlace", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComprobanteEnlace_PagoCliente_PagoClienteId",
                        column: x => x.PagoClienteId,
                        principalTable: "PagoCliente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComprobanteEnlace_VentaCliente_VentaClienteId",
                        column: x => x.VentaClienteId,
                        principalTable: "VentaCliente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Negocio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NombreComercial = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Telefono = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Ciudad = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    CorreoRespuesta = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Mensaje = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FormatoPredeterminado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Logo = table.Column<byte[]>(type: "bytea", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Negocio", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComprobanteEnlace_PagoClienteId",
                table: "ComprobanteEnlace",
                column: "PagoClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_ComprobanteEnlace_Token",
                table: "ComprobanteEnlace",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComprobanteEnlace_VentaClienteId",
                table: "ComprobanteEnlace",
                column: "VentaClienteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComprobanteEnlace");

            migrationBuilder.DropTable(
                name: "Negocio");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Cliente");
        }
    }
}
