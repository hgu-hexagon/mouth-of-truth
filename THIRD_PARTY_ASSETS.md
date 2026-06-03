# 서드파티 자산과 런타임

## 포함 상태 요약

| 항목 | Git 포함 | 별도 설치/복원 | 비고 |
| --- | --- | --- | --- |
| Ultraleap Unity package `com.ultraleap.tracking` | 포함 | 선택 | Unity project integration용 package입니다. |
| Ultraleap Hand Tracking Software | 미포함 | 필요 | Leap Motion/Ultraleap camera를 실제로 쓰는 PC에 설치합니다. |
| Dungeon Modular Pack | 미포함 | 필요 | Unity Asset Store에서 라이선스 보유 계정으로 import합니다. |
| Persiang Carpets URP | 미포함 | 필요 | Unity Asset Store에서 라이선스 보유 계정으로 import합니다. |
| Runtime art/audio/question assets | 포함 | 불필요 | `unity-app/Assets/StreamingAssets/` 아래에 포함되어 있습니다. |
| Face/voice model binaries | 미포함 | 필요 | 모델 bundle을 받아 `python-engine/models/`에 복원합니다. |
| Whisper cache | 미포함 | 선택 | 전사 기능을 켤 때만 필요합니다. |
| Python runtime bundle | 미포함 | 릴리스 빌드 시 생성 | `python-runtime/`, `python-runtime-windows/`는 Git에 넣지 않습니다. |
| Build output | 미포함 | 릴리스 빌드 시 생성 | `dist/`는 Git에 넣지 않습니다. |

## Ultraleap

### Git에 포함된 항목

```text
unity-app/Packages/com.ultraleap.tracking/
unity-app/Packages/manifest.json
```

현재 프로젝트는 Ultraleap Unity package `7.3.0`을 embedded package로 사용합니다.

```text
"com.ultraleap.tracking": "file:com.ultraleap.tracking"
```

package 정보:

```text
name: com.ultraleap.tracking
version: 7.3.0
license: Apache-2.0
```

### 실행 PC에 별도 설치하는 항목

Leap Motion 또는 Ultraleap camera 입력을 사용하려면 실행 PC에 Ultraleap Hand
Tracking Software를 설치하고 tracking service를 실행합니다. Unity package만으로는
camera tracking service가 설치되지 않습니다.

공식 경로:

```text
https://docs.ultraleap.com/hand-tracking/getting-started
https://docs.ultraleap.com/hand-tracking/Hyperion/index.html
```

### Unity package를 다시 받아야 하는 경우

정상 clone에는 `unity-app/Packages/com.ultraleap.tracking/`이 포함됩니다. 해당
폴더가 빠진 경우에만 다시 받습니다.

GitHub release:

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

OpenUPM 목록에 `7.3.0`이 없으면 GitHub release package를 사용합니다.

## Unity Asset Store 환경 자산

아래 원본 자산은 Git에 포함하지 않습니다. Unity Asset Store 자산은 빌드된 제품에
embedded component로 사용할 수 있지만, 원본 asset file을 공개 source repository에
그대로 재배포하지 않습니다.

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
2. Unity Asset Store 또는 Package Manager `My Assets`에서 위 두 asset을 내려받습니다.
3. import 위치가 위 경로와 다르면 폴더 이름을 맞춥니다.
4. `Mouth Of Truth > Build Main Scene`을 실행합니다.
5. `Assets/Scenes/Main.unity`가 정상 저장되는지 확인합니다.

`BuildMainSceneEditor`는 Dungeon Modular Pack의 demo scene과 prefab을 source scene으로
사용합니다.

```text
unity-app/Assets/Editor/BuildMainSceneEditor.cs
```

## 모델 자산

모델 바이너리는 Git에 포함하지 않습니다. 로컬 개발 또는 릴리스 빌드 전에 required
model bundle을 `python-engine/models/`에 복원합니다.

필수 모델:

```text
python-engine/models/face/yolo26x_rafdb_best.pt
python-engine/models/voice/best_wav2vec2_iemocap/config.json
python-engine/models/voice/best_wav2vec2_iemocap/model.safetensors
python-engine/models/voice/best_wav2vec2_iemocap/preprocessor_config.json
```

모델 bundle 복원:

```bash
tools/restore-model-assets.sh <path-to>/mouth-of-truth-models-required.tar.gz
```

Windows PowerShell:

```powershell
.\tools\restore-model-assets.ps1 -ModelBundlePath <path-to>\mouth-of-truth-models-required.tar.gz
```

릴리스 담당자가 모델 bundle을 만들 때:

```bash
tools/package-model-assets.sh
```

생성 위치:

```text
dist/model-assets/mouth-of-truth-models-required.tar.gz
dist/model-assets/mouth-of-truth-models-required.tar.gz.sha256
```

Whisper 전사 cache까지 bundle로 만들 때:

```bash
MOUTH_OF_TRUTH_INCLUDE_WHISPER_CACHE=1 tools/package-model-assets.sh
```

선택 bundle:

```text
dist/model-assets/mouth-of-truth-models-whisper-cache.tar.gz
dist/model-assets/mouth-of-truth-models-whisper-cache.tar.gz.sha256
```

모델 checksum과 교체 기준은 아래 문서에 고정되어 있습니다.

```text
python-engine/models/README.md
python-engine/models/face/README.md
python-engine/models/voice/README.md
python-engine/models/whisper/README.md
```

## 포함된 runtime art/audio

아래 runtime 자산은 Git에 포함됩니다.

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
