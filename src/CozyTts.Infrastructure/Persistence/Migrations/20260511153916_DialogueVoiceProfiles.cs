using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CozyTts.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DialogueVoiceProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SpeakerVoiceProfileCodesJson",
                table: "voice_generation_jobs",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UseDialogueVoices",
                table: "voice_generation_jobs",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SpeakerVoiceProfileCodesJson",
                table: "voice_generation_jobs");

            migrationBuilder.DropColumn(
                name: "UseDialogueVoices",
                table: "voice_generation_jobs");
        }
    }
}
