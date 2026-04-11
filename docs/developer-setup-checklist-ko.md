# Mouth of Truth 개발 환경 설정 가이드

## 개요

이 문서는 GitHub에서 저장소를 처음 내려받은 개발자가 Mouth of Truth 프로젝트를
로컬 환경에서 실행 가능한 상태로 만드는 방법을 설명한다.

이 문서의 대상 독자는 Unity와 Python 환경을 함께 다루는 개발자다.

이 문서를 완료하면 아래 작업을 수행할 수 있어야 한다.

- 저장소를 내려받는다.
- Python 분석 환경을 만든다.
- Unity 프로젝트를 올바른 경로로 연다.
- 메인 씬을 실행한다.
- 로컬 분석 모델이 없는 경우 deterministic 모드로 기본 플레이 흐름을 확인한다.
- 로컬 분석 모델이 있는 경우 Python 분석 경로까지 함께 실행한다.

## 사전 조건

아래 도구를 먼저 설치한다.

- Git
- Unity Hub
- Unity Editor `6000.4.1f1`
- Miniforge 또는 Mambaforge

현재 프로젝트는 macOS 기준으로 가장 먼저 검증되었다.

## 저장소 받기

원하는 작업 디렉토리에서 저장소를 클론한다.

```bash
git clone <repository-url>
cd new-mouth-of-truth
```

이 문서에서는 현재 작업 디렉토리를 `<repo-root>` 라고 부른다.

## 프로젝트 구조

이 저장소의 핵심 경로는 아래 둘이다.

- Unity 앱:
  - `<repo-root>/unity-app`
- Python 엔진:
  - `<repo-root>/python-engine`

중요:

- Unity로 직접 열어야 하는 경로는 저장소 루트가 아니라 `unity-app/` 이다.
- 저장소 루트를 Unity 프로젝트로 열지 않는다.

## Python 환경 만들기

### 1. conda 환경 생성

아래 명령으로 프로젝트 전용 conda 환경을 만든다.

```bash
conda env create -f python-engine/environment.yml
```

기본 환경 이름은 아래와 같다.

- `mouth-of-truth`

### 2. conda 환경 활성화

```bash
conda activate mouth-of-truth
```

### 3. Python 의존성 기준 파일 확인

현재 Python 의존성 기준 파일은 아래다.

- `python-engine/environment.yml`
- `python-engine/requirements.txt`

`environment.yml` 은 환경 생성용 파일이다.
`requirements.txt` 는 핵심 런타임 패키지 버전 기준이다.

## Unity 프로젝트 열기

### 1. Unity Hub에서 프로젝트 열기

아래 경로를 Unity Hub에서 연다.

- `<repo-root>/unity-app`

### 2. Unity 버전 확인

프로젝트는 아래 버전을 기준으로 맞춰져 있다.

- `6000.4.1f1`

### 3. 메인 씬 열기

Unity에서 아래 씬을 연다.

- `Assets/Scenes/Main.unity`

## 로컬 분석 모델

### 개요

프로젝트는 얼굴 분석, 음성 감정 분석, Whisper 전사를 위한 로컬 모델 자산을 사용한다.

이 모델 바이너리는 저장소에 포함되지 않는다.
따라서 전체 분석 경로를 사용하려면 로컬 모델 디렉토리를 별도로 채워야 한다.

### 필수 경로

아래 경로가 채워져 있으면 전체 분석 경로를 사용할 수 있다.

- 얼굴 모델:
  - `python-engine/models/face/yolo26x_rafdb_best.pt`
- 음성 감정 모델:
  - `python-engine/models/voice/best_wav2vec2_iemocap/`
- Whisper 캐시:
  - `python-engine/models/whisper/models--openai--whisper-tiny/`

### 중요

- 런타임 코드는 더 이상 옛 프로젝트 경로를 fallback 하지 않는다.
- 현재 프로젝트는 `python-engine/models/` 아래만 사용한다.

## 실행 모드

프로젝트는 두 가지 실행 모드를 지원한다.

### 1. deterministic 모드

이 모드는 로컬 분석 모델 없이도 기본 게임 흐름을 검증할 수 있는 모드다.

이 모드를 쓰면 아래를 확인할 수 있다.

- 시작 화면
- 카드 선택
- 질문 공개
- TTS
- 손 입력 흐름
- 답변 수집 흐름
- 결과 화면

이 모드는 분석 결과를 결정론적 규칙으로 생성한다.

### 2. Python 분석 모드

이 모드는 실제 마이크 입력, 얼굴 프레임 캡처, Whisper 전사, 얼굴/음성 분석까지 포함한다.

