# Mouth of Truth 최종 배포 가이드

이 문서는 공개 레포를 받은 사람이 프로젝트를 그대로 복원하고, 왜 일부 대용량
파일이 Git에 없는지 이해하며, 판정 정책을 어디에서 바꿔야 하는지 알 수 있도록
정리한 최종 배포용 문서입니다.

## 1. 포함/미포함 원칙

공개 레포에 포함하는 항목:

- Unity 프로젝트 소스와 씬
- 런타임용 `StreamingAssets` 이미지/음성
- Python 분석 브리지 소스
- 릴리스 빌드 스크립트
- 모델을 배치할 빈 디렉터리와 README
- 서드파티 자산/SDK의 출처와 복원 방법

공개 레포에 포함하지 않는 항목:

- 학습된 모델 바이너리
- Hugging Face Whisper 캐시
- `python-runtime/`, `python-runtime-windows/`
- `dist/` 릴리스 결과물
- `bridge/*.json` 런타임 교환 파일
- Unity `Library/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/`
- 개발 중 검증/캡처/체크리스트 용도로 만든 문서와 산출물

GitHub 일반 Git 저장소는 100 MiB를 초과하는 단일 파일 push를 차단합니다.
현재 로컬 모델 중 `model.safetensors`와 Whisper model blob은 이 제한을 넘거나
레포를 불필요하게 크게 만들 수 있으므로 Git에 넣지 않습니다. 필요하면 Git LFS,
GitHub Release asset, 사내 드라이브, 별도 모델 저장소 중 하나로 관리하세요.
관련 공식 문서: `https://docs.github.com/en/repositories/working-with-files/managing-large-files/about-large-files-on-github`

## 2. 개발 환경

기준 환경:

- Unity Editor `6000.4.1f1`
- Python conda 환경 이름: `mouth-truth` 또는 `mouth-of-truth`
- macOS 릴리스 패키징: `python-engine/scripts/package_python_runtime.sh`
- Windows 릴리스 패키징: `python-engine/scripts/package_python_runtime.ps1`

Python 환경 생성:

```bash
conda env create -f python-engine/environment.yml
conda activate mouth-of-truth
python -m compileall -q python-engine/src
```

Unity는 반드시 저장소 루트가 아니라 아래 경로를 프로젝트로 엽니다.

```text
unity-app
```

## 3. 대용량 모델 자산 복원

모델 루트 기본값은 아래입니다.

```text
python-engine/models
```

다른 위치에 모델을 둘 때는 환경 변수로 루트를 바꿀 수 있습니다.

```bash
export MOUTH_OF_TRUTH_MODELS_ROOT="/path/to/model-root"
```

필수 로컬 배치:

```text
python-engine/models/face/yolo26x_rafdb_best.pt
python-engine/models/voice/best_wav2vec2_iemocap/config.json
python-engine/models/voice/best_wav2vec2_iemocap/model.safetensors
python-engine/models/voice/best_wav2vec2_iemocap/preprocessor_config.json
```

선택 로컬 배치:

```text
python-engine/models/whisper/models--openai--whisper-tiny/
```

Whisper 캐시는 답변 전사를 켤 때만 필요합니다.

```bash
export MOUTH_OF_TRUTH_ENABLE_TRANSCRIPTION=1
```

## 4. 모델과 데이터 lineage

현재 공개 레포에는 학습 스크립트와 원본 데이터셋을 포함하지 않습니다. 이 레포가
확정적으로 요구하는 것은 아래 학습 결과물의 경로와 형식입니다.

얼굴 모델:

- 사용 데이터: RAF-DB 얼굴 표정 데이터셋
- 방식: Ultralytics YOLO 계열 classification 모델로 얼굴 표정 class 확률 추론
- 결과물: `python-engine/models/face/yolo26x_rafdb_best.pt`
- 런타임 로더: `python-engine/src/mouth_of_truth/face/infer_face.py`
- 점수화 규칙: `python-engine/src/mouth_of_truth/face/face_score_logic.py`

