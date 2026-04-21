# Third-Party Assets

This project currently uses third-party placeholder audio with permissive
licenses. These assets are temporary runtime replacements until final sound
design is delivered.

## Audio Sources

### Kenney UI Audio

- Source: [Kenney UI Audio](https://kenney.nl/assets/ui-audio)
- License: CC0 1.0
- Runtime mappings:
  - `unity-app/Assets/StreamingAssets/audio/ui/button_confirm.ogg`
  - `unity-app/Assets/StreamingAssets/audio/cards/card_hover.ogg`
  - `unity-app/Assets/StreamingAssets/audio/cards/card_select.ogg`
  - `unity-app/Assets/StreamingAssets/audio/interaction/hand_pause.ogg`

### Kenney Impact Sounds

- Source: [Kenney Impact Sounds](https://kenney.nl/assets/impact-sounds)
- License: CC0 1.0
- Runtime mappings:
  - `unity-app/Assets/StreamingAssets/audio/interaction/hand_insert.ogg`

### Kenney Music Jingles

- Source: [Kenney Music Jingles](https://kenney.nl/assets/music-jingles)
- License: CC0 1.0
- Runtime mappings:
  - `unity-app/Assets/StreamingAssets/audio/cards/card_reveal.ogg`
  - `unity-app/Assets/StreamingAssets/audio/results/result_true.ogg`
  - `unity-app/Assets/StreamingAssets/audio/results/result_false.ogg`
  - `unity-app/Assets/StreamingAssets/audio/results/result_uncertain.ogg`

### OpenGameArt Dungeon Ambience

- Source: [Dungeon Ambience](https://opengameart.org/content/dungeon-ambience)
- License: CC0 1.0
- Runtime mappings:
  - `unity-app/Assets/StreamingAssets/audio/ambience/title_temple_ambience_loop.ogg`

## Imported Asset Store Content

The following Asset Store packages are already imported into this repository and
support the current Unity environment build pipeline.

- [Persiang Carpets URP](https://assetstore.unity.com/packages/3d/props/persiang-carpets-urp-261455)
- [Dungeon Modular Pack](https://assetstore.unity.com/packages/3d/environments/dungeons/dungeon-modular-pack-295430)

Current repository usage:

- `Dungeon Modular Pack` provides the source environment scene, stage prefabs, and
  materials consumed by `unity-app/Assets/Editor/BuildMainSceneEditor.cs`
- `Persiang Carpets URP` remains available in the project as imported third-party
  content, while the shipped runtime carpet presentation currently uses the curated
  product asset at `unity-app/Assets/StreamingAssets/art/environment/floor_red_carpet_runner.png`
