For the latest cumulative installation list and attribute rules, see ATTRIBUTE_COMPATIBILITY_README.md.

# unity_path: primitive mesh names and hierarchy

Add a STRING attribute named unity_path on PRIMITIVES. Example Primitive Wrangle:

```c
s@unity_path = "parent/child/sphere_01";
```

All polygons of this sphere should have that value. The output hierarchy is
parent > child > sphere_01, with the mesh on sphere_01. A value of sphere_01
creates a mesh GameObject named sphere_01 without adding parent groups.

Different paths within one HAPI part generate separate meshes; matching paths
combine their primitives into the same mesh. Shared parent paths reuse groups.
Material assignments and point/vertex attributes retain their original indices.
The existing point-based or vertex-based generation setting still applies.
Collision and LOD groups are filtered per path and use the existing generators.

## Scope and fallback

- Grouping is local to each generated output part, not across separate outputs.
- When all paths share a first segment, the existing output object becomes that
  segment. Otherwise the usual output object remains as a container for the paths.
- Empty paths in a mixed part keep those polygons on the usual output object.
- Missing, entirely empty, or invalid attributes use normal mesh generation.
- Leading/trailing/repeated slashes are ignored. Names are case-sensitive.
- This applies to normal HDA mesh generation, not PDG/GeoSync or terrain/curves.
- On a path node that also owns an LODGroup, reserve the generated LOD child names
  (e.g. lod0) and do not use them as separate unity_path children.
- Generated path objects are rebuilt on recook. Set materials and transforms in
  Houdini; manual child edits and material overrides are not preserved by this
  path-generation branch.

## Install

Replace these FOUR files from this cumulative package:

- Scripts/Asset/HEU_PartData.cs
- Scripts/Asset/HEU_GeneratedOutput.cs
- Scripts/Core/HEU_Defines.cs
- Scripts/Utility/HEU_GenerateGeoCache.cs

Earlier unity_parent, unity_instance_name, and unity_instance_suffix changes
are included. Recook after replacing the files.

## Validation

Source reviewed; hierarchy and face-partition algorithm checks run outside
Unity. No Unity compiler, editor, or Houdini session is available here.

In Unity, verify:
1. Single sphere_01 path: exact name and no extra groups.
2. parent/child/sphere_01: mesh at leaf, identity local group transforms.
3. parent/child/sphere_01 and parent/child/cube_01: shared parent and child.
4. Merge primitives with different paths: each mesh has only its own faces,
   correct materials, UVs, colors and normals. Test both mesh generation modes.
5. Rename/remove paths and remove the attribute, recook, then save/reopen and
   recook again: no orphan groups, meshes, or duplicate colliders.
6. Bake new GameObject, prefab, and update an existing bake: inspect nested
   meshes/materials and confirm the bake survives deletion of the source HDA.
7. Test collision groups, LOD groups, instanced mesh parts, visibility toggles,
   and the existing unity_instance attributes in the same asset.
