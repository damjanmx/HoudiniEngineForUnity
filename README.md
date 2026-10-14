# Houdini Engine for Unity — Custom Fork

Customizations maintained by Nikola Damjanov, based on SideFX Houdini Engine for Unity **22.0.466**. Development branch: `custom/houdini22.0`. The earlier 22.0.459 baseline is recorded as `baseline/houdini-22.0.459`; the current plugin includes the official 22.0.466 update.

## Changes and usage

These attributes control the names and hierarchy of generated Unity GameObjects. Use scalar string attributes in Houdini.

| Attribute | Where to author it | Result |
| --- | --- | --- |
| `unity_instance_parent` | Prefab instancer points; packed instance points, primitives or detail | Creates a parent hierarchy relative to the instancer output. `vegetation/trees` places the instance under `trees`, beneath `vegetation`. |
| `unity_instance_name` | Prefab instancer points; packed instance points, primitives or detail | Sets the instance root's name. |
| `unity_instance_suffix` | Prefab instancer points; packed instance points, primitives or detail | Appends text verbatim to the source prefab/prototype name, or to `unity_instance_name` when both are supplied. |
| `unity_path` | Mesh primitives | Splits generated mesh output by path and sets its hierarchy. `parent/child/sphere_01` creates a mesh object named `sphere_01` under `child`, beneath `parent`. |

For prefab instances, use a Point Wrangle on the instancer points:

```c
s@unity_instance_parent = "vegetation/trees";
s@unity_instance_name = sprintf("tree_%03d", @ptnum);
s@unity_instance_suffix = "_summer";
i@unity_use_instance_flags = 1;
```

The first instance is named `tree_000_summer`. Leave the custom name empty to append the suffix to the original prefab name. Separators and uniqueness counters are not added automatically.

For packed instances, put the naming and parent attributes on the outer instances. Lookup order is point, primitive, then detail. Point and primitive attribute counts must match the exported instance transform count; detail values apply to every instance.

For generated meshes, use a Primitive Wrangle:

```c
s@unity_path = "environment/props/sphere_01";
```

Primitives sharing a path form the same mesh output. Give different paths to objects that need different object-level properties. Shared parent paths are reused within the same output part.

## Existing attributes and groups

Generated outputs resolve properties from their own source elements, including `unity_tag`, `unity_layer`, `unity_static`, scripts and stored attributes. Mesh generation retains material, collision-group and LOD processing, including groups such as `collision_geo`.

With `i@unity_use_instance_flags = 1;`, prefab and packed instances preserve source tags, layers and individual static flags, including different settings on descendants. Instancer `unity_tag`, `unity_layer` and `unity_static` values are skipped for those instances. Custom names, suffixes and parenting still apply. Use `0` to allow instance flag overrides.

Packed instances retain source prototype materials and collision geometry; this fork does not add prefab-style material or collision-asset replacement to packed instances. Ordinary non-instance meshes using `unity_path` continue to resolve their own attributes.

## Multiparm asset drag and drop

Drag several Project assets onto a multiparm’s **Drop assets here to add entries** area to append one entry per asset path. Each list is independent; other fields use their HDA defaults. Supports one scalar `heuassetpath` string field per entry. See [usage and limitations](Documentation/MULTIPARM_ASSET_DROP_README.md).

## Component profiles

Assigning a template prefab automatically selects all supported root components. Untick any you do not need; protected geometry components remain excluded.

Template component settings are copied during cooking and baking, including nested serialized values. The serialized-property iteration issue found during testing has been fixed.

Create **Houdini Engine > Component Profile** assets from a template prefab and select configured root components to copy. Reference them with `unity_component_profile` for individual outputs or detail `unity_component_profile_root` for the baked prefab root. Profiles default to **Overwrite Settings**; **Keep Existing** preserves settings on components already present while adding missing ones. Component references are remapped to the generated counterparts. See [setup, usage and limitations](Documentation/COMPONENT_PROFILES_README.md).

Profiles also provide **Attribute Bindings**: select a component property and bind a `unity_cp_` Houdini attribute to it. Values follow each output’s attribute scope; root bindings read detail only. See [binding usage and supported types](Documentation/COMPONENT_PROFILE_BINDINGS_README.md).

## Baked prefab root attributes

