using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OrganizationRegistry.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClassificationDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Scheme = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LabelEn = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    LabelCs = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassificationDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RegistrationNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Slug = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    LegalName = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    TradingName = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    LegalFormCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Purpose = table.Column<string>(type: "text", nullable: false),
                    RegisteredAddress = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByPersonId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegistrationApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicantPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    LegalName = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    TradingName = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    LegalFormCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Purpose = table.Column<string>(type: "text", nullable: false),
                    RegisteredAddress = table.Column<string>(type: "text", nullable: false),
                    RequestedClassificationCodes = table.Column<string[]>(type: "text[]", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewerPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    DecisionReason = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationApplications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationAccessGrants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GrantedByPersonId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationAccessGrants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationAccessGrants_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Visibility = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CategoryCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileName = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AltText = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByPersonId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationAssets_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationClassifications",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    DefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AssignedByPersonId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationClassifications", x => new { x.OrganizationId, x.DefinitionId });
                    table.ForeignKey(
                        name: "FK_OrganizationClassifications_ClassificationDefinitions_Defin~",
                        column: x => x.DefinitionId,
                        principalTable: "ClassificationDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationClassifications_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationCorrectionRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CurrentValue = table.Column<string>(type: "text", nullable: true),
                    ProposedValue = table.Column<string>(type: "text", nullable: true),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationCorrectionRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationCorrectionRequests_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegistrationTransitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ToStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ChangedByPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationTransitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrationTransitions_RegistrationApplications_Applicatio~",
                        column: x => x.ApplicationId,
                        principalTable: "RegistrationApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ClassificationDefinitions",
                columns: new[] { "Id", "Code", "IsActive", "LabelCs", "LabelEn", "Scheme", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), "business", true, "Podnik", "Business", "organization-category", 10 },
                    { new Guid("10000000-0000-0000-0000-000000000002"), "non-profit", true, "Nezisková organizace", "Non-profit organization", "organization-category", 20 },
                    { new Guid("10000000-0000-0000-0000-000000000003"), "political-party", true, "Politická strana", "Political party", "organization-category", 30 },
                    { new Guid("10000000-0000-0000-0000-000000000004"), "public-body", true, "Veřejný orgán", "Public body", "organization-category", 40 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClassificationDefinitions_Scheme_Code",
                table: "ClassificationDefinitions",
                columns: new[] { "Scheme", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationAccessGrants_OrganizationId_PersonId_RoleCode",
                table: "OrganizationAccessGrants",
                columns: new[] { "OrganizationId", "PersonId", "RoleCode" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationAccessGrants_PersonId_RevokedAt",
                table: "OrganizationAccessGrants",
                columns: new[] { "PersonId", "RevokedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationAssets_OrganizationId_Kind_Visibility",
                table: "OrganizationAssets",
                columns: new[] { "OrganizationId", "Kind", "Visibility" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationClassifications_DefinitionId",
                table: "OrganizationClassifications",
                column: "DefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationCorrectionRequests_OrganizationId_Status",
                table: "OrganizationCorrectionRequests",
                columns: new[] { "OrganizationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_LegalName",
                table: "Organizations",
                column: "LegalName");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_RegistrationNumber",
                table: "Organizations",
                column: "RegistrationNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Slug",
                table: "Organizations",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationApplications_ApplicantPersonId_Status",
                table: "RegistrationApplications",
                columns: new[] { "ApplicantPersonId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationApplications_Status_SubmittedAt",
                table: "RegistrationApplications",
                columns: new[] { "Status", "SubmittedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationTransitions_ApplicationId_ChangedAt",
                table: "RegistrationTransitions",
                columns: new[] { "ApplicationId", "ChangedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrganizationAccessGrants");

            migrationBuilder.DropTable(
                name: "OrganizationAssets");

            migrationBuilder.DropTable(
                name: "OrganizationClassifications");

            migrationBuilder.DropTable(
                name: "OrganizationCorrectionRequests");

            migrationBuilder.DropTable(
                name: "RegistrationTransitions");

            migrationBuilder.DropTable(
                name: "ClassificationDefinitions");

            migrationBuilder.DropTable(
                name: "Organizations");

            migrationBuilder.DropTable(
                name: "RegistrationApplications");
        }
    }
}
