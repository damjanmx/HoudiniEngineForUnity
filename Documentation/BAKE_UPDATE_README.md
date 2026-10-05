# Update Prefab: Delete all baked data

The **Delete all baked data** toggle sits next to the Bake Update button and defaults to **off**, including on existing HDAs that do not yet store the setting. The choice is saved per HDA; it does not automatically reset after a click.

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
