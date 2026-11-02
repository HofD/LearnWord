using LearnWord.DAL;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace LearnWord.Migrations.Migrations;
[DbContext(typeof(WordsDbContext))]
[Migration("20261102000000_AddCoachDiagnostics")]
public class AddCoachDiagnostics : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
        => migrationBuilder.AddColumn<string>(name: "DiagnosticJson", schema: "words", table: "CoachAnalyses", type: "text", nullable: true);
    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.DropColumn(name: "DiagnosticJson", schema: "words", table: "CoachAnalyses");
}
