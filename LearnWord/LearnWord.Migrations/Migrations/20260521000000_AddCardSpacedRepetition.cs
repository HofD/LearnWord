using System;
using LearnWord.DAL;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnWord.Migrations.Migrations
{
    [DbContext(typeof(WordsDbContext))]
    [Migration("20260521000000_AddCardSpacedRepetition")]
    public partial class AddCardSpacedRepetition : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DueDate",
                schema: "words",
                table: "Cards",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<decimal>(
                name: "EaseFactor",
                schema: "words",
                table: "Cards",
                type: "numeric",
                nullable: false,
                defaultValue: 2.5m);

            migrationBuilder.AddColumn<int>(
                name: "IntervalDays",
                schema: "words",
                table: "Cards",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReviewedAt",
                schema: "words",
                table: "Cards",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReviewCount",
                schema: "words",
                table: "Cards",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DueDate",
                schema: "words",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "EaseFactor",
                schema: "words",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "IntervalDays",
                schema: "words",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "LastReviewedAt",
                schema: "words",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "ReviewCount",
                schema: "words",
                table: "Cards");
        }
    }
}
