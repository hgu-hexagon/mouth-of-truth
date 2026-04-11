# Mouth of Truth 개발 환경 체크리스트

## 개요

이 문서는 새 개발 머신에서 Mouth of Truth 프로젝트를 실행 가능한 상태로 맞추기 위한
최소 설정 절차를 정리한다.

이 문서의 대상 독자는 Unity와 Python 개발 환경을 함께 다루는 개발자다.

이 문서를 완료하면 아래 작업을 수행할 수 있어야 한다.

- Unity 프로젝트를 올바른 경로로 연다.
- Python 분석 러너를 현재 프로젝트 기준으로 실행한다.
- 로컬 모델 자산을 현재 프로젝트 구조에 맞게 배치한다.
- 메인 씬을 열고 기본 컴파일 상태를 확인한다.

## 사전 조건

아래 조건을 먼저 만족해야 한다.

- 운영 체제는 macOS다.
- Unity `6000.4.1f1` 이 설치되어 있다.
- 기존 conda 환경 `mouth-truth` 를 사용할 수 있다.
- Git이 설치되어 있다.
- 프로젝트 저장소가 로컬에 체크아웃되어 있다.

프로젝트 루트 경로 예:

- `/Users/potterlim/Developments/Projects/new-mouth-of-truth`

## 프로젝트 구조

이 저장소에서 중요한 루트는 아래 둘이다.

- Unity 앱:
  - `unity-app/`
- Python 엔진:
  - `python-engine/`

중요:

- Unity로 직접 열어야 하는 경로는 저장소 루트가 아니라 `unity-app/` 이다.
- 즉 아래 경로를 열어야 한다.
  - `/Users/potterlim/Developments/Projects/new-mouth-of-truth/unity-app`

## Python 환경 설정

### 1. conda 환경 활성화

터미널에서 아래 순서로 환경을 활성화한다.

```bash
source /Users/potterlim/Developments/Packages/miniforge/etc/profile.d/conda.sh
conda activate mouth-truth
```

### 2. Python 패키지 기준 파일 확인

현재 프로젝트의 Python 패키지 기준 파일은 아래다.

- `python-engine/requirements.txt`

이 파일은 현재 프로젝트 런타임이 직접 사용하는 핵심 패키지 버전을 적는다.

주요 패키지 예:

- `torch`
- `torchaudio`
- `transformers`
- `huggingface_hub`
- `librosa`
- `soundfile`
- `opencv-python`
- `ultralytics`

### 3. Python 패키지 설치 또는 확인

기존 `mouth-truth` 환경을 그대로 쓰는 것이 기본이다.

필요하면 아래 명령으로 현재 기준 패키지를 맞춘다.

```bash
pip install -r /Users/potterlim/Developments/Projects/new-mouth-of-truth/python-engine/requirements.txt
```

## 로컬 모델 자산 배치

모델 파일은 Git에 포함되지 않는다.
따라서 아래 경로는 로컬 개발 환경에서 직접 채워져 있어야 한다.

### 필수 경로

- 얼굴 모델:
  - `python-engine/models/face/yolo26x_rafdb_best.pt`
- 음성 감정 모델:
  - `python-engine/models/voice/best_wav2vec2_iemocap/`
- Whisper 캐시:
  - `python-engine/models/whisper/models--openai--whisper-tiny/`

### 참고

- 런타임 코드는 더 이상 옛 프로젝트 경로를 fallback 하지 않는다.
- 현재 프로젝트는 `python-engine/models/` 안만 본다.
- 필요하면 환경 변수 `MOUTH_OF_TRUTH_MODELS_ROOT` 로 루트를 명시적으로 바꿀 수 있다.

## Unity 환경 설정

### 1. Unity 프로젝트 열기

Unity Hub에서 아래 경로를 연다.

- `/Users/potterlim/Developments/Projects/new-mouth-of-truth/unity-app`

루트 폴더를 Unity로 열지 않는다.

### 2. Unity 버전 확인

현재 기준 Unity 버전은 아래다.

