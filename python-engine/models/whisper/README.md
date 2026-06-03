# Whisper 캐시

선택 디렉터리:

```text
models--openai--whisper-tiny/
```

모델:

```text
openai/whisper-tiny
https://huggingface.co/openai/whisper-tiny
```

전사 활성화:

```bash
export MOUTH_OF_TRUTH_ENABLE_TRANSCRIPTION=1
```

Hugging Face 캐시 구조를 그대로 유지합니다.

캐시가 없고 네트워크를 사용할 수 있으면 Transformers가 최초 실행 때 같은 캐시 루트 아래로 모델을 내려받습니다. 오프라인 배포본은 `models--openai--whisper-tiny/` 디렉터리를 모델 번들에 포함합니다.
