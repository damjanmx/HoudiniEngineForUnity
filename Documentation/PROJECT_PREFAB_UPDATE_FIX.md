# Project prefab update — 22.0.466 custom plugin

## Reported issue and evidence

Updating a prefab asset selected from Project generated resources but showed missing mesh/material references. Updating a scene target worked. The initial save-order patch did not resolve it. The user then confirmed that manually reimporting the prefab restored the references.

## Current behavior

1. Capture the destination prefab path before rebuilding resources.
2. Build the baked hierarchy and save generated resources before saving the prefab.
3. Save with SaveAsPrefabAsset at the existing destination.
4. Save and refresh the asset database.
5. Force a synchronous reimport of that prefab and reload its GameObject before reporting success.

Empty output, failed save and failed reload report failure. The explicit clone-stage refresh is deferred until after saving; other imports required by resource copying may still occur.

The automatic reimport reproduces the successful manual recovery step, but has not yet been confirmed in Unity. It does not redesign mesh asset overwriting or guarantee stable mesh subasset IDs.

## Related behavior

The cumulative source also preserves cooked prefab output names without adding `_bakedClone`. Mesh `(Clone)` names remain unchanged. New-prefab naming and target registration are described in [BAKE_UPDATE_README.md](BAKE_UPDATE_README.md). Existing prefab updates keep their destination filename. Standalone scene-object updates use their existing path.

## Verification

Use 01_baseline_mesh, bake a prefab, change its geometry, and update using the Project prefab target. Repeat without manual Reimport. Check MeshFilter, renderer and collider references, then reopen the scene/editor. Repeat with a scene instance and both resource-cleanup modes in disposable test folders. See [testing.md](testing.md) for the checklist.
