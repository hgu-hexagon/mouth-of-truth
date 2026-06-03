# Mouth of Truth

Unity와 Python 분석 엔진으로 구성된 인터랙티브 진실 판정 프로젝트입니다.
Unity는 화면 흐름, Leap Motion/Ultraleap 입력, 마이크 녹음, 웹캠 얼굴 프레임
캡처를 처리합니다. Python 엔진은 얼굴/음성 분석 결과를 결합해 `TRUE`,
`FALSE`, `UNCERTAIN` 판정을 반환합니다.

## 바로 시작

```bash
conda env create -f python-engine/environment.yml
conda activate mouth-of-truth
python -m compileall -q python-engine/src
```

Unity Hub에서 아래 폴더를 엽니다.

```text
unity-app
```

기준 Unity 버전:

```text
6000.4.1f1
```

## 필수 로컬 자산

아래 모델 파일은 Git에 포함되지 않습니다. Release asset 또는 별도 모델 저장소로
제공되는 모델 번들을 로컬에 배치합니다. SHA-256은
[모델 자산](python-engine/models/README.md)에 적힌 값으로 확인합니다.

```text
python-engine/models/face/yolo26x_rafdb_best.pt
python-engine/models/voice/best_wav2vec2_iemocap/config.json
python-engine/models/voice/best_wav2vec2_iemocap/model.safetensors
python-engine/models/voice/best_wav2vec2_iemocap/preprocessor_config.json
```

Whisper 전사를 사용할 때만 아래 캐시를 추가합니다.

```text
python-engine/models/whisper/models--openai--whisper-tiny/
```

다른 모델 루트를 사용하려면:

```bash
export MOUTH_OF_TRUTH_MODELS_ROOT="/path/to/model-root"
```

## 주요 문서

- [최종 배포 가이드](docs/final-release-guide-ko.md)
- [서드파티 자산과 SDK](THIRD_PARTY_ASSETS.md)
- [모델 자산 배치](python-engine/models/README.md)

## 프로젝트 구조

```text
unity-app/       Unity 프로젝트, 게임 화면, 입력, 빌드 자동화
python-engine/   Python 분석 브리지, 모델 로더, 판정 정책
bridge/          Unity/Python 런타임 JSON 교환 디렉터리
tools/           릴리스 빌드 스크립트
```

## 판정 정책

Python 브리지 기준:

- 얼굴 증거와 음성 증거가 모두 있으면 `TRUE` 또는 `FALSE`
- 얼굴 또는 음성 증거가 부족하면 `UNCERTAIN`
- 얼굴 점수 가중치: `0.80`
- 음성 점수 가중치: `0.20`
- 최종 결합 점수 `< 33.0`: `TRUE`
- 최종 결합 점수 `>= 33.0`: `FALSE`

수정 위치:

```text
python-engine/src/mouth_of_truth/fusion/judgment_policy.py
python-engine/src/mouth_of_truth/fusion/multimodal_fusion.py
python-engine/src/mouth_of_truth/fusion/verdict_policy.py
unity-app/Assets/Scripts/Game/Analysis/DeterministicAnswerAnalysisClient.cs
```

## 빌드 확인

```bash
python -m compileall -q python-engine/src
dotnet build unity-app/Assembly-CSharp.csproj --no-restore /m:1
dotnet build unity-app/Assembly-CSharp-Editor.csproj --no-restore /m:1
```

## 모델 교체

배포본은 학습된 모델 artifact를 사용합니다. 다른 모델을 사용할 때는 같은 경로와
출력 포맷을 맞추고, 얼굴/음성 점수 규칙이 기대하는 label 체계를 유지합니다.

## macOS 릴리스 빌드

```bash
conda activate mouth-of-truth
python-engine/scripts/package_python_runtime.sh

UNITY_EDITOR_PATH="<unity-editor-executable>" \
./tools/build-macos-release.sh
```

빌드 결과물은 `dist/` 아래에 생성됩니다. `dist/`는 Git에 포함하지 않습니다.