이 모드를 쓰려면 아래가 모두 필요하다.

- Python 환경 생성 완료
- 로컬 모델 자산 준비 완료
- 마이크 사용 가능
- 웹캠 사용 가능

## deterministic 모드로 실행하기

### 1. Unity를 환경 변수와 함께 실행

아래처럼 `MOUTH_OF_TRUTH_ANALYSIS_MODE=deterministic` 를 설정하고 Unity를 실행한다.

```bash
cd <repo-root>
MOUTH_OF_TRUTH_ANALYSIS_MODE=deterministic open -a "Unity" unity-app
```

이미 Unity Hub에서 프로젝트를 열었다면, Play 전에 같은 환경 변수로 Unity를 다시 시작하는 편이 가장 안전하다.

### 2. 메인 씬에서 Play

아래 흐름이 동작하면 기본 실행은 성공이다.

- 시작 화면 표시
- 카드 3장 등장
- 카드 hover / dwell 선택
- 질문 공개
- 질문 TTS
- 손 삽입 흐름
- 결과 화면

## Python 분석 모드로 실행하기

### 1. 로컬 모델 자산 배치

아래 경로를 모두 채운다.

- `python-engine/models/face/yolo26x_rafdb_best.pt`
- `python-engine/models/voice/best_wav2vec2_iemocap/`
- `python-engine/models/whisper/models--openai--whisper-tiny/`

### 2. Python 소스 컴파일 확인

프로젝트 루트에서 아래 명령을 실행한다.

```bash
conda activate mouth-of-truth
cd <repo-root>
python -m compileall python-engine/src
```

### 3. Unity에서 그대로 Play

추가 환경 변수를 주지 않으면 프로젝트는 Python 분석 경로를 우선 사용한다.

분석이 실패하면 런타임은 deterministic 모드로 fallback 한다.

## 확인 방법

### deterministic 모드 확인

아래 항목이 모두 맞으면 deterministic 기준 기본 실행은 성공이다.

- Unity 프로젝트가 `unity-app/` 에서 열린다.
- `Assets/Scenes/Main.unity` 가 열린다.
- Unity 콘솔에 치명적 컴파일 오류가 없다.
- 카드 선택이 동작한다.
- 질문 TTS가 동작한다.
- 결과 화면이 표시된다.

### Python 분석 모드 확인

아래 항목이 모두 맞으면 Python 분석 경로까지 실행 가능한 상태다.

- `python -m compileall python-engine/src` 가 통과한다.
- 얼굴 모델 경로가 존재한다.
- 음성 모델 경로가 존재한다.
- Whisper 캐시 경로가 존재한다.
- 분석 후 결과 파일이 생성된다.

## 문제 해결

### Unity가 빈 기본 씬으로 열린다

원인:

- 저장소 루트를 Unity 프로젝트로 잘못 열었을 가능성이 높다.

조치:

- Unity를 닫는다.
- `<repo-root>/unity-app` 경로만 다시 연다.

### Python이 모델을 찾지 못한다

원인:

- `python-engine/models/` 아래 로컬 모델 자산이 비어 있을 수 있다.

조치:

- 얼굴, 음성, Whisper 캐시 경로를 확인한다.
- 필요하면 `MOUTH_OF_TRUTH_MODELS_ROOT` 를 명시적으로 설정한다.

### Whisper가 다운로드를 시도한다

원인:

- 현재 프로젝트 내부 Whisper 캐시가 비어 있거나 불완전할 수 있다.

조치:

- `python-engine/models/whisper/models--openai--whisper-tiny/` 경로를 채운다.
- 오프라인 검증 시 `TRANSFORMERS_OFFLINE=1` 과 `HF_HUB_OFFLINE=1` 을 함께 사용한다.

### Unity에서 Python 분석이 실행되지 않는다

원인:

- Python 런처가 conda 환경을 찾지 못했을 수 있다.

조치:

- `conda env create -f python-engine/environment.yml` 가 끝났는지 확인한다.
- `conda activate mouth-of-truth` 가 가능한지 확인한다.
- 필요하면 `MOUTH_OF_TRUTH_PYTHON` 환경 변수로 Python 실행 파일을 직접 지정한다.
- conda 환경 이름을 다르게 만들었다면 `MOUTH_OF_TRUTH_CONDA_ENV` 로 환경 이름을 직접 지정한다.

## 관련 문서

- `README.md`
- `python-engine/environment.yml`
- `python-engine/requirements.txt`
- `python-engine/models/README.md`
- `unity-app/Packages/manifest.json`
