# Houdini Engine for Unity — Custom Hierarchy Extensions

## Changelog and usage documentation

**Documentation date:** 7 October 2026  
**Scope:** Custom output features and subsequent fixes, rename and defaults for Houdini 22.0.459.  
**Status:** Earlier custom version reported working by the user. Reapplication to the clean, compilation-fixed baseline requires a fresh Unity check.

Stage numbers below describe this project's changes; they are not official SideFX release numbers.

**Compatibility notice:** The current patch is **not strictly opt-in**. Some instance-property, cleanup, grouping, baking, and material-reset changes run even when none of the four custom attributes exist. A change that restores the original code paths whenever custom attributes are absent has been discussed but **has not been implemented**.

---

## 1. Cumulative changelog

### Component profiles — 7 October 2026

- Added a Component Profile asset and Inspector with template selection, component checkboxes and existing-component policy.
- Added `unity_component_profile` on generated outputs/instances and detail `unity_component_profile_root` on baked prefab roots.
- Added serialized settings copying, component-reference remapping, template validation and generated-geometry protection.
- See [COMPONENT_PROFILES_README.md](COMPONENT_PROFILES_README.md) for supported components, attribute precedence and additive recook behavior.


### Baked prefab root attributes — 7 October 2026

- Added detail strings `unity_layer_root` (root and all descendants) and `unity_script_root` (root only).
- Applied both before saving new or updated prefabs, using existing layer and script utilities.
- Added detail/type validation and warnings for conflicting output values or unknown layers.
- See [PREFAB_ROOT_ATTRIBUTES_README.md](PREFAB_ROOT_ATTRIBUTES_README.md).


### Scene-folder prefab baking — 7 October 2026

- Added **Bake To The Scene Folder**, default off, in a separate **Bake Options** section below **Bake Update**.
- New prefab bakes can save their prefab and usual resource folders under the active scene directory's `HdaBakedData` folder.
- Existing prefab updates stay at their target location; explicit API destinations retain precedence.
- Renamed **Delete all baked data** to **Delete All Baked Folders**, retaining its serialized value and behavior.
- See [BAKE_UPDATE_README.md](BAKE_UPDATE_README.md) for usage and scope.


### Multiparm asset drops — 6 October 2026

- Added an independent asset drop area to supported non-ramp multiparms.
- Appends one entry per distinct dropped asset path, assigning the single `heuassetpath` string field and retaining defaults for the other new fields.
- Supports empty lists through template validation; failed batches attempt to roll back only their own entries.
- Uses the existing automatic/manual cooking workflow.
- See [MULTIPARM_ASSET_DROP_README.md](MULTIPARM_ASSET_DROP_README.md) for usage, scope and verification status.

### Latest updates — 2 October 2026

- Renamed the instance parent attribute to `unity_instance_parent`, with no alias for the previous name.
- Preserved source tag, layer and individual static flags when `unity_use_instance_flags = 1`, including descendants and bake copies.
- Set new-instance defaults to Generate Tangents off, Generate Mesh Using Points on, and spline Sampling Resolution 12. Existing serialized values remain unchanged.
- Rebased custom features on official 22.0.459. The public assembly-enumeration API fix is maintained in the preceding baseline commit.
- Collected custom documentation under the repository's `Documentation` folder; optional validation files are omitted.


### Stage 1 — Custom prefab-instance parents

Added the string point attribute `unity_instance_parent` for instances created using `unity_instance`.

- A value such as `parent_A` creates/reuses an empty parent GameObject.
- A value such as `parent_A/parent_B` creates/reuses both hierarchy levels.
- Matching paths share parents within one generated instancer output.
- Empty values leave the instance directly under the output object.
- Parent transforms are created with zero position, identity rotation, and unit scale.
- Generated parent ownership is recorded for cleanup after recooking and editor reloads.
- The bake traversal was extended to handle nested generated parents and prefab instances.
- A valid `unity_instance_parent` attribute takes precedence over `unity_split_attr`.

### Stage 2 — Instance names and suffixes

Added two string point attributes for `unity_instance` prefabs:

| Attribute | Behavior |
|---|---|
| `unity_instance_name` | Replaces the generated instance root's name. |
| `unity_instance_suffix` | Appends text to the source prefab name, or to the custom name when both attributes are set. |

Names and suffixes are used literally. No separator or uniqueness counter is automatically appended. When both values are absent or empty, the existing naming function is used, including applicable `instance_prefix` behavior. Prefab assets and their child names are not renamed by these attributes.

