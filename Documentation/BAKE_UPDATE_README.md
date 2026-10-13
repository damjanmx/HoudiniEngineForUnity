# Update Prefab: Delete All Baked Folders

The **Delete All Baked Folders** toggle sits directly below **Keep Previous Transform Values** in the Bake Update section and defaults to **off**, including on existing HDAs that do not yet store the setting. The choice is saved per HDA; it does not automatically reset after a click.

| Toggle | Behavior |
| --- | --- |
| Off (default) | Keeps the existing baked folders. Generates the new prefab output, creates new files, and overwrites matching file paths. Unrelated files and old outputs no longer generated are retained. |
| On | Uses the original plugin behavior: removes the Meshes, Materials, Textures and Terrain folders next to the target prefab before regenerating its output. |

This setting applies to prefab asset targets. Standalone GameObject updates keep their previous behavior. The prefab hierarchy itself is still rebuilt/replaced using the existing bake process.

Matching paths are overwritten even if their contents were edited manually. Renamed outputs create new files; old filenames are not cleaned up while the toggle is off. This is file overwrite behavior, not a guarantee of stable mesh subasset IDs or preservation of manual edits inside regenerated files.

The overwrite mode is scoped to the target prefab folder for the duration of the update and restored even if the update throws. Copied material/texture/terrain files are refreshed rather than silently reused. Existing metadata for copied files is retained. Generated mesh asset files continue through the plugin's native asset creation/overwrite path.

## Verification

C# parsing was checked with modern and legacy editor symbols and modern player symbols. Source checks covered the default, cleanup guard, file-copy overwrite path and scope restoration. Unity compilation, Inspector layout and actual baking must be verified in the target project.

To verify: bake a prefab, place an unrelated test asset in its Meshes folder, change the HDA geometry and material, and update twice with the toggle off. Check the outputs refresh, the unrelated asset remains and unused files remain. In a disposable bake folder, enable the toggle and confirm the original folder cleanup behavior.

## Bake To The Scene Folder

The **Bake To The Scene Folder** toggle sits in the **Bake Options** section directly below **Bake Update** and defaults to **off**. It is saved per HDA.

- **Off:** new prefab bakes use the existing HoudiniEngineAssetCache destination.
- **On:** new prefab bakes save under `HdaBakedData` beside the currently active saved Unity scene. For `Assets/Scenes/Level01.unity`, the destination is `Assets/Scenes/HdaBakedData/<asset folder>/`.
- The prefab and the usual generated resource folders (Meshes, Materials, Textures and Terrain as needed) share that asset folder. Resource generation is unchanged.
- Repeated new bakes use unique asset folder names, following the existing new-bake behavior.
- With multiple scenes open, the active scene determines the destination. Scenes in the same directory share `HdaBakedData`.
- Save the active scene under `Assets` first. An unsaved scene, package scene or Prefab Mode context without a saved active `.unity` scene cannot supply this destination; the bake is cancelled with a Console warning.
- **Update Prefab** updates its selected existing target in place, including prefabs already baked into a scene folder. It does not move existing prefabs. **Delete All Baked Folders** still controls resource cleanup there.
- Standalone bakes retain their existing behavior. An explicit destination supplied to `BakeToNewPrefab(destinationPrefabPath)` takes precedence over this setting.

The previous deletion label was **Delete all baked data**. Only its displayed label changed; its saved value and behavior are retained.

Scene-folder routing was source-checked; Unity compilation and actual prefab/resource baking still need verification in the Unity project.

## Scene-folder creation

The plugin creates `HdaBakedData` before choosing the unique asset folder. The normal bake process creates resource subfolders only when needed; it does not pre-create empty Meshes, Materials, Textures or Terrain folders.

The prefab button applies current Inspector settings before starting. Terrain updates preserve the selected bake destination. Use **Bake Prefab** for a new scene-folder bake; **Update Prefab** continues to update the existing target in place.

Empty folders created by the previous patch are not automatically removed. This update prevents new unused resource folders from being created.


## Recent bake workflow — 13 October 2026


## Bake foldout

Bake starts expanded on new HDAs. Existing HDAs receive a one-time expansion when this inspector version is first used. Afterward, the foldout respects your saved choice: collapsing it does not cause it to reopen on every repaint.

## Automatic prefab targets

After a new prefab is successfully saved, its project asset reference is appended to the originating HDA's bake target list. Existing targets are preserved. A duplicate reference to the same prefab is not appended.

A new bake that produces a different prefab adds another target. The update button processes the list, so review it when you have baked several variants. Failed new bakes do not add a target. Baking a standalone GameObject does not use this new automatic prefab-registration behavior.

