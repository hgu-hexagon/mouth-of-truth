# Mouth of Truth 개발 환경 설정 가이드

## 개요

이 문서는 GitHub에서 `mouth-of-truth` 저장소를 처음 내려받은 개발자가
현재 프로젝트를 실행하고 기본 검증까지 마치는 방법을 설명한다.

이 문서를 완료하면 아래 작업을 수행할 수 있어야 한다.

- 저장소를 클론한다.
- Python 환경을 만든다.
- Unity 프로젝트를 올바른 경로에서 연다.
- deterministic 모드와 Python bridge 모드를 각각 실행한다.
- 로컬 모델 자산을 준비한다.
- 소프트웨어 흐름 검증과 브리지 검증을 실행한다.

## 사전 조건

아래 도구를 먼저 설치한다.

- Git
- Unity Hub
- Unity Editor `6000.4.1f1`
- Miniforge 또는 Mambaforge

Windows에서 Unity 빌드까지 하려면 아래도 추가로 필요하다.

- `Windows Build Support (IL2CPP)`
- Visual Studio 2026
- Visual Studio Installer의 `C++를 사용한 데스크톱 개발` 워크로드
- `MSVC x64/x86 build tools`
- `Windows 11 SDK`

중요:

- Unity Hub 설치 목록에 `6000.4.1f1`이 바로 보이지 않으면 Unity Download Archive에서 해당 버전을 직접 설치한다.
- 현재 프로젝트 기준 버전은 `6000.4.1f1`이다.

## 저장소 받기

원하는 작업 디렉토리에서 아래 명령을 실행한다.

```bash
git clone https://github.com/hgu-hexagon/mouth-of-truth.git
cd mouth-of-truth
```

이 문서에서는 현재 작업 디렉토리를 `<repo-root>` 라고 부른다.

## 프로젝트 구조

중요하게 보는 경로는 아래 둘이다.

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

## 로컬 모델 자산 준비

전체 분석 모드는 아래 로컬 모델 자산을 사용한다.

- 얼굴 모델:
  - `python-engine/models/face/yolo26x_rafdb_best.pt`
- 음성 감정 모델:
  - `python-engine/models/voice/best_wav2vec2_iemocap/`
- Whisper 캐시 루트:
  - `python-engine/models/whisper/`

중요:

- 얼굴 모델과 음성 모델은 저장소에 포함되지 않는다.
- 이 두 자산은 프로젝트 관리자에게 별도 전달받아야 한다.
- Whisper는 인터넷이 가능한 환경이라면 첫 실행 시 `whisper-tiny`를 자동으로 내려받을 수 있다.

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

## 실행 모드

현재 분석 클라이언트 선택 규칙은 아래와 같다.

1. `MOUTH_OF_TRUTH_ANALYSIS_MODE=python` 이면 Python bridge 강제
2. `MOUTH_OF_TRUTH_ANALYSIS_MODE=deterministic` 이면 deterministic 강제
3. 위 설정이 없으면:
   - Python bridge 런처와 Python 모듈 루트가 존재하면 Python bridge 사용
   - 그렇지 않으면 deterministic 사용

## deterministic 모드 실행

deterministic 모드는 로컬 분석 모델 없이도 게임 흐름을 검증하는 모드다.

이 모드에서는 아래를 확인할 수 있다.

- 시작 화면
- 카드 3장 생성
- hover + dwell 카드 선택
- 질문 공개
- 질문 TTS 낭독
- 입 진입 / 답변 / 일시정지 / 재개
- 결과 화면 표시

macOS에서는 저장소 루트에서 아래처럼 Unity를 연다.

```bash
MOUTH_OF_TRUTH_ANALYSIS_MODE=deterministic open -a "Unity" unity-app
```

Windows에서는 PowerShell에서 아래처럼 Unity를 연다.

