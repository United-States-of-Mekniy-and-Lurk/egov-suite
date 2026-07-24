using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrganizationRegistry.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHistoricalOrganizationImport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "EstablishedOn",
                table: "Organizations",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImportNote",
                table: "Organizations",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImportSourceReference",
                table: "Organizations",
                type: "character varying(240)",
                maxLength: 240,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstablishedOn",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "ImportNote",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "ImportSourceReference",
                table: "Organizations");
        }
    }
}
