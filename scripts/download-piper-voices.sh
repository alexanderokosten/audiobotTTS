#!/usr/bin/env sh
set -eu

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
MODELS="$ROOT/models"
mkdir -p "$MODELS"

download_voice() {
  name="$1"
  url="$2"

  if [ ! -f "$MODELS/$name.onnx" ]; then
    echo "Downloading $name.onnx"
    curl -L -o "$MODELS/$name.onnx" "$url/$name.onnx"
  fi

  if [ ! -f "$MODELS/$name.onnx.json" ]; then
    echo "Downloading $name.onnx.json"
    curl -L -o "$MODELS/$name.onnx.json" "$url/$name.onnx.json"
  fi
}

download_voice "en_US-lessac-medium" "https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/lessac/medium"
download_voice "en_US-ryan-medium" "https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/ryan/medium"
download_voice "en_GB-alba-medium" "https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_GB/alba/medium"
download_voice "en_US-amy-medium" "https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/amy/medium"

echo "Piper voices are ready in $MODELS"