Use detail strings `unity_layer_root` to assign a layer to the baked root and all descendants, and `unity_script_root` to attach scripts to the baked root. Both apply to new prefab bakes and prefab updates. See [usage and precedence](Documentation/PREFAB_ROOT_ATTRIBUTES_README.md).

## Prefab bake location

Enable **Bake To The Scene Folder** to create new prefabs and their generated resource folders under `HdaBakedData` beside the active saved scene. It defaults to off. **Delete All Baked Folders** defaults to **off**: updates overwrite matching generated files while retaining unrelated and unused files. Turning it on restores the original resource-folder cleanup. Both settings persist per HDA; existing targets continue to update in place. See [bake settings](Documentation/BAKE_UPDATE_README.md).

## Bake workflow and prefab names

**Add Prefab To Scene**, enabled by default beneath **Add _prefab Suffix**, places a newly baked prefab in the HDA’s scene and deactivates the HDA after successful placement. Only the **Bake Prefab** button performs this action; updates remain unchanged. Disable the toggle to keep asset-only baking.

Use the string detail attribute **`unity_asset_name_suffix`** to insert custom text after the asset name and before the optional `_prefab` suffix. For example, `s@unity_asset_name_suffix = "_large";` produces `Bridge_1_2_large_prefab.prefab`. Missing or empty preserves current naming. Applies to new prefab bakes; existing targets retain their filenames. See [naming details](Documentation/UNITY_ASSET_NAME_SUFFIX_README.md).

The **Bake** section opens expanded by default, including once for existing HDAs. You can still collapse it afterward. A successful new prefab bake automatically appends that prefab to the HDA's update target list, preserving existing entries. Review the list before updating multiple baked variants.

Under **Bake Options**, **Add _prefab Suffix** defaults to **on**. New prefab filenames and root names receive `_prefab`; an existing exact suffix is not doubled. Turning it off restores the previous naming behavior. The setting persists, and changing it does not rename previously baked prefabs.

Prefab output GameObjects retain their cooked names without an added `_bakedClone`. Mesh `(Clone)` names remain unchanged. The top-level prefab root follows the prefab naming convention.

Updating a Project prefab now saves its resources and prefab, then forces a synchronous prefab reimport and reload. This addresses stale mesh/material references after updating a prefab asset from the Project window. See [bake settings and naming](Documentation/BAKE_UPDATE_README.md) and [prefab update details](Documentation/PROJECT_PREFAB_UPDATE_FIX.md).

## Input defaults

Input Nodes start with one slot. New **Instance Inputs** override lists start empty; generated instances use their HDA source until an override is added manually. Existing saved overrides are preserved. See [input defaults](Documentation/INPUT_DEFAULT_SLOT_README.md).

## Changed defaults

| Setting | Custom default |
| --- | --- |
| Generate Tangents | Off (`false`) |
| Generate Mesh Using Points | On (`true`) |
| Spline Sampling Resolution | `12.0` |
| Bake foldout | Expanded |
| Add _prefab Suffix | On |

Generation defaults apply to newly created instances; existing saved values remain. Bake expands once for existing HDAs, then respects your foldout choice. The suffix toggle is enabled by default and retains your chosen value.

## Unity compatibility

Removed an unused Codice namespace import that caused player-build compilation failures when that optional version-control dependency was unavailable.

Assembly discovery uses `UnityEngine.Assemblies.CurrentAssemblies.GetLoadedAssemblies()` on Unity 6.6 or newer, with `AppDomain.CurrentDomain.GetAssemblies()` retained for earlier versions.

## Documentation and current limitations

Custom documentation is collected in [Documentation](Documentation/). See the [full changelog and usage guide](Documentation/CHANGELOG_AND_GUIDE.md) and [instance flag behavior](Documentation/INSTANCE_FLAGS_README.md).

- `unity_path` support targets regular HDA mesh generation; terrain, curves, PDG and GeoSync are outside this extension's scope.
- Generated path hierarchies are rebuilt on recook. Manual edits to generated children and material overrides may not persist.
- The integration is not yet fully opt-in: some instancer processing differs from upstream even when custom attributes are absent.
- On 13 October 2026, the maintainer reported that the complete test round passed, including the baked-root binding variant. This records testing in that project, not a guarantee for every Unity version or asset configuration.


Based on SideFX Houdini Engine for Unity. Original copyright and license terms remain in [LICENSE.md](LICENSE.md).
