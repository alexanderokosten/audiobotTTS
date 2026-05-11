FROM python:3.12-slim

WORKDIR /app

ENV PYTHONUNBUFFERED=1 \
    QWEN_DEVICE_MAP=auto \
    QWEN_DEFAULT_MODEL=Qwen/Qwen3-TTS-12Hz-0.6B-CustomVoice

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        ca-certificates \
        curl \
        ffmpeg \
        git \
        libsndfile1 \
        sox \
    && rm -rf /var/lib/apt/lists/*

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY main.py .

EXPOSE 8090

HEALTHCHECK --interval=30s --timeout=10s --retries=10 CMD curl -f http://localhost:8090/health || exit 1

CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8090"]