음성 모델:

- 사용 데이터: IEMOCAP 음성 감정 데이터셋
- 방식: wav2vec2-base 계열 audio classification fine-tuning
- class label: `ang`, `hap`, `exc`, `neu`, `sad`, `fru`
- 결과물: `python-engine/models/voice/best_wav2vec2_iemocap/`
- 런타임 로더: `python-engine/src/mouth_of_truth/voice/infer_voice.py`
- 점수화 규칙: `python-engine/src/mouth_of_truth/voice/voice_score_logic.py`

현재 제품 기본 경로는 지연 시간을 줄이기 위해 음성 쪽에서 빠른 waveform dynamics
기반 분석을 먼저 사용합니다. 학습된 wav2vec2 음성 모델을 사용하려면 아래 환경
변수를 켭니다.

```bash
export MOUTH_OF_TRUTH_USE_TRAINED_VOICE_MODEL=1
```

Whisper:

- 모델: `openai/whisper-tiny`
- 목적: Unity가 전달한 답변 transcript가 비어 있을 때 선택적으로 전사
- 직접 학습한 모델이 아니라 Hugging Face cache를 로컬에 보관해 사용하는 방식
- 코드 위치: `python-engine/src/mouth_of_truth/speech/whisper_transcriber.py`

## 5. Ultraleap / Leap Motion 복원

현재 프로젝트의 embedded 패키지:

```text
unity-app/Packages/com.ultraleap.tracking
```

현재 패키지 버전:

```text
com.ultraleap.tracking 7.3.0
```

외부에서 다시 받는 방법:

1. Unity에서 `Edit > Project Settings > Package Manager`를 엽니다.
2. Scoped Registry를 추가합니다.
   - Name: `Ultraleap`
   - URL: `https://package.openupm.com`
   - Scope: `com.ultraleap`
3. `Window > Package Manager`에서 `My Registries`를 선택합니다.
4. `Ultraleap Tracking` 또는 `com.ultraleap.tracking`을 설치합니다.
5. 이 프로젝트와 동일하게 맞추려면 `7.3.0`을 우선 사용합니다.

공식 출처:

- Ultraleap Unity Plugin: `https://github.com/ultraleap/UnityPlugin`
- OpenUPM package: `https://openupm.com/packages/com.ultraleap.tracking/`
- Ultraleap Unity 문서: `https://docs.ultraleap.com/xr-and-tabletop/xr/unity/`

실제 Leap Motion 입력을 쓰는 시연 장비에는 Unity 패키지만으로는 부족합니다.
Ultraleap Hand Tracking Software를 설치하고 tracking service가 실행 중이어야 합니다.

## 6. 서드파티 환경 자산

Unity 환경 소스 자산:

- Dungeon Modular Pack
- Persiang Carpets URP

현재 프로젝트에서는 `BuildMainSceneEditor`가 Dungeon Modular Pack의 DemoScene과
prefab을 원본으로 사용해 배경/무대 구성을 생성합니다. 서드파티 자산이 빠진
slim mirror를 만들 경우 아래 경로로 복원해야 합니다.

```text
unity-app/Assets/ThirdParty/Environment/DungeonModularPack
unity-app/Assets/ThirdParty/Environment/PersianCarpetUrp
```

런타임 화면은 주로 `StreamingAssets/art`의 정리된 이미지 자산을 사용합니다.
배경을 다시 생성하거나 에디터 생성 메뉴를 쓰려면 위 원본 패키지가 필요합니다.

## 7. 릴리스 빌드

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

빌드 결과물은 `dist/` 아래에 생성되며 Git에 포함하지 않습니다.

## 8. 분석 모드

Unity 분석 클라이언트 선택:

- `MOUTH_OF_TRUTH_ANALYSIS_MODE=python`
  Python bridge 강제
- `MOUTH_OF_TRUTH_ANALYSIS_MODE=deterministic`
  Unity deterministic fallback 강제
- 설정이 없으면 Python bridge launcher와 Python module root가 있을 때 Python bridge 사용
- Python bridge를 찾지 못하면 deterministic fallback 사용