```powershell
$env:MOUTH_OF_TRUTH_ANALYSIS_MODE = "deterministic"
$unityEditorPath = "<path-to-unity-editor>"
Start-Process $unityEditorPath -ArgumentList "-projectPath `"<repo-root>\unity-app`""
```

## Python bridge 모드 실행

Python bridge 모드는 Whisper 전사와 얼굴/음성 분석 경로를 사용한다.

저장소 루트에서 Python 환경을 활성화한 뒤 Unity를 일반 실행하면 된다.

macOS 예시:

```bash
conda activate mouth-of-truth
open -a "Unity" unity-app
```

Windows 예시:

```powershell
conda activate mouth-of-truth
$unityEditorPath = "<path-to-unity-editor>"
Start-Process $unityEditorPath -ArgumentList "-projectPath `"<repo-root>\unity-app`""
```

## Python 브리지 검증

### macOS

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
- voice-only verdict fallback 생성

### Windows

Windows에서는 macOS `say` 기반 검증 스크립트를 그대로 쓰지 않는다.
대신 아래 항목을 먼저 확인한다.

- `python -m compileall python-engine/src`
- Unity `Mouth Of Truth > Validate Software Flow`
- Unity `Mouth Of Truth > Validate Product Readiness`

## 기본 동작 확인

아래 항목이 모두 맞으면 기본 실행은 성공이다.

- Unity가 `unity-app/` 경로에서 열린다.
- `Assets/Scenes/Main.unity` 가 열린다.
- Unity Console에 치명적 컴파일 오류가 없다.
- 시작 화면에서 `START GAME`과 `EXIT GAME`이 보인다.
- 카드 선택이 동작한다.
- 질문 TTS가 재생된다.
- 입 진입 후 답변 단계로 넘어간다.
- 결과 화면이 표시된다.

아래 항목이 추가로 맞으면 전체 분석 경로도 실행 가능한 상태다.

- `python -m compileall python-engine/src` 가 통과한다.
- 얼굴 모델 파일이 존재한다.
- 음성 모델 디렉토리가 존재한다.
- Whisper 캐시 루트 디렉토리가 존재한다.
- macOS에서 `validate_bridge_runtime.sh` 가 통과한다.

## 자주 발생하는 실수

### Unity를 저장소 루트에서 열었다

원인:

- `unity-app/` 대신 저장소 루트를 Unity 프로젝트로 열었다.

조치:

- 잘못 열린 프로젝트를 닫는다.
- Unity Hub에서 `<repo-root>/unity-app` 경로를 다시 연다.

### Unity Hub에서 `6000.4.1f1`이 안 보인다

원인:

- Hub 설치 목록에는 최신 패치만 먼저 보일 수 있다.

조치:

- Unity Download Archive에서 `6000.4.1f1` 페이지를 열어 직접 설치한다.

### 게임은 실행되지만 분석은 deterministic 으로만 돈다

원인:

- Python bridge 런처 또는 Python 모듈 루트가 보이지 않거나,
  분석 모드가 `deterministic` 으로 강제되어 있다.

조치:

- `MOUTH_OF_TRUTH_ANALYSIS_MODE` 값을 확인한다.
- `python-engine/scripts/` 와 `python-engine/src/` 경로가 정상인지 확인한다.

### 마이크가 없어 텍스트 입력 fallback 으로 내려간다

원인:

- 사용 가능한 마이크 장치가 없거나 마이크 시작에 실패했다.

조치:

- 운영 체제 마이크 권한을 확인한다.
- 실제 입력 장치가 있는지 확인한다.
- fallback 자체는 정상 동작이므로 흐름 검증은 계속 진행할 수 있다.

## 관련 문서

- [build-and-distribution-guide-ko.md](build-and-distribution-guide-ko.md)
- [manual-validation-without-leap-ko.md](manual-validation-without-leap-ko.md)
- [session-architecture-ko.md](session-architecture-ko.md)
- [python-engine/models/README.md](../python-engine/models/README.md)
