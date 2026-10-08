# Component profiles

## Create a profile

1. Create a GameObject in Unity and add the desired root components, for example Rigidbody, AudioSource and your custom MonoBehaviour. Configure their settings.
2. Save it as a prefab, such as `Assets/HDAProfiles/InteractivePropTemplate.prefab`.
3. In the Project window, choose **Create > Houdini Engine > Component Profile**. Save it as `Assets/HDAProfiles/InteractiveProp.asset`.
4. Assign **Template Prefab** in the profile Inspector.
5. Under **Components to Copy**, tick the components to include. Select any other root components that those components reference.
6. Choose **Existing Components**: **Overwrite Settings** (default for new profiles) or **Keep Existing**. Existing saved profiles retain their selected mode.

The Inspector reports invalid selections and unsupported template references. Changing the template clears the selection. The template prefab must remain available while cooking and baking.

## Reference it from Houdini

For an individual generated mesh object, add a scalar primitive string (or a detail string to provide a shared value):

```c
s@unity_path = "props/crate";
s@unity_component_profile = "Assets/HDAProfiles/InteractiveProp.asset";
```

For prefab or packed instances, assign `unity_component_profile` on the instance points. It applies to each instance root, not its descendants.

For the final baked prefab root, use a scalar detail string:

```c
s@unity_component_profile_root = "Assets/HDAProfiles/EnvironmentRoot.asset";
```

| Attribute | Target and timing |
| --- | --- |
| `unity_component_profile` | Generated object or instance root during cooking; carried into its bake output. |
| `unity_component_profile_root` | Final baked prefab root, during Bake Prefab and Update Prefab. Does not affect live HDA output or standalone bake roots. |

Use project-relative asset paths, including `Assets/` and `.asset`. One profile is accepted per attribute value.

## Attribute ownership

Per-object profiles use the existing scoped lookup: mesh primitive > point > vertex > detail; prefab-instance point > detail; packed-instance point > primitive > detail with matching transform counts. Separate `unity_path` outputs can use different profiles. Conflicting values within a single mesh output use the first source value and warn; split the output when it needs different properties.

`unity_component_profile_root` is detail-only. Conflicting values on multiple baked outputs use the first non-empty value and warn, like the other root attributes.

## Existing components and references

- **Keep Existing** preserves existing component settings and adds missing components.
- **Overwrite Settings** copies the selected serialized settings onto matching existing components and adds missing ones.
- Components match by exact type and occurrence order. Multiple selected components of the same type map in profile selection order. Unity may prohibit multiple instances of some component types.
- References to selected template-root components are remapped to their target counterparts. References to the template root GameObject or Transform are remapped to the generated root.
- References between selected components can be forward or circular. All target components are allocated before settings are copied.
- Materials, textures, audio clips and other persistent project asset references are retained.
- References to template children, unselected template components or scene objects are rejected before applying the profile.
- Baking also remaps references between copied profile components from the generated source object to the baked object. Unsupported scene references introduced after profile application are cleared with a warning instead of retaining links back to live scene objects.
- Unity's required components may be added automatically. A requirement for a protected component is rejected; select required configurable components explicitly if their settings or references matter.

## Geometry protection and scope

Transforms, MeshFilters, Renderers, Colliders (including 2D), Terrain, LODGroup and Houdini Engine components cannot be selected. Their generated meshes, hierarchy, materials and collision setup remain owned by the existing plugin. Use Houdini collision groups for collision generation. Scripts requiring one of these protected components are also rejected, except Transform, which every target already has.

Profiles copy serialized component settings; they do not copy the template hierarchy, layer, tag, static flags or Transform. They do not invoke `unity_script` function strings themselves. Unity component lifecycle callbacks can still run when components are added or serialized values change.

Per-object profiles run after ordinary script attributes. The root profile runs before `unity_script_root`, and `unity_layer_root` then sets the final root/descendant layers. Profile assignment is independent of `unity_use_instance_flags`.

Profiles are additive, not a component-removal system. Removing or changing an attribute does not remove components already attached to an output that survives recooking, and Keep Existing does not refresh their settings. Rebuild/recreate that output to remove stale additions, or choose Overwrite Settings to refresh matching settings. Extra existing components are retained.

Missing or invalid profiles log warnings and are skipped. Copy errors attempt to restore overwritten settings and remove newly added components. Nonserialized runtime state, references to arbitrary other generated objects and template-child hierarchies are outside this feature.

## Verification in Unity

Source syntax and integration checks passed; Unity compilation and live cook/bake tests have not been run here.

- Create a profile with Rigidbody, AudioSource and two custom scripts referring to each other; include a reference to an AudioClip asset.
- Assign it to two differently named mesh outputs and prefab/packed instance roots. Confirm each gets its own component references and retains its generated mesh and flags.
- Recook using Keep Existing and Overwrite Settings, checking that components do not duplicate and settings follow the selected policy.
- Bake and Update Prefab; confirm no profile component points back to the template or live HDA counterparts.
- Apply a root profile alongside `unity_script_root` and `unity_layer_root`.
- Test an invalid child reference and confirm the profile is rejected with a warning.

## Attribute-driven property values

Profiles now support **Attribute Bindings** with required `unity_cp_` names. Use the component/property pickers to connect scoped Houdini attributes to serialized Unity values. See [COMPONENT_PROFILE_BINDINGS_README.md](COMPONENT_PROFILE_BINDINGS_README.md) for setup, types, arrays and root bindings.
