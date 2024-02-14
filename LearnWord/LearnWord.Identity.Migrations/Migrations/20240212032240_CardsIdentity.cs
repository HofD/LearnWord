using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnWord.Identity.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class CardsIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CardIdentities",
                schema: "identitywords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CardId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardIdentities", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CardIdentities",
                schema: "identitywords");
        }
    }
}
