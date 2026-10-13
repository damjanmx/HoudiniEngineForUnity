# Houdini Engine for Unity — Testing Checklist

Manual regression and acceptance tests for the official plugin and our custom fork. This is a test plan, not a record of completed tests. Unity compilation and live cook/bake testing must be performed in the target project.

## Test run

| Item | Value |
| --- | --- |
| Tester / date | |
| Unity version | |
| Houdini / Engine version | |
| Official baseline commit | |
| Custom fork commit | |
| OS / render pipeline | |
| Test scene / HDA versions | |
| Overall result | Not run |

Check an item only after it passes. Record failures, blocked tests and justified N/A results in the results table at the end. Include the test ID, Console output and screenshots where useful.

## 1. Setup and comparison

- [ ] **SET-01** Prepare separate official and custom Unity projects using identical Unity/Houdini versions and test assets. Use the official baseline corresponding to the custom fork, not an unrelated upstream release.
- [ ] **SET-02** Match generation settings before comparing geometry. Our changed defaults are intentional differences.
- [ ] **SET-03** Use disposable bake folders for deletion tests; keep a copy of the test project.
- [ ] **SET-04** Prepare mesh, prefab-instance, packed-instance, terrain/curve and multiparm test HDAs. Include a version with none of our custom attributes.
- [ ] **SET-05** Create the required Unity tags, layers, template prefabs, component profiles and test MonoBehaviours.

Run important cases through: **initial cook → change parameters → repeated recook → save/reopen → new bake → update bake**. Compare hierarchy, transforms, geometry, materials, colliders, components, flags and asset references.

The custom branch includes changes that execute even without custom attributes. Original-plugin regression tests are therefore required.

## 2. Original-plugin regression — both projects

Use HDAs without our custom attributes. Expect equivalent results after matching settings; record any unexplained differences.

- [ ] **REG-01** Load a simple mesh HDA, change parameters and recook: geometry and transforms match.
- [ ] **REG-02** Generate multiple mesh outputs: correct counts, no missing or duplicate objects.
- [ ] **REG-03** Test UV seams, normals, vertex colors, alpha and multiple material slots: visual result and submeshes match.
- [ ] **REG-04** Test normal, rendered, convex, trigger and simple collision groups: collider types and geometry match.
- [ ] **REG-05** Test LOD groups and screen sizes: structure and transitions match.
- [ ] **REG-06** Instance Unity prefabs: correct prefab, transforms, materials and connections.
- [ ] **REG-07** Generate packed instances, including several source parts: correct copies and prototype geometry.
- [ ] **REG-08** Test original tag, layer, static, script and stored-attribute handling.
- [ ] **REG-09** Edit mesh inputs, curves/splines and terrain: cooking and baking remain functional.
- [ ] **REG-10** Test ordinary multiparm add/remove/Clear and ramp controls.
- [ ] **REG-11** Bake new standalone objects and prefabs, then update both: valid hierarchy and resource references.
- [ ] **REG-12** Delete/rebuild one HDA: other HDAs, source assets and existing bakes remain intact.
- [ ] **REG-13** If used in production, test PDG/GeoSync workflows without custom attributes. Custom unity_path support is not expected there.

## 3. Instance naming and hierarchy — prefab and packed

Repeat the following for Unity prefab instances and packed instances.

