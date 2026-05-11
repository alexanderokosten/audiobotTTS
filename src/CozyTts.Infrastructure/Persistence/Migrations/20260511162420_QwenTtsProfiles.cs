using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CozyTts.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class QwenTtsProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Engine",
                table: "voice_profiles",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "piper");

            migrationBuilder.AddColumn<string>(
                name: "QwenInstruction",
                table: "voice_profiles",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QwenLanguage",
                table: "voice_profiles",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QwenMode",
                table: "voice_profiles",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QwenModel",
                table: "voice_profiles",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QwenSpeaker",
                table: "voice_profiles",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmotionPrompt",
                table: "voice_generation_jobs",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "voice_generation_jobs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "voice_profiles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "Engine", "QwenInstruction", "QwenLanguage", "QwenMode", "QwenModel", "QwenSpeaker" },
                values: new object[] { "piper", null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "voice_profiles",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "Engine", "QwenInstruction", "QwenLanguage", "QwenMode", "QwenModel", "QwenSpeaker" },
                values: new object[] { "piper", null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "voice_profiles",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "Engine", "QwenInstruction", "QwenLanguage", "QwenMode", "QwenModel", "QwenSpeaker" },
                values: new object[] { "piper", null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "voice_profiles",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "Engine", "QwenInstruction", "QwenLanguage", "QwenMode", "QwenModel", "QwenSpeaker" },
                values: new object[] { "piper", null, null, null, null, null });

            migrationBuilder.InsertData(
                table: "voice_profiles",
                columns: new[] { "Id", "Code", "Description", "DisplayName", "Engine", "IsEnabled", "PiperConfigPath", "PiperModelPath", "QwenInstruction", "QwenLanguage", "QwenMode", "QwenModel", "QwenSpeaker" },
                values: new object[,]
                {
                    { new Guid("55555555-5555-5555-5555-555555555555"), "qwen_ryan_en", "Qwen3-TTS English male profile with instruction and emotion control.", "Qwen Ryan EN", "qwen3", true, null, "", "Warm, natural podcast voice with clear articulation.", "English", "custom_voice", "Qwen/Qwen3-TTS-12Hz-0.6B-CustomVoice", "Ryan" },
                    { new Guid("66666666-6666-6666-6666-666666666666"), "qwen_aiden_en", "Qwen3-TTS English male profile for natural dialogues.", "Qwen Aiden EN", "qwen3", true, null, "", "Friendly, relaxed American podcast voice.", "English", "custom_voice", "Qwen/Qwen3-TTS-12Hz-0.6B-CustomVoice", "Aiden" },
                    { new Guid("77777777-7777-7777-7777-777777777777"), "qwen_ru_soft_female", "Qwen3-TTS Russian-capable soft female profile.", "Qwen RU Soft Female", "qwen3", true, null, "", "Говори мягко, тепло и спокойно, как ведущая уютного подкаста.", "Russian", "custom_voice", "Qwen/Qwen3-TTS-12Hz-0.6B-CustomVoice", "Serena" },
                    { new Guid("88888888-8888-8888-8888-888888888888"), "qwen_ru_calm_male", "Qwen3-TTS Russian-capable calm male profile.", "Qwen RU Calm Male", "qwen3", true, null, "", "Говори спокойно, дружелюбно и естественно, как ведущий подкаста.", "Russian", "custom_voice", "Qwen/Qwen3-TTS-12Hz-0.6B-CustomVoice", "Ryan" },
                    { new Guid("99999999-9999-9999-9999-999999999999"), "qwen_voice_design", "Qwen3-TTS voice design profile. Heavier model, best with GPU.", "Qwen Voice Design", "qwen3", true, null, "", "Создай мягкий, эмоциональный голос для уютного подкастового диалога.", "Russian", "voice_design", "Qwen/Qwen3-TTS-12Hz-1.7B-VoiceDesign", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "voice_profiles",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"));

            migrationBuilder.DeleteData(
                table: "voice_profiles",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"));

            migrationBuilder.DeleteData(
                table: "voice_profiles",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"));

            migrationBuilder.DeleteData(
                table: "voice_profiles",
                keyColumn: "Id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888888"));

            migrationBuilder.DeleteData(
                table: "voice_profiles",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"));

            migrationBuilder.DropColumn(
                name: "Engine",
                table: "voice_profiles");

            migrationBuilder.DropColumn(
                name: "QwenInstruction",
                table: "voice_profiles");

            migrationBuilder.DropColumn(
                name: "QwenLanguage",
                table: "voice_profiles");

            migrationBuilder.DropColumn(
                name: "QwenMode",
                table: "voice_profiles");

            migrationBuilder.DropColumn(
                name: "QwenModel",
                table: "voice_profiles");

            migrationBuilder.DropColumn(
                name: "QwenSpeaker",
                table: "voice_profiles");

            migrationBuilder.DropColumn(
                name: "EmotionPrompt",
                table: "voice_generation_jobs");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "voice_generation_jobs");
        }
    }
}
