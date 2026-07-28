using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrganizationRegistry.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DescriptionCs",
                table: "LegalFormDefinitions",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionEn",
                table: "LegalFormDefinitions",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "LegalFormDefinitions",
                keyColumn: "Code",
                keyValue: "ASSOCIATION",
                columns: new[] { "DescriptionCs", "DescriptionEn" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "LegalFormDefinitions",
                keyColumn: "Code",
                keyValue: "COOP",
                columns: new[] { "DescriptionCs", "DescriptionEn" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "LegalFormDefinitions",
                keyColumn: "Code",
                keyValue: "FOUNDATION",
                columns: new[] { "DescriptionCs", "DescriptionEn" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "LegalFormDefinitions",
                keyColumn: "Code",
                keyValue: "LTD",
                columns: new[] { "DescriptionCs", "DescriptionEn" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "LegalFormDefinitions",
                keyColumn: "Code",
                keyValue: "PUBLIC",
                columns: new[] { "DescriptionCs", "DescriptionEn" },
                values: new object[] { null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DescriptionCs",
                table: "LegalFormDefinitions");

            migrationBuilder.DropColumn(
                name: "DescriptionEn",
                table: "LegalFormDefinitions");
        }
    }
}
