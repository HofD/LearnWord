using LearnWord.DAL;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace LearnWord.Migrations.Migrations;
[DbContext(typeof(WordsDbContext))]
[Migration("20261028000000_AddLearnerModel")]
public class AddLearnerModel : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "LearningRevision", schema: "words", table: "Cards", type: "integer", nullable: false, defaultValue: 0);
        // Additive migration only: no legacy SRS rewrite and no invented evidence.
        migrationBuilder.Sql("""
            CREATE TABLE words."LearningSessions" (
                "Id" uuid PRIMARY KEY,
                "CollectionId" integer NOT NULL REFERENCES words."Collections"("Id") ON DELETE RESTRICT,
                "StartedAt" timestamp with time zone NOT NULL,
                "CompletedAt" timestamp with time zone NULL,
                "PlannedCardsCount" integer NOT NULL,
                "CompletedCardsCount" integer NOT NULL,
                "SuccessfulReviews" integer NOT NULL,
                "FailedReviews" integer NOT NULL
            );
            CREATE INDEX "IX_LearningSessions_CollectionId_StartedAt" ON words."LearningSessions" ("CollectionId", "StartedAt");
            CREATE TABLE words."LearningSessionCards" (
                "SessionId" uuid NOT NULL REFERENCES words."LearningSessions"("Id") ON DELETE CASCADE,
                "CardId" integer NOT NULL REFERENCES words."Cards"("Id") ON DELETE RESTRICT,
                "Position" integer NOT NULL,
                "LearningRevision" integer NOT NULL,
                PRIMARY KEY ("SessionId", "CardId")
            );
            CREATE INDEX "IX_LearningSessionCards_CardId" ON words."LearningSessionCards" ("CardId");
            CREATE TABLE words."LearnerCardStates" (
                "CardId" integer PRIMARY KEY REFERENCES words."Cards"("Id") ON DELETE RESTRICT,
                "LearningRevision" integer NOT NULL,
                "ModelVersion" integer NOT NULL,
                "Source" text NOT NULL,
                "MasteryScore" numeric NOT NULL,
                "DifficultyScore" numeric NOT NULL,
                "AttemptsCount" integer NOT NULL,
                "SuccessfulAttempts" integer NOT NULL,
                "FailedAttempts" integer NOT NULL,
                "ConsecutiveSuccesses" integer NOT NULL,
                "ConsecutiveFailures" integer NOT NULL,
                "OutcomePoints" numeric NOT NULL,
                "LastOutcome" integer NULL,
                "LastAttemptAt" timestamp with time zone NULL,
                "Status" integer NOT NULL
            );
            CREATE INDEX "IX_LearnerCardStates_Status" ON words."LearnerCardStates" ("Status");
            CREATE TABLE words."LearningAttempts" (
                "Id" uuid PRIMARY KEY,
                "CardId" integer NOT NULL REFERENCES words."Cards"("Id") ON DELETE RESTRICT,
                "SessionId" uuid NULL REFERENCES words."LearningSessions"("Id") ON DELETE RESTRICT,
                "RequestId" uuid NULL,
                "LearningRevision" integer NOT NULL,
                "Outcome" integer NOT NULL,
                "ReviewedAt" timestamp with time zone NOT NULL,
                "PreviousIntervalDays" integer NOT NULL,
                "NewIntervalDays" integer NOT NULL,
                "PreviousEaseFactor" numeric NOT NULL,
                "NewEaseFactor" numeric NOT NULL,
                "PreviousDueDate" timestamp with time zone NOT NULL,
                "NewDueDate" timestamp with time zone NOT NULL,
                "ResponseTimeMs" integer NULL
            );
            CREATE INDEX "IX_LearningAttempts_CardId_ReviewedAt" ON words."LearningAttempts" ("CardId", "ReviewedAt");
            CREATE UNIQUE INDEX "IX_LearningAttempts_CardId_RequestId" ON words."LearningAttempts" ("CardId", "RequestId");
            CREATE UNIQUE INDEX "IX_LearningAttempts_SessionId_CardId" ON words."LearningAttempts" ("SessionId", "CardId");
            CREATE INDEX "IX_LearningAttempts_ReviewedAt" ON words."LearningAttempts" ("ReviewedAt");
            """);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE words."LearningAttempts";
            DROP TABLE words."LearnerCardStates";
            DROP TABLE words."LearningSessionCards";
            DROP TABLE words."LearningSessions";
            """);
        migrationBuilder.DropColumn(name: "LearningRevision", schema: "words", table: "Cards");
    }
}
