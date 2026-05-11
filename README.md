# Cozy TTS

Self-hosted MVP for generating cozy English and multilingual podcast/dialogue voiceovers with ASP.NET Core 8, React, PostgreSQL, Hangfire, Piper TTS, optional Qwen3-TTS and FFmpeg.

## Architecture

- `CozyTts.Domain`: entities and enums.
- `CozyTts.Application`: DTOs, validation, orchestration services, TTS/storage/queue abstractions.
- `CozyTts.Infrastructure`: EF Core PostgreSQL, repositories, local audio storage, Hangfire queue adapter, Piper and Qwen3 TTS engines.
- `CozyTts.Api`: Web API, Swagger, healthcheck, Hangfire dashboard.
- `frontend`: React + TypeScript UI.

The default MVP path uses Piper because it is light, CPU-friendly and easy to self-host. Qwen3-TTS is available as an optional sidecar for Russian, multilingual generation and instruction-based emotion/style control. Coqui TTS and Silero can be added later by implementing another routed TTS engine.

## Services

Docker Compose starts:

- `postgres`: metadata, jobs, voice profiles and audio history.
- `api`: ASP.NET Core API plus Hangfire worker in the same container.
- `frontend`: Nginx-served React app with `/api` proxy to the backend.
- `qwen-tts`: optional FastAPI sidecar, enabled with the `qwen` Docker Compose profile.

Audio files are saved through `IAudioStorage` to `AUDIO_OUTPUT_PATH`. Piper models are mounted from `./models`.

## Quick Start

```bash
cp .env.example .env
mkdir -p models
```

Download the seeded Piper voices into `models/`:

PowerShell:

```powershell
.\scripts\download-piper-voices.ps1
```

Bash:

```bash
./scripts/download-piper-voices.sh
```

Manual download commands:

```bash
curl -L -o models/en_US-lessac-medium.onnx https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/lessac/medium/en_US-lessac-medium.onnx
curl -L -o models/en_US-lessac-medium.onnx.json https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/lessac/medium/en_US-lessac-medium.onnx.json

curl -L -o models/en_US-ryan-medium.onnx https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/ryan/medium/en_US-ryan-medium.onnx
curl -L -o models/en_US-ryan-medium.onnx.json https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/ryan/medium/en_US-ryan-medium.onnx.json

curl -L -o models/en_GB-alba-medium.onnx https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_GB/alba/medium/en_GB-alba-medium.onnx
curl -L -o models/en_GB-alba-medium.onnx.json https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_GB/alba/medium/en_GB-alba-medium.onnx.json

curl -L -o models/en_US-amy-medium.onnx https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/amy/medium/en_US-amy-medium.onnx
curl -L -o models/en_US-amy-medium.onnx.json https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/amy/medium/en_US-amy-medium.onnx.json
```

Start:

```bash
docker compose up -d --build
```

Start with optional Qwen3-TTS:

```bash
docker compose --profile qwen up -d --build
```

The first Qwen request downloads model weights into the `qwen-cache` Docker volume. A GPU is strongly recommended for practical generation speed; CPU can work but is slow.

On Windows, Docker Desktop Linux engine needs the Windows hypervisor. If Docker reports `hasNoVirtualization=true`, check:

```powershell
bcdedit /enum | Select-String hypervisorlaunchtype
Get-ComputerInfo -Property HyperVisorPresent
```

`hypervisorlaunchtype` must be `Auto`. If it was changed from `Off` to `Auto`, reboot Windows before running Docker. After reboot:

```powershell
.\scripts\check-docker.ps1
docker compose up -d --build
```

Open:

- Frontend: http://localhost:3000
- API Swagger: http://localhost:8080/swagger
- Hangfire: http://localhost:8080/hangfire
- Health: http://localhost:8080/health

## Configuration

Important environment variables:

- `POSTGRES_CONNECTION`: PostgreSQL connection string.
- `PIPER_BIN_PATH`: Piper CLI path. Docker image defaults to `/opt/piper/bin/piper`.
- `PIPER_MODELS_PATH`: mounted voice model directory. Defaults to `/models`.
- `AUDIO_OUTPUT_PATH`: final audio storage directory. Defaults to `/data/audio`.
- `FFMPEG_BIN_PATH`: FFmpeg binary path. Defaults to `/usr/bin/ffmpeg`.
- `TTS_MAX_CHUNK_CHARACTERS`: text chunk size before Piper synthesis. Defaults to `1800`.
- `QWEN_TTS_ENDPOINT`: Qwen sidecar URL from the API container. Defaults to `http://qwen-tts:8090`.
- `QWEN_TTS_TIMEOUT_MINUTES`: HTTP timeout for heavy Qwen generations. Defaults to `30`.
- `QWEN_MAX_CHUNK_CHARACTERS`: text chunk size before Qwen synthesis. Defaults to `1200`.
- `QWEN_DEVICE_MAP`, `QWEN_TORCH_DTYPE`, `QWEN_ATTN_IMPLEMENTATION`: passed to the Python Qwen model loader.
- `APPLY_MIGRATIONS`: applies EF migrations on API startup. Defaults to `true`.

## API

Create a project:

```bash
curl -X POST http://localhost:8080/api/projects \
  -H "Content-Type: application/json" \
  -d '{"title":"Rainy Evening Dialogue","sourceText":"Emma: It is raining again, but I like this sound.\nAlex: Me too. It makes the room feel quiet and warm."}'
```

