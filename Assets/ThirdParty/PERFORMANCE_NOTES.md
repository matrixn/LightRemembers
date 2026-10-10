# Environment asset performance notes

Audit performed after Unity 6000.6.5f1/URP import. No source meshes or textures
were destructively optimized.

## Findings

- The selected vendor payload is approximately **151.2 MiB** (349 FBX, 54 PNG,
  three license files, and KayKit's one useful contents image). No retained file
  exceeds 50 MiB. The only retained file over 10 MiB is
  `Quaternius/MedievalVillageMegaKit/Textures/T_BrushedNoise.png` at 12.45 MiB.
- The 349 imported models contain approximately **301,423 triangles** in total;
  the largest single imported mesh is `TwistedTree_5.fbx` at 10,104 triangles.
  No imported model contains an LODGroup. This is a small graybox selection, not
  a substitute for target-scene profiling.
- Static import audit found no imported animation clips or SkinnedMeshRenderers.
  FBX importers use `importAnimation=false`, `animationType=None`,
  `avatarSetup=NoAvatar`, scale 1, imported normals, calculated MikkTSpace
  tangents, and no automatically generated colliders. Decorative assets therefore
  need deliberate, simple game-owned colliders only where gameplay requires them.
- There are 492 renderer material slots across the source models and 489 unique
  material references. Every inspected imported shader resolves to URP/Lit; no
  missing or unsupported shader was found. Seven renderer slots use the
  transparent `MI_WindowGlass` material; transparent foliage was not observed in
  the imported materials. Check glass overdraw when several windows overlap.
- Eighteen files named with the `_Normal` suffix are imported as Normal Maps;
  other textures remain color/data textures. Unity currently imports the largest
  texture at 2048 max size with compression and mipmaps enabled. The six
  Quaternius files under `Normals Godot-Unity` differ bytewise from the
  same-named root textures; they are retained as alternate variants. Imported
  materials currently resolve the root Unity normal maps, not both variants.
- Representative model scale remains at source `globalScale=1`. Observed tree
  heights vary from about 4.2 m (KayKit Tree 1) to 7.3 m (Quaternius Pine) and
  9.5 m (Quaternius Dead Tree); the modular plaster wall is about 3.1 m high.
  These differences are visible in the gallery and were not normalized away.
- The packs use several materials/textures and include texture variants; importing
  all retained PNGs costs more than the low-poly mesh geometry. The nature meshes
  shown in the gallery render with their atlas/material assignments. Some
  non-gallery medieval submaterials (for example glass and metal ornaments) have
  no dedicated base-color texture and use material color/other shader properties;
  that is not a missing shader, but should be checked when those props are used.

## Platform guidance

- **Windows:** The selected geometry is modest for prototype scenes. Keep the
  current 2048 import cap for iteration, then profile scene draw calls and shadow
  cost after dressing a real level. Avoid attaching colliders to every leaf or
  prop.
- **Steam Deck:** The absence of LODs is acceptable for the present graybox count,
  but add authored/impostor LODs or reduce distance/shadow settings before using
  dense forests or large village vistas. Watch transparent glass and shadow-casting
  foliage if added later.
- **Android:** These packs are not mobile-optimized as imported. Reassess texture
  resolution and platform compression (especially the 12.45 MiB source PNG),
  reduce simultaneous foliage/material count, and add LOD/culling before a mobile
  target. Do not change the master vendor imports until a platform-specific build
  and visual check justify overrides.

The gallery is a test-only scene and is not added to Build Settings. The project
must still choose when (or whether) to include these assets in gameplay scenes.
