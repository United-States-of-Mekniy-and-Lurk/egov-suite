using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OrganizationRegistry.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLegalFormDefinitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LegalFormDefinitions",
                columns: table => new
                {
                    Code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    LabelEn = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    LabelCs = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalFormDefinitions", x => x.Code);
                });

            migrationBuilder.InsertData(
                table: "LegalFormDefinitions",
                columns: new[] { "Code", "IsActive", "LabelCs", "LabelEn", "SortOrder" },
                values: new object[,]
                {
                    { "ASSOCIATION", true, "Spolek", "Association", 30 },
                    { "COOP", true, "Družstvo", "Cooperative", 20 },
                    { "FOUNDATION", true, "Nadace", "Foundation", 40 },
                    { "LTD", true, "Společnost s ručením omezeným", "Limited company", 10 },
                    { "PUBLIC", true, "Veřejná instituce", "Public institution", 50 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LegalFormDefinitions");
        }
    }
}
