# 서드파티 자산과 SDK

## Ultraleap Tracking

```text
패키지명: com.ultraleap.tracking
버전: 7.3.0
경로: unity-app/Packages/com.ultraleap.tracking/
라이선스: Apache-2.0
Unity manifest: "com.ultraleap.tracking": "file:com.ultraleap.tracking"
```

현재 저장소에는 Ultraleap package가 embedded package로 포함되어 있습니다.
패키지를 다시 받아 같은 상태로 복원할 때는 `Unity Release 7.3.0`의
`com.ultraleap.tracking` UPM package를 아래 경로에 둡니다.

```text
unity-app/Packages/com.ultraleap.tracking/
```

OpenUPM 설치:

```text
Scoped Registry Name: Ultraleap
Scoped Registry URL: https://package.openupm.com
Scope: com.ultraleap
Package: com.ultraleap.tracking
```

OpenUPM 목록에 `7.3.0`이 없으면 GitHub release 방식으로 복원합니다.

공식 경로:

```text
https://github.com/ultraleap/UnityPlugin/releases/latest
https://github.com/ultraleap/UnityPlugin
https://openupm.com/packages/com.ultraleap.tracking/
https://docs.ultraleap.com/xr-and-tabletop/xr/unity/
```

Leap Motion 입력을 사용할 장비에는 Ultraleap Hand Tracking Software를 설치합니다.

## Unity Asset Store 자산

Dungeon Modular Pack:

```text
https://assetstore.unity.com/packages/3d/environments/dungeons/dungeon-modular-pack-295430
unity-app/Assets/ThirdParty/Environment/DungeonModularPack/
```

Persiang Carpets URP:

```text
https://assetstore.unity.com/packages/3d/props/persiang-carpets-urp-261455
unity-app/Assets/ThirdParty/Environment/PersianCarpetUrp/
```

`BuildMainSceneEditor`는 Dungeon Modular Pack의 DemoScene과 prefab을 원본으로
사용합니다.

```text
unity-app/Assets/Editor/BuildMainSceneEditor.cs
```

## 런타임 이미지

```text
unity-app/Assets/StreamingAssets/art/
```

주요 파일:

```text
backgrounds/title_background_stone_wall.jpeg
backgrounds/stage_card_selection_generated.png
backgrounds/stage_mouth_chamber_generated.png
cards/question_card_back.png
cards/question_card_front.png
environment/floor_red_carpet_runner.png
input/hand_pointer_cursor.png
input/leap_motion_device.png
input/ritual_hand_insert.png
mouth/truth_mouth_face.png
ui/logo_title_main.png
verdict/verdict_true.png
verdict/verdict_false.png
verdict/verdict_uncertain.png
```

## 런타임 음성

```text
unity-app/Assets/StreamingAssets/audio/
```

주요 파일:

```text
ambience/title_temple_ambience_loop.wav
ui/button_confirm.wav
cards/card_hover.wav
cards/card_select.wav
cards/card_reveal.wav
interaction/hand_insert.wav
interaction/hand_prompt.wav
results/result_true.wav
results/result_false.wav
results/result_uncertain.wav
questions/Q0001.wav
...
questions/Q0012.wav
```

## 모델 자산

모델 바이너리는 Git에 포함하지 않습니다.

```text
python-engine/models/face/yolo26x_rafdb_best.pt
python-engine/models/voice/best_wav2vec2_iemocap/
python-engine/models/whisper/models--openai--whisper-tiny/
```

모델 배치 기준:

```text
python-engine/models/README.md
```
