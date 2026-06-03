# Mouth of Truth 최종 배포 가이드

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
```

기존 환경 이름이 `mouth-truth`인 경우에도 패키징 스크립트가 인식합니다.
다른 환경 이름을 사용하려면:

```bash
export MOUTH_OF_TRUTH_CONDA_ENV="<conda-env-name>"
```

## 4. 모델 파일 배치

아래 파일을 로컬에 배치합니다.

```text
python-engine/models/face/yolo26x_rafdb_best.pt
python-engine/models/voice/best_wav2vec2_iemocap/config.json
python-engine/models/voice/best_wav2vec2_iemocap/model.safetensors
python-engine/models/voice/best_wav2vec2_iemocap/preprocessor_config.json
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

## 5. 모델 산출물

얼굴 모델:

- 데이터셋: RAF-DB
- 학습 방식: Ultralytics YOLO 분류 모델 학습
- 출력: 얼굴 표정 class별 확률
- 산출물: `yolo26x_rafdb_best.pt`
- 로더: `python-engine/src/mouth_of_truth/face/infer_face.py`
- 점수 규칙: `python-engine/src/mouth_of_truth/face/face_score_logic.py`

음성 모델:

- 데이터셋: IEMOCAP
- 학습 방식: wav2vec2-base 음성 분류 파인튜닝
- 레이블: `ang`, `hap`, `exc`, `neu`, `sad`, `fru`
- 산출물: `best_wav2vec2_iemocap/`
- 로더: `python-engine/src/mouth_of_truth/voice/infer_voice.py`
- 점수 규칙: `python-engine/src/mouth_of_truth/voice/voice_score_logic.py`

Whisper:

- 모델: `openai/whisper-tiny`
- 용도: 답변 transcript가 비어 있을 때 선택 전사
- 산출물: Hugging Face 캐시
- 로더: `python-engine/src/mouth_of_truth/speech/whisper_transcriber.py`

학습 스크립트와 원본 데이터셋은 이 저장소에 포함하지 않습니다. 이 저장소는
학습된 산출물을 배치해 실행하는 프로젝트입니다.

## 6. 대용량 파일 관리

Git에 포함하지 않는 항목:

- 학습된 모델 바이너리
- Whisper 캐시
- `python-runtime/`
- `python-runtime-windows/`
- `dist/`
- `bridge/*.json`
- Unity `Library/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/`

GitHub 일반 Git 저장소는 100 MiB를 초과하는 단일 파일 push를 차단합니다.
모델 파일은 Git LFS, GitHub Release asset, 별도 모델 저장소, 또는 사내 저장소로
관리합니다.

공식 참고:

```text
https://docs.github.com/en/repositories/working-with-files/managing-large-files/about-large-files-on-github
```

## 7. Ultraleap 설치

현재 프로젝트 패키지:

```text
unity-app/Packages/com.ultraleap.tracking
com.ultraleap.tracking 7.3.0
```

패키지를 다시 설치할 때:

1. Unity에서 `Edit > Project Settings > Package Manager`를 엽니다.
2. Scoped Registry를 추가합니다.
   - Name: `Ultraleap`
   - URL: `https://package.openupm.com`
   - Scope: `com.ultraleap`
3. `Window > Package Manager`를 엽니다.
4. `My Registries`에서 `com.ultraleap.tracking`을 설치합니다.
5. 프로젝트와 같은 구성을 맞출 때는 `7.3.0`을 사용합니다.

공식 경로:

```text
https://github.com/ultraleap/UnityPlugin
https://openupm.com/packages/com.ultraleap.tracking/
https://docs.ultraleap.com/xr-and-tabletop/xr/unity/
```

Leap Motion 입력 장비에는 Ultraleap Hand Tracking Software를 설치하고 tracking
service를 실행합니다.

## 8. 서드파티 환경 자산

필요한 Unity Asset Store 자산:

- Dungeon Modular Pack
- Persiang Carpets URP

복원 경로:

```text
unity-app/Assets/ThirdParty/Environment/DungeonModularPack
unity-app/Assets/ThirdParty/Environment/PersianCarpetUrp
```

`BuildMainSceneEditor`는 Dungeon Modular Pack의 DemoScene과 prefab을 원본으로
사용합니다.

```text
unity-app/Assets/Editor/BuildMainSceneEditor.cs
```

런타임 이미지는 아래 경로에서 로드됩니다.

```text
unity-app/Assets/StreamingAssets/art
```

런타임 음성은 아래 경로에서 로드됩니다.

```text
unity-app/Assets/StreamingAssets/audio
```

## 9. 실행 모드

Python bridge 강제:

```bash
export MOUTH_OF_TRUTH_ANALYSIS_MODE=python
```

Unity 대체 판정 강제:

```bash
export MOUTH_OF_TRUTH_ANALYSIS_MODE=deterministic
```

Whisper 전사 활성화:

```bash
export MOUTH_OF_TRUTH_ENABLE_TRANSCRIPTION=1
```

학습된 wav2vec2 음성 모델 활성화:

```bash
export MOUTH_OF_TRUTH_USE_TRAINED_VOICE_MODEL=1
```

설정이 없으면 Unity는 Python 브리지 launcher와 Python module root를 찾고,
없으면 deterministic 대체 판정을 사용합니다.

## 10. 판정 정책

최종 판정 경로:

```text
python-engine/src/mouth_of_truth/fusion/judgment_policy.py
python-engine/src/mouth_of_truth/fusion/multimodal_fusion.py
python-engine/src/mouth_of_truth/fusion/verdict_policy.py
```

`UNCERTAIN` 조건:

- `MIN_FACE_RECOGNITIONS_FOR_JUDGMENT = 1`
- `MIN_VOICE_SEGMENTS_FOR_JUDGMENT = 1`
- 얼굴 summary `dominant_label`이 `N/A`
- 음성 summary `dominant_label`이 `N/A`

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

대체 판정은 Python 브리지가 없을 때 사용하는 보조 경로입니다. 얼굴 프레임 수와
음성 segment 수가 부족하면 `UNCERTAIN`을 반환하고, 충분하면 질문 ID와 답변
transcript checksum parity로 `TRUE` / `FALSE`를 반환합니다.

## 11. 질문과 질문 음성

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

## 12. 릴리스 빌드

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
python-engine/scripts/package_python_runtime.ps1

$env:UNITY_EDITOR_PATH="<unity-editor-executable>"
.\tools\build-windows-release.ps1
```

결과물:

```text
dist/macos/MouthOfTruth/
dist/windows/MouthOfTruth/
```

## 13. 공개 전 확인

```bash
git status --short
python -m compileall -q python-engine/src
dotnet build unity-app/Assembly-CSharp.csproj --no-restore /m:1
dotnet build unity-app/Assembly-CSharp-Editor.csproj --no-restore /m:1
```

금지 패턴 검색:

```bash
git grep -n -I -E "$PUBLIC_RELEASE_FORBIDDEN_PATTERN" -- .
```
