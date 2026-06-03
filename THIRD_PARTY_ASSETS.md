# 서드파티 자산과 런타임

## 포함 상태 요약

| 항목 | Git 포함 | 별도 설치/복원 | 비고 |
| --- | --- | --- | --- |
| Ultraleap Unity package `com.ultraleap.tracking` | 포함 | 선택 | Unity 프로젝트에 포함된 패키지입니다. |
| Ultraleap Hand Tracking Software | 미포함 | 필요 | Leap Motion/Ultraleap 카메라를 연결한 실행 PC에 설치합니다. |
| Dungeon Modular Pack | 미포함 | 필요 | Unity Asset Store에서 라이선스를 보유한 계정으로 가져옵니다. |
| Persiang Carpets URP | 미포함 | 필요 | Unity Asset Store에서 라이선스를 보유한 계정으로 가져옵니다. |
| 실행 이미지, 오디오, 질문 자산 | 포함 | 불필요 | `unity-app/Assets/StreamingAssets/` 아래에 포함되어 있습니다. |
| 얼굴/음성 모델 파일 | 미포함 | 필요 | 모델 묶음 파일을 받아 `python-engine/models/`에 복원합니다. |
| Whisper 캐시 | 미포함 | 선택 | 전사 기능을 켤 때만 필요합니다. |
| Python 실행 환경 묶음 | 미포함 | 릴리스 빌드 시 생성 | `python-runtime/`, `python-runtime-windows/`는 Git에 넣지 않습니다. |
| 빌드 결과물 | 미포함 | 릴리스 빌드 시 생성 | `dist/`는 Git에 넣지 않습니다. |

## Ultraleap

### Git에 포함된 항목

```text
unity-app/Packages/com.ultraleap.tracking/
unity-app/Packages/manifest.json
```

현재 프로젝트는 Ultraleap Unity package `7.3.0`을 프로젝트 안에 포함해 사용합니다.

```text
"com.ultraleap.tracking": "file:com.ultraleap.tracking"
```

패키지 정보:

```text
name: com.ultraleap.tracking
version: 7.3.0
license: Apache-2.0
```

### 실행 PC에 별도 설치하는 항목

Leap Motion 또는 Ultraleap 카메라 입력을 사용하려면 실행 PC에 Ultraleap Hand Tracking Software를 설치하고 트래킹 서비스를 실행해야 합니다. Unity 패키지만으로는 카메라 트래킹 서비스가 설치되지 않습니다.

설치 절차:

1. `https://leap2.ultraleap.com/`에 접속합니다.
2. 보유한 카메라를 선택합니다. 예: Leap Motion Controller 2, Leap Motion Controller, Stereo IR 170, 3Di.
3. 실행 PC의 운영체제를 선택한 뒤 Hand Tracking Software를 내려받아 설치합니다.
4. 카메라를 USB로 연결하고 Ultraleap Control Panel 또는 시각화 도구에서 손 트래킹이 정상적으로 보이는지 확인합니다.
5. 이 프로젝트를 실행하기 전 트래킹 서비스가 실행 중인지 확인합니다.

공식 경로:

```text
https://leap2.ultraleap.com/
https://support.ultraleap.com/hc/en-us/articles/360004324078-How-do-I-set-up-my-Leap-Motion-Controller-2-Ultraleap-Stereo-IR-170-3Di-or-Leap-Motion-Controller
https://docs.ultraleap.com/hand-tracking/getting-started
https://docs.ultraleap.com/hand-tracking/Hyperion/index.html
```

### Unity 패키지를 다시 받아야 하는 경우

저장소를 정상적으로 받았다면 `unity-app/Packages/com.ultraleap.tracking/`이 포함됩니다. 해당 폴더가 빠진 경우에만 다시 받습니다.

GitHub 릴리스:

```text
https://github.com/ultraleap/UnityPlugin/releases
```

복원 위치:

```text
unity-app/Packages/com.ultraleap.tracking/
```

OpenUPM 사용 시:

```text
Scoped Registry Name: Ultraleap
Scoped Registry URL: https://package.openupm.com
Scope: com.ultraleap
Package: com.ultraleap.tracking
Version: 7.3.0
```

OpenUPM 목록에 `7.3.0`이 없으면 GitHub 릴리스 패키지를 사용합니다.

## Unity Asset Store 환경 자산

아래 원본 자산은 Git에 포함하지 않습니다. Unity Asset Store 자산은 빌드 결과물에 포함해 사용할 수 있지만, 원본 파일을 공개 Git 저장소에 그대로 재배포하지 않습니다.

공식 참고:

```text
https://support.unity.com/hc/en-us/articles/360013314731-Can-I-redistribute-assets-that-I-ve-licensed
https://unity.com/legal/as-terms
```

### Dungeon Modular Pack

다운로드:

```text
https://assetstore.unity.com/packages/3d/environments/dungeons/dungeon-modular-pack-295430
```

복원 위치:

