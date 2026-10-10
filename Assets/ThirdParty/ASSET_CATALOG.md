# Environment asset catalog

The first fenced block is strict JSON so tooling can consume the pack, category,
license, representative-file, and Light Remembers relevance inventory. The notes
below add human-facing art and use guidance. Counts refer to imported FBX model
files, not every mesh/material subasset embedded in them.

```json
{
  "schema_version": 1,
  "project": "Light Remembers",
  "asset_root": "Assets/ThirdParty",
  "packs": [
    {
      "id": "quaternius_stylized_nature_standard",
      "display_name": "Quaternius Stylized Nature MegaKit (Standard)",
      "source_directory": "Stylized Nature MegaKit[Standard]",
      "selected_variant": "FBX (Unity)",
      "models_imported": 68,
      "textures_imported": 20,
      "license_file": "Quaternius/StylizedNatureMegaKit/License_Standard.txt",
      "license": "CC0 1.0 Universal (CC0 1.0) — Public Domain Dedication",
      "creator": "@Quaternius",
      "attribution_required_by_supplied_text": false,
      "categories": {
        "nature": ["CommonTree_1.fbx", "DeadTree_1.fbx", "Pine_1.fbx", "Bush_Common.fbx", "Clover_1.fbx", "Rock_Medium_1.fbx"]
      },
      "high_value_for_light_remembers": ["forest_paths", "island_environment", "ruined_memory_locations"]
    },
    {
      "id": "quaternius_medieval_village_standard",
      "display_name": "Quaternius Medieval Village MegaKit (Standard)",
      "source_directory": "Medieval Village MegaKit[Standard]/Medieval Village MegaKit[Standard]",
      "selected_variant": "FBX",
      "models_imported": 176,
      "textures_imported": 32,
      "license_file": "Quaternius/MedievalVillageMegaKit/License_Standard.txt",
      "license": "CC0 1.0 Universal (CC0 1.0) — Public Domain Dedication",
      "creator": "@Quaternius",
      "attribution_required_by_supplied_text": false,
      "categories": {
        "architecture": ["Wall_Plaster_Straight.fbx", "DoorFrame_Flat_Brick.fbx", "Floor_WoodDark.fbx", "Roof_RoundTiles_4x4.fbx", "Stairs_Exterior_Straight.fbx"],
        "props": ["Prop_Crate.fbx", "Prop_MetalFence_Simple.fbx", "Prop_WoodenFence_Single.fbx", "Prop_Vine1.fbx"]
      },
      "high_value_for_light_remembers": ["abandoned_village", "grandfathers_house", "lighthouse_workshop", "old_light_chambers", "childhood_memory_locations"]
    },
    {
      "id": "kaykit_forest_nature_free",
      "display_name": "KayKit Forest Nature Pack",
      "source_directory": "KayKit_Forest_Nature_Pack_1.0_FREE/KayKit_Forest_Nature_Pack_1.0_FREE",
      "selected_variant": "Assets/fbx(unity)",
      "models_imported": 105,
      "textures_imported": 1,
      "documentation_images_imported": 1,
      "license_file": "KayKit/ForestNaturePack/License.txt",
      "license": "Creative Commons Zero (CC0); CC0 1.0 Universal dedication linked in supplied text",
      "creator": "Kay Lousberg",
      "attribution_required_by_supplied_text": false,
      "categories": {
        "nature": ["Tree_1_A_Color1.fbx", "Tree_2_A_Color1.fbx", "Tree_Bare_1_A_Color1.fbx", "Bush_1_A_Color1.fbx", "Grass_1_Mesh.fbx", "Rock_1_A_Color1.fbx", "Rock_2_A_Color1.fbx"]
      },
      "high_value_for_light_remembers": ["forest_paths", "island_environment", "ruined_memory_locations"]
    }
  ],
  "relevance_notes": {
    "high_value": ["forest_paths", "island_environment", "abandoned_village", "grandfathers_house", "lighthouse_workshop", "old_light_chambers", "childhood_memory_locations"],
    "no_dedicated_models_identified_by_name": ["cliffs", "shoreline", "docks", "boats", "lighthouse"]
  }
}
```

## Nature

- **Quaternius Stylized Nature:** 68 Standard models (the supplied license says
  68/116), with stylized trees, bushes, clover, and rocks. The forms are graphic
  and readable; strong candidates for island dressing, forest paths, and memory
  locations. HIGH VALUE FOR LIGHT REMEMBERS: `Pine_1.fbx`, `CommonTree_1.fbx`,
  `DeadTree_1.fbx`, `Bush_Common.fbx`, and `Rock_Medium_1.fbx`.
- **KayKit Forest Nature:** 105 models share one atlas texture. Colorful,
  low-poly tree, bush, grass, and rock variants; useful for readable woodland
  paths and a warmer childhood-memory palette. HIGH VALUE FOR LIGHT REMEMBERS:
  `Tree_1_A_Color1.fbx`, `Tree_2_A_Color1.fbx`, `Bush_1_A_Color1.fbx`,
  `Rock_1_A_Color1.fbx`, and `Grass_1_Mesh.fbx`.

## Village / architecture

The 176-model Quaternius Medieval Village pack is modular plaster/brick/wood
architecture. Representative files include `Wall_Plaster_Straight.fbx`,
`DoorFrame_Flat_Brick.fbx`, `Floor_WoodDark.fbx`,
`Roof_RoundTiles_4x4.fbx`, and `Stairs_Exterior_Straight.fbx`. HIGH VALUE FOR
LIGHT REMEMBERS: wall, doorway, floor, roof, and stair modules for an abandoned
village, the grandfather's house, lighthouse workshop support spaces, Old Light
chambers, and their remembered/restored variants.

## Props and boundaries

Representative modular detail includes `Prop_Crate.fbx`,
`Prop_MetalFence_Simple.fbx`, `Prop_WoodenFence_Single.fbx`, and
`Prop_Vine1.fbx`. These help dress village edges and routes. The inspected pack
filenames do not identify dedicated cliffs, shoreline, dock, boat, or lighthouse
models; the nature rocks are only general-purpose dressing candidates, not a
replacement for purpose-built coastal geometry.

## Gallery and game-owned examples

`Assets/_Game/Scenes/Tests/EnvironmentAssetGallery.unity` is a non-gameplay
comparison scene with side-by-side sections: A — Quaternius Stylized Nature,
B — Quaternius Medieval Village, C — KayKit Forest Nature. Twenty sample
models are shown at their source scale with labels and neutral lighting. The
medieval section contains the wall, doorway, floor, roof, stairs, and three props
(crate plus two fence variants).

The only game-owned vendor wrappers created for this workflow are:

- `Assets/_Game/Prefabs/Environment/Samples/LR_SampleTree.prefab`
- `Assets/_Game/Prefabs/Environment/Samples/LR_SampleRock.prefab`
- `Assets/_Game/Prefabs/Environment/Samples/LR_SampleWall.prefab`

They reference imported model assets; they do not duplicate mesh data. The wall
and rock examples use simple BoxColliders, while the tree remains decorative.
