# Mouth of Truth 빌드 및 배포 가이드

## 1. 목적

이 문서는 Mouth of Truth를 macOS와 Windows에서 실행 가능한 배포본으로
만드는 방법을 설명한다.

개발자는 이 문서를 따라 아래 결과물을 만들 수 있어야 한다.

- macOS 실제 실행용 배포 폴더와 ZIP
- macOS 발표/캡처 테스트용 실행 스크립트
- Windows 실제 실행용 배포 폴더
- Windows 발표/캡처 테스트용 실행 스크립트
- 사용자에게 어떤 파일을 전달하고 실행하라고 안내해야 하는지에 대한 기준

## 2. 배포 구조

배포본은 Unity 실행 파일만으로 구성되지 않는다. 제품 실행에는 아래 항목이
같이 필요하다.

- Unity Player
- `python-engine/`
- 패키징된 Python 런타임
- `bridge/`
- `StreamingAssets`에 포함된 아트, 오디오, 질문 데이터, 튜토리얼 데이터

최종 사용자는 Unity Editor나 Python 개발 환경을 설치하지 않아도 된다.
단, Leap Motion을 사용하는 시연 환경에서는 Ultraleap tracking runtime이
별도로 설치되어 실행 중이어야 한다.

## 3. 빌드 호스트 전제

### macOS 빌드

아래 항목을 설치한다.

- macOS
- Unity Hub
- Unity Editor `6000.4.1f1`
- Miniforge 또는 Mambaforge
- `conda-pack`

### Windows 빌드

아래 항목을 설치한다.

- Windows
- Unity Hub
- Unity Editor `6000.4.1f1`
- `Windows Build Support (IL2CPP)`
- Visual Studio 2026 또는 Unity `6000.4.1f1`과 호환되는 Visual Studio
- Visual Studio Installer의 `C++를 사용한 데스크톱 개발` 워크로드
- `MSVC x64/x86 build tools`
- `Windows 11 SDK`
- Miniforge 또는 Mambaforge
- `conda-pack`

Unity Hub에 `6000.4.1f1`이 보이지 않으면 Unity Download Archive에서 해당
버전을 직접 설치한다. Windows 배포본은 Windows Unity Editor에서 만드는
것이 가장 안전하다.

## 4. 배포 전 검증

저장소 루트에서 아래 항목을 확인한다.

- Unity 프로젝트는 `unity-app/` 경로에서 열린다.
- 메인 씬은 `unity-app/Assets/Scenes/Main.unity` 이다.
- `python -m compileall python-engine/src` 가 통과한다.
- Unity 메뉴의 `Mouth Of Truth > Validate Software Flow` 가 통과한다.
- Unity 메뉴의 `Mouth Of Truth > Validate Product Readiness` 가 통과한다.
- `unity-app/Assets/StreamingAssets/audio/questions/` 안에 `Q0001.wav`부터 현재 질문 ID에 맞는 음성 파일이 있다.
- `unity-app/Assets/StreamingAssets/art/` 안에 제품용 PNG/JPEG 자산이 있다.

`Validate Product Readiness`와 릴리즈 빌드는 `Assets/Scenes/Main.unity`와 생성 배경 이미지를 다시 저장할 수 있다.
배포 직전에는 이 변경이 현재 릴리즈 씬 산출물인지 확인한 뒤 함께 커밋한다.

## 5. Python 런타임 패키징

### macOS

저장소 루트에서 아래 명령을 실행한다.

```bash
conda env create -f python-engine/environment.yml
conda activate mouth-of-truth
python-engine/scripts/package_python_runtime.sh
```

이미 환경을 만든 상태라면 `conda env create`는 다시 실행하지 않아도 된다.
다른 환경 이름을 쓰는 경우에는 아래처럼 지정한다.

```bash
MOUTH_OF_TRUTH_CONDA_ENV=<conda-env-name> \
python-engine/scripts/package_python_runtime.sh
```

완료 후 저장소 루트에 `python-runtime/` 디렉토리가 있어야 한다.

### Windows

PowerShell에서 저장소 루트로 이동한 뒤 아래 명령을 실행한다.

```powershell
conda env create -f python-engine/environment.yml
conda activate mouth-of-truth
.\python-engine\scripts\package_python_runtime.ps1
```

이미 환경을 만든 상태라면 `conda env create`는 다시 실행하지 않아도 된다.
다른 환경 이름을 쓰는 경우에는 아래처럼 지정한다.

```powershell
$env:MOUTH_OF_TRUTH_CONDA_ENV = "<conda-env-name>"
.\python-engine\scripts\package_python_runtime.ps1
```

완료 후 저장소 루트에 `python-runtime-windows/` 디렉토리가 있어야 한다.

## 6. macOS 릴리즈 빌드

저장소 루트에서 아래 명령을 실행한다.

```bash
UNITY_EDITOR_PATH="/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity" \
./tools/build-macos-release.sh
```

Unity 설치 경로가 다르면 `UNITY_EDITOR_PATH` 값을 해당 환경에 맞게 바꾼다.

성공하면 아래 결과가 생성된다.

- `dist/macos/MouthOfTruth/MouthOfTruth.app`
- `dist/macos/MouthOfTruth/Run Mouth of Truth.command`
- `dist/macos/MouthOfTruth/Run Mouth of Truth Presentation Test.command`
- `dist/macos/MouthOfTruth/python-engine/`
- `dist/macos/MouthOfTruth/python-runtime/`
- `dist/macos/MouthOfTruth/bridge/`
- `dist/macos/MouthOfTruth-macos.zip`

