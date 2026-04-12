# Mouth of Truth 개발 환경 설정 가이드

## 개요

이 문서는 GitHub에서 `mouth-of-truth` 저장소를 처음 내려받은 개발자가
로컬 환경에서 프로젝트를 실행하는 방법을 설명한다.

이 문서를 완료하면 아래 작업을 수행할 수 있어야 한다.

- 저장소를 클론한다.
- Python 환경을 만든다.
- Unity 프로젝트를 올바른 경로로 연다.
- deterministic 모드로 기본 게임 흐름을 실행한다.
- 전체 분석 모드에 필요한 로컬 모델 자산을 준비한다.
- Python 브리지 검증을 실행한다.

## 사전 조건

아래 도구를 먼저 설치한다.

- Git
- Unity Hub
- Unity Editor `6000.4.1f1`
- Miniforge 또는 Mambaforge

현재 프로젝트는 macOS를 기준으로 가장 먼저 검증되었다.

## 저장소 받기

원하는 작업 디렉토리에서 아래 명령을 실행한다.

```bash
git clone https://github.com/hgu-hexagon/mouth-of-truth.git
cd mouth-of-truth
```

이 문서에서는 현재 작업 디렉토리를 `<repo-root>` 라고 부른다.

## 프로젝트 구조

이 저장소에서 중요하게 보는 경로는 아래 둘이다.

- Unity 앱:
  - `<repo-root>/unity-app`
- Python 엔진:
  - `<repo-root>/python-engine`

중요:

- Unity는 저장소 루트가 아니라 `unity-app/` 경로에서만 연다.
- 저장소 루트를 Unity 프로젝트로 열지 않는다.

## Python 환경 만들기

### 1. conda 환경 생성

저장소 루트에서 아래 명령을 실행한다.

```bash
conda env create -f python-engine/environment.yml
```

기본 환경 이름은 `mouth-of-truth` 이다.

### 2. conda 환경 활성화

```bash
conda activate mouth-of-truth
```

### 3. Python 환경 확인

```bash
python --version
python -m compileall python-engine/src
```

위 명령이 통과하면 Python 소스는 기본적으로 읽을 수 있는 상태다.

## Unity 프로젝트 열기

### 1. Unity Hub에서 프로젝트 열기

아래 경로를 Unity Hub에서 연다.

- `<repo-root>/unity-app`

### 2. Unity 버전 확인

프로젝트 기준 버전은 아래다.

- `6000.4.1f1`

### 3. 메인 씬 열기

Unity에서 아래 씬을 연다.

- `Assets/Scenes/Main.unity`

## 기본 실행

### deterministic 모드로 실행

deterministic 모드는 로컬 분석 모델 없이도 게임 흐름을 검증할 수 있는 모드다.

이 모드를 쓰면 아래를 확인할 수 있다.

- 시작 화면
- 카드 3장 생성
- hover + dwell 카드 선택
- 질문 공개
- macOS TTS 질문 낭독
- 답변 수집 상태 전환
- 결과 화면 표시

Unity를 deterministic 모드로 열려면 저장소 루트에서 아래 명령을 실행한다.

```bash
MOUTH_OF_TRUTH_ANALYSIS_MODE=deterministic open -a "Unity" unity-app
```

Unity가 이미 열려 있으면 닫은 뒤 다시 실행하는 편이 안전하다.

## 전체 분석 모드 준비

전체 분석 모드는 아래 기능을 함께 사용한다.

- 마이크 입력 수집
- 얼굴 프레임 캡처
- Whisper 전사
- 얼굴 감정 분석
- 음성 감정 분석

이 경로를 쓰려면 로컬 모델 자산이 필요하다.

### 로컬 모델 자산 경로

아래 경로를 준비한다.

- 얼굴 모델:
  - `python-engine/models/face/yolo26x_rafdb_best.pt`
- 음성 감정 모델:
  - `python-engine/models/voice/best_wav2vec2_iemocap/`
- Whisper 캐시 루트:
  - `python-engine/models/whisper/`

Whisper는 인터넷이 가능한 환경이라면 첫 실행 시 `whisper-tiny` 모델을
자동으로 내려받을 수 있다.

얼굴 모델과 음성 감정 모델은 저장소에 포함되지 않는다.
이 두 자산은 프로젝트 관리자에게 별도 전달받아야 한다.

## Python 브리지 검증

저장소 루트에서 아래 명령을 실행한다.

```bash
conda activate mouth-of-truth
python-engine/scripts/validate_bridge_runtime.sh
```

이 검증은 아래를 확인한다.

- 브리지 요청 JSON 생성
- Python 분석 러너 실행
- Whisper 전사
- 결과 JSON 생성

검증이 성공하면 `Bridge runtime validation succeeded.` 메시지가 출력된다.

## 실행 확인

아래 항목이 모두 맞으면 기본 실행은 성공이다.

- Unity가 `unity-app/` 경로에서 열린다.
- `Assets/Scenes/Main.unity` 가 열린다.
- Unity 콘솔에 치명적 컴파일 오류가 없다.
- 카드 선택이 동작한다.
- 질문 TTS가 재생된다.
- 결과 화면이 표시된다.

아래 항목이 추가로 맞으면 전체 분석 경로도 실행 가능한 상태다.

- `python -m compileall python-engine/src` 가 통과한다.
- `validate_bridge_runtime.sh` 가 통과한다.
- 얼굴 모델 파일이 존재한다.
- 음성 모델 디렉토리가 존재한다.
- Whisper 캐시 루트 디렉토리가 존재한다.

## 자주 발생하는 실수

### Unity를 저장소 루트에서 열었다

원인:

- `unity-app/` 대신 저장소 루트를 Unity 프로젝트로 열었다.

조치:

- 잘못 열린 프로젝트를 닫는다.
- Unity Hub에서 `<repo-root>/unity-app` 경로를 다시 연다.

### 분석은 안 되지만 게임은 실행된다

원인:

- deterministic 모드로 실행 중이거나 로컬 모델 자산이 없다.

조치:

- 전체 분석이 필요하면 얼굴/음성 모델 자산을 준비한다.
- deterministic 모드가 아니라 일반 모드로 Unity를 실행한다.

### Whisper 전사가 시작되지 않는다

원인:

- `python-engine/models/whisper/` 경로가 없거나 네트워크가 차단되어 있다.

조치:

- `python-engine/models/whisper/` 디렉토리가 있는지 확인한다.
- 네트워크가 가능하면 브리지 검증을 다시 실행한다.

## 관련 문서

- `docs/build-and-distribution-guide-ko.md`
- `python-engine/environment.yml`
- `python-engine/requirements.txt`
- `python-engine/models/README.md`
