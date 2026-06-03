# Mouth of Truth 프로젝트 설정과 릴리스 빌드

## 1. 필수 환경

- Unity Editor `6000.4.1f1`
- Git
- Miniforge, Mambaforge, Anaconda 중 하나
- `conda-pack`
- macOS 릴리스 빌드: macOS 환경
- Windows 릴리스 빌드: Windows Build Support, Visual Studio C++ 빌드 도구, Windows SDK
- Leap Motion 입력: Ultraleap Hand Tracking Software, Leap Motion 또는 Ultraleap 호환 장치

## 2. 저장소 열기

```bash
git clone <repository-url>
cd mouth-of-truth
```

Unity Hub에서 아래 폴더를 엽니다.

```text
unity-app
```

저장소 루트를 Unity 프로젝트로 열지 않습니다.

## 3. Python 환경 생성

```bash
conda env create -f python-engine/environment.yml
conda activate mouth-of-truth
python -m compileall -q python-engine/src
PYTHONPATH=python-engine/src python -m unittest discover -s python-engine/tests
```

기존 환경 이름이 `mouth-truth`인 경우에도 패키징 스크립트가 인식합니다. 다른 환경 이름을 사용하려면:

```bash
export MOUTH_OF_TRUTH_CONDA_ENV="<conda-env-name>"
```

## 4. 모델 파일 복원

모델 파일은 Git에 포함하지 않습니다. `mouth-of-truth-models-required.tar.gz` 묶음 파일을 받은 뒤 저장소 루트에서 복원합니다.

```bash
tools/restore-model-assets.sh <path-to>/mouth-of-truth-models-required.tar.gz
```

Windows PowerShell:

```powershell
.\tools\restore-model-assets.ps1 -ModelBundlePath <path-to>\mouth-of-truth-models-required.tar.gz
```

복원 후 구조:

```text
python-engine/models/face/yolo26x_rafdb_best.pt
python-engine/models/voice/best_wav2vec2_iemocap/config.json
python-engine/models/voice/best_wav2vec2_iemocap/model.safetensors
python-engine/models/voice/best_wav2vec2_iemocap/preprocessor_config.json
```

필수 모델 SHA-256:

```text
48e47f019b8214b4c6869af87a3ab8a23fa34a0e891a6d4caf7fd25f7492e35a  python-engine/models/face/yolo26x_rafdb_best.pt
e80a86c0d4e859cd46cc852d4f5864f3de78be8e64f47c1f79b31b687099f5be  python-engine/models/voice/best_wav2vec2_iemocap/config.json
699c55de39fddb538eee49a24afc1008a20bb78918b7a50429b63b59dc62f5c3  python-engine/models/voice/best_wav2vec2_iemocap/model.safetensors
8cdfd65ff4115423185a1512bdae100e2e0cd744f5b322417429944aaafd0827  python-engine/models/voice/best_wav2vec2_iemocap/preprocessor_config.json
```

검증 명령:

```bash
shasum -a 256 \
  python-engine/models/face/yolo26x_rafdb_best.pt \
  python-engine/models/voice/best_wav2vec2_iemocap/config.json \
  python-engine/models/voice/best_wav2vec2_iemocap/model.safetensors \
  python-engine/models/voice/best_wav2vec2_iemocap/preprocessor_config.json
```

macOS와 Windows 릴리스 빌드는 위 필수 모델의 존재 여부와 SHA-256을 자동으로 확인합니다. 필수 파일이 없거나 검사값이 다르면 빌드가 중단됩니다.

로컬 모델 파일로 묶음 파일을 만들 때:

```bash
tools/package-model-assets.sh
```

생성 위치:

```text
dist/model-assets/mouth-of-truth-models-required.tar.gz
dist/model-assets/mouth-of-truth-models-required.tar.gz.sha256
```

Whisper 캐시까지 함께 배포할 때:

```bash
MOUTH_OF_TRUTH_INCLUDE_WHISPER_CACHE=1 tools/package-model-assets.sh
```

