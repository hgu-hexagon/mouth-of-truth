# Mouth of Truth

`Mouth of Truth`는 Unity UI와 Leap Motion/Ultraleap 손 추적을 사용하는
인터랙티브 진실 판정 체험 프로젝트입니다. Unity는 화면 흐름, 입력, 녹음,
얼굴 프레임 캡처를 담당하고, Python 엔진은 얼굴/음성 분석 결과를 결합해
`TRUE`, `FALSE`, `UNCERTAIN` 판정을 반환합니다.

## 공개 레포 기준

이 저장소는 최종 공개 배포용 소스 레포를 기준으로 정리되어 있습니다.

- Unity 프로젝트, 게임 흐름, 런타임 이미지/음성 자산, Python 분석 소스는 포함됩니다.
- 학습된 모델 바이너리, Whisper 캐시, 패키징된 Python 런타임, 빌드 결과물,
  로컬 검증 산출물은 Git에 포함하지 않습니다.
- GitHub 일반 Git push는 100 MiB를 초과하는 단일 파일을 차단하므로, 대용량
  모델 산출물은 별도 저장소, 릴리스 자산, 사내 스토리지, 또는 Git LFS 정책으로
  관리해야 합니다.
- 개발 중 임시로 작성한 체크리스트/검증 문서는 추적 대상에서 제외했습니다.

## 주요 문서

- [최종 배포 가이드](docs/final-release-guide-ko.md)
- [서드파티 자산과 SDK](THIRD_PARTY_ASSETS.md)
- [모델 자산 배치](python-engine/models/README.md)

## 프로젝트 구조

- `unity-app/`
  Unity 프로젝트, 게임 프레젠테이션, 입력 어댑터, 빌드 자동화
- `python-engine/`
  Python 분석 브리지, 모델 경로 해석, 얼굴/음성 분석, 판정 정책
- `bridge/`
  Unity와 Python 브리지 사이의 런타임 JSON 교환 디렉터리
- `tools/`
  macOS/Windows 릴리스 빌드 보조 스크립트

## 필수 복원 자산

다음 파일은 Git에 포함되지 않으므로, 그대로 개발하거나 릴리스 빌드를 만들려면
로컬에 직접 배치해야 합니다.

- `python-engine/models/face/yolo26x_rafdb_best.pt`
- `python-engine/models/voice/best_wav2vec2_iemocap/`
- `python-engine/models/whisper/models--openai--whisper-tiny/`  
  Whisper 전사를 사용할 때만 필요합니다.

자세한 배치와 모델 lineage는 [최종 배포 가이드](docs/final-release-guide-ko.md)를
확인하세요.

## 판정 정책 요약

Python 브리지 기준:

- 얼굴 evidence와 음성 evidence가 모두 있어야 `TRUE` 또는 `FALSE`를 반환합니다.
- 둘 중 하나라도 부족하면 `UNCERTAIN`을 반환합니다.
- 현재 결합 가중치는 얼굴 80%, 음성 20%입니다.
- 최종 fused score가 `33.0` 미만이면 `TRUE`, `33.0` 이상이면 `FALSE`입니다.

수정 위치:

- 결합 가중치: `python-engine/src/mouth_of_truth/fusion/multimodal_fusion.py`
- TRUE/FALSE 기준점: `python-engine/src/mouth_of_truth/fusion/verdict_policy.py`
- UNCERTAIN 조건: `python-engine/src/mouth_of_truth/fusion/judgment_policy.py`
- Unity fallback 판정: `unity-app/Assets/Scripts/Game/Analysis/DeterministicAnswerAnalysisClient.cs`

## 빠른 검증

```bash
python -m compileall -q python-engine/src
dotnet build unity-app/Assembly-CSharp.csproj --no-restore /m:1
dotnet build unity-app/Assembly-CSharp-Editor.csproj --no-restore /m:1
```

현재 MSBuild 검증은 제공 패키지인 Ultraleap 어셈블리의 알려진 vendor warning만
별도로 억제해 우리 코드 기준 warning-clean 상태를 유지합니다.
