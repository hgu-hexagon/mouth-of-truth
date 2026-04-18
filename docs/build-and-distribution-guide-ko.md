# Mouth of Truth 빌드 및 배포 가이드

## 개요

이 문서는 Mouth of Truth를 macOS 및 Windows용 제품 형태로 빌드하고 배포하는 방법을 설명한다.

이 문서를 완료하면 아래 작업을 수행할 수 있어야 한다.

- 배포용 Python 런타임을 준비한다.
- macOS 릴리즈 빌드를 실행한다.
- Windows 릴리즈 빌드를 실행한다.
- 사용자에게 전달해야 할 폴더를 판단한다.
- 사용자가 어떤 파일을 실행해야 하는지 안내한다.

## 빌드 호스트 전제

### macOS 빌드

아래 조건이 필요하다.

- macOS
- Unity Editor `6000.4.1f1`
- conda 환경 `mouth-truth` 또는 `mouth-of-truth`
- `conda-pack`

### Windows 빌드

아래 조건이 필요하다.

- Windows
- Unity Editor `6000.4.1f1`
- `Windows Build Support (IL2CPP)`
- Visual Studio 2026
- `C++를 사용한 데스크톱 개발` 워크로드
- `MSVC x64/x86 build tools`
- `Windows 11 SDK`
- conda 환경 `mouth-truth` 또는 `mouth-of-truth`
- `conda-pack`

중요:

- Unity Hub 설치 목록에 `6000.4.1f1`이 바로 보이지 않으면 Unity Download Archive에서 직접 설치한다.
- Windows 배포 폴더는 Windows Unity Editor에서 만드는 것이 가장 안전하다.

## 배포 전 확인

아래 항목을 먼저 확인한다.

- Unity 프로젝트가 `unity-app/` 경로에서 열린다.
- 메인 씬이 `Assets/Scenes/Main.unity` 이다.
- 런타임 아트 자산이 `unity-app/Assets/StreamingAssets/art/` 아래에 반영되어 있다.
- `python -m compileall python-engine/src` 가 통과한다.
- macOS에서는 `python-engine/scripts/validate_bridge_runtime.sh` 가 통과한다.
- Unity 메뉴의 `Mouth Of Truth > Validate Software Flow` 가 통과한다.
- Unity 메뉴의 `Mouth Of Truth > Validate Product Readiness` 가 통과한다.

## 배포용 Python 런타임 만들기

배포본에는 Unity 실행 파일만 넣으면 안 된다.
Unity 빌드와 함께 Python 실행 환경도 묶어야 한다.

### macOS용 Python 런타임

저장소 루트에서 아래 명령을 실행한다.

```bash
conda activate mouth-of-truth
python-engine/scripts/package_python_runtime.sh
```

이 스크립트는 아래 작업을 수행한다.

- `mouth-truth` 또는 `mouth-of-truth` conda 환경을 패키징한다.
- 저장소 루트에 `python-runtime/` 디렉토리를 만든다.
- 배포용 Python 실행 파일과 패키지를 그 안에 풀어 둔다.

다른 환경 이름을 쓰고 있다면 환경 변수를 먼저 설정한다.

```bash
MOUTH_OF_TRUTH_CONDA_ENV=<conda-env-name> \
python-engine/scripts/package_python_runtime.sh
```

### Windows용 Python 런타임

Windows PowerShell에서 저장소 루트로 이동한 뒤 아래 명령을 실행한다.

```powershell
conda activate mouth-of-truth
.\python-engine\scripts\package_python_runtime.ps1
```

이 스크립트는 아래 작업을 수행한다.

- `mouth-truth` 또는 `mouth-of-truth` conda 환경을 패키징한다.
- 저장소 루트에 `python-runtime-windows/` 디렉토리를 만든다.
- 배포용 Windows Python 실행 파일과 패키지를 그 안에 풀어 둔다.

다른 conda 환경 이름을 쓴다면 환경 변수를 먼저 설정한다.

```powershell
$env:MOUTH_OF_TRUTH_CONDA_ENV = "<conda-env-name>"
.\python-engine\scripts\package_python_runtime.ps1
```

## macOS 릴리즈 빌드

### 1. 메인 씬 갱신

Unity 메뉴에서 아래 항목을 실행한다.

- `Mouth Of Truth > Build Main Scene`

### 2. macOS 배포 빌드 실행

Unity 메뉴에서 아래 항목을 실행한다.

- `Mouth Of Truth > Build Mac Release`

또는 저장소 루트에서 아래 스크립트를 실행한다.

```bash
./tools/build-macos-release.sh
```

이 빌드는 아래 결과를 만든다.

- `dist/macos/MouthOfTruth/MouthOfTruth.app`
- `dist/macos/MouthOfTruth/Run Mouth of Truth.command`
- `dist/macos/MouthOfTruth/python-engine/`
- `dist/macos/MouthOfTruth/python-runtime/`
- `dist/macos/MouthOfTruth/bridge/`

## Windows 릴리즈 빌드

Windows 빌드는 Windows Build Support가 설치된 Windows Unity Editor에서 실행한다.