```text
unity-app/Assets/ThirdParty/Environment/DungeonModularPack/
```

필수 경로:

```text
unity-app/Assets/ThirdParty/Environment/DungeonModularPack/Scenes/DemoScene.unity
unity-app/Assets/ThirdParty/Environment/DungeonModularPack/Materials/M_Wall.mat
unity-app/Assets/ThirdParty/Environment/DungeonModularPack/Prefabs/Torch_B.prefab
unity-app/Assets/ThirdParty/Environment/DungeonModularPack/Prefabs/Arch_A.prefab
unity-app/Assets/ThirdParty/Environment/DungeonModularPack/Meshes/
```

### Persiang Carpets URP

다운로드:

```text
https://assetstore.unity.com/packages/3d/props/persiang-carpets-urp-261455
```

복원 위치:

```text
unity-app/Assets/ThirdParty/Environment/PersianCarpetUrp/
```

필수 경로:

```text
unity-app/Assets/ThirdParty/Environment/PersianCarpetUrp/Models/
unity-app/Assets/ThirdParty/Environment/PersianCarpetUrp/Materials/
unity-app/Assets/ThirdParty/Environment/PersianCarpetUrp/Prefab/
unity-app/Assets/ThirdParty/Environment/PersianCarpetUrp/Textures/
```

### Unity에서 복원하는 순서

1. Unity Hub에서 `unity-app`을 엽니다.
2. Unity Asset Store 또는 Package Manager `My Assets`에서 위 두 자산을 내려받습니다.
3. 가져온 위치가 위 경로와 다르면 폴더 이름을 맞춥니다.
4. `Mouth Of Truth > Build Main Scene`을 실행합니다.
5. `Assets/Scenes/Main.unity`가 정상 저장되는지 확인합니다.

`BuildMainSceneEditor`는 Dungeon Modular Pack의 demo scene과 prefab을 원본으로 사용합니다.

```text
unity-app/Assets/Editor/BuildMainSceneEditor.cs
```

## 모델 자산

모델 바이너리는 Git에 포함하지 않습니다. 로컬 개발 또는 릴리스 빌드 전에 필수 모델 묶음 파일을 `python-engine/models/`에 복원합니다.

필수 모델:

```text
python-engine/models/face/yolo26x_rafdb_best.pt
python-engine/models/voice/best_wav2vec2_iemocap/config.json
python-engine/models/voice/best_wav2vec2_iemocap/model.safetensors
python-engine/models/voice/best_wav2vec2_iemocap/preprocessor_config.json
```

모델 묶음 복원:

```bash
tools/restore-model-assets.sh <path-to>/mouth-of-truth-models-required.tar.gz
```

Windows PowerShell:

```powershell
.\tools\restore-model-assets.ps1 -ModelBundlePath <path-to>\mouth-of-truth-models-required.tar.gz
```

로컬 모델 파일로 묶음 파일을 만들 때:

```bash
tools/package-model-assets.sh
```

생성 위치:

```text
dist/model-assets/mouth-of-truth-models-required.tar.gz
dist/model-assets/mouth-of-truth-models-required.tar.gz.sha256
```

Whisper 전사 캐시까지 묶음 파일로 만들 때:

```bash
MOUTH_OF_TRUTH_INCLUDE_WHISPER_CACHE=1 tools/package-model-assets.sh
```

선택 모델 묶음:

```text
dist/model-assets/mouth-of-truth-models-whisper-cache.tar.gz
dist/model-assets/mouth-of-truth-models-whisper-cache.tar.gz.sha256
```

모델 검사값과 교체 기준은 아래 문서에 정리되어 있습니다.

```text
docs/model-training-and-datasets.md
python-engine/models/README.md
python-engine/models/face/README.md
python-engine/models/voice/README.md
python-engine/models/whisper/README.md
```

## 포함된 실행 자산

아래 실행 자산은 Git에 포함됩니다.

```text
unity-app/Assets/StreamingAssets/art/
unity-app/Assets/StreamingAssets/audio/
unity-app/Assets/StreamingAssets/questions/question_pool.json
```

대표 파일:

```text
art/backgrounds/title_background_stone_wall.jpeg
art/cards/question_card_back.png
art/mouth/truth_mouth_face.png
art/verdict/verdict_true.png
art/verdict/verdict_false.png
art/verdict/verdict_uncertain.png
audio/ambience/title_temple_ambience_loop.wav
audio/questions/Q0001.wav
audio/questions/Q0012.wav
questions/question_pool.json
```

## Git에 넣지 않는 항목

```text
unity-app/Assets/ThirdParty/Environment/DungeonModularPack/
unity-app/Assets/ThirdParty/Environment/PersianCarpetUrp/
python-engine/models/face/yolo26x_rafdb_best.pt
python-engine/models/voice/best_wav2vec2_iemocap/
python-engine/models/whisper/models--openai--whisper-tiny/
python-runtime/
python-runtime-windows/
dist/
bridge/*.json
```