### Stage 3 — Mesh names and hierarchy paths

Added the string primitive attribute `unity_path` for normal HDA mesh generation.

- Primitives with the same normalized path are combined into one mesh output.
- Different paths produce separate mesh outputs.
- The last segment identifies the mesh GameObject; earlier segments define parents.
- Matching parents are shared within the same output part.
- When every path shares its first segment, the existing output object becomes that segment. Otherwise the normal output object remains as a container.
- Geometry splitting retains original face/vertex indices for materials and attributes.
- Existing mesh-generation, collision, and LOD routines are used for each selected path.
- Path-output cleanup, visibility, recursive baking, and root/child material reset handling were added.

Path-generated objects are rebuilt on recook. Manual child edits and material overrides are not preserved by this generation branch.

### Stage 4 — Standard attributes and groups per output

Changed attribute application so separately generated objects can have different properties.

- Added source-ownership records for mesh primitives/points/vertices and prefab instance points.
- Added a per-phase attribute reader that selects values from an output's own source elements.
- Routed tags, layers, static flags, scripts, and stored attributes through that selection.
- Prevented mesh-parent properties from recursively overwriting separate mesh outputs.
- Added per-mesh `unity_mesh_readable` lookup before mesh upload.
- Corrected point/detail lookup for prefab material and collision-asset overrides.
- Made `unity_use_instance_flags` selectable per prefab point, with detail fallback.
- Kept collision-only path outputs enabled according to part visibility, despite having no renderer.
- Gave separate collision groups on one mesh path separate collider components, including repeated collider types.
- Added cleanup for script/store components owned by the new mesh-generation branch.
- Extended bake handling for node flags, scoped scripts/stores, collider components/meshes, and prefab renderer material overrides.

These changes preserve the existing generators' supported features and restrictions; they do not add behavior for every arbitrary attribute or group name.

### Stage 5 — Packed-primitive instances

Extended `unity_instance_parent`, `unity_instance_name`, and `unity_instance_suffix` to packed-primitive instances.

- Added point → primitive → detail lookup on packed instancer parts.
- Indexed values by instance transform, not by prototype-part index.
- Added validation against the number of instance transforms.
- Reused the prefab naming rules and parent hierarchy builder.
- Added explicit packed-instance references so custom names do not defeat recook cleanup.
- Cleanup destroys the generated copies without destroying shared prototype meshes.
- Added packed-instance scopes for tags, layers, static flags, scripts, and stored attributes.
- Extended recursive baking for packed output trees.
- Added `unity_split_attr` fallback grouping when `unity_instance_parent` is absent.

Packed instances continue to obtain material and collision geometry from their prototypes. Prefab-style material replacement and collision-asset replacement were not added to packed primitives.

### Compatibility review — Current finding

The absence of custom attributes does not guarantee stock-plugin behavior. This review changed the documentation, not the implementation. Strict opt-in gating remains pending.

---

## 2. Attribute reference

All four custom attributes are **scalar strings**. They are geometry attributes, not new HDA UI parameters or plugin settings.

| Attribute | `unity_instance` prefabs | Packed instances | Ordinary generated meshes |
|---|---|---|---|
| `unity_instance_parent` | Point | Point → primitive → detail | Not used for mesh-path generation. |
| `unity_instance_name` | Point | Point → primitive → detail | Not used for mesh-path generation. |
| `unity_instance_suffix` | Point | Point → primitive → detail | Not used for mesh-path generation. |
| `unity_path` | Not a prefab hierarchy attribute. | Does not replace the packed-instance naming attributes; may organize a generated mesh prototype. | Primitive |

The arrows indicate owner lookup order, not attribute promotion. In particular, the three custom **prefab** attributes remain point-only; their packed equivalents additionally accept primitive and detail ownership.

### `unity_instance_parent`

```c
s@unity_instance_parent = "environment/vegetation/trees";
```

The instance is placed under `environment → vegetation → trees`, beneath its generated instancer output.

Paths are local to that output. Identical paths on separate output parts do not merge into a single asset-wide group. Leading, trailing, and repeated `/` separators are ignored. Names are case-sensitive. `/` is the separator; backslashes are not treated as hierarchy separators.

An existing, valid attribute with an empty value leaves that instance directly under the output and still takes precedence over `unity_split_attr`. If the attribute is absent or invalid, the applicable split-attribute fallback can be used.

### `unity_instance_name` and `unity_instance_suffix`

For a source named `Tree`:

| Name value | Suffix value | Result |
|---|---|---|
| `Oak` | Empty/absent | `Oak` |
| Empty/absent | `_large` | `Tree_large` |
| `Oak` | `_large` | `Oak_large` |
| Empty/absent | Empty/absent | Existing plugin-generated name. |

Custom naming overrides `instance_prefix`. A suffix is appended exactly as supplied, so include `_`, `-`, or a space if wanted. Duplicate custom names are allowed.

For packed copies, “source name” means the prototype's generated GameObject name. For prefab copies, it means the source prefab/GameObject name. The final name is applied to the instance root only.

### `unity_path`

Set this on every primitive that belongs to the intended mesh output:

```c
s@unity_path = "parent/child/sphere_01";
```

This produces `parent → child → sphere_01`, with the mesh on `sphere_01`. A single value of `sphere_01` names the output directly, without adding parent groups.

| Primitive path values in one part | Result |
|---|---|
| All `props/sphere_01` | Output root becomes `props`; mesh child is `sphere_01`. |
| `props/sphere_01`, `props/cube_01` | Shared `props` root with two mesh children. |
| `trees/oak`, `rocks/granite` | Normal output container remains; two separate path branches are created. |
| Empty values mixed with nonempty paths | Empty-path geometry remains on the normal output object; other geometry uses its paths. |
| Missing, invalid, or entirely empty attribute | Normal mesh generation is used. This does not disable unrelated patch behavior. |

Paths share the same slash normalization and case sensitivity as `unity_instance_parent`. Primitives sharing a path are combined, even if they are disconnected. A path node can itself contain geometry as well as child paths. Avoid using a generated LOD child name, such as `lod0`, as a separate path child of the same LOD-owning node.

---

## 3. Examples

### Prefab instances — Point Wrangle

Use alongside the existing `unity_instance` setup:

```c
s@unity_instance = "Assets/Prefabs/Tree.prefab";
s@unity_instance_parent = "environment/trees";
s@unity_instance_name = sprintf("tree_%03d", @ptnum);
s@unity_instance_suffix = "_summer";
```

Point zero generates `tree_000_summer` under `environment/trees`. The example prefab asset must exist in the Unity project.

### Packed instances — Point Wrangle on the packed output

```c
s@unity_instance_parent = "environment/rocks";
s@unity_instance_name = sprintf("rock_%03d", @ptnum);
s@unity_instance_suffix = "_large";
```

Author these on the outer packed instances, for example after Copy to Points. If the HDA exports primitive attributes instead, use a Primitive Wrangle and replace `@ptnum` with `@primnum` for numbering. Values authored only on polygons inside the packed geometry do not necessarily become attributes of the outer instancer.

Point/primitive values must match the cooked instance-transform count. Detail values are broadcast. A mismatched owner is warned about and skipped, rather than guessed. An existing empty value does not fall back to another owner.

One packed source can export several prototype parts. Each part is copied for each transform, and all those copies use that transform's attribute values. An exact custom name can therefore repeat across the parts. Suffix-only naming retains their different prototype names.

Nested packed instancers process their own attributes at each exposed instancer level. The cooked HAPI instancing mode determines which levels and attributes are available.

### Different mesh properties — Primitive Wrangle

Assume `sphere_prims` is a primitive group identifying the sphere:

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

Create these tags and layers in Unity before cooking. Assign collision-group membership separately on the relevant primitives. Use distinct paths when objects require different object-level properties.

---

## 4. Standard attributes and groups

### Property ownership

| Output | Scalar-property lookup order |
|---|---|
| `unity_path` mesh | Primitive → point → vertex → detail. |
| `unity_instance` prefab | Point → detail. |
| Packed instance | Point → primitive → detail, with instance-count checks. |

This table concerns the scoped property reader; material, geometry and LOD processing retain their own native rules. A more specific existing empty string is explicit and does not mean “use the detail default.”

A GameObject can have only one tag, layer, or static setting. If source elements combined into one mesh path disagree on a scalar property, the patch warns and uses the first selected source element. Per-face materials and per-point/per-vertex geometry attributes can still vary within one mesh.

Mesh flags apply to the mesh node and its own generated LOD renderers without overwriting other mesh outputs. Instance flags apply recursively within that instance's copied hierarchy. When `unity_use_instance_flags = 1`, the source tag, layer and exact static flags are preserved on each instance and its descendants; instance tag/layer/static attributes do not override them. Naming, hierarchy and other output attributes remain active.

### Feature coverage