- [ ] **INS-01** Set `unity_instance_parent = "A/B"`: correct nested hierarchy relative to the output.
- [ ] **INS-02** Several instances share `A/B`: one shared hierarchy. `A/B` and `C/B` create distinct B parents.
- [ ] **INS-03** Empty parent paths place instances directly under their output. Leading/trailing/repeated slashes do not create empty containers.
- [ ] **INS-04** Name only gives the exact name; suffix only appends to the source name; both concatenate without an automatic separator.
- [ ] **INS-05** Empty/absent naming attributes preserve the original naming fallback, including instance_prefix where applicable.
- [ ] **INS-06** Custom parent takes precedence over unity_split_attr; removing it restores original grouping.
- [ ] **INS-07** Change paths/names and reduce instance counts: no stale instances or empty branches.
- [ ] **INS-08** Rotate/nonuniformly scale the HDA and instances: generated parenting preserves intended transforms.
- [ ] **INS-09** Save/reopen or recompile scripts, then recook: cleanup ownership remains correct.
- [ ] **INS-10** Bake and update nested instances: hierarchy, names, transforms and prefab links survive.
- [ ] **INS-11** Packed point > primitive > detail precedence works. Empty specific values do not fall back to detail.
- [ ] **INS-12** Packed attributes with mismatched transform counts warn and skip the invalid owner instead of indexing incorrectly.
- [ ] **INS-13** Test multiple prototype parts per transform and nested packed instances. Repeated exact names across parts are permitted.
- [ ] **INS-14** Removing a packed copy does not destroy a shared prototype mesh.

## 4. unity_path mesh outputs

- [ ] **PATH-01** `sphere_01` names the generated output correctly.
- [ ] **PATH-02** `parent/child/sphere_01` creates the requested hierarchy.
- [ ] **PATH-03** Shared parents are reused; distinct paths remain separate mesh outputs.
- [ ] **PATH-04** Primitives with the same path combine into the corresponding output.
- [ ] **PATH-05** Shared points, UV seams, normals, colors and multiple material slots survive splitting.
- [ ] **PATH-06** Change/remove paths: no stale meshes or branches remain.
- [ ] **PATH-07** Change a former mesh leaf into a parent container: no leftover renderer, collider or owned script remains on the container.
- [ ] **PATH-08** Bake/update after path changes: hierarchy and resource references are correct.

Path-generated children are rebuilt on recook. Preservation of manual child edits is not an expected result.

## 5. Existing attributes and groups with custom outputs

Use two different outputs under a shared parent.

- [ ] **ATTR-01** Different tags, layers, static flags and scripts stay assigned to their own objects, not siblings or shared containers.
- [ ] **ATTR-02** Detail defaults work; more specific primitive/point values override them according to output ownership rules.
- [ ] **ATTR-03** An explicitly empty string does not silently fall back to detail.
- [ ] **ATTR-04** Conflicting object-level values within one mesh output warn and use the first source value.
- [ ] **ATTR-05** hengine_attr_store filters integer, float and string tuples to the output's original source indices.
- [ ] **ATTR-06** unity_mesh_readable works independently on separate generated meshes without modifying imported prefab meshes.
- [ ] **ATTR-07** Per-face materials/submeshes remain correct after splitting.
- [ ] **ATTR-08** Collision-only and rendered collision groups remain correct; repeated collider types are retained independently.
- [ ] **ATTR-09** Test LODs and collision groups together with unity_path.
- [ ] **ATTR-10** Different prefab points receive independent material and collision-asset overrides within existing plugin restrictions.

Packed instances retain prototype materials and collision geometry. New prefab-style replacement behavior is not expected for packed instances.

## 6. unity_use_instance_flags

Use a source prefab with different child tags, layers and individual static flags; repeat applicable cases for packed instances.

- [ ] **FLAG-01** With value 1, source root/child flags survive, including individual static masks.
- [ ] **FLAG-02** Conflicting instance tag/layer/static attributes do not override preserved flags.
- [ ] **FLAG-03** With value 0, supported attribute flag overrides apply.
- [ ] **FLAG-04** Names, hierarchy, materials, scripts, profiles and bindings still work with value 1.
- [ ] **FLAG-05** New bake and Update Prefab preserve the expected flags.
- [ ] **FLAG-06** An explicit unity_layer_root overrides the baked root and all descendant layers even when instance flags were preserved.

## 7. Defaults, bake settings and destinations

