using LearnWord.DAL;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace LearnWord.Migrations.Migrations;
[DbContext(typeof(WordsDbContext))]
[Migration("20261029000000_AddAiCoach")]
public class AddAiCoach : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Purely additive advisory storage; no existing SRS or learner rows are touched.
        migrationBuilder.Sql("""
            CREATE TABLE words."CoachAnalyses" (
                "Id" uuid PRIMARY KEY,
                "SessionId" uuid NOT NULL REFERENCES words."LearningSessions"("Id") ON DELETE RESTRICT,
                "CollectionId" integer NOT NULL REFERENCES words."Collections"("Id") ON DELETE RESTRICT,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL,
                "Status" text NOT NULL,
                "Decision" text NOT NULL,
                "Language" text NOT NULL,
                "Provider" text NOT NULL,
                "Model" text NOT NULL,
                "PromptVersion" text NOT NULL,
                "SchemaVersion" integer NOT NULL,
                "Generation" integer NOT NULL,
                "RetryCount" integer NOT NULL,
                "FailureCode" text NULL,
                "Summary" text NOT NULL,
                "ObservationsJson" text NOT NULL
            );
            CREATE UNIQUE INDEX "IX_CoachAnalyses_SessionId_PromptVersion" ON words."CoachAnalyses" ("SessionId", "PromptVersion");
            CREATE INDEX "IX_CoachAnalyses_CollectionId" ON words."CoachAnalyses" ("CollectionId");
            CREATE TABLE words."CoachInterventions" (
                "Id" uuid PRIMARY KEY,
                "AnalysisId" uuid NOT NULL REFERENCES words."CoachAnalyses"("Id") ON DELETE RESTRICT,
                "CardId" integer NOT NULL REFERENCES words."Cards"("Id") ON DELETE RESTRICT,
                "LearningRevision" integer NOT NULL,
                "Type" text NOT NULL,
                "Title" text NOT NULL,
                "Status" text NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "StartedAt" timestamp with time zone NULL,
                "CompletedAt" timestamp with time zone NULL,
                "PayloadJson" text NOT NULL
            );
            CREATE INDEX "IX_CoachInterventions_AnalysisId" ON words."CoachInterventions" ("AnalysisId");
            CREATE INDEX "IX_CoachInterventions_CardId_LearningRevision_CompletedAt" ON words."CoachInterventions" ("CardId", "LearningRevision", "CompletedAt");
            CREATE TABLE words."InterventionAttempts" (
                "Id" uuid PRIMARY KEY,
                "InterventionId" uuid NOT NULL REFERENCES words."CoachInterventions"("Id") ON DELETE RESTRICT,
                "ExerciseId" uuid NOT NULL,
                "CardId" integer NOT NULL REFERENCES words."Cards"("Id") ON DELETE RESTRICT,
                "SubmittedAt" timestamp with time zone NOT NULL,
                "Correct" boolean NOT NULL,
                "ResponseTimeMs" integer NULL,
                "OptionId" text NOT NULL
            );
            CREATE UNIQUE INDEX "IX_InterventionAttempts_InterventionId_ExerciseId" ON words."InterventionAttempts" ("InterventionId", "ExerciseId");
            CREATE INDEX "IX_InterventionAttempts_CardId" ON words."InterventionAttempts" ("CardId");
            """);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.Sql("""
            DROP TABLE words."InterventionAttempts";
            DROP TABLE words."CoachInterventions";
            DROP TABLE words."CoachAnalyses";
            """);
}
