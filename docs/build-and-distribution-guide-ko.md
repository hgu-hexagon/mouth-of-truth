# Mouth of Truth 빌드 및 배포 가이드

## 개요

이 문서는 Mouth of Truth를 macOS용 제품 형태로 빌드하고 배포하는 방법을 설명한다.

이 문서를 완료하면 아래 작업을 수행할 수 있어야 한다.

- 배포용 Python 런타임을 준비한다.
- macOS 릴리즈 빌드를 실행한다.
- 사용자에게 전달해야 할 파일을 판단한다.
- 사용자가 어떤 파일을 실행해야 하는지 안내한다.

## 배포 전 확인

아래 항목을 먼저 확인한다.

- Unity 프로젝트가 `unity-app/` 경로에서 열린다.
- 메인 씬이 `Assets/Scenes/Main.unity` 이다.
- 최종 PNG 자산이 `unity-app/Assets/StreamingAssets/art/` 아래에 반영되어 있다.
- Leap Motion 입력 연결과 장치 검증이 완료되어 있다.
- Python 분석에 필요한 로컬 모델 자산이 준비되어 있다.
- `python-engine/scripts/validate_bridge_runtime.sh` 가 통과한다.

## 배포용 Python 런타임 만들기

배포본에는 `.app` 번들만 넣으면 안 된다.
Unity 앱과 함께 Python 실행 환경도 묶어야 한다.

저장소 루트에서 아래 명령을 실행한다.

```bash
conda activate mouth-of-truth
python-engine/scripts/package_python_runtime.sh
```

이 스크립트는 아래 작업을 수행한다.

- `mouth-of-truth` conda 환경을 패키징한다.
- 저장소 루트에 `python-runtime/` 디렉토리를 만든다.
- 배포용 Python 실행 파일과 패키지를 그 안에 풀어 둔다.

만약 다른 환경 이름을 쓰고 있다면 환경 변수를 먼저 설정한다.

```bash
MOUTH_OF_TRUTH_CONDA_ENV=<conda-env-name> \
python-engine/scripts/package_python_runtime.sh
```

## macOS 릴리즈 빌드

### 1. 메인 씬 갱신

Unity 메뉴에서 아래 항목을 실행한다.

- `Mouth Of Truth > Build Main Scene`

이 단계는 현재 환경 자산과 앵커 배치를 기준으로 `Main.unity` 를 다시 정리한다.

### 2. macOS 배포 빌드 실행

Unity 메뉴에서 아래 항목을 실행한다.

- `Mouth Of Truth > Build Mac Release`

이 빌드는 아래 결과를 만든다.

- `dist/macos/MouthOfTruth/MouthOfTruth.app`
- `dist/macos/MouthOfTruth/Run Mouth of Truth.command`
- `dist/macos/MouthOfTruth/python-engine/`
- `dist/macos/MouthOfTruth/python-runtime/`
- `dist/macos/MouthOfTruth/bridge/`

## 배포 전에 확인할 파일

빌드가 끝나면 아래가 모두 존재해야 한다.

- `dist/macos/MouthOfTruth/MouthOfTruth.app`
- `dist/macos/MouthOfTruth/Run Mouth of Truth.command`
- `dist/macos/MouthOfTruth/python-engine/`
- `dist/macos/MouthOfTruth/python-runtime/`
- `dist/macos/MouthOfTruth/bridge/`

위 다섯 항목 중 하나라도 빠지면 배포본으로 사용하지 않는다.

## 사용자에게 전달할 파일

사용자에게는 아래 디렉토리 전체를 전달한다.

- `dist/macos/MouthOfTruth/`

전달 방식은 아래를 권장한다.

- `MouthOfTruth/` 디렉토리를 ZIP으로 압축해서 전달한다.

중요:

- `MouthOfTruth.app` 파일 하나만 따로 전달하지 않는다.
- 반드시 `MouthOfTruth/` 전체 디렉토리를 전달한다.

## 사용자가 실행할 파일

사용자는 압축을 해제한 뒤 아래 파일을 실행한다.

- `Run Mouth of Truth.command`

이 런처는 제품 루트 경로를 잡고 `MouthOfTruth.app` 를 올바른 위치에서 실행한다.

보조적으로 아래 파일을 직접 실행할 수도 있다.

- `MouthOfTruth.app`

다만 운영 기준으로는 `.command` 런처 실행을 기본 안내로 사용한다.

## 첫 실행 시 사용자에게 안내할 사항

사용자가 처음 실행할 때 아래 권한 요청이 뜰 수 있다.

- 카메라 권한
- 마이크 권한

이 권한은 분석 기능에 필요하므로 허용해야 한다.

내부 시연용 빌드라면 아래도 같이 안내한다.

- macOS가 실행을 차단하면 Finder에서 파일을 우클릭한 뒤 **열기**를 선택한다.

## 배포 전 최종 점검

최종 배포 전에 아래 흐름을 실제로 확인한다.

- 시작 화면이 열린다.
- `START GAME` 이 동작한다.
- 카드 선택이 된다.
- 질문 TTS가 재생된다.
- 손 삽입 뒤 답변 수집이 시작된다.
- 손을 빼면 일시정지된다.
- 손을 다시 넣으면 재개된다.
- 결과 화면이 표시된다.
- `TRY AGAIN` 과 `BACK TO TITLE` 이 동작한다.

## 문제 해결

### 앱은 열리지만 분석이 동작하지 않는다

원인:

- `python-runtime/` 또는 `python-engine/` 가 빠졌을 수 있다.

조치:

- 배포 디렉토리 안에 두 폴더가 모두 있는지 확인한다.

### 사용자가 `.app` 만 따로 실행했다

원인:

- 제품 루트가 아니라 앱 번들만 단독으로 옮겼다.

조치:

- `MouthOfTruth/` 전체 디렉토리를 다시 전달한다.
- 가능하면 `Run Mouth of Truth.command` 실행을 다시 안내한다.

### python-runtime 생성이 실패한다

원인:

- `conda-pack` 가 환경에 없거나 conda 환경 이름이 다르다.

조치:

- `conda env create -f python-engine/environment.yml` 로 환경을 다시 만든다.
- 필요하면 `MOUTH_OF_TRUTH_CONDA_ENV` 를 설정해 패키징 스크립트를 다시 실행한다.

## 관련 문서

- `docs/developer-setup-checklist-ko.md`
- `python-engine/environment.yml`
- `python-engine/requirements.txt`
- `python-engine/scripts/package_python_runtime.sh`