`MouthOfTruth-macos.zip`은 macOS 사용자에게 전달하기 위한 기본 압축본이다.

## 7. Windows 릴리즈 빌드

Windows PowerShell에서 저장소 루트로 이동한 뒤 아래 명령을 실행한다.

```powershell
.\tools\build-windows-release.ps1
```

Unity 설치 경로가 기본값과 다르면 직접 넘긴다.

```powershell
.\tools\build-windows-release.ps1 -UnityEditorPath "C:\Path\To\Unity.exe"
```

성공하면 아래 결과가 생성된다.

- `dist/windows/MouthOfTruth/MouthOfTruth.exe`
- `dist/windows/MouthOfTruth/Run Mouth of Truth.bat`
- `dist/windows/MouthOfTruth/Run Mouth of Truth Presentation Test.bat`
- `dist/windows/MouthOfTruth/MouthOfTruth_Data/`
- `dist/windows/MouthOfTruth/python-engine/`
- `dist/windows/MouthOfTruth/python-runtime/`
- `dist/windows/MouthOfTruth/bridge/`

Windows 사용자에게는 `dist/windows/MouthOfTruth/` 폴더 전체를 압축해 전달한다.

## 8. 사용자 실행 안내

### macOS 사용자

전달 파일:

- `MouthOfTruth-macos.zip`

실행 방법:

1. ZIP 파일을 압축 해제한다.
2. `MouthOfTruth/Run Mouth of Truth.command`를 실행한다.
3. macOS가 실행을 차단하면 Finder에서 파일을 우클릭한 뒤 **열기**를 선택한다.
4. 카메라와 마이크 권한 요청이 뜨면 허용한다.

### Windows 사용자

전달 파일:

- Windows에서 만든 `MouthOfTruth/` 폴더 ZIP

실행 방법:

1. ZIP 파일을 압축 해제한다.
2. `MouthOfTruth\Run Mouth of Truth.bat`을 실행한다.
3. 카메라와 마이크 권한 요청이 뜨면 허용한다.

### Leap Motion 사용 시 공통 안내

Leap Motion으로 시연하려면 사용자 PC에 Ultraleap tracking runtime이 설치되어
있어야 한다. 장치가 연결되어 있고 runtime이 손 추적 데이터를 제공해야
제품 안의 Leap 포인터가 동작한다.

## 9. 발표/캡처 테스트 실행

macOS 배포 폴더에는 아래 테스트 실행 파일이 포함된다.

- `Run Mouth of Truth Presentation Test.command`

이 파일은 실제 제품과 같은 화면/사운드 흐름을 자동으로 진행하면서
배포 폴더의 `PresentationTestCaptures/`에 캡처용 결과를 남긴다. 실제 음성 답변은
요구하지 않으며, `TRUE`, `FALSE`, `UNCERTAIN` 결과 화면을 검증하는 용도로
사용한다.

현재 캡처 결과는 아래 순서로 생성된다.

- `01_start.png`
- `02_first_run_tutorial.png`
- `03_temple_approach.png`
- `04_cards.png`
- `05_card_focus.png`
- `06_card_question.png`
- `07_card_absorption.png`
- `08_mouth_arrival.png`
- `09_hand_prompt.png`
- `10_hand_insertion.png`
- `11_answering.png`
- `12_analyzing.png`
- `13_result_true.png`
- `14_result_false.png`
- `15_result_uncertain.png`

Windows 배포 폴더에는 아래 테스트 실행 파일이 포함된다.

- `Run Mouth of Truth Presentation Test.bat`

## 10. 배포 전 최종 점검

최종 배포 전에 실제 실행용 런처로 아래를 확인한다.

- 시작 화면이 열린다.
- `START GAME` 이 동작한다.
- 첫 실행 튜토리얼이 표시된다.
- 성전 접근 연출이 진행된다.
- 카드 3장이 표시된다.
- 카드 hover/dwell 선택이 된다.
- 카드가 뒤집히고 질문 음성이 재생된다.
- 카드가 진실의 입으로 흡수되며 카메라 줌이 동시에 진행된다.
- 안내 음성 중 포인터가 하단 중앙에 고정되고, 손 움직임 감지 시 안내 바가 사라진다.
- 손이 아래에서 진실의 입 쪽으로 올라간 뒤 답변 수집 화면으로 넘어간다.
- 답변 수집 중 고정 밑변 y축 스윕 레이저 연출이 보인다.
- 판정 중 줌/오라/흔들림 연출이 보인다.
- 결과 화면이 표시된다.
- `TRY AGAIN` 이 다시 카드 선택 흐름으로 돌아간다.
- 왼쪽 위 종료 버튼이 동작한다.

## 11. 문제 해결

### 앱은 열리지만 분석이 동작하지 않는다

배포 폴더 안에 아래 항목이 모두 있는지 확인한다.

- `python-engine/`
- `python-runtime/` 또는 Windows 배포본의 `python-runtime/`
- `bridge/`

### Leap Motion 포인터가 움직이지 않는다

아래를 확인한다.

- Ultraleap tracking runtime이 설치되어 있다.
- runtime이 실행 중이다.
- Leap Motion 장치가 운영체제와 Ultraleap Control Panel에서 인식된다.
- 장치 위에서 손 추적이 실제로 잡힌다.

### Windows 빌드가 실패한다

아래를 확인한다.

- Unity에 `Windows Build Support (IL2CPP)`가 설치되어 있다.
- Visual Studio Installer에서 `C++를 사용한 데스크톱 개발` 워크로드가 설치되어 있다.
- `MSVC x64/x86 build tools`와 `Windows 11 SDK`가 설치되어 있다.
