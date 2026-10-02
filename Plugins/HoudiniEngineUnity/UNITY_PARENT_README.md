For the latest cumulative installation list and attribute rules, see ATTRIBUTE_COMPATIBILITY_README.md.

For the current cumulative installation list, see UNITY_PATH_README.md.

# unity_parent: custom prefab instance hierarchy

For the supplied Houdini Engine for Unity v2 source (Houdini 22.0.429).

## Install

Back up your plugin folder. Replace these existing files with the files in this archive, preserving their existing .meta files:

- Scripts/Asset/HEU_PartData.cs
- Scripts/Core/HEU_Defines.cs

The archive also includes the rest of the original plugin, unchanged. Do not install a second copy alongside your existing plugin. Let Unity compile, then recook your HDA.

## Houdini setup

On the same output points that have unity_instance, create a single string point attribute named unity_parent. In a Point Wrangle:

```c
s@unity_parent = "parent_A/parent_B";
```

Each point can have a different value. Paths are relative to the generated part/output GameObject, not the scene root or HDA root. The plugin's existing output containers remain. Matching paths share empty GameObjects within that output; separate outputs do not merge their groups.

Examples:

| Value | Parent of the instance |
| --- | --- |
| parent_A | Output / parent_A |
| parent_A/parent_B | Output / parent_A / parent_B |
| parent_A/parent_C | Output / parent_A / parent_C |
| empty string | Output directly |

Names are case-sensitive. Leading, trailing, and repeated slashes are ignored. Other characters, including spaces and dots, are literal names; paths never traverse to existing scene objects. A slash is always a separator.

When a valid unity_parent attribute exists, it takes precedence over unity_split_attr for the whole output, including points whose path is empty. If absent or invalid, the existing hierarchy behavior is used. Only string point attributes of tuple size 1 are supported.

Generated groups use identity local transforms so point positions, rotations, and scales retain their existing meaning. They inherit the output's flags. Prefab creation and instance flag behavior use the original code. Generated groups are managed output: recooking removes and recreates them, including any manual children placed inside them.

## Baking and cleanup

Group references are serialized on HEU_PartData so cleanup can identify owned groups after an editor domain reload. ClearInstances removes owned groups from deepest to shallowest. Baking recursively processes these groups through the existing prefab instancing bake code.

## Verification status

Reviewed the patch against the original archive and checked the called API signatures in the supplied source. Only the two C# files above changed; original .meta files are preserved. No Unity editor or C# compiler is installed in the execution environment, so compilation, recooking, and baking have NOT been executed here.

## Unity acceptance checks

1. Create points with paths parent_A, parent_A/parent_B (twice), parent_A/parent_C, other/parent_B, and an empty string. Expect shared parents and distinct parent_B groups beneath different ancestors.
2. Confirm prefab connections and identical world transforms versus the original flat output; include rotated and nonuniformly scaled HDA roots and points with orient/pscale/scale.
3. Recook unchanged, then change paths and remove points. Verify no duplicate groups or stale branches. Remove unity_parent entirely and verify the original layout returns.
4. Save/reopen the scene or trigger script recompilation, then recook to verify serialized cleanup ownership.
5. Bake to a new GameObject and prefab, then update an existing bake after moving points between paths. Verify nested groups, connected prefab roots, and removal of stale groups.
6. Test missing unity_parent and unity_split_attr alone for unchanged behavior. Test both together for unity_parent precedence.
7. Verify prefab LODs, colliders, materials, and unity_use_instance_flags behave as before.


## Prefab instance names

String point attributes on the same points as `unity_instance`:

- `unity_instance_name`: exact instance root name (no generated index).
- `unity_instance_suffix`: appended verbatim to the original source prefab name.
- Both nonempty: custom name + suffix.
- Both absent or empty: existing plugin naming, including `instance_prefix`, is preserved.
- Custom naming takes precedence over `instance_prefix`. No separator is inserted.
- Attributes must be scalar strings on points. Invalid attributes are ignored with a warning.
- Applies to Unity asset path instances; packed geometry naming is unchanged.
- Only instance roots are renamed; prefab assets and child names are unchanged.

Example (Point Wrangle):
```c
s@unity_parent = "parent_A/parent_B";
s@unity_instance_name = "Oak";
s@unity_instance_suffix = "_large";
```
Result: `parent_A/parent_B/Oak_large`.
With an empty name and a prefab named `Tree`, the result is `Tree_large`.

Install: replace Scripts/Asset/HEU_PartData.cs and Scripts/Core/HEU_Defines.cs,
then recook. This package includes the previous unity_parent implementation.

Unity checks: test name-only, suffix-only, both, empty strings, and different
values on different points. Recook with changed values; bake nested groups and
check names and prefab links. Unity compilation/runtime tests were not available
in the editing environment.
