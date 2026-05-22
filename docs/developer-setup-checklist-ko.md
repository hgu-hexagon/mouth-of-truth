# Mouth of Truth 개발 환경 설정 가이드

## 1. 목적

이 문서는 GitHub에서 `mouth-of-truth` 저장소를 처음 내려받은 개발자가
프로젝트를 실행하고 기본 검증을 마치는 방법을 설명한다.

이 문서를 완료하면 아래 작업을 할 수 있어야 한다.

- 저장소를 클론한다.
- Python 개발 환경을 만든다.
- Unity 프로젝트를 올바른 경로에서 연다.
- 로컬 모델 자산을 배치한다.
- 소프트웨어 흐름 검증과 제품 준비 검증을 실행한다.
- macOS 배포 빌드를 실행한다.
- 실제 실행용 런처와 발표/캡처 테스트용 런처를 구분한다.

## 2. 사전 설치 항목

아래 도구를 설치한다.

- Git
- Unity Hub
- Unity Editor `6000.4.1f1`
- Miniforge 또는 Mambaforge
- `conda-pack`

Windows 배포 빌드까지 수행하려면 Windows 환경에 아래 항목도 설치한다.

- `Windows Build Support (IL2CPP)`
- Visual Studio 2026 또는 Unity `6000.4.1f1`과 호환되는 Visual Studio
- Visual Studio Installer의 `C++를 사용한 데스크톱 개발` 워크로드
- `MSVC x64/x86 build tools`
- `Windows 11 SDK`

Leap Motion으로 실제 시연을 검증하려면 아래도 필요하다.

- Ultraleap tracking runtime
- Leap Motion 또는 Ultraleap 호환 손 추적 장치

## 3. 저장소 클론

원하는 작업 디렉토리에서 아래 명령을 실행한다.

```bash
git clone https://github.com/hgu-hexagon/mouth-of-truth.git
cd mouth-of-truth
```

이 문서에서는 현재 디렉토리를 `<repo-root>` 라고 부른다.

## 4. 프로젝트 구조

중요한 루트는 아래 두 곳이다.

- Unity 프로젝트:
  - `<repo-root>/unity-app`
- Python 엔진:
  - `<repo-root>/python-engine`

Unity Hub에서는 반드시 `<repo-root>/unity-app`을 연다. 저장소 루트를 Unity
프로젝트로 열지 않는다.

## 5. Python 환경 만들기

저장소 루트에서 아래 명령을 실행한다.

```bash
conda env create -f python-engine/environment.yml
conda activate mouth-of-truth
python --version
python -m compileall python-engine/src
```

`compileall`이 통과하면 Python 소스가 현재 환경에서 읽히고 컴파일되는 상태다.

## 6. 로컬 모델 자산 준비

전체 분석 경로는 아래 로컬 모델 자산을 사용한다. 모델 파일은 Git에 커밋하지
않으므로 프로젝트 관리자에게 별도로 전달받아 배치한다.

- 얼굴 표정 모델:
  - `python-engine/models/face/yolo26x_rafdb_best.pt`
- 음성 감정 모델:
  - `python-engine/models/voice/best_wav2vec2_iemocap/`
- Whisper 캐시:
  - `python-engine/models/whisper/`

기본 제품 경로는 결과 속도를 위해 빠른 얼굴/음성 증거 계산을 먼저 사용한다.
Whisper 전사와 무거운 wav2vec2 추론은 선택 검증 경로로 남아 있다.

## 7. Unity 프로젝트 열기

1. Unity Hub를 연다.
2. `<repo-root>/unity-app` 경로를 프로젝트로 추가한다.
3. Unity Editor `6000.4.1f1`로 연다.
4. `Assets/Scenes/Main.unity`가 열리는지 확인한다.

## 8. 실행 모드

분석 클라이언트 선택 규칙은 아래와 같다.

1. `MOUTH_OF_TRUTH_ANALYSIS_MODE=python` 이면 Python bridge 모드 강제
2. `MOUTH_OF_TRUTH_ANALYSIS_MODE=deterministic` 이면 deterministic 모드 강제
3. 별도 설정이 없으면 Python bridge 런처와 모듈 루트가 있을 때 Python bridge 사용
4. Python bridge를 사용할 수 없으면 deterministic fallback 사용

Python bridge 기본 경로는 얼굴 증거와 음성 증거가 모두 있을 때만 `TRUE` 또는
`FALSE`를 반환한다. 둘 중 하나라도 판정 가능한 증거가 없으면 `UNCERTAIN`을
반환한다.