| Feature | Current handling and boundary |
|---|---|
| `unity_tag`, `unity_layer`, `unity_static` | Selected per output. Tags and named layers must already exist in Unity. |
| `unity_script` | Selected per output and attached/invoked on the mesh or instance root using the existing script syntax. |
| `hengine_attr_store` | Attribute-name list selected per output; stored int, float and string arrays are filtered to the output's source indices, preserving tuples. |
| `unity_mesh_readable` | Selected before generated mesh upload. Editable-mesh fallback retained. Does not change imported prefab mesh assets. |
| Mesh materials / `unity_material` | Native per-face/submesh generation retained through path splitting. |
| Prefab `unity_material` override | Point/detail lookup. Existing root `MeshRenderer` and single-material-slot restriction remains. |
| Packed-instance materials | Inherited from prototype geometry; no new per-copy material replacement. |
| `collision_geo` and rendered collision groups | Membership filtered to each mesh path; native supported collision-generation rules retained. |
| Convex, trigger and simple collision variants | Existing interpretations retained. Separate path collision groups receive independent collider components. |
| Collision-only path outputs | Collider visibility follows part visibility rather than renderer presence. |
| Prefab collision-asset string attribute | Selects a collision asset per point or through a detail default. |
| Packed-instance collisions | Prototype collision geometry is copied/shared as applicable; no new prefab-style collision-asset override. |
| LOD groups / `lod_screensizes` | Native generation per mesh path. Screen-size settings retain their existing detail scope. |
| `P`, `N`, UV sets, `Cd`, alpha and tangents | Original indexed geometry data passes through the existing generators. |
| `unity_use_instance_flags` | Point/detail lookup for prefabs and point/primitive/detail for packed instances. A value of 1 preserves source tag/layer/static flags, taking precedence over instance flag attributes. |
| `instance_prefix` | Used by normal naming when custom name and suffix are both empty/absent. |
| `unity_split_attr` | Fallback instance grouping when a valid custom parent attribute does not take precedence; packed fallback is an added behavior. |

Stored arrays retain source-element order, not Unity's final vertex order after splitting or deduplication. Shared points naturally share point attributes; author primitive properties when meshes on opposite sides of a shared point need different values.

The patch does not turn terrain/input-only attributes into mesh properties, add arbitrary custom-attribute behavior, or extend `unity_path` to PDG/GeoSync, terrain, or curves. Native unsupported combinations remain unsupported.

---

## 5. Recooking, transforms and baking

### Recooking and ownership

Generated parents and packed copies are tracked by references rather than relying solely on generated names. These records are serialized for editor reloads. Packed-copy cleanup preserves shared prototype meshes.

Path-generated meshes are rebuilt on recook. Author persistent settings in Houdini: manual child edits and material overrides are not preserved in this branch. Owned script/store components are removed when an old mesh output is rebuilt, including when its root becomes a grouping parent.

### Transforms

Custom parent groups have identity local transforms. Instances receive the existing Houdini-to-Unity transform conversion under those groups. Custom naming does not change geometry or transforms. Nonuniform scale, rotation, nested packed transforms, and recooking still require scene validation.

### Baking

The source now includes recursive traversal for generated parent/path/packed trees, per-node flag copying, scoped script/store copying, and collision component/mesh copying. Prefab baking retains the native prefab-instantiation path and transfers instance flags and renderer material overrides.

Existing root bake-name conventions still apply; custom descendant naming does not remove the plugin's normal root bake suffixes. Baking behavior has not yet been runtime-verified in Unity.

---

## 6. Behavior when custom attributes are absent

**The current patch is not a drop-in guarantee of identical stock behavior.**

| Area | Behavior without our custom attributes |
|---|---|
| Custom name and suffix | Falls back to the existing naming function. |
| Custom instance parent | No custom path groups; applicable split-attribute behavior can still run. |
| Mesh-path splitting | Not activated by a missing/invalid/all-empty `unity_path`. |
| Prefab and packed properties | New per-output handling still runs for these instance types. |
| Packed cleanup | Explicit copy tracking still runs. |
| Packed split grouping | Added `unity_split_attr` fallback can still change hierarchy. |
| Baking | Some modified flag, component and recursive-copy paths still run. |
| Material reset | Root material overrides are now reset along with registered child outputs. |

An HDA using only built-in attributes may therefore behave differently. Removing custom attributes does not revert the plugin source. Returning to stock behavior requires restoring the original plugin, or implementing and validating explicit gating of all changed paths.

**Pending, not implemented:** activate extensions only when the relevant custom attributes exist and preserve the original paths otherwise. No compatibility switch or global opt-in setting currently exists.