- [ ] **BAKE-01** New HDAs default to Generate Tangents off and Generate Mesh Using Points on; new spline inputs default to Sampling Resolution 12. Existing serialized settings remain unchanged.
- [ ] **BAKE-02** Delete All Baked Folders and Bake To The Scene Folder default off.
- [ ] **BAKE-03** Inspector layout: deletion toggle below Keep Previous Transform Values in Bake Update; scene-folder toggle in Bake Options beneath it.
- [ ] **BAKE-04** Scene-folder mode off: new prefab uses the normal cache destination.
- [ ] **BAKE-05** Scene-folder mode on with no destination yet: HdaBakedData is created beside the active saved scene before baking.
- [ ] **BAKE-06** Only necessary resource subfolders are created. Mesh-only output does not pre-create empty texture/material/terrain folders unless resources actually require them.
- [ ] **BAKE-07** Repeated new prefab bakes get unique asset folders and preserve previous bakes.
- [ ] **BAKE-08** With multiple scenes open, the active scene determines the destination. Scenes in one directory share HdaBakedData.
- [ ] **BAKE-09** Unsaved active scene: scene-folder bake cancels with a warning.
- [ ] **BAKE-10** Updating an existing prefab keeps its original location, even when scene-folder mode is enabled.
- [ ] **BAKE-11** Terrain bake/update preserves the selected destination and valid TerrainData/TerrainLayer references.
- [ ] **BAKE-12** An explicit BakeToNewPrefab destination supplied by code takes precedence over the scene-folder toggle.
- [ ] **BAKE-13** Standalone bakes retain their existing destination behavior.
- [ ] **BAKE-14** Keep Previous Transform Values on/off produces its documented update behavior, including LOD transforms.

### Deletion off

- [ ] **FILES-01** Add an unrelated test asset to a baked resource folder; change geometry/materials/textures and update twice. Matching resources refresh and the test asset remains.
- [ ] **FILES-02** Rename/remove output resources: unused files may remain, but the prefab references current resources.
- [ ] **FILES-03** Save/reopen and inspect references: no missing mesh, material, texture or terrain assets.

### Deletion on — disposable folders only

- [ ] **FILES-04** Original baked-resource folder cleanup occurs, followed by successful regeneration.
- [ ] **FILES-05** Neighboring bake folders and unrelated assets outside the target resource folders are untouched.
- [ ] **FILES-06** Toggle selection persists as implemented: default-off, not automatically reset after each click.

Copied-file metadata retention does not guarantee stable generated mesh subasset IDs. Record broken references rather than assuming file overwrite preserves every reference.

## 8. Multiparm asset drag and drop

Use two independent multiparms with one scalar heuassetpath field and a Weight field each.

- [ ] **MULTI-01** Drop several assets into an empty list: one entry per distinct asset path, with correct paths.
- [ ] **MULTI-02** Drop into a populated list: append entries and preserve existing values.
- [ ] **MULTI-03** New Weight values use HDA defaults; the other multiparm remains unchanged.
- [ ] **MULTI-04** Folder and scene-object drops are ignored. Duplicate paths within one batch produce one entry; a later batch can add the same asset again.
- [ ] **MULTI-05** Automatic cooking applies the batch; with automatic cooking off, Recook applies it.
- [ ] **MULTI-06** Ordinary add/remove/Clear controls still work afterward.
- [ ] **MULTI-07** Unsupported empty multiparm templates warn and roll back their new entries without removing old entries.
- [ ] **MULTI-08** Save/reopen: assigned paths remain correct.

## 9. Baked root attributes

Test both single-output and multiple-output HDAs.

- [ ] **ROOT-01** unity_layer_root sets the final root and every descendant, including inactive children.
- [ ] **ROOT-02** unity_script_root attaches scripts to the final root only. Test multiple scripts and existing invocation syntax.
- [ ] **ROOT-03** Missing/empty values preserve normal behavior; wrong type/owner and invalid names are handled without crashing.
- [ ] **ROOT-04** Conflicting values across outputs use the first non-empty value and warn.
- [ ] **ROOT-05** Change values, then Update Prefab: new values are applied.
- [ ] **ROOT-06** Live HDA outputs and standalone bakes are unaffected by root-only attributes.

## 10. Component profiles

