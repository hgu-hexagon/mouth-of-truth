# Mouth of Truth 빌드 및 배포 가이드

## 개요

이 문서는 Mouth of Truth 프로젝트를 최종 제품 형태로 빌드하고 배포하는 방법을 설명한다.

이 문서는 아래 상황을 대상으로 한다.

- 최종 PNG 자산이 프로젝트에 반영되어 있다.
- Leap Motion 입력이 정상적으로 연결되어 있다.
- Python 분석 런타임과 로컬 모델 자산이 준비되어 있다.

이 문서를 완료하면 아래 작업을 수행할 수 있어야 한다.

- macOS용 배포 패키지를 빌드한다.
- 배포에 필요한 파일을 빠짐없이 묶는다.
- 사용자에게 어떤 파일을 전달해야 하는지 판단한다.
- 사용자가 어떤 파일을 실행해야 하는지 안내한다.

## 빌드 전 확인

아래 항목을 먼저 확인한다.

- Unity 프로젝트가 `unity-app/` 경로에서 열린다.
- 메인 씬이 `Assets/Scenes/Main.unity` 이다.
- Python 환경이 준비되어 있다.
- 배포용 Python 런타임 폴더가 준비되어 있다.
- 얼굴 모델, 음성 모델, Whisper 캐시가 `python-engine/models/` 아래에 있다.
- 최종 PNG 자산이 `unity-app/Assets/StreamingAssets/art/` 아래에 반영되어 있다.
- Leap Motion 관련 패키지와 씬 연결이 완료되어 있다.

## 배포 패키지 구성

최종 배포 단위는 **앱 파일 하나가 아니라 디렉토리 전체**다.

최종 배포 디렉토리는 아래 구조를 기준으로 한다.

- `MouthOfTruth/`
  - `MouthOfTruth.app`
  - `Run Mouth of Truth.command`
  - `python-engine/`
  - `python-runtime/`
  - `bridge/`

각 항목의 역할은 아래와 같다.

- `MouthOfTruth.app`
  - Unity 플레이어 본체
- `Run Mouth of Truth.command`
  - macOS에서 제품을 올바른 런타임 루트로 실행하는 런처
- `python-engine/`
  - 분석 브리지 스크립트와 Python 모듈
- `python-runtime/`
  - 배포용 Python 실행 파일과 패키지 런타임
- `bridge/`
  - Unity와 Python이 요청/응답 JSON을 주고받는 런타임 디렉토리

중요:

- `.app` 파일만 따로 전달하면 안 된다.
- 반드시 `MouthOfTruth/` 디렉토리 전체를 묶어서 전달한다.

## macOS 배포본 빌드

### 0. 배포용 Python 런타임 준비

아래 둘 중 하나를 준비한다.

- 저장소 루트에 `python-runtime/` 폴더를 둔다.
- 또는 `MOUTH_OF_TRUTH_PYTHON_RUNTIME_ROOT` 환경 변수로 배포용 Python 런타임 폴더를 지정한다.

배포용 Python 런타임은 아래를 포함해야 한다.

- `bin/python` 또는 동등한 Python 실행 파일
- 프로젝트가 요구하는 Python 패키지

### 1. Unity에서 메인 씬 갱신

Unity 메뉴에서 아래 항목을 먼저 실행한다.

- `Mouth Of Truth > Build Main Scene`

이 단계는 메인 씬을 현재 환경 자산과 앵커 배치 기준으로 다시 정리한다.

### 2. macOS 배포 빌드 실행

Unity 메뉴에서 아래 항목을 실행한다.

- `Mouth Of Truth > Build Mac Release`

이 빌드는 아래 작업을 수행한다.

- `Main.unity` 를 macOS 앱으로 빌드한다.
- 결과물을 `dist/macos/MouthOfTruth/` 아래에 만든다.
- `python-engine/` 를 함께 복사한다.
- `bridge/` 디렉토리를 함께 만든다.
- `Run Mouth of Truth.command` 런처를 함께 만든다.
- `python-runtime/` 디렉토리가 준비되어 있으면 함께 복사한다.

주의:

- 첫 macOS 배포 빌드는 Unity가 URP 셰이더 캐시를 준비하느라 오래 걸릴 수 있다.
- 빌드 시간이 길더라도 콘솔 오류가 없다면 바로 중단하지 말고 완료까지 기다린다.

## 빌드 결과 위치

빌드가 성공하면 아래 경로가 생성된다.

- `dist/macos/MouthOfTruth/`

핵심 결과물은 아래다.

- `dist/macos/MouthOfTruth/MouthOfTruth.app`
- `dist/macos/MouthOfTruth/Run Mouth of Truth.command`

## 배포 전 점검

배포 전에 아래를 확인한다.

- `MouthOfTruth.app` 가 존재한다.
- `Run Mouth of Truth.command` 가 존재한다.
- `python-engine/` 가 존재한다.
- `python-runtime/` 가 존재한다.
- `bridge/` 가 존재한다.

그리고 아래 흐름을 실제로 확인한다.

- 시작 화면이 열린다.
- 카드 선택이 된다.
- 질문 TTS가 재생된다.
- 손 삽입 후 답변 수집이 시작된다.
- 결과 화면이 표시된다.

## 사용자에게 전달할 파일

사용자에게는 아래 디렉토리 전체를 전달한다.

- `dist/macos/MouthOfTruth/`

전달 방식은 아래 중 하나를 권장한다.

- ZIP으로 압축해 전달
- 설치 패키지로 다시 감싸기 전의 내부 배포본으로 전달

## 사용자가 실행할 파일

사용자는 압축을 해제한 뒤 아래 파일을 실행한다.

- `Run Mouth of Truth.command`

이 파일은 제품 루트 경로를 올바르게 잡은 뒤 `MouthOfTruth.app` 를 실행한다.

보조 방법으로 아래 파일을 직접 실행할 수도 있다.

- `MouthOfTruth.app`

다만 운영 기준으로는 `.command` 런처 실행을 기본 안내로 사용하는 편이 안전하다.

## 문제 해결

### 앱은 켜지지만 분석이 동작하지 않는다

원인:

- `python-runtime/` 또는 `python-engine/` 가 빠졌을 수 있다.

조치:

- 배포 디렉토리 안에 두 폴더가 모두 있는지 확인한다.

### 앱만 복사해서 전달했다

원인:

- `.app` 파일만 따로 전달하면 Python 런타임과 브리지 디렉토리가 빠진다.

조치:

- `MouthOfTruth/` 전체 디렉토리를 다시 묶어서 전달한다.

### 사용자 환경에서 실행이 차단된다

원인:

- macOS 보안 설정이나 서명 정책 문제일 수 있다.

조치:

- 배포 방식에 맞는 코드 서명과 공증 절차를 추가한다.
- 내부 시연용이라면 보안 설정 안내를 함께 제공한다.

## 관련 문서

- `docs/developer-setup-checklist-ko.md`
- `python-engine/environment.yml`
- `python-engine/requirements.txt`
- `unity-app/Assets/Scenes/Main.unity`
