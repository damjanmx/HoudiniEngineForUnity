# Houdini Engine for Unity — Custom Fork

Customizations maintained by Nikola Damjanov, based on SideFX Houdini Engine for Unity **22.0.459**. Development branch: `custom/houdini22.0`. The official installer baseline is recorded as `baseline/houdini-22.0.459`.

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

## Changed defaults

| Setting | Custom default |
| --- | --- |
| Generate Tangents | Off (`false`) |
| Generate Mesh Using Points | On (`true`) |
| Spline Sampling Resolution | `12.0` |

These defaults apply to newly created instances. Existing serialized assets and components retain their saved values.

## Documentation and current limitations

Custom documentation is collected in [Documentation](Documentation/). See the [full changelog and usage guide](Documentation/CHANGELOG_AND_GUIDE.md) and [instance flag behavior](Documentation/INSTANCE_FLAGS_README.md).

- `unity_path` support targets regular HDA mesh generation; terrain, curves, PDG and GeoSync are outside this extension's scope.
- Generated path hierarchies are rebuilt on recook. Manual edits to generated children and material overrides may not persist.
- The integration is not yet fully opt-in: some instancer processing differs from upstream even when custom attributes are absent.
- Source syntax and merge checks have been performed. Unity compilation and HDA cook/bake verification have not yet been confirmed for this custom 459 revision.

This is an independent customization of SideFX's plugin. SideFX's original documentation and attribution follow below.

---

# Houdini Engine for Unity
Houdini Engine for Unity is a Unity plug-in that allows deep integration of
Houdini technology into Unity through the use of Houdini Engine.

This plug-in brings Houdini's powerful and flexible procedural workflow into
Unity through Houdini Digital Assets. Artists can interactively adjust the
asset's parameters inside Unity, and use Unity geometries as an asset's inputs.
Houdini's procedural engine will then "cook" the asset and the results will be
available right inside Unity.

The easiest way for artists to access the plug-in is to download the latest
production build of Houdini and install the Unity plug-in along with the Houdini interactive software.
Houdini Digital Assets created in Houdini can then be loaded into Unity through the plug-in. 
A growing library of Digital Assets for use in Unity will be available at the [Orbolt Smart 3D Asset
Store](http://www.orbolt.com/unity).

For more information:

* [Houdini Engine for Unity Product Info](https://www.sidefx.com/products/houdini-engine/unity-plug-in/)
* [Houdini Enigne for Unity Documentation](https://www.sidefx.com/docs/unity/index.html)
* [FAQ](https://www.sidefx.com/faq/houdini-engine-faq/)

For support and reporting bugs:

* [SideFX Houdini Engine for Unity forum](https://www.sidefx.com/forum/50/)
* [Bug Submission](https://www.sidefx.com/bugs/submit/)

## Supported Unity versions
Currently, the supported Unity versions are:

* 2018.1 and newer

## Installing from Source
1. Fork this repository to your own Github account using the Fork button at the top.
1. Clone the forked repository to your file system.
1. Download and install the correct build of Houdini. You must have the exact build number and version as HOUDINI_MAJOR, HOUDINI_MINOR, and HOUDINI_BUILD int values in Plugins/HoudiniEngineUnity/Scripts/HEU_HoudiniVersion.cs. You can get the correct build from: http://www.sidefx.com/download/daily-builds (you might need to wait for the build to finish and show up if you're updating to the very latest version of the plugin)
1. Open a project in Unity. Note that if a previous version of the plugin exists in the project (usually located at Assets/Plugins/HoudiniEngineUnity), then you'll need to remove it from the Unity project. To do so, in Unity, in the Project browser, right-click on HoudiniEngineUnity folder in Assets/Plugins and select Delete.
1. Copy the Plugins/HoudiniEngineUnity folder from the cloned repository from step 2, and paste it into your Unity project's Assets/Plugins folder. If the Plugins folder exists, you can simply merge with it.
1. Restart Unity.
1. Ensure Houdini Engine loaded successfully by going to the "HoudiniEngine" top menu and selecting "Installation Info" and making sure all the versions match.