Use Rigidbody, AudioSource and two custom scripts with references to each other and to a project AudioClip.

- [ ] **CP-01** Create a profile, select template components, save/reopen: selections persist.
- [ ] **CP-02** New profiles default to Overwrite Settings; existing saved profiles retain their mode.
- [ ] **CP-03** Keep Existing preserves current settings and adds missing components.
- [ ] **CP-04** Overwrite Settings refreshes selected settings and adds missing components.
- [ ] **CP-05** Repeated recooks do not create unintended duplicate components. Test multiple selected components of the same supported type.
- [ ] **CP-06** References between selected components, including circular references, point to the generated counterparts.
- [ ] **CP-07** References to the template root/Transform are remapped; project asset references are retained.
- [ ] **CP-08** Template-child, unselected-component and scene references are rejected with useful warnings.
- [ ] **CP-09** Protected geometry components and unsupported dependencies cannot replace Houdini-managed output components.
- [ ] **CP-10** Changing the template clears component selections and bindings.
- [ ] **CP-11** Separate mesh outputs, prefab instances and packed instances receive their own profiles.
- [ ] **CP-12** unity_component_profile_root applies to the final baked root during new bake and update.
- [ ] **CP-13** Baked components do not retain references to live HDA objects or template counterparts. Unsupported scene references warn when cleared.
- [ ] **CP-14** Source template prefabs remain unchanged.

Profiles are additive. Removing a profile or selection does not automatically delete components from an output that survives recooking.

## 11. Component property bindings

For each conversion, confirm the property in the generated object and again after bake/update.

- [ ] **BIND-01** Float/double and integer fields receive valid numeric values within supported source precision.
- [ ] **BIND-02** Boolean accepts integer 0/1; string and single-character fields receive the intended text.
- [ ] **BIND-03** Enum accepts exact member names and raw integer/flag values; LayerMask receives a bitmask.
- [ ] **BIND-04** Vector2/3/4 and integer vectors receive matching tuple sizes without unintended coordinate conversion.
- [ ] **BIND-05** RGB uses alpha 1; RGBA preserves alpha; HDR color values are not clamped.
- [ ] **BIND-06** Quaternion XYZW normalization and optional Euler-degree conversion work; invalid values warn.
- [ ] **BIND-07** Rect/RectInt and Bounds/BoundsInt use their documented tuple ordering.
- [ ] **BIND-08** Asset paths resolve to the correct type; empty paths clear references; missing/wrong-type assets warn.
- [ ] **BIND-09** Fixed tuples populate scalar and Vector3 lists with the configured Array Element Size.
- [ ] **BIND-10** Invalid list element conversion skips the entire binding without partially resizing/modifying the list.
- [ ] **BIND-11** Nested serialized fields work; unsupported nonserialized properties are not offered as valid bindings.
- [ ] **BIND-12** unity_cp_ prefix and non-empty suffix are enforced.
- [ ] **BIND-13** Bindings override profile values even with Keep Existing; later bindings to the same property win.
- [ ] **BIND-14** Detail profile plus different primitive values produces different values on separate unity_path outputs.
- [ ] **BIND-15** Prefab and packed instance points receive independent values with the existing ownership rules.
- [ ] **BIND-16** Root bindings read detail only. unity_cp_root_ is a naming convention, not a target selector.
- [ ] **BIND-17** Missing values leave the profile-applied/current value intact. Invalid types/sizes warn without blocking other bindings.
- [ ] **BIND-18** Conflicting values within one output or across root detail sources warn and use the documented first value.
- [ ] **BIND-19** Changing/removing attributes and recooking follows Keep Existing versus Overwrite Settings behavior; no automatic undo of old values is assumed.
- [ ] **BIND-20** Bindings remain correct through Bake Prefab and Update Prefab, including Keep Existing.

Lists use fixed-size tuples, not variable-length VEX array attributes. Source transport currently supports HAPI 32-bit integers/floats and strings, not full 64-bit precision. Root script invocation occurs after root profile bindings and may intentionally change values.

