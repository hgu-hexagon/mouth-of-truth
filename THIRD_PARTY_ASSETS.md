# Third-Party Assets

This project uses third-party runtime assets and SDKs alongside custom project
assets. The files listed here are the active product-facing paths, not early
prototype placeholders.

## Imported Unity Asset Store Content

The following Asset Store packages are imported into the Unity project and are
used as source material for the generated release scene.

- [Persiang Carpets URP](https://assetstore.unity.com/packages/3d/props/persiang-carpets-urp-261455)
- [Dungeon Modular Pack](https://assetstore.unity.com/packages/3d/environments/dungeons/dungeon-modular-pack-295430)

Current repository usage:

- `Dungeon Modular Pack` provides the source dungeon environment, stage prefabs,
  and materials consumed by `unity-app/Assets/Editor/BuildMainSceneEditor.cs`.
- `Persiang Carpets URP` remains imported as third-party environment content.
  The shipped presentation currently uses the curated runtime carpet image at
  `unity-app/Assets/StreamingAssets/art/environment/floor_red_carpet_runner.png`.

## Hand Tracking SDK

- Ultraleap tracking package:
  - `unity-app/Packages/com.ultraleap.tracking/`
- Runtime requirement:
  - demo machines using Leap Motion input must have the Ultraleap tracking runtime installed and running.

The project code treats Leap Motion as one input adapter behind the common
hand-input contract. Mouse fallback remains available for software-only
development and validation.

## Runtime Art Assets

Primary runtime art is loaded from `unity-app/Assets/StreamingAssets/art/`.

Important paths:

- `backgrounds/title_background_stone_wall.jpeg`
- `backgrounds/stage_card_selection_generated.png`
- `backgrounds/stage_mouth_chamber_generated.png`
- `cards/question_card_back.png`
- `cards/question_card_front.png`
- `environment/floor_red_carpet_runner.png`
- `input/hand_pointer_cursor.png`
- `input/ritual_hand_insert.png`
- `mouth/truth_mouth_face.png`
- `ui/button_start_game.png`
- `ui/button_try_again.png`
- `ui/button_end_game.png`
- `ui/button_exit_icon.png`
- `ui/logo_title_main.png`
- `verdict/verdict_true.png`
- `verdict/verdict_false.png`
- `verdict/verdict_uncertain.png`

## Runtime Audio Assets

Runtime audio is loaded from `unity-app/Assets/StreamingAssets/audio/`.
The product path uses WAV files for low-friction runtime loading.

Important paths:

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

Question narration also keeps MP3 copies in the repository, but the runtime
service checks WAV first.

## Model and Dataset Lineage

The local model files are not committed to Git. The current model lineage is:

- face expression recognition: RAF-DB dataset, YOLO classification model
- voice emotion recognition: IEMOCAP dataset, wav2vec2-base fine-tuned model
- speech transcription support: Whisper-family model cache

The release path prioritizes fast face/voice evidence extraction. The heavier
trained voice model and Whisper transcription path remain available for
validation or extended analysis modes.