Whisper 전사를 사용할 때만 아래 캐시를 배치합니다.

```text
python-engine/models/whisper/models--openai--whisper-tiny/
```

모델을 다른 위치에 둘 경우:

```bash
export MOUTH_OF_TRUTH_MODELS_ROOT="/path/to/model-root"
```

`MOUTH_OF_TRUTH_MODELS_ROOT` 아래에도 같은 구조를 둡니다.

```text
face/yolo26x_rafdb_best.pt
voice/best_wav2vec2_iemocap/
whisper/models--openai--whisper-tiny/
```

## 5. 모델 자산

데이터셋 신청, 레이블 매핑, 학습 산출물 기준은 아래 문서에 정리되어 있습니다.

```text
docs/model-training-and-datasets.md
```

얼굴 모델:

- 사용 데이터: RAF-DB
- 데이터셋 신청: `https://www.whdeng.cn/RAF/model1.html`
- 학습 방식: Ultralytics YOLO 분류 모델 학습
- 출력: 얼굴 표정 class별 확률
- 산출물: `yolo26x_rafdb_best.pt`
- 로더: `python-engine/src/mouth_of_truth/face/infer_face.py`
- 점수 규칙: `python-engine/src/mouth_of_truth/face/face_score_logic.py`

음성 모델:

- 사용 데이터: IEMOCAP
- 데이터셋 신청: `https://sail.usc.edu/iemocap/`
- 학습 방식: wav2vec2-base 음성 분류 파인튜닝
- 레이블: `ang`, `hap`, `exc`, `neu`, `sad`, `fru`
- 산출물: `best_wav2vec2_iemocap/`
- 로더: `python-engine/src/mouth_of_truth/voice/infer_voice.py`
- 점수 규칙: `python-engine/src/mouth_of_truth/voice/voice_score_logic.py`

Whisper:

- 모델: `openai/whisper-tiny`
- 다운로드: `https://huggingface.co/openai/whisper-tiny`
- 용도: 답변 전사 텍스트가 비어 있을 때 선택 전사
- 산출물: Hugging Face 캐시
- 로더: `python-engine/src/mouth_of_truth/speech/whisper_transcriber.py`

## 6. 모델 교체

배포본은 학습된 모델 산출물을 기준으로 실행됩니다. 원본 데이터셋은 모델 출처 확인과 새 모델 학습에만 필요하며, 프로젝트 실행에는 필요하지 않습니다.

얼굴 모델을 교체할 때:

- `python-engine/models/face/yolo26x_rafdb_best.pt` 위치에 Ultralytics YOLO 분류 체크포인트를 둡니다.
- `python-engine/src/mouth_of_truth/face/face_score_logic.py`가 기대하는 레이블과 점수 매핑을 맞춥니다.

음성 모델을 교체할 때:

- `python-engine/models/voice/best_wav2vec2_iemocap/` 위치에 Hugging Face `AutoModelForAudioClassification` 호환 모델 디렉터리를 둡니다.
- `config.json`, `model.safetensors`, `preprocessor_config.json`을 포함합니다.
- 레이블은 `ang`, `hap`, `exc`, `neu`, `sad`, `fru` 체계를 유지하거나 `python-engine/src/mouth_of_truth/voice/voice_score_logic.py`를 함께 수정합니다.

Whisper 모델을 교체할 때:

- `python-engine/src/mouth_of_truth/speech/whisper_transcriber.py`의 `WHISPER_MODEL_NAME`을 바꿉니다.
- 오프라인 배포본에는 변경한 모델의 Hugging Face 캐시를 함께 포함합니다.

## 7. 대용량 파일 관리

Git에 포함하지 않는 항목:

- 모델 바이너리
- Whisper 캐시
- Unity Asset Store 원본 자산
- `python-runtime/`
- `python-runtime-windows/`
- `dist/`
- `bridge/*.json`
- Unity `Library/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/`

GitHub 일반 Git 저장소는 100 MiB를 초과하는 단일 파일 push를 차단합니다. 모델 묶음 파일은 GitHub Release asset, 별도 모델 저장소, 사내 저장소, 또는 다른 외부 저장소로 관리합니다.

공식 참고:

```text
https://docs.github.com/en/repositories/working-with-files/managing-large-files/about-large-files-on-github
```

## 8. Ultraleap 설치

현재 프로젝트 패키지:

```text
unity-app/Packages/com.ultraleap.tracking
com.ultraleap.tracking 7.3.0
unity-app/Packages/manifest.json: "com.ultraleap.tracking": "file:com.ultraleap.tracking"
```

저장소를 정상적으로 받았다면 Unity 패키지가 프로젝트 안에 포함되어 있으므로 Unity Package Manager에서 추가 설치가 필요하지 않습니다.

패키지가 빠진 상태를 복원할 때:

1. `https://github.com/ultraleap/UnityPlugin/releases/latest`에서 Unity Release `7.3.0`을 받습니다.
2. `com.ultraleap.tracking` UPM 패키지를 `unity-app/Packages/com.ultraleap.tracking`에 배치합니다.
3. `unity-app/Packages/com.ultraleap.tracking/package.json`의 `name`과 `version`을 확인합니다.

```text
"name": "com.ultraleap.tracking"
"version": "7.3.0"
```

OpenUPM에서 설치할 때:

1. Unity에서 `Edit > Project Settings > Package Manager`를 엽니다.
2. Scoped Registry를 추가합니다.
   - Name: `Ultraleap`
   - URL: `https://package.openupm.com`
   - Scope: `com.ultraleap`
3. `Window > Package Manager`를 엽니다.
4. `My Registries`에서 `com.ultraleap.tracking`을 설치합니다.
5. 프로젝트와 같은 구성을 맞출 때는 `7.3.0`을 사용합니다.
6. OpenUPM 목록에 `7.3.0`이 없으면 GitHub 릴리스 방식으로 복원합니다.

공식 경로:

```text
https://leap2.ultraleap.com/
https://support.ultraleap.com/hc/en-us/articles/360004324078-How-do-I-set-up-my-Leap-Motion-Controller-2-Ultraleap-Stereo-IR-170-3Di-or-Leap-Motion-Controller
https://github.com/ultraleap/UnityPlugin/releases/latest
https://github.com/ultraleap/UnityPlugin
https://openupm.com/packages/com.ultraleap.tracking/
https://docs.ultraleap.com/xr-and-tabletop/xr/unity/
```

Leap Motion 입력 장비에는 Ultraleap Hand Tracking Software를 설치하고 트래킹 서비스를 실행합니다. `leap2.ultraleap.com`에서 보유한 카메라와 운영체제를 선택한 뒤 Hand Tracking Software를 내려받습니다. 설치 후 Ultraleap Control Panel 또는 시각화 도구에서 손 트래킹이 보이는지 확인합니다.

## 9. 서드파티 환경 자산

필요한 Unity Asset Store 자산은 Git에 포함하지 않습니다. 각 개발자는 본인 라이선스 계정으로 내려받아 지정 경로에 가져옵니다.

- Dungeon Modular Pack
- Persiang Carpets URP

복원 경로:

```text
unity-app/Assets/ThirdParty/Environment/DungeonModularPack
unity-app/Assets/ThirdParty/Environment/PersianCarpetUrp
```

`BuildMainSceneEditor`는 Dungeon Modular Pack의 DemoScene과 prefab을 원본으로 사용합니다.

```text
unity-app/Assets/Editor/BuildMainSceneEditor.cs
```

자세한 포함/복원 기준:

```text
THIRD_PARTY_ASSETS.md
```

런타임 이미지는 아래 경로에서 로드됩니다.

```text
unity-app/Assets/StreamingAssets/art
```