## 9. Unity 검증

Unity Editor 메뉴에서 아래 항목을 실행한다.

- `Mouth Of Truth > Validate Software Flow`
- `Mouth Of Truth > Validate Product Readiness`

명령줄에서 실행하려면 Unity 경로를 환경에 맞게 바꿔 아래처럼 실행한다.

```bash
"/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode \
  -nographics \
  -projectPath unity-app \
  -quit \
  -executeMethod MouthOfTruth.Editor.ValidateSoftwareFlowEditor.Run \
  -logFile unity-app/Logs/validate-software-flow.log
```

```bash
"/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode \
  -nographics \
  -projectPath unity-app \
  -quit \
  -executeMethod MouthOfTruth.Editor.ValidateProductReadinessEditor.Run \
  -logFile unity-app/Logs/validate-product-readiness.log
```

## 10. 개발 중 기본 확인 흐름

Editor Play Mode 또는 배포 실행 파일에서 아래 흐름을 확인한다.

1. 시작 화면에서 `START GAME` 선택
2. 첫 실행 튜토리얼 확인
3. 성전 접근 연출 확인
4. 카드 3장 표시 확인
5. 카드 hover/dwell 선택 확인
6. 카드 뒤집힘과 사전 녹음된 질문 음성 확인
7. 카드가 진실의 입으로 흡수되며 카메라 줌이 동시에 진행되는지 확인
8. 안내 음성 중 포인터가 하단 중앙에 고정되고, 실제 삽입 움직임 때 안내 바가 사라지는지 확인
9. 손이 아래에서 진실의 입 쪽으로 올라오는 삽입 연출 확인
10. 답변 수집 레이저가 고정 밑변으로 y축 스윕되는지 확인
11. 판정 줌/오라/흔들림 연출 확인
12. `TRUE`, `FALSE`, `UNCERTAIN` 결과 확인
13. `TRY AGAIN`과 왼쪽 위 종료 버튼 확인

## 11. macOS 배포 빌드

배포 전 Python 런타임을 만든다.

```bash
conda activate mouth-of-truth
python-engine/scripts/package_python_runtime.sh
```

그 다음 저장소 루트에서 아래 명령을 실행한다.

```bash
UNITY_EDITOR_PATH="/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity" \
./tools/build-macos-release.sh
```

성공하면 아래 파일이 생성된다.

- `dist/macos/MouthOfTruth/Run Mouth of Truth.command`
- `dist/macos/MouthOfTruth/Run Mouth of Truth Presentation Test.command`
- `dist/macos/MouthOfTruth-macos.zip`

일반 시연과 사용자 실행에는 `Run Mouth of Truth.command`를 사용한다.
자동 화면 캡처와 발표 흐름 점검에는 `Run Mouth of Truth Presentation Test.command`를
사용한다.

로컬 macOS 빌드 후 저장소 루트에서 `./tools/run-presentation-capture.sh`를 실행하면
동일한 캡처 흐름이 `presentation-captures/latest/`에 생성된다.

## 12. 자주 발생하는 실수

### Unity를 저장소 루트에서 열었다

`unity-app/`이 아니라 저장소 루트를 Unity 프로젝트로 열면 빈 프로젝트처럼 보일
수 있다. Unity를 닫고 `<repo-root>/unity-app`을 다시 연다.

### Unity Hub에서 `6000.4.1f1`이 보이지 않는다

Unity Download Archive에서 `6000.4.1f1`을 직접 설치한다.

### Leap Motion 포인터가 움직이지 않는다

아래를 확인한다.

- Ultraleap tracking runtime이 설치되어 있다.
- runtime이 실행 중이다.
- Control Panel에서 장치와 hand tracking이 확인된다.

### 분석 결과가 계속 `UNCERTAIN`이다

현재 제품 정책은 얼굴과 음성 증거가 모두 있어야 `TRUE` 또는 `FALSE`를 낸다.
카메라 권한, 마이크 권한, 얼굴 위치, 음성 입력을 확인한다.

## 13. 관련 문서

- [build-and-distribution-guide-ko.md](build-and-distribution-guide-ko.md)
- [developer-setup-checklist-en.md](developer-setup-checklist-en.md)
- [build-and-distribution-guide-en.md](build-and-distribution-guide-en.md)
- [python-engine/models/README.md](../python-engine/models/README.md)
