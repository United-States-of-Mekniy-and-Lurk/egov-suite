using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectionService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddElectionSeatsAndWinners : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SeatCount",
                table: "Elections",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsWinner",
                table: "Candidates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "WinnerSelectedAt",
                table: "Candidates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WinnerSelectedByPersonId",
                table: "Candidates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Elections_SeatCount",
                table: "Elections",
                sql: "\"SeatCount\" IS NULL OR \"SeatCount\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Candidates_WinnerAudit",
                table: "Candidates",
                sql: "(NOT \"IsWinner\" AND \"WinnerSelectedAt\" IS NULL AND \"WinnerSelectedByPersonId\" IS NULL) OR (\"IsWinner\" AND \"WinnerSelectedAt\" IS NOT NULL AND \"WinnerSelectedByPersonId\" IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Elections_SeatCount",
                table: "Elections");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Candidates_WinnerAudit",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "SeatCount",
                table: "Elections");

            migrationBuilder.DropColumn(
                name: "IsWinner",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "WinnerSelectedAt",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "WinnerSelectedByPersonId",
                table: "Candidates");
        }
    }
}