## Add _prefab Suffix

Location: Bake → Bake Options.

Default: ON. This is a persistent setting, not a toggle that resets after every bake.

When ON, a new prefab named from `Bridge` is saved as `Bridge_prefab.prefab`, with root GameObject name `Bridge_prefab`. A base name already ending in the exact lowercase `_prefab` suffix is not suffixed a second time.

When OFF, new prefab naming follows the previous behavior: the filename uses the sanitized HDA asset name, while the baked root follows the existing baked-root naming convention.

The option affects new prefab naming. It does not rename existing prefab files or add suffixes to materials, meshes, bake folders, or child outputs. Updating a prefab keeps its existing destination filename. Toggling the option after baking is not a rename operation.

## Output GameObject names

Prefab baking preserves the names of cooked output objects instead of adding `_bakedClone` to part roots. This applies to new prefab bakes and updates targeting prefab assets in the Project window.

For example, a cooked output named `sphere_01` remains `sphere_01` within the baked prefab. Hierarchy and instance names already generated by attributes are copied with the output hierarchy.

The top-level prefab root still follows prefab root naming rules. In particular, a single output promoted to the prefab root receives the root name rather than keeping a separate child name. Add _prefab Suffix controls new prefab root naming as described above.

Mesh `(Clone)` names remain unchanged; this change concerns GameObjects. Standalone baking and scene-object updates retain the suffix-based matching behavior used by that path. Existing baked objects change only when baked again or updated through the applicable prefab path.

## Project prefab update and reimport

For a prefab asset selected as an update target, the updated workflow:

1. Captures the destination path and builds the new baked hierarchy.
2. Saves generated mesh and material assets.
3. Saves the rebuilt prefab to the existing destination.
4. Saves and refreshes the asset database.
5. Forces a synchronous reimport of the destination prefab.
6. Reloads the prefab before sending the successful baked event.

The purpose of the explicit reimport is to resolve stale imported references after resource overwrites. A general asset database refresh alone did not solve the reported issue. Manual Reimport did restore references in the user's test.

This change does not redesign resource overwriting or guarantee stable mesh subasset IDs for every case. Test repeated updates and references from scene instances in your project.

## Interaction with existing options

- Delete All Baked Folders continues to select the existing delete-versus-overwrite behavior. The reimport occurs after the prefab update in either mode.
- Bake To The Scene Folder continues to control the destination for new prefabs. The naming toggle can be used with either destination mode.
- Keep Previous Transform Values retains its existing behavior.
- Custom attributes, groups, profiles, and root assignments are not redefined by these changes.
- Updating a scene object uses the standalone update path; the new Project-prefab reimport step applies to prefab asset targets.


## Custom asset name suffix

See [unity_asset_name_suffix usage and verification](UNITY_ASSET_NAME_SUFFIX_README.md). This addition needs a new Unity check following the previously completed test round.


## Add Prefab To Scene — 13 October 2026

**Add Prefab To Scene** appears directly below **Add _prefab Suffix** in Bake Options and defaults to **on**. The setting persists per HDA; it does not reset after each bake.

When you press **Bake Prefab**, a successful bake is instantiated as a connected prefab instance in the HDA's scene. It uses the baked transform and is placed under the same parent as the HDA, preserving world placement. The HDA root GameObject is then deactivated, hiding its generated children while retaining it for future edits. The new prefab instance is selected and the scene is marked modified.

Turn the setting off to bake the asset without placing it or deactivating the HDA. Failed/cancelled bakes do not deactivate it. Reactivate the HDA in the Hierarchy when you need to edit it again. Placement and HDA deactivation participate in editor Undo; the saved prefab asset is not deleted by that scene undo.

This behavior is exclusive to the Inspector's Bake Prefab button. Update Prefab, standalone baking and direct BakeToNewPrefab API calls do not place instances or deactivate the HDA. Automatic registration in the bake target list still points to the prefab asset.

### Verification

- Bake with the toggle on: one connected prefab appears, HDA becomes inactive, output stays in the same world position.
- Test a rotated/scaled parent and a single-part output with an offset; check transform preservation.
- Test a multi-scene setup: instance goes into the HDA's scene.
- Toggle off: bake creates only the asset and HDA stays active.
- Update Prefab: no extra scene instance and no HDA deactivation.
- Cancel/fail a bake: HDA remains active.
- Undo scene changes: instance is removed and HDA activity restored; saved asset remains.

Source checks completed; Unity runtime verification is required for this new behavior.