런타임 음성은 아래 경로에서 로드됩니다.

```text
unity-app/Assets/StreamingAssets/audio
```

## 10. 실행 모드

Python 브리지 사용:

```bash
export MOUTH_OF_TRUTH_ANALYSIS_MODE=python
```

Unity 결정적 대체 판정 사용:

```bash
export MOUTH_OF_TRUTH_ANALYSIS_MODE=deterministic
```

Whisper 전사 활성화:

```bash
export MOUTH_OF_TRUTH_ENABLE_TRANSCRIPTION=1
```

음성 분석은 기본적으로 응답 속도를 우선하는 빠른 음향 요약을 사용합니다. 학습된 wav2vec2 음성 모델을 사용할 때:

```bash
export MOUTH_OF_TRUTH_USE_TRAINED_VOICE_MODEL=1
```

설정이 없으면 Unity는 Python 브리지 실행 파일과 Python module root를 찾고, 찾지 못하면 결정적 대체 판정을 사용합니다.

## 11. 판정 해석과 한계

`TRUE`, `FALSE`, `UNCERTAIN`은 인터랙티브 설치 경험을 위한 게임 판정입니다. 얼굴 표정과 음성 신호를 사용하지만, 실제 거짓말 탐지, 신뢰도 평가, 채용/심사, 의사결정 근거로 사용하지 않습니다.

운영 전 확인 대상:

- 참가자에게 카메라/마이크 수집 목적을 안내하고 동의를 받습니다.
- 조명, 카메라 각도, 마이크 품질, 주변 소음에 따라 얼굴/음성 신호 품질을 다시 확인합니다.
- 얼굴/음성 모델과 기준값은 현재 설치 경험에 맞춘 정책값입니다. 데이터셋 일반화 성능이나 과학적 lie-detection 정확도를 보증하지 않습니다.
- `MOUTH_OF_TRUTH_ANALYSIS_MODE=deterministic`은 Python 브리지가 없을 때 쓰는 개발/시연용 대체 경로입니다. 실제 모델 판정 품질을 대표하지 않습니다.

지연 시간을 줄이기 위한 현재 구현:

- Python persistent worker가 얼굴 모델을 미리 로드합니다.
- 얼굴 분석은 대표 프레임 최대 3개를 샘플링하고, 현재 정책은 첫 유효 얼굴 인식 1개만으로 세션 요약을 만듭니다.
- 음성 분석은 기본적으로 빠른 음향 요약을 사용하고, 학습된 wav2vec2 모델은 `MOUTH_OF_TRUTH_USE_TRAINED_VOICE_MODEL=1`일 때만 사용합니다.
- Unity 브리지는 persistent worker 실패 시 일회성 Python 프로세스를 사용하고, 그마저 실패하면 결정적 대체 판정으로 내려갑니다.

## 12. 판정 정책

최종 판정 경로:

```text
python-engine/src/mouth_of_truth/fusion/judgment_policy.py
python-engine/src/mouth_of_truth/fusion/multimodal_fusion.py
python-engine/src/mouth_of_truth/fusion/verdict_policy.py
```

`UNCERTAIN` 조건:

- `MIN_FACE_RECOGNITIONS_FOR_JUDGMENT = 1`
- `MIN_VOICE_SEGMENTS_FOR_JUDGMENT = 1`
- 얼굴 요약 `dominant_label`이 `N/A`
- 음성 요약 `dominant_label`이 `N/A`

점수 결합:

- `FACE_WEIGHT = 0.80`
- `VOICE_WEIGHT = 0.20`
- `final_score = face_score * FACE_WEIGHT + voice_score * VOICE_WEIGHT`

`TRUE` / `FALSE` 기준:

- `MULTIMODAL_FALSE_PIVOT_SCORE = 33.0`
- `final_score < 33.0`: `TRUE`
- `final_score >= 33.0`: `FALSE`

Unity 대체 판정:

