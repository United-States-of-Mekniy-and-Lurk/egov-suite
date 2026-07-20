using System;
using CitizenService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CitizenService.Infrastructure.Migrations
{
    [DbContext(typeof(CitizenDbContext))]
    [Migration("20260720210000_AddApplicationFormDrafts")]
    public partial class AddApplicationFormDrafts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationFormDrafts",
                columns: table => new
                {
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DefinitionJson = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByPersonId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationFormDrafts", x => x.Name);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ApplicationFormDrafts");
        }
    }
}