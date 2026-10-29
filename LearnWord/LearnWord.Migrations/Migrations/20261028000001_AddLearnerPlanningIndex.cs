using LearnWord.DAL;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
namespace LearnWord.Migrations.Migrations;
[DbContext(typeof(WordsDbContext))]
[Migration("20261028000001_AddLearnerPlanningIndex")]
public class AddLearnerPlanningIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
        => migrationBuilder.CreateIndex(name: "IX_Cards_CollectionId_DueDate_Id", schema: "words", table: "Cards", columns: new[] { "CollectionId", "DueDate", "Id" });
    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.DropIndex(name: "IX_Cards_CollectionId_DueDate_Id", schema: "words", table: "Cards");
}