## 12. Combined tests and lifecycle

Build a stress-test HDA with two unity_path meshes, different materials/collision groups, prefab and packed instances, nested parents, both instance-flag modes, profiles/bindings and root attributes.

- [ ] **LIFE-01** Complete the full cook/change/recook/reopen/bake/update cycle using scene-folder baking.
- [ ] **LIFE-02** Change paths, instance counts, materials and bound values together: no stale objects, duplicate components or cross-output assignments.
- [ ] **LIFE-03** Recompile scripts/reload the editor, then recook and update: serialized ownership and profile settings remain valid.
- [ ] **LIFE-04** Repeat cook/update several times: stable object/component counts and no unexpected resource growth beyond intentional retained files/new-bake folders.
- [ ] **LIFE-05** Check the Console: no unexpected errors or repeated warning floods on valid input.
- [ ] **LIFE-06** Verify source prefabs, neighboring HDAs and unrelated project assets were not modified.
- [ ] **LIFE-07** Build and run a player containing baked outputs: no editor-only API compilation failures or missing baked components/resources.
- [ ] **LIFE-08** If supporting both, compile in Unity before 6.6 and Unity 6.6+. Test socket/pipe session creation and custom script type lookup in each.

## Results and issues

| Test ID | Official result | Custom result | Actual behavior / Console output | Evidence / issue | Retest |
| --- | --- | --- | --- | --- | --- |
| | Not run | Not run | | | |

Use **Pass**, **Fail**, **Blocked** or **N/A** with a reason. Custom-only tests normally have Official result = N/A. Report shared failures separately from regressions introduced by the custom branch.

For a failure, record the smallest reproducing HDA, relevant attribute values, steps, expected/actual result and project versions. Retest the failing case and its directly related regression cases after a fix.

## Recommended order

1. Compilation, ordinary mesh cook and official-versus-custom comparison.
2. Scene-folder bake, file retention/deletion and prefab update.
3. Instance names/parents, unity_path, collision/LOD compatibility and instance flags.
4. Root attributes, profiles and property bindings.
5. Multiparm helper, persistence, combined stress test and player build.

Do not mark the run complete while required cases remain unrun or failures remain unexplained.


---

# Recent bake changes — testing checklist

Use a small HDA first, then an HDA with multiple outputs, unity_path hierarchy, and named instances. Record Unity and Houdini versions plus the result for each check. No tests in this document have been run in Unity by the patch author.

## Current evidence

- [x] User confirmed removal of added `_bakedClone` names.
- [x] User confirmed manual prefab Reimport restores the missing mesh/material links.
- [ ] Confirm the automatic reimport achieves the same result.
- [ ] Confirm the newest Bake inspector and suffix options.

## Inspector and settings

- [ ] Drop a new HDA into the scene: Bake starts expanded.
- [ ] Inspect an existing HDA: Bake expands once with the new version.
- [ ] Collapse Bake, deselect and reselect: it respects the saved choice.
- [ ] Add _prefab Suffix appears under Bake Options and starts ON.
- [ ] Disable the toggle, save and reopen the scene: the chosen value persists.

## New prefab naming

- [ ] Bake with suffix ON: filename and prefab root end in `_prefab`.
- [ ] Bake an asset whose name already ends in `_prefab`: no repeated suffix.
- [ ] Bake with suffix OFF: previous filename/root naming behavior is used.
- [ ] Confirm meshes, materials and child outputs do not acquire `_prefab` from this option.
- [ ] Repeat with Bake To The Scene Folder OFF and ON in a saved scene.

## Automatic update targets

- [ ] Bake a new prefab: its asset reference appears in the originating HDA's target list.
- [ ] Start with an existing target and bake another prefab: both remain listed.
- [ ] Bake several variants: verify each distinct prefab appears once.
- [ ] Save and reopen the scene: target references persist.
- [ ] Remove unwanted targets and confirm Update processes only the remaining list.
- [ ] If a new bake fails, verify that it does not add an invalid target.

## Project prefab updates — priority regression

