FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY CozyTts.sln ./
COPY src/CozyTts.Domain/CozyTts.Domain.csproj src/CozyTts.Domain/
COPY src/CozyTts.Application/CozyTts.Application.csproj src/CozyTts.Application/
COPY src/CozyTts.Infrastructure/CozyTts.Infrastructure.csproj src/CozyTts.Infrastructure/
COPY src/CozyTts.Api/CozyTts.Api.csproj src/CozyTts.Api/
RUN dotnet restore CozyTts.sln

COPY . .
RUN dotnet publish src/CozyTts.Api/CozyTts.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        ca-certificates \
        curl \
        espeak-ng \
        ffmpeg \
        libespeak-ng1 \
        python3 \
        python3-venv \
    && rm -rf /var/lib/apt/lists/*

RUN python3 -m venv /opt/piper \
    && /opt/piper/bin/pip install --no-cache-dir --upgrade pip \
    && /opt/piper/bin/pip install --no-cache-dir piper-tts

ENV ASPNETCORE_URLS=http://+:8080 \
    PIPER_BIN_PATH=/opt/piper/bin/piper \
    PIPER_MODELS_PATH=/models \
    FFMPEG_BIN_PATH=/usr/bin/ffmpeg \
    AUDIO_OUTPUT_PATH=/data/audio \
    TTS_TEMP_PATH=/tmp/cozytts

RUN mkdir -p /models /data/audio /tmp/cozytts \
    && chown -R app:app /data/audio /tmp/cozytts

COPY --from=build /app/publish .

USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "CozyTts.Api.dll"]
