from functools import lru_cache
import io
import os
from typing import Any

import numpy as np
import soundfile as sf
from fastapi import FastAPI, HTTPException
from fastapi.responses import StreamingResponse
from pydantic import BaseModel, Field


app = FastAPI(title="Cozy Qwen TTS", version="1.0.0")


class SynthesizeRequest(BaseModel):
    text: str = Field(min_length=1, max_length=12000)
    model: str | None = None
    mode: str = "custom_voice"
    speaker: str | None = None
    language: str = "Auto"
    instruction: str | None = None


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok"}


@lru_cache(maxsize=4)
def load_model(model_id: str) -> Any:
    from qwen_tts import Qwen3TTSModel

    kwargs: dict[str, Any] = {}

    device_map = os.getenv("QWEN_DEVICE_MAP", "auto").strip()
    if device_map:
        kwargs["device_map"] = device_map

    dtype = resolve_torch_dtype(os.getenv("QWEN_TORCH_DTYPE"))
    if dtype is not None:
        kwargs["dtype"] = dtype

    attn = os.getenv("QWEN_ATTN_IMPLEMENTATION", "").strip()
    if attn:
        kwargs["attn_implementation"] = attn

    return Qwen3TTSModel.from_pretrained(model_id, **kwargs)


@app.post("/synthesize")
def synthesize(request: SynthesizeRequest) -> StreamingResponse:
    model_id = (request.model or os.getenv("QWEN_DEFAULT_MODEL") or "").strip()
    if not model_id:
        raise HTTPException(status_code=400, detail="Qwen model is required.")

    mode = request.mode.strip().lower()
    language = request.language.strip() or "Auto"
    instruct = (request.instruction or "").strip()

    try:
        model = load_model(model_id)
        if mode in {"voice_design", "voice-design", "design"}:
            wavs, sample_rate = model.generate_voice_design(
                text=request.text,
                language=language,
                instruct=instruct or None,
            )
        elif mode in {"custom_voice", "custom-voice", "custom"}:
            speaker = (request.speaker or "").strip()
            if not speaker:
                raise HTTPException(status_code=400, detail="speaker is required for custom_voice mode.")

            wavs, sample_rate = model.generate_custom_voice(
                text=request.text,
                language=language,
                speaker=speaker,
                instruct=instruct or None,
            )
        else:
            raise HTTPException(status_code=400, detail=f"Unsupported Qwen TTS mode '{request.mode}'.")

        wav = first_waveform(wavs)
        buffer = io.BytesIO()
        sf.write(buffer, wav, sample_rate, format="WAV")
        buffer.seek(0)
        return StreamingResponse(buffer, media_type="audio/wav")
    except HTTPException:
        raise
    except Exception as exc:
        raise HTTPException(status_code=500, detail=str(exc)) from exc


def resolve_torch_dtype(value: str | None) -> Any | None:
    if not value:
        return None

    normalized = value.strip().lower()
    if not normalized:
        return None

    import torch

    mapping = {
        "float16": torch.float16,
        "fp16": torch.float16,
        "bfloat16": torch.bfloat16,
        "bf16": torch.bfloat16,
        "float32": torch.float32,
        "fp32": torch.float32,
    }
    if normalized not in mapping:
        raise ValueError(f"Unsupported QWEN_TORCH_DTYPE '{value}'.")

    return mapping[normalized]


def first_waveform(wavs: Any) -> np.ndarray:
    if isinstance(wavs, (list, tuple)):
        wav = wavs[0]
    else:
        wav = wavs

    if hasattr(wav, "detach"):
        wav = wav.detach().cpu().numpy()

    array = np.asarray(wav)
    if array.ndim > 1 and array.shape[0] == 1:
        array = array[0]

    return array.astype(np.float32, copy=False)