List voices:

```bash
curl http://localhost:8080/api/voices
```

Update a project:

```bash
curl -X PUT http://localhost:8080/api/projects/{projectId} \
  -H "Content-Type: application/json" \
  -d '{"title":"Updated Dialogue","sourceText":"Emma: The tea is ready.\nAlex: Perfect timing."}'
```

Create a generation job:

```bash
curl -X POST http://localhost:8080/api/projects/{projectId}/generate \
  -H "Content-Type: application/json" \
  -d '{"voiceProfileCode":"cozy_female","speed":"slow","outputFormat":"mp3"}'
```

Create a podcast-style dialogue job with separate voices:

```bash
curl -X POST http://localhost:8080/api/projects/{projectId}/generate \
  -H "Content-Type: application/json" \
  -d '{
    "voiceProfileCode":"narrator",
    "speed":"slow",
    "outputFormat":"mp3",
    "useDialogueVoices":true,
    "speakerVoiceProfileCodes":{
      "Emma":"cozy_female",
      "Alex":"calm_male"
    }
  }'
```

Dialogue mode parses lines like `Emma: Good morning.` and `Alex: Good morning, Emma.`. Speaker labels are not spoken; each line is rendered with the mapped voice and stitched into one audio file.

Create a Russian Qwen job with emotion/style instruction:

```bash
curl -X POST http://localhost:8080/api/projects/{projectId}/generate \
  -H "Content-Type: application/json" \
  -d '{
    "voiceProfileCode":"qwen_ru_soft_female",
    "speed":"slow",
    "outputFormat":"mp3",
    "language":"Russian",
    "emotionPrompt":"Радостно, мягко, с улыбкой, как уютный вечерний подкаст."
  }'
```

Create a Russian podcast dialogue with separate Qwen voices:

```bash
curl -X POST http://localhost:8080/api/projects/{projectId}/generate \
  -H "Content-Type: application/json" \
  -d '{
    "voiceProfileCode":"qwen_ru_soft_female",
    "speed":"slow",
    "outputFormat":"mp3",
    "language":"Russian",
    "emotionPrompt":"Теплый, живой диалог без спешки.",
    "useDialogueVoices":true,
    "speakerVoiceProfileCodes":{
      "Анна":"qwen_ru_soft_female",
      "Марк":"qwen_ru_calm_male"
    }
  }'
```

Dialogue speaker names may use Latin or Cyrillic letters.

Check status:

```bash
curl http://localhost:8080/api/jobs/{jobId}
```

Stream audio:

```bash
curl -L http://localhost:8080/api/jobs/{jobId}/audio --output preview.mp3
```

Download:

```bash
curl -L http://localhost:8080/api/jobs/{jobId}/download --output cozy-dialogue.mp3
```

Retry:

```bash
curl -X POST http://localhost:8080/api/jobs/{jobId}/retry
```

## Local Development

Start PostgreSQL, then run:

```bash
dotnet restore
dotnet ef database update --project src/CozyTts.Infrastructure --startup-project src/CozyTts.Api
dotnet run --project src/CozyTts.Api
```

Frontend:

```bash
cd frontend
npm install
npm run dev
```

The Vite dev server proxies `/api`, `/health`, and `/hangfire` to `http://localhost:8080`.

## Piper Notes

The seeded voice profiles point to file names in `./models`. To use different voices, either replace those files with matching names or update rows in `voice_profiles`.

Piper model files come as a pair:

- `*.onnx`
- `*.onnx.json`

The official Piper voices are hosted at [rhasspy/piper-voices on Hugging Face](https://huggingface.co/rhasspy/piper-voices). Piper releases are available at [rhasspy/piper on GitHub](https://github.com/rhasspy/piper/releases).

## Qwen3-TTS Notes

Qwen3-TTS is integrated through `services/qwen-tts`, not inside the API image. This keeps the normal Piper stack small and lets you run the heavier Python/Torch runtime only when needed.

Seeded Qwen profiles:

- `qwen_ryan_en`
- `qwen_aiden_en`
- `qwen_ru_soft_female`
- `qwen_ru_calm_male`
- `qwen_voice_design`

The sidecar uses the official `qwen-tts` Python package. The package supports `generate_custom_voice` with `language`, `speaker` and optional `instruct`, and `generate_voice_design` with natural-language voice instructions. Official docs list Russian among the supported languages and describe instruction control for emotion/prosody:

- [QwenLM/Qwen3-TTS](https://github.com/QwenLM/Qwen3-TTS)
- [qwen-tts on PyPI](https://pypi.org/project/qwen-tts/)

Useful model download commands if you want to warm the cache manually inside the sidecar or a Python environment:

```bash
pip install -U "huggingface_hub[cli]"
huggingface-cli download Qwen/Qwen3-TTS-12Hz-0.6B-CustomVoice
huggingface-cli download Qwen/Qwen3-TTS-12Hz-1.7B-VoiceDesign
```

## Future Extensions

The current MVP supports one default voice or per-speaker dialogue voice mapping. The extension points are already present for:

- LLM-generated scripts.
- Subtitles and timestamps.
- Video generation.
- Publishing automation for YouTube Shorts or TikTok.
- Batch generation API.