```text
unity-app/Assets/Scripts/Game/Analysis/DeterministicAnswerAnalysisClient.cs
```

대체 판정은 Python 브리지가 없을 때 사용하는 보조 경로입니다. 얼굴 프레임 수와 음성 segment 수가 부족하면 `UNCERTAIN`을 반환하고, 충분하면 질문 ID와 답변 전사 텍스트에서 계산한 검사값으로 `TRUE` / `FALSE`를 반환합니다.

## 13. 질문과 질문 음성

질문 풀:

```text
unity-app/Assets/StreamingAssets/questions/question_pool.json
```

질문 음성:

```text
unity-app/Assets/StreamingAssets/audio/questions/Q0001.wav
unity-app/Assets/StreamingAssets/audio/questions/Q0002.wav
...
unity-app/Assets/StreamingAssets/audio/questions/Q0012.wav
```

질문 추가:

1. `question_pool.json`에 새 `id`를 추가합니다.
2. 같은 `id`의 WAV 파일을 `audio/questions/`에 추가합니다.

질문 비활성화:

```json
"enabled": false
```

## 14. 릴리스 빌드

릴리스 빌드는 아래 항목을 배포물에 포함하고, 필수 파일과 모델 검사값을 검증합니다.

```text
python-engine/src/
python-engine/scripts/
python-engine/models/
python-engine/requirements.txt
python-engine/environment.yml
python-runtime/ 또는 python-runtime-windows/
bridge/
```

macOS:

```bash
conda activate mouth-of-truth
python-engine/scripts/package_python_runtime.sh

UNITY_EDITOR_PATH="<unity-editor-executable>" \
./tools/build-macos-release.sh
```

Windows:

```powershell
conda activate mouth-of-truth
.\python-engine\scripts\package_python_runtime.ps1

$env:UNITY_EDITOR_PATH="<unity-editor-executable>"
.\tools\build-windows-release.ps1
```

결과물:

```text
dist/macos/MouthOfTruth/
dist/windows/MouthOfTruth/
```

## 15. 공개 전 확인

```bash
git status --short
python -m compileall -q python-engine/src
PYTHONPATH=python-engine/src python -m unittest discover -s python-engine/tests
dotnet build unity-app/MouthOfTruth.Game.csproj /m:1
dotnet build unity-app/Assembly-CSharp-Editor.csproj /m:1
dotnet build unity-app/MouthOfTruth.Editor.Tests.csproj /m:1
```

`--no-restore`는 위 빌드나 Unity Editor가 NuGet restore를 한 번 끝낸 뒤 반복 검증할 때만 사용합니다.

Unity EditMode 테스트:

```bash
/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -projectPath unity-app \
  -runTests \
  -testPlatform editmode \
  -testResults /tmp/mouth-of-truth-editmode-results.xml
```

자동 테스트 범위:

- Python 브리지 교환 형식, 판정 정책, 얼굴/음성 점수 규칙
- Unity 상태 머신, 손 머무름 선택, 답변 종료 정책, 결정적 대체 판정
- Unity 런타임/에디터 C# 컴파일

장비가 필요한 Ultraleap 손 입력, 마이크 녹음, 웹캠 캡처, 실제 모델 지연 시간은 릴리스 전 현장 수동 QA로 확인합니다.

금지 패턴 검색:

```bash
git grep -n -I --fixed-strings "$HOME" -- . ':!unity-app/Packages/com.ultraleap.tracking'

PUBLIC_RELEASE_SECRET_PATTERN='(api[_-]?key|access[_-]?token|auth[_-]?token|secret|password|PRIVATE KEY)'
git grep -n -I -E "$PUBLIC_RELEASE_SECRET_PATTERN" -- . ':!docs/setup-and-release.md' ':!unity-app/Packages/com.ultraleap.tracking'
```

Windows PowerShell:

```powershell
git grep -n -I --fixed-strings $env:USERPROFILE -- . ':!unity-app/Packages/com.ultraleap.tracking'
```
