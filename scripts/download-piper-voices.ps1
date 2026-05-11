$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$models = Join-Path $root "models"
New-Item -ItemType Directory -Force -Path $models | Out-Null

$voices = @(
    @{
        Name = "en_US-lessac-medium"
        Url = "https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/lessac/medium"
    },
    @{
        Name = "en_US-ryan-medium"
        Url = "https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/ryan/medium"
    },
    @{
        Name = "en_GB-alba-medium"
        Url = "https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_GB/alba/medium"
    },
    @{
        Name = "en_US-amy-medium"
        Url = "https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/amy/medium"
    }
)

foreach ($voice in $voices) {
    $onnxPath = Join-Path $models "$($voice.Name).onnx"
    $jsonPath = Join-Path $models "$($voice.Name).onnx.json"

    if (!(Test-Path $onnxPath)) {
        Write-Host "Downloading $($voice.Name).onnx"
        Invoke-WebRequest -Uri "$($voice.Url)/$($voice.Name).onnx" -OutFile $onnxPath
    }

    if (!(Test-Path $jsonPath)) {
        Write-Host "Downloading $($voice.Name).onnx.json"
        Invoke-WebRequest -Uri "$($voice.Url)/$($voice.Name).onnx.json" -OutFile $jsonPath
    }
}

Write-Host "Piper voices are ready in $models"
