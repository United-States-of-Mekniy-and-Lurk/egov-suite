using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectionService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCandidateWithdrawnAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "WithdrawnAt",
                table: "Candidates",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WithdrawnAt",
                table: "Candidates");
        }
    }
}
