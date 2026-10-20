using System;
using LearnWord.DAL;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnWord.Migrations.Migrations;

[DbContext(typeof(WordsDbContext))]
[Migration("20261019000000_AddSocialCollections")]
public partial class AddSocialCollections : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "Visibility", schema: "words", table: "Collections",
            type: "integer", nullable: false, defaultValue: 0);
        // PostgreSQL evaluates this volatile expression separately for every existing row.
        migrationBuilder.AddColumn<Guid>(name: "PublicId", schema: "words", table: "Collections",
            type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()");
        migrationBuilder.AddColumn<int>(name: "ForkedFromCollectionId", schema: "words", table: "Collections",
            type: "integer", nullable: true);
        migrationBuilder.CreateIndex(name: "IX_Collections_PublicId", schema: "words", table: "Collections",
            column: "PublicId", unique: true);
        migrationBuilder.CreateIndex(name: "IX_Collections_Visibility_Id", schema: "words", table: "Collections",
            columns: new[] { "Visibility", "Id" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Collections_PublicId", schema: "words", table: "Collections");
        migrationBuilder.DropIndex(name: "IX_Collections_Visibility_Id", schema: "words", table: "Collections");
        migrationBuilder.DropColumn(name: "Visibility", schema: "words", table: "Collections");
        migrationBuilder.DropColumn(name: "PublicId", schema: "words", table: "Collections");
        migrationBuilder.DropColumn(name: "ForkedFromCollectionId", schema: "words", table: "Collections");
    }
}
