using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectionService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCertificationAndReceipt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Token",
                table: "VotingInvitations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CertificationQuorum",
                table: "Elections",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CertifiedAt",
                table: "Elections",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptHash",
                table: "AnonymousBallots",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "CertificationDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ElectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CertifierPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificationDecisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CertificationDecisions_Elections_ElectionId",
                        column: x => x.ElectionId,
                        principalTable: "Elections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnonymousBallots_ElectionId_ReceiptHash",
                table: "AnonymousBallots",
                columns: new[] { "ElectionId", "ReceiptHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CertificationDecisions_ElectionId_CertifierPersonId",
                table: "CertificationDecisions",
                columns: new[] { "ElectionId", "CertifierPersonId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CertificationDecisions");

            migrationBuilder.DropIndex(
                name: "IX_AnonymousBallots_ElectionId_ReceiptHash",
                table: "AnonymousBallots");

            migrationBuilder.DropColumn(
                name: "Token",
                table: "VotingInvitations");

            migrationBuilder.DropColumn(
                name: "CertificationQuorum",
                table: "Elections");

            migrationBuilder.DropColumn(
                name: "CertifiedAt",
                table: "Elections");

            migrationBuilder.DropColumn(
                name: "ReceiptHash",
                table: "AnonymousBallots");
        }
    }
}
