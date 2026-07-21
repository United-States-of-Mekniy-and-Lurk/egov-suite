using System;
using CitizenService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CitizenService.Infrastructure.Migrations
{
    [DbContext(typeof(CitizenDbContext))]
    [Migration("20260721130000_AddTemporalRegistryCorrections")]
    public partial class AddTemporalRegistryCorrections : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CitizenFieldValues");

            migrationBuilder.CreateTable(
                name: "FieldCorrectionRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CitizenId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentValue = table.Column<string>(type: "text", nullable: true),
                    ProposedValue = table.Column<string>(type: "text", nullable: false),
                    RequestReason = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewReason = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldCorrectionRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FieldCorrectionRequests_Citizens_CitizenId",
                        column: x => x.CitizenId,
                        principalTable: "Citizens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FieldCorrectionRequests_RegistryFieldDefinitions_FieldDefi~",
                        column: x => x.FieldDefinitionId,
                        principalTable: "RegistryFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                    SourceCorrectionRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                        name: "FK_CitizenFieldValues_FieldCorrectionRequests_SourceCorrecti~",
                        column: x => x.SourceCorrectionRequestId,
                        principalTable: "FieldCorrectionRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CitizenFieldValues_RegistryFieldDefinitions_FieldDefiniti~",
                        column: x => x.FieldDefinitionId,
                        principalTable: "RegistryFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FieldCorrectionRequests_CitizenId",
                table: "FieldCorrectionRequests",
                column: "CitizenId");
            migrationBuilder.CreateIndex(
                name: "IX_FieldCorrectionRequests_FieldDefinitionId",
                table: "FieldCorrectionRequests",
                column: "FieldDefinitionId");
            migrationBuilder.CreateIndex(
                name: "IX_FieldCorrectionRequests_Status",
                table: "FieldCorrectionRequests",
                column: "Status");
            migrationBuilder.CreateIndex(
                name: "IX_FieldCorrectionRequests_CitizenId_FieldDefinitionId",
                table: "FieldCorrectionRequests",
                columns: new[] { "CitizenId", "FieldDefinitionId" },
                unique: true,
                filter: "\"Status\" = 'Submitted'");

            migrationBuilder.CreateIndex(
                name: "IX_CitizenFieldValues_CitizenId_FieldDefinitionId",
                table: "CitizenFieldValues",
                columns: new[] { "CitizenId", "FieldDefinitionId" },
                unique: true,
                filter: "\"ValidTo\" IS NULL");
            migrationBuilder.CreateIndex(
                name: "IX_CitizenFieldValues_CitizenId_FieldDefinitionId_Valid~",
                table: "CitizenFieldValues",
                columns: new[] { "CitizenId", "FieldDefinitionId", "ValidFrom" });
            migrationBuilder.CreateIndex(
                name: "IX_CitizenFieldValues_FieldDefinitionId",
                table: "CitizenFieldValues",
                column: "FieldDefinitionId");
            migrationBuilder.CreateIndex(
                name: "IX_CitizenFieldValues_SourceApplicationId",
                table: "CitizenFieldValues",
                column: "SourceApplicationId");
            migrationBuilder.CreateIndex(
                name: "IX_CitizenFieldValues_SourceCorrectionRequestId",
                table: "CitizenFieldValues",
                column: "SourceCorrectionRequestId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CitizenFieldValues");
            migrationBuilder.DropTable(name: "FieldCorrectionRequests");

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
                    table.ForeignKey("FK_CitizenFieldValues_Applications_SourceApplicationId", x => x.SourceApplicationId, "Applications", "Id", onDelete: ReferentialAction.SetNull);
                    table.ForeignKey("FK_CitizenFieldValues_Citizens_CitizenId", x => x.CitizenId, "Citizens", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_CitizenFieldValues_RegistryFieldDefinitions_FieldDefiniti~", x => x.FieldDefinitionId, "RegistryFieldDefinitions", "Id", onDelete: ReferentialAction.Restrict);
                });
            migrationBuilder.CreateIndex("IX_CitizenFieldValues_FieldDefinitionId", "CitizenFieldValues", "FieldDefinitionId");
            migrationBuilder.CreateIndex("IX_CitizenFieldValues_SourceApplicationId", "CitizenFieldValues", "SourceApplicationId");
            migrationBuilder.CreateIndex("IX_CitizenFieldValues_CitizenId_FieldDefinitionId", "CitizenFieldValues", new[] { "CitizenId", "FieldDefinitionId" }, unique: true);
        }
    }
}