using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectionService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialElectionSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Elections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    Type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    EligibilityMode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CredentialHashKeyVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    VotingStartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VotingEndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TerritoryCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EligibleVoterCount = table.Column<int>(type: "integer", nullable: true),
                    HistoricalParticipatingVoterCount = table.Column<int>(type: "integer", nullable: true),
                    HistoricalInvalidBallotCount = table.Column<int>(type: "integer", nullable: true),
                    IsHistorical = table.Column<bool>(type: "boolean", nullable: false),
                    HistoricalSourceReference = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ImportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ImportedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FinalizedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByPersonId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Elections", x => x.Id);
                    table.CheckConstraint("CK_Elections_EligibleVoterCount", "\"EligibleVoterCount\" IS NULL OR \"EligibleVoterCount\" >= 0");
                    table.CheckConstraint("CK_Elections_HistoricalInvalidBallotCount", "\"HistoricalInvalidBallotCount\" IS NULL OR \"HistoricalInvalidBallotCount\" >= 0");
                    table.CheckConstraint("CK_Elections_HistoricalMetadata", "(\"IsHistorical\" AND \"HistoricalSourceReference\" IS NOT NULL AND \"ImportedAt\" IS NOT NULL AND \"ImportedByPersonId\" IS NOT NULL AND \"HistoricalParticipatingVoterCount\" IS NOT NULL AND \"HistoricalInvalidBallotCount\" IS NOT NULL) OR (NOT \"IsHistorical\" AND \"HistoricalSourceReference\" IS NULL AND \"ImportedAt\" IS NULL AND \"ImportedByPersonId\" IS NULL AND \"HistoricalParticipatingVoterCount\" IS NULL AND \"HistoricalInvalidBallotCount\" IS NULL)");
                    table.CheckConstraint("CK_Elections_HistoricalParticipatingVoterCount", "\"HistoricalParticipatingVoterCount\" IS NULL OR \"HistoricalParticipatingVoterCount\" >= 0");
                    table.CheckConstraint("CK_Elections_HistoricalParticipationWithinEligible", "\"EligibleVoterCount\" IS NULL OR \"HistoricalParticipatingVoterCount\" IS NULL OR \"HistoricalParticipatingVoterCount\" <= \"EligibleVoterCount\"");
                });

            migrationBuilder.CreateTable(
                name: "AnonymousBallots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ElectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SelectionType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SelectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TerritoryCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnonymousBallots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnonymousBallots_Elections_ElectionId",
                        column: x => x.ElectionId,
                        principalTable: "Elections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ElectionResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ElectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SelectionType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SelectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SelectionLabel = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    TerritoryCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    VoteCount = table.Column<int>(type: "integer", nullable: false),
                    FinalizedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElectionResults", x => x.Id);
                    table.CheckConstraint("CK_ElectionResults_VoteCount", "\"VoteCount\" >= 0");
                    table.ForeignKey(
                        name: "FK_ElectionResults_Elections_ElectionId",
                        column: x => x.ElectionId,
                        principalTable: "Elections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ElectionTransitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ElectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ToStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ChangedByPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElectionTransitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ElectionTransitions_Elections_ElectionId",
                        column: x => x.ElectionId,
                        principalTable: "Elections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ParticipationRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ElectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Channel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CredentialHash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RecordedOn = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParticipationRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParticipationRecords_Elections_ElectionId",
                        column: x => x.ElectionId,
                        principalTable: "Elections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PartyLists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ElectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartyOrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    PartyRegistrationNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PartyName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ListName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartyLists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartyLists_Elections_ElectionId",
                        column: x => x.ElectionId,
                        principalTable: "Elections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReferendumOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ElectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferendumOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReferendumOptions_Elections_ElectionId",
                        column: x => x.ElectionId,
                        principalTable: "Elections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VoterRollEntries",
                columns: table => new
                {
                    ElectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AddedByPersonId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoterRollEntries", x => new { x.ElectionId, x.PersonId });
                    table.ForeignKey(
                        name: "FK_VoterRollEntries_Elections_ElectionId",
                        column: x => x.ElectionId,
                        principalTable: "Elections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VotingInvitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ElectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VotingInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VotingInvitations_Elections_ElectionId",
                        column: x => x.ElectionId,
                        principalTable: "Elections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Candidates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartyListId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Candidates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Candidates_PartyLists_PartyListId",
                        column: x => x.PartyListId,
                        principalTable: "PartyLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnonymousBallots_ElectionId_SelectionId_TerritoryCode",
                table: "AnonymousBallots",
                columns: new[] { "ElectionId", "SelectionId", "TerritoryCode" });

            migrationBuilder.CreateIndex(
                name: "IX_Candidates_PartyListId_PersonId",
                table: "Candidates",
                columns: new[] { "PartyListId", "PersonId" },
                unique: true,
                filter: "\"PersonId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Candidates_PartyListId_Position",
                table: "Candidates",
                columns: new[] { "PartyListId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ElectionResults_ElectionId_SelectionId_TerritoryCode",
                table: "ElectionResults",
                columns: new[] { "ElectionId", "SelectionId", "TerritoryCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Elections_Slug",
                table: "Elections",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ElectionTransitions_ElectionId_ChangedAt",
                table: "ElectionTransitions",
                columns: new[] { "ElectionId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ParticipationRecords_ElectionId_Channel_CredentialHash",
                table: "ParticipationRecords",
                columns: new[] { "ElectionId", "Channel", "CredentialHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartyLists_ElectionId_PartyOrganizationId",
                table: "PartyLists",
                columns: new[] { "ElectionId", "PartyOrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartyLists_ElectionId_SortOrder",
                table: "PartyLists",
                columns: new[] { "ElectionId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReferendumOptions_ElectionId_Code",
                table: "ReferendumOptions",
                columns: new[] { "ElectionId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReferendumOptions_ElectionId_SortOrder",
                table: "ReferendumOptions",
                columns: new[] { "ElectionId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VotingInvitations_ElectionId_TokenHash",
                table: "VotingInvitations",
                columns: new[] { "ElectionId", "TokenHash" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnonymousBallots");

            migrationBuilder.DropTable(
                name: "Candidates");

            migrationBuilder.DropTable(
                name: "ElectionResults");

            migrationBuilder.DropTable(
                name: "ElectionTransitions");

            migrationBuilder.DropTable(
                name: "ParticipationRecords");

            migrationBuilder.DropTable(
                name: "ReferendumOptions");

            migrationBuilder.DropTable(
                name: "VoterRollEntries");

            migrationBuilder.DropTable(
                name: "VotingInvitations");

            migrationBuilder.DropTable(
                name: "PartyLists");

            migrationBuilder.DropTable(
                name: "Elections");
        }
    }
}
