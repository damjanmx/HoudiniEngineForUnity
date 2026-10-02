Packed-primitive support is described in PACKED_INSTANCES_README.md.

# Custom hierarchies and standard output attributes

This cumulative package includes unity_instance_parent, unity_instance_name,
unity_instance_suffix, unity_path, and per-output attribute ownership.

## Install

Replace these existing files from the package:

- Scripts/Asset/HEU_PartData.cs
- Scripts/Asset/HEU_GeoNode.cs
- Scripts/Asset/HEU_GeneratedOutput.cs
- Scripts/Core/HEU_Defines.cs
- Scripts/Utility/HEU_GenerateGeoCache.cs

Add this new helper AND its supplied .meta file:

- Scripts/Utility/HEU_OutputAttributeScope.cs

Let Unity recompile, then rebuild the HDA once to populate the new ownership
records on assets cooked with the older patch. Subsequent changes use Recook.

## Ownership rules

Each unity_path mesh retains its original primitive, point, and vertex indices.
Each unity_instance prefab retains its original instancer point index.
Matching hierarchy parents are containers; assigning a property to one output
never recursively overwrites another mesh output's properties.

For scalar mesh properties, lookup order is primitive, point, vertex, detail.
For prefab-instance properties it is point, detail. For packed instances it is point, primitive, detail, with counts validated against the instance transforms. Detail values are defaults
when the more specific attribute does not exist. An existing empty string is an
explicit empty value, not a request to fall back to detail. When unity_use_instance_flags=1, source tag, layer and static flags take precedence
over unity_tag, unity_layer and unity_static on that instance, including its descendants.
Naming, parenting, materials, scripts and stored attributes remain independent.

All source elements combined into one mesh path should agree on an object-level
property. If they disagree, a warning names the attribute and path; the first
selected source element wins. Materials, UVs, normals and colors can still vary
within the mesh. Use separate unity_path values for separate object properties.
Tags and named layers must already exist in Unity's project settings.

## Compatibility

| Feature | Handling |
|---|---|
| unity_tag, unity_layer, unity_static | Per mesh source scope / per instancer point, with detail defaults. Mesh flags also reach that mesh's LOD renderers. Instance flags reach its own prefab descendants. |
| unity_script | Selected per output; native script syntax and invocation behavior. Attached to the actual mesh or instance root, not a shared hierarchy parent. |
| hengine_attr_store | Per-output attribute-name list; stored int, float and string arrays are filtered to that output's source indices, preserving tuple components. |
| unity_mesh_readable | Selected per mesh before UploadMeshData; editable-mesh default is retained when absent. Does not modify imported prefab mesh assets. |
| unity_material / native mesh materials | Existing per-face material/submesh generation is preserved. Prefab point/detail material lookup is corrected; native root renderer / single-slot restriction still applies to prefab overrides. |
| collision_geo and rendered collision groups | Existing group names and membership retained; group vertices restricted to their own path. Collision-only outputs remain enabled when the part is visible. |
| Convex, trigger and simple collision variants | Existing generation rules retained. Multiple collision groups on a path get independent colliders, including repeated collider types. |
| Instance collision_geo string attribute | Point value or detail default selects the collision asset for that instance, independently of hierarchy and custom naming. |
| LOD groups / lod_screensizes | Native LOD generation per path; lod_screensizes retains its existing detail scope and behavior. |
| P, N, uv sets, Cd, Alpha, tangents | Original indexed attribute data is retained through mesh splitting and processed by the native generators. |
| unity_use_instance_flags | Point/detail for prefab instances; point/primitive/detail for packed instances. When 1, source tag/layer/static flags are preserved and their instance attribute overrides are skipped. |
| instance_prefix / unity_split_attr | Existing behavior retained when the corresponding custom name/hierarchy attribute does not take precedence. |

Native scopes and restrictions still apply to features not extended here. This
patch does not make terrain/input-only attributes into mesh properties, invent
behavior for arbitrary custom attributes, or add unity_path to PDG/GeoSync.
For prefabs continue using unity_instance_parent plus unity_instance_name/suffix;
unity_path remains the primitive mesh attribute.

Stored arrays use source element order (ascending selected indices), not Unity's
final vertex order after splitting/deduplication. On shared points, a point
attribute is naturally shared by both meshes; use primitive attributes for
properties that must differ on either side.

## Example: primitive wrangle

Assuming prim group `sphere_prims` distinguishes the two source objects:

```c
if (inprimgroup(0, "sphere_prims", @primnum))
{
    s@unity_path = "props/sphere_01";
    s@unity_tag = "Sphere";
    s@unity_layer = "Props";
    i@unity_static = 1;
}
else
{
    s@unity_path = "props/wall_01";
    s@unity_tag = "Wall";
    s@unity_layer = "Environment";
    i@unity_static = 0;
}
```

Set collision-group membership independently on the corresponding primitives.
For prefab instances, author tag/layer/static/material/collision overrides on
points alongside unity_instance and unity_instance_parent.

## Recooking and baking

Path-generated mesh children are rebuilt on recook; manual child edits and
material overrides are still not preserved. Ownership records survive editor
reloads. Generated script/store components owned by this branch are removed
when their old mesh output is rebuilt, including when its root becomes a parent.
Bake code copies flags per node, scoped scripts/stores, and independent collider
components/meshes. Prefab baking keeps native prefab instantiation and transfers
instance flags and renderer material overrides.

## Validation and tests

Source integration, delimiters/preprocessor balance, installation manifest and
ZIP integrity were checked locally. There is no Unity editor, C# compiler, or
Houdini session in the editing environment. Compilation and runtime behavior
are NOT verified here.

Focused Unity Editor tests are supplied in Validation~/ScopedOutputs.
The trailing ~ keeps them out of normal Unity import. To run them, copy that
folder to Assets/Tests/ScopedOutputs in a project with Unity Test Framework,
then run EditMode tests. If the project already has an assembly named
HoudiniEngineUnityEditorTests, add the test .cs to that assembly instead of
creating a second assembly with the same name.

Integration checklist (requires a cooked HDA):

1. Two mesh paths under one parent: different valid tags/layers/static values,
   materials, readability, stored data and scripts. Confirm parent/child paths
   cannot overwrite one another's flags.
2. Primitive collision-only, rendered, convex, trigger, simple collision and
   multiple collision groups; inspect each output's geometry and colliders.
3. Different paths with LOD groups and UV/material variation. Exercise both
   native mesh generation modes and the collision variants your HDA uses.
4. Two unity_instance points referencing one prefab, sharing unity_instance_parent but
   with different names, tags/layers/static, collision assets and materials.
   Exercise detail defaults and point unity_use_instance_flags=0/1.
5. Change paths/properties; remove attributes/groups; recook, save/reload and
   recook again. Check for stale components, duplicate colliders and flag leaks.
6. Bake new GameObjects/prefabs and update existing bakes. Check components,
   prefab links, collision meshes, script data/references and stored arrays;
   delete the source HDA and verify baked output remains valid.
