# Duplicate HDA state

Updated 16 October 2026. Based on Houdini Engine for Unity 22.0.466. Unity/Houdini verification pending.

## Usage

Configure an HDA and press its inspector **Duplicate** button. The duplicate uses the same HDA definition, parameter preset and input references. It is placed at the same transform, under the same parent and in the same scene; its root uses the duplicate's own generated Houdini asset name, which is also the base name used for baking.

There is no extra toggle. This does not change prefab baking or prefab updating.

## What is copied

### Empty bake targets on duplication — 16 October 2026

New duplicates start with an empty **Drag & drop GameObjects / Prefabs** list. All copied bake option values remain intact. The original HDA's target list is untouched; a successful new prefab bake from the duplicate populates its own list normally. This supersedes the initial duplication patch's target-list copying. Existing duplicates are not cleared retroactively.

Source and package checks passed; Unity verification pending.


- Current parameter values, including changes made while auto-cook is off. The preset restores multiparm counts and ramps before input assignments are matched. Pending multiparm modifiers may require cooking the source first.
- Input Node and parameter input assignments, slot order, empty slots, input type, transform offsets, Keep World Transform and Pack Geometry Before Merging.
- Mesh, tilemap and spline input settings, copied into separate settings objects.
- Instance Inputs overrides and rotation/scale offsets for corresponding output parts and source objects.
- Existing curve node data and parameter presets, editable attribute values and terrain settings, using the plugin's existing matching rules.
- Generation, cooking, curve editing, handle and inspector options, including all custom Bake Options and Bake Update toggles.
- Bake option values are copied, but the bake target list starts empty. The original HDA keeps all its existing targets. Baking a new prefab from the duplicate adds that prefab to its own target list as usual.
- Root transform, scene, parent, tag, layer, static flags and active state. Additional root components are copied in the editor. Existing output component/material override copying is retained.
- Serialized event listeners, with separate event containers. Connection-specific downstream subscriptions are established by the duplicate's own inputs.

## Independence and limits

The duplicate creates its own Houdini nodes, connections and generated resources. Referenced input scene objects, prefab assets and materials are intentionally the same references; changing those shared source objects affects both HDAs. Changing a duplicate's parameter, slot assignment, input settings or override list should not change the original's corresponding settings.

This is a configured HDA copy, not a frozen scene snapshot. It cooks to regenerate geometry. Time-dependent or random networks may produce different output unless their time/seed is controlled. Arbitrary extra child hierarchies, runtime simulation state and references inside user scripts are not cloned/remapped by this change. Existing output copying requires matching output layouts and does not overwrite components already generated on the destination. Curve/terrain/instance matching still depends on corresponding output nodes and parts.

If a required cook fails, the plugin reports an incomplete duplicate; check the Console rather than assuming every setting was restored. A working Houdini session and valid HDA are required. Duplication under an inactive parent is still subject to the plugin's Cook Disabled GameObjects option.

### Consistent duplicate names — 16 October 2026

New duplicates use their own generated Houdini asset name for the scene root instead of the original name plus `_copy`. For example, a duplicate named `golf_course_zone_1_7` bakes as `golf_course_zone_1_7_gravity_prefab` when the custom asset-name suffix is `_gravity` and Add _prefab Suffix is enabled. The generated number is assigned by Houdini, not forced to the next integer by this patch.

This affects new duplicates only. Existing scene objects and prefab files are not renamed. Parameter/input copying and bake naming rules are unchanged. Source checks completed; Unity verification pending.

## Input-order correction

### Duplicate input-order fix — 16 October 2026

The previous duplication patch cooked the HDA to restore its parameter layout before assigning the duplicate's inputs. That could stop duplication with `Duplicate: unable to restore parameter layout`, followed by `Duplicate is incomplete`.

The layout is now restored directly from the parameter preset, with refreshed node information and parameter input controls. Input assignments are copied before the next geometry cook. Preset application and parameter generation are checked separately; real cook failures still stop the operation.

This correction awaits Unity/Houdini testing. It does not change bake behavior or remove genuine error checks.

## Implementation changes

The old path copied a cached preset, matched inputs by list index, reused the source's clean upload flags, omitted input interface settings and several HDA options, and did not copy instance overrides. The revised path refreshes the preset, restores the parameter layout before matching parameter inputs by name, forces fresh input uploads and copies instance overrides after their destination parts exist. Multi-definition HDA files reuse the selected subasset without another selection prompt.

## Verification

Run the Duplicate HDA regression checklist in [TESTING.md](TESTING.md). Source and package checks were performed; no Unity editor, player build or Houdini cook was available in the packaging environment.
