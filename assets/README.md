# Homeland assets

Homeland intentionally uses external CC0 art for characters/clothes, vehicles, buildings and roads instead of shipping placeholder cubes as the intended visual style.

The runtime searches these folders recursively:

- `assets/ThirdParty/Quaternius/` — characters, outfits, vehicles and modular buildings.
- `assets/ThirdParty/Kenney/` — roads, city/building props and environment pieces.

Recommended packs for the first art pass:

- Quaternius **Universal Base Characters** (CC0) for generated civilian bodies/hair.
- Quaternius modular outfit packs (CC0) for civilian disguises and CISF clothing.
- Quaternius vehicle/building packs (CC0) for cars, trucks, village/bazaar structures.
- Kenney **City Kit (Roads)** and other CC0 city kits for roads/barriers/street dressing.

`HomelandAssets` prefers glTF/GLB when present and falls back to OBJ/FBX or Rivet primitive geometry. This lets the game run while asset packs are still being assembled, but the intended checkout should install the art packs.
