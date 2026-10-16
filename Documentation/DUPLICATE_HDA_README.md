# Duplicate HDA state

Updated 15 October 2026. Based on Houdini Engine for Unity 22.0.466. Unity/Houdini verification pending.

## Usage

Configure an HDA and press its inspector **Duplicate** button. The duplicate uses the same HDA definition, parameter preset and input references. It is placed at the same transform, under the same parent and in the same scene; its root name ends in `_copy`.

There is no extra toggle. This does not change prefab baking or prefab updating.

## What is copied

- Current parameter values, including changes made while auto-cook is off. The preset restores multiparm counts and ramps before input assignments are matched. Pending multiparm modifiers may require cooking the source first.
- Input Node and parameter input assignments, slot order, empty slots, input type, transform offsets, Keep World Transform and Pack Geometry Before Merging.
- Mesh, tilemap and spline input settings, copied into separate settings objects.
- Instance Inputs overrides and rotation/scale offsets for corresponding output parts and source objects.
- Existing curve node data and parameter presets, editable attribute values and terrain settings, using the plugin's existing matching rules.
- Generation, cooking, curve editing, handle and inspector options, including all custom Bake Options and Bake Update toggles.
- Bake target references in a separate list. Both HDAs initially target the same prefabs; edit the duplicate's target list if it should update different prefabs.
- Root transform, scene, parent, tag, layer, static flags and active state. Additional root components are copied in the editor. Existing output component/material override copying is retained.
- Serialized event listeners, with separate event containers. Connection-specific downstream subscriptions are established by the duplicate's own inputs.

## Independence and limits

The duplicate creates its own Houdini nodes, connections and generated resources. Referenced scene objects, prefab assets, materials and bake targets are intentionally the same references; changing those shared source objects affects both HDAs. Changing a duplicate's parameter, slot assignment, input settings or override list should not change the original's corresponding settings.

This is a configured HDA copy, not a frozen scene snapshot. It cooks to regenerate geometry. Time-dependent or random networks may produce different output unless their time/seed is controlled. Arbitrary extra child hierarchies, runtime simulation state and references inside user scripts are not cloned/remapped by this change. Existing output copying requires matching output layouts and does not overwrite components already generated on the destination. Curve/terrain/instance matching still depends on corresponding output nodes and parts.

If a required cook fails, the plugin reports an incomplete duplicate; check the Console rather than assuming every setting was restored. A working Houdini session and valid HDA are required. Duplication under an inactive parent is still subject to the plugin's Cook Disabled GameObjects option.

## Implementation changes

The old path copied a cached preset, matched inputs by list index, reused the source's clean upload flags, omitted input interface settings and several HDA options, and did not copy instance overrides. The revised path refreshes the preset, restores the parameter layout before matching parameter inputs by name, forces fresh input uploads and copies instance overrides after their destination parts exist. Multi-definition HDA files reuse the selected subasset without another selection prompt.

## Verification

Run the Duplicate HDA regression checklist in [TESTING.md](TESTING.md). Source and package checks were performed; no Unity editor, player build or Houdini cook was available in the packaging environment.