- `6000.4.1f1`

다른 버전으로 열면 패키지나 직렬화 결과가 달라질 수 있다.

### 3. Unity 패키지 기준 확인

Unity 패키지 기준은 아래 파일이다.

- `unity-app/Packages/manifest.json`

현재 핵심 패키지는 아래를 포함한다.

- `com.ultraleap.tracking`
- `com.unity.inputsystem`
- `com.unity.render-pipelines.universal`
- `com.unity.ugui`
- `com.unity.ai.navigation`

## 메인 씬 확인

Unity에서 아래 씬을 연다.

- `Assets/Scenes/Main.unity`

현재 이 씬은 아래를 포함해야 한다.

- `MouthOfTruthApp`
- `Main Camera`
- `EventSystem`
- 메인 무대용 environment anchor
- 카드/입 anchor 구조

## Python 런타임 확인

프로젝트 루트에서 아래 명령으로 Python 소스가 컴파일되는지 확인한다.

```bash
source /Users/potterlim/Developments/Packages/miniforge/etc/profile.d/conda.sh
conda activate mouth-truth
cd /Users/potterlim/Developments/Projects/new-mouth-of-truth
python -m compileall python-engine/src
```

이 명령이 오류 없이 끝나야 한다.

## Whisper 오프라인 캐시 확인

현재 Whisper는 Hugging Face 전역 캐시가 아니라 현재 프로젝트 내부 캐시를 사용한다.

아래 경로가 존재하는지 확인한다.

- `python-engine/models/whisper/models--openai--whisper-tiny/`

오프라인 전사 테스트 예:

```bash
source /Users/potterlim/Developments/Packages/miniforge/etc/profile.d/conda.sh
conda activate mouth-truth
cd /Users/potterlim/Developments/Projects/new-mouth-of-truth
TRANSFORMERS_OFFLINE=1 HF_HUB_OFFLINE=1 PYTHONPATH=python-engine/src python - <<'PY'
from mouth_of_truth.speech.whisper_transcriber import WhisperTranscriber
transcriber = WhisperTranscriber()
print(transcriber._cache_directory)
print(transcriber._is_model_cached())
PY
```

위 테스트에서 현재 프로젝트의 `python-engine/models/whisper` 경로가 출력되어야 한다.

## 확인 방법

아래 항목이 모두 맞으면 기본 셋업이 완료된 상태다.

- Unity에서 `unity-app/` 프로젝트가 열린다.
- `Assets/Scenes/Main.unity` 가 열린다.
- Unity 콘솔에 치명적 컴파일 오류가 없다.
- Python `compileall` 이 통과한다.
- 얼굴 모델 경로가 존재한다.
- 음성 모델 경로가 존재한다.
- Whisper 캐시 경로가 존재한다.

## 문제 해결

### Unity가 빈 기본 씬으로 열린다

원인:

- 저장소 루트를 Unity 프로젝트로 잘못 열었을 가능성이 높다.

조치:

- 현재 Unity를 닫는다.
- `unity-app/` 경로만 다시 연다.

### Python이 모델을 찾지 못한다

원인:

- `python-engine/models/` 아래 로컬 모델 자산이 비어 있을 수 있다.

조치:

- 얼굴, 음성, Whisper 캐시 경로를 다시 확인한다.
- 필요하면 `MOUTH_OF_TRUTH_MODELS_ROOT` 를 명시적으로 설정한다.

### Whisper가 다운로드를 시도한다

원인:

- 현재 프로젝트 내부 Whisper 캐시가 비어 있을 수 있다.

조치:

- `python-engine/models/whisper/models--openai--whisper-tiny/` 경로를 채운다.
- 오프라인 검증 시 `TRANSFORMERS_OFFLINE=1` 과 `HF_HUB_OFFLINE=1` 을 함께 사용한다.

## 관련 문서

- `python-engine/requirements.txt`
- `unity-app/Packages/manifest.json`
- `python-engine/models/README.md`
- `docs/session-architecture-ko.md`
