using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gonzai_API.Migrations
{
    /// <inheritdoc />
    public partial class AgregarClaveGeminiNegocio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GeminiApiKeyCifrada",
                table: "Negocio",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GeminiApiKeyFecha",
                table: "Negocio",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GeminiApiKeyCifrada",
                table: "Negocio");

            migrationBuilder.DropColumn(
                name: "GeminiApiKeyFecha",
                table: "Negocio");
        }
    }
}
