# Third-Party Assets And SDKs

This project uses custom runtime assets together with third-party Unity content
and the Ultraleap hand-tracking SDK. This file records the final public-release
lineage and restore locations.

## Ultraleap Tracking

- Current package name: `com.ultraleap.tracking`
- Current project version: `7.3.0`
- Current embedded path: `unity-app/Packages/com.ultraleap.tracking/`
- License declared by package: Apache-2.0

Restore through OpenUPM if the embedded package is missing:

- Scoped registry name: `Ultraleap`
- Scoped registry URL: `https://package.openupm.com`
- Scope: `com.ultraleap`
- Package: `com.ultraleap.tracking`

Official sources:

- `https://github.com/ultraleap/UnityPlugin`
- `https://openupm.com/packages/com.ultraleap.tracking/`
- `https://docs.ultraleap.com/xr-and-tabletop/xr/unity/`

Leap Motion input also requires the Ultraleap Hand Tracking Software on the demo
machine. The Unity package alone does not provide the local tracking service.

## Unity Environment Assets

Imported source packages:

- Persiang Carpets URP
  `https://assetstore.unity.com/packages/3d/props/persiang-carpets-urp-261455`
- Dungeon Modular Pack
  `https://assetstore.unity.com/packages/3d/environments/dungeons/dungeon-modular-pack-295430`

Expected restore paths:

- `unity-app/Assets/ThirdParty/Environment/DungeonModularPack/`
- `unity-app/Assets/ThirdParty/Environment/PersianCarpetUrp/`

`Dungeon Modular Pack` is used by
`unity-app/Assets/Editor/BuildMainSceneEditor.cs` as scene-generation source
material. The curated product runtime primarily loads generated images and
audio from `unity-app/Assets/StreamingAssets/`.

## Runtime Art Assets

Runtime art is loaded from:

```text
unity-app/Assets/StreamingAssets/art/
```

Important product-facing paths:

- `backgrounds/title_background_stone_wall.jpeg`
- `backgrounds/stage_card_selection_generated.png`
- `backgrounds/stage_mouth_chamber_generated.png`
- `cards/question_card_back.png`
- `cards/question_card_front.png`
- `environment/floor_red_carpet_runner.png`
- `input/hand_pointer_cursor.png`
- `input/leap_motion_device.png`
- `input/ritual_hand_insert.png`
- `mouth/truth_mouth_face.png`
- `ui/logo_title_main.png`
- `verdict/verdict_true.png`
- `verdict/verdict_false.png`
- `verdict/verdict_uncertain.png`

## Runtime Audio Assets

Runtime audio is loaded from:

```text
unity-app/Assets/StreamingAssets/audio/
```

Important product-facing paths:

- `ambience/title_temple_ambience_loop.wav`
- `ui/button_confirm.wav`
- `cards/card_hover.wav`
- `cards/card_select.wav`
- `cards/card_reveal.wav`
- `interaction/hand_insert.wav`
- `interaction/hand_prompt.wav`
- `results/result_true.wav`
- `results/result_false.wav`
- `results/result_uncertain.wav`
- `questions/Q0001.wav` through `questions/Q0012.wav`

## Model Assets

Model binaries are intentionally not tracked in Git. See
`docs/final-release-guide-ko.md` and `python-engine/models/README.md` for the
required local layout.
