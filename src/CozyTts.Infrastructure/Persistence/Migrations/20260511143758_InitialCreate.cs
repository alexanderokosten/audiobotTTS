using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CozyTts.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "voice_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    PiperModelPath = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    PiperConfigPath = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_voice_profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "voice_projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    SourceText = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_voice_projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "voice_generation_jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    VoiceProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Speed = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OutputFormat = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    QueueJobId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_voice_generation_jobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_voice_generation_jobs_voice_profiles_VoiceProfileId",
                        column: x => x.VoiceProfileId,
                        principalTable: "voice_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_voice_generation_jobs_voice_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "voice_projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "audio_files",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    MimeType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    DurationSeconds = table.Column<double>(type: "double precision", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audio_files", x => x.Id);
                    table.ForeignKey(
                        name: "FK_audio_files_voice_generation_jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "voice_generation_jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "voice_profiles",
                columns: new[] { "Id", "Code", "Description", "DisplayName", "IsEnabled", "PiperConfigPath", "PiperModelPath" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "cozy_female", "Warm American English voice for soft listening practice.", "Cozy Female", true, "en_US-lessac-medium.onnx.json", "en_US-lessac-medium.onnx" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "calm_male", "Calm American English male voice for dialogue narration.", "Calm Male", true, "en_US-ryan-medium.onnx.json", "en_US-ryan-medium.onnx" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "british_soft", "Soft British English profile. Replace model file if you prefer another Piper voice.", "British Soft", true, "en_GB-alba-medium.onnx.json", "en_GB-alba-medium.onnx" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "narrator", "Neutral narrator voice for monologues and long-form listening.", "Narrator", true, "en_US-amy-medium.onnx.json", "en_US-amy-medium.onnx" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_audio_files_JobId",
                table: "audio_files",
                column: "JobId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_voice_generation_jobs_CreatedAt",
                table: "voice_generation_jobs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_voice_generation_jobs_ProjectId",
                table: "voice_generation_jobs",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_voice_generation_jobs_Status",
                table: "voice_generation_jobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_voice_generation_jobs_VoiceProfileId",
                table: "voice_generation_jobs",
                column: "VoiceProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_voice_profiles_Code",
                table: "voice_profiles",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audio_files");

            migrationBuilder.DropTable(
                name: "voice_generation_jobs");

            migrationBuilder.DropTable(
                name: "voice_profiles");

            migrationBuilder.DropTable(
                name: "voice_projects");
        }
    }
}