### 1. 메인 씬 갱신

Unity 메뉴에서 아래 항목을 실행한다.

- `Mouth Of Truth > Build Main Scene`

### 2. Windows 배포 빌드 실행

Unity 메뉴에서 아래 항목을 실행한다.

- `Mouth Of Truth > Build Windows Release`

또는 PowerShell에서 아래 스크립트를 실행한다.

```powershell
.\tools\build-windows-release.ps1
```

Unity 설치 경로가 기본값과 다르면 직접 넘긴다.

```powershell
.\tools\build-windows-release.ps1 -UnityEditorPath "C:\Path\To\Unity.exe"
```

이 빌드는 아래 결과를 만든다.

- `dist/windows/MouthOfTruth/MouthOfTruth.exe`
- `dist/windows/MouthOfTruth/Run Mouth of Truth.bat`
- `dist/windows/MouthOfTruth/MouthOfTruth_Data/`
- `dist/windows/MouthOfTruth/python-engine/`
- `dist/windows/MouthOfTruth/python-runtime/`
- `dist/windows/MouthOfTruth/bridge/`

## 빌드 후 확인할 폴더

macOS 배포본은 아래 항목이 모두 존재해야 한다.

- `dist/macos/MouthOfTruth/MouthOfTruth.app`
- `dist/macos/MouthOfTruth/Run Mouth of Truth.command`
- `dist/macos/MouthOfTruth/python-engine/`
- `dist/macos/MouthOfTruth/python-runtime/`
- `dist/macos/MouthOfTruth/bridge/`

Windows 배포본은 아래 항목이 모두 존재해야 한다.

- `dist/windows/MouthOfTruth/MouthOfTruth.exe`
- `dist/windows/MouthOfTruth/Run Mouth of Truth.bat`
- `dist/windows/MouthOfTruth/MouthOfTruth_Data/`
- `dist/windows/MouthOfTruth/python-engine/`
- `dist/windows/MouthOfTruth/python-runtime/`
- `dist/windows/MouthOfTruth/bridge/`

위 항목 중 하나라도 빠지면 배포본으로 사용하지 않는다.

## 사용자에게 전달할 파일

사용자에게는 아래 디렉토리 전체를 전달한다.

- `dist/macos/MouthOfTruth/`
- `dist/windows/MouthOfTruth/`

전달 방식은 아래를 권장한다.

- 각 플랫폼의 `MouthOfTruth/` 디렉토리를 ZIP으로 압축해서 전달한다.

중요:

- macOS에서는 `MouthOfTruth.app` 파일 하나만 따로 전달하지 않는다.
- Windows에서는 `MouthOfTruth.exe` 파일 하나만 따로 전달하지 않는다.
- 항상 `MouthOfTruth/` 전체 디렉토리를 전달한다.

## 사용자가 실행할 파일

사용자는 압축을 해제한 뒤 아래 파일을 실행한다.

- macOS:
  - `Run Mouth of Truth.command`
- Windows:
  - `Run Mouth of Truth.bat`

이 런처는 제품 루트 경로를 잡고 각 플랫폼 실행 파일을 올바른 위치에서 실행한다.

보조적으로 아래 파일을 직접 실행할 수도 있다.

- `MouthOfTruth.app`
- `MouthOfTruth.exe`

다만 운영 기준으로는 플랫폼별 런처 실행을 기본 안내로 사용한다.

## 첫 실행 시 사용자에게 안내할 사항

사용자가 처음 실행할 때 아래 권한 요청이 뜰 수 있다.

- 카메라 권한
- 마이크 권한

이 권한은 분석 기능에 필요하므로 허용해야 한다.

macOS에서는 아래도 같이 안내한다.

- 실행이 차단되면 Finder에서 파일을 우클릭한 뒤 **열기**를 선택한다.

## 배포 전 최종 점검

최종 배포 전에 아래 흐름을 실제로 확인한다.

- 시작 화면이 열린다.
- `START GAME` 이 동작한다.
- `EXIT GAME` 이 동작한다.
- 카드 선택이 된다.
- 질문 TTS가 재생된다.
- 손 삽입 뒤 답변 수집이 시작된다.
- 손을 빼면 일시정지된다.
- 손을 다시 올리면 재개된다.
- 분석 뒤 결과 화면이 표시된다.
- 결과 화면의 `TRY AGAIN` 이 동작한다.
- 결과 화면의 `EXIT GAME` 이 동작한다.

## 문제 해결

### 앱은 열리지만 분석이 동작하지 않는다

원인:

- `python-runtime/` 또는 `python-engine/` 가 빠졌을 가능성이 있다.

조치:

- 배포 폴더 안에 두 디렉토리가 모두 존재하는지 확인한다.

### Windows 빌드가 시작도 되지 않는다

원인:

- `Windows Build Support (IL2CPP)` 가 없거나
- Visual Studio의 C++ 빌드 도구가 빠져 있을 가능성이 있다.

조치:

- Unity Hub에서 `Windows Build Support (IL2CPP)` 를 설치한다.
- Visual Studio Installer에서 `C++를 사용한 데스크톱 개발` 을 확인한다.
