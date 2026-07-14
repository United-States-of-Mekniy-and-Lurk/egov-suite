using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CitizenService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationForms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    DefinitionJson = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationForms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Applications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    FormName = table.Column<string>(type: "text", nullable: false),
                    FormVersion = table.Column<int>(type: "integer", nullable: false),
                    FormAnswers = table.Column<string>(type: "jsonb", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DecisionReason = table.Column<string>(type: "text", nullable: true),
                    ReviewerPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByPersonId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationTransitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromStatus = table.Column<string>(type: "text", nullable: false),
                    ToStatus = table.Column<string>(type: "text", nullable: false),
                    ChangedByPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationTransitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Citizens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    CitizenNumber = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ImportSource = table.Column<string>(type: "text", nullable: true),
                    CreatedByPersonId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Citizens", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ApplicationForms",
                columns: new[] { "Id", "CreatedAt", "DefinitionJson", "IsActive", "Name", "Version" },
                values: new object[] { new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "{\"title\":\"Citizenship Application\",\"fields\":[{\"name\":\"legal_name\",\"type\":\"text\",\"label\":\"Legal Name\",\"required\":true},{\"name\":\"motivation\",\"type\":\"textarea\",\"label\":\"Motivation\",\"required\":true},{\"name\":\"date_of_birth\",\"type\":\"date\",\"label\":\"Date of Birth\",\"required\":true}]}", true, "citizenship_application", 1 });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationForms_Name_Version",
                table: "ApplicationForms",
                columns: new[] { "Name", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationTransitions_ApplicationId",
                table: "ApplicationTransitions",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_Citizens_CitizenNumber",
                table: "Citizens",
                column: "CitizenNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Citizens_PersonId",
                table: "Citizens",
                column: "PersonId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationForms");

            migrationBuilder.DropTable(
                name: "Applications");

            migrationBuilder.DropTable(
                name: "ApplicationTransitions");

            migrationBuilder.DropTable(
                name: "Citizens");
        }
    }
}
