using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gonzai_API.Migrations
{
    /// <inheritdoc />
    public partial class AgregarSmtpNegocio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SmtpFecha",
                table: "Negocio",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmtpHost",
                table: "Negocio",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmtpPasswordCifrada",
                table: "Negocio",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SmtpPuerto",
                table: "Negocio",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmtpSeguridad",
                table: "Negocio",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmtpUsuario",
                table: "Negocio",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SmtpFecha",
                table: "Negocio");

            migrationBuilder.DropColumn(
                name: "SmtpHost",
                table: "Negocio");

            migrationBuilder.DropColumn(
                name: "SmtpPasswordCifrada",
                table: "Negocio");

            migrationBuilder.DropColumn(
                name: "SmtpPuerto",
                table: "Negocio");

            migrationBuilder.DropColumn(
                name: "SmtpSeguridad",
                table: "Negocio");

            migrationBuilder.DropColumn(
                name: "SmtpUsuario",
                table: "Negocio");
        }
    }
}
