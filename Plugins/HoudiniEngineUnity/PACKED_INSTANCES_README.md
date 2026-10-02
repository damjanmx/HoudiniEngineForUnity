# Packed primitive instances

unity_parent, unity_instance_name and unity_instance_suffix now also apply to
packed primitive instances generated through GeneratePartInstances.

Example on the packed output points (Point Wrangle after Copy to Points):

```c
s@unity_parent = "vegetation/trees";
s@unity_instance_name = sprintf("tree_%03d", @ptnum);
s@unity_instance_suffix = "_summer";
```

The first instance is named tree_000_summer under vegetation/trees, relative to
its instancer output. Parents with matching paths are shared within that output.
Use a Primitive Wrangle on the packed primitives instead if your HDA exports
these attributes on primitives. Place attributes on the outer packed instances,
not solely on polygons inside the packed source geometry.

## Lookup and naming

- Lookup order for packed instances: point, primitive, detail.
- Point/primitive arrays must have one scalar string per HAPI instance transform.
  Invalid owners are warned about and skipped; indices are never guessed.
- Detail values apply to every instance. An existing empty point/primitive value
  remains empty rather than falling back to detail.
- Name only: exact custom name. Suffix only: source prototype GameObject name +
  suffix. Both: custom name + suffix. Both empty: existing plugin naming.
- No separator or uniqueness counter is appended to custom names.
- Empty unity_parent leaves the instance directly under its instancer output.
- When unity_parent is absent, unity_split_attr can supply the grouping.
- Attributes are indexed by transform, not by prototype part. If one packed
  source contains several exported parts, the plugin still emits each part's
  copy; the same transform's attributes apply to each copy. An exact custom name
  can therefore repeat. Suffix-only naming keeps the source part names distinct.
- Nested packed instancers are processed at their own instancer level. HAPI's
  cooked instancing mode determines which levels and attributes are exposed.

The implementation follows HAPI's distinction between instancedPartCount
(prototype parts) and instanceCount (transforms):
https://www.sidefx.com/docs/hengine/_h_a_p_i__instancing.html

## Properties and lifecycle

Packed copies participate in the existing per-output tag/layer/static/script/
stored-attribute handling, using point, primitive, then detail values. Material
and collision geometry remain on their source prototype. This update does not
add prefab-style collision-asset replacement or material replacement to packed
primitives.

Instances are tracked by references so renaming does not prevent cleanup.
Deleting a generated copy does not destroy its shared prototype mesh. Parent
transforms are identity transforms; the original HAPI instance transforms are
applied unchanged. Nested generated meshes are included in recursive baking.

## Install and validate

If you installed the immediately preceding attribute-compatibility package,
replace only:

- Scripts/Asset/HEU_PartData.cs
- Scripts/Utility/HEU_OutputAttributeScope.cs

For a fresh install, follow ATTRIBUTE_COMPATIBILITY_README.md; all earlier
changes are included. Rebuild the HDA once after installation.

Optional Unity tests in Validation~/ScopedOutputs cover the shared naming rules,
packed owner precedence/count validation, and custom-name cleanup without
shared-mesh destruction. The Unity tests have not been run here. Source checks
and ZIP verification do not substitute for compilation or an HDA scene test.

In Unity test several Copy to Points instances with mixed names, suffixes and
parent paths; recook after changing/removing them. Also test primitive/detail
attributes, multiple source parts, nested packed instances, shared mesh identity,
nonuniform scales/rotations, per-instance flags and baking. Confirm the original
unity_instance prefab behavior remains correct in the same project.