- [ ] Use 01_baseline_mesh, bake a prefab, change geometry, and cook.
- [ ] Update with the prefab asset as target: mesh/material references remain valid without manual Reimport.
- [ ] Repeat updates at least three times with changed geometry and materials.
- [ ] Open the prefab and inspect MeshFilter, MeshRenderer and any MeshCollider references.
- [ ] Place a prefab instance in the scene and verify it displays the updated geometry/materials.
- [ ] Save, close and reopen the scene/project: references remain valid.
- [ ] Test Delete All Baked Folders OFF and ON using disposable test bake folders.
- [ ] Confirm existing prefab destination filename is unchanged by toggling Add _prefab Suffix before Update.
- [ ] Check the Console for errors after each update.

## Names and compatibility

- [ ] Multi-output prefab children retain cooked names without an added `_bakedClone`.
- [ ] unity_path hierarchy and unity_instance_name/unity_instance_suffix results survive baking and updating.
- [ ] An intentional source name ending in `_bakedClone` is preserved exactly.
- [ ] Single-output prefab root follows root naming rules.
- [ ] Mesh `(Clone)` names remain as before.
- [ ] Standalone scene-object bake/update still works.
- [ ] Keep Previous Transform Values still behaves as expected.
- [ ] Tags, layers, collision groups and component profiles survive the bake/update cycle.

## If references still break

Record whether manual Reimport fixes them, whether the target came from Project or Hierarchy, and the two bake-option states. Capture Console errors and retain the generated prefab, meshes, materials and their .meta files together for diagnosis.


## Instance Inputs — 13 October 2026

New Instance Inputs override lists now start empty. They no longer automatically add the original source object as an override. The generated instances continue using the source specified by the HDA when no overrides are present. Add an override manually when you want to substitute another object.

The default-slot feature for Input Nodes remains unchanged: newly created input nodes still start with one slot. Existing saved instance override entries are preserved; remove unwanted entries manually or use a fresh HDA instance to check the new default.

### Verification

- Create a fresh 05_baseline_prefab, assign a valid Unity Prefab and recook. Expect three instances and an empty Instance Inputs override list.
- Recook again: the list remains empty and instances are still generated.
- Add an instance override manually and recook: the replacement is used. Remove it and recook: the original HDA source is used again.
- Check packed/object instancing with no overrides and with an explicitly added override.
- Create a fresh HDA with input nodes: one input slot is still present by default.
- Save/reopen an HDA with explicit overrides: they remain intact.

Source paths were reviewed; Unity compilation and runtime verification are still required.


## Serialized settings copy fix — 13 October 2026

Fixed the `Invalid iteration` error when applying a component profile. The copier previously called `Next(false)` on a fresh SerializedProperty iterator. It now enters the first property with `Next(true)`, then advances across top-level fields with `Next(false)`. Nested values and arrays are copied with their containing property; the existing recursive reference-remapping traversal is retained.

This addresses components being added with default values rather than the template settings. It applies to the shared profile application path used during cooking and baking. Existing-component policy and attribute-binding precedence are unchanged.

### Retest with 20_profiles

1. Set the profile to Overwrite Settings so components already created with defaults are refreshed.
2. Temporarily disable the unity_cp_mass attribute in the HDA to isolate template copying.
3. Set template Rigidbody Mass to 3 and Use Gravity off, save the template, then recook.
4. Confirm all three generated Rigidbodies have Mass 3 and Use Gravity off, with no new Invalid iteration error.
5. Re-enable unity_cp_mass with a binding to Rigidbody mass: expect 2, 7 and 12, while Use Gravity stays off.
6. Recook again, bake a new prefab, then update it. Check values and absence of duplicate components.
7. Check a custom script with nested/array values and component references to verify copying and remapping still work.

Source review confirms the other profile iterator loops already start with Next(true). Unity compilation and live execution remain to be verified in the user's project. Keep Existing intentionally preserves existing component settings; use Overwrite Settings or a fresh output when retesting this fix.


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
