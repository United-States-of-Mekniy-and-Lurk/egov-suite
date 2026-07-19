using System;
using CitizenService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CitizenService.Infrastructure.Migrations
{
    [DbContext(typeof(CitizenDbContext))]
    [Migration("20260719190000_AddRegistryFields")]
    public partial class AddRegistryFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RegistryFieldDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    LabelsJson = table.Column<string>(type: "jsonb", nullable: false),
                    FieldType = table.Column<string>(type: "text", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    UserEditable = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    OptionSourceType = table.Column<string>(type: "text", nullable: false),
                    StaticOptionsJson = table.Column<string>(type: "jsonb", nullable: true),
                    OptionSourceService = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    OptionSourcePath = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistryFieldDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CitizenFieldValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CitizenId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    SourceApplicationId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CitizenFieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CitizenFieldValues_Applications_SourceApplicationId",
                        column: x => x.SourceApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CitizenFieldValues_Citizens_CitizenId",
                        column: x => x.CitizenId,
                        principalTable: "Citizens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CitizenFieldValues_RegistryFieldDefinitions_FieldDefinit~",
                        column: x => x.FieldDefinitionId,
                        principalTable: "RegistryFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CitizenFieldValues_CitizenId_FieldDefinitionId",
                table: "CitizenFieldValues",
                columns: new[] { "CitizenId", "FieldDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CitizenFieldValues_FieldDefinitionId",
                table: "CitizenFieldValues",
                column: "FieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CitizenFieldValues_SourceApplicationId",
                table: "CitizenFieldValues",
                column: "SourceApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistryFieldDefinitions_Key",
                table: "RegistryFieldDefinitions",
                column: "Key",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CitizenFieldValues");
            migrationBuilder.DropTable(name: "RegistryFieldDefinitions");
        }
    }
}