실제 제품 판정을 확인하려면 Python bridge와 모델 자산이 준비되어 있어야 합니다.
deterministic fallback은 제품 분석 모델이 아니라 개발/비상 fallback입니다.

## 9. TRUE/FALSE/UNCERTAIN 판정 정책

Python bridge 기준 최종 판정 흐름:

1. Unity가 답변 음성 파일과 얼굴 프레임 디렉터리를 Python bridge request로 전달합니다.
2. Python이 얼굴 분석과 음성 분석을 병렬로 실행합니다.
3. 얼굴 evidence와 음성 evidence가 모두 있으면 점수를 결합합니다.
4. 둘 중 하나라도 usable evidence가 없으면 `UNCERTAIN`입니다.
5. 결합 점수 기준으로 `TRUE` 또는 `FALSE`를 반환합니다.

UNCERTAIN 조건:

```text
python-engine/src/mouth_of_truth/fusion/judgment_policy.py
```

현재 기준:

- `MIN_FACE_RECOGNITIONS_FOR_JUDGMENT = 1`
- `MIN_VOICE_SEGMENTS_FOR_JUDGMENT = 1`
- 얼굴 summary의 `dominant_label`이 `N/A`이면 얼굴 evidence 없음
- 음성 summary의 `dominant_label`이 `N/A`이면 음성 evidence 없음

얼굴/음성 결합:

```text
python-engine/src/mouth_of_truth/fusion/multimodal_fusion.py
```

현재 기준:

- `FACE_WEIGHT = 0.80`
- `VOICE_WEIGHT = 0.20`
- `final_score = face_score * 0.80 + voice_score * 0.20`

TRUE/FALSE 기준:

```text
python-engine/src/mouth_of_truth/fusion/verdict_policy.py
```

현재 기준:

- `MULTIMODAL_FALSE_PIVOT_SCORE = 33.0`
- `score < 33.0` 이면 `TRUE`
- `score >= 33.0` 이면 `FALSE`

얼굴 점수 조정:

```text
python-engine/src/mouth_of_truth/face/face_score_logic.py
```

음성 점수 조정:

```text
python-engine/src/mouth_of_truth/voice/voice_score_logic.py
```

Unity fallback 판정:

```text
unity-app/Assets/Scripts/Game/Analysis/DeterministicAnswerAnalysisClient.cs
```

fallback은 얼굴 프레임 수와 음성 segment 수가 부족하면 `UNCERTAIN`을 반환하고,
충분하면 질문 ID와 답변 transcript checksum parity로 `TRUE/FALSE`를 나눕니다.
이 로직은 모델 기반 제품 판정이 아니라 Python bridge가 없을 때의 보조 경로입니다.

## 10. 질문과 질문 음성

질문 풀:

```text
unity-app/Assets/StreamingAssets/questions/question_pool.json
```

질문 음성:

```text
unity-app/Assets/StreamingAssets/audio/questions/Q0001.wav
...
unity-app/Assets/StreamingAssets/audio/questions/Q0012.wav
```

질문을 추가하려면 JSON의 `id`와 동일한 WAV 파일을 `audio/questions/`에 추가합니다.
비활성화하려면 질문 항목의 `enabled`를 `false`로 바꿉니다.

## 11. 공개 전 점검

```bash
git status --short
python -m compileall -q python-engine/src
dotnet build unity-app/Assembly-CSharp.csproj --no-restore /m:1
dotnet build unity-app/Assembly-CSharp-Editor.csproj --no-restore /m:1
```

로컬 절대경로/비밀키/검증 샘플이 남았는지 확인할 때는, 팀에서 쓰는 금지
패턴을 하나의 환경 변수나 스크립트로 관리해 실행합니다.

```bash
git grep -n -I -E "$PUBLIC_RELEASE_FORBIDDEN_PATTERN" -- .
```

대용량 모델 파일은 `python-engine/models/.gitignore`가 막고 있어야 합니다.
