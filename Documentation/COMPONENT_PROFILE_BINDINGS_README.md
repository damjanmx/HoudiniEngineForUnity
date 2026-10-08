# Component profile attribute bindings

## Set up a binding

1. Open a Component Profile asset and select its template components as usual.
2. Under **Attribute Bindings**, click **Add Attribute Binding**.
3. Choose one of **Components to Copy**, then choose its serialized **Property**. The picker displays its friendly label, exact property path and type.
4. Enter a Houdini attribute name starting with **`unity_cp_`**, for example `unity_cp_mass`.
5. Author that attribute on the HDA output and recook.

For a Rigidbody profile, select its Mass property in the picker and bind it to `unity_cp_mass`:

```c
s@unity_component_profile = "Assets/HDAProfiles/PhysicsProp.asset";
f@unity_cp_mass = 10.0;
```

Custom fields work the same way. Public serialized fields and fields marked `[SerializeField]`, including nested fields, appear in the picker. Select properties from the actual component; don't guess internal names such as `m_Mass`.

```c
f@unity_cp_movement_speed = 3.5;
i@unity_cp_audio_enabled = 1;
v@unity_cp_offset = set(0, 1, 0);
v@unity_cp_tint = set(1, 0.2, 0.1);
s@unity_cp_audio_clip = "Assets/Audio/Impact.wav";
```

Each line only affects a component property when a profile binding connects it to that property. Names do not infer a component or property automatically. All binding names require the prefix and a non-empty suffix.

## Scope

Bound values use the same source-element scope and owner precedence as the profile:

- Mesh outputs: primitive > point > vertex > detail. Values on primitives grouped into one `unity_path` output should agree. If they disagree, the first source element wins and a warning is logged.
- Prefab instances: point > detail.
- Packed instances: point > primitive > detail, with existing transform-count validation.
- Baked prefab root: detail only. Conflicting detail values across output parts use the first found value and warn.

The profile path and bound values do not need to use the same owner. A detail profile with primitive values is valid. The more specific owner wins independently for each bound value.

For root profiles, use the recommended `unity_cp_root_` naming convention:

```c
s@unity_component_profile_root = "Assets/HDAProfiles/EnvironmentRoot.asset";
f@unity_cp_root_mass = 20.0;
```

The root prefix is a naming convention; `unity_component_profile_root` determines root targeting. Root bindings must still use detail attributes. The same profile can be reused on different outputs if its binding names exist in each scope.

## Supported conversions

| Unity property | Houdini value |
| --- | --- |
| Float / double | One float or integer |
| Integer | One integer |
| Boolean | Integer `0` or `1` |
| String | One string |
| Character | One single-character string |
| Enum | Exact enum member name as string, or raw integer enum value (including flag masks) |
| LayerMask | Integer bitmask, not a layer index |
| Vector2 / Vector3 / Vector4 | Numeric tuple of 2 / 3 / 4 values |
| Vector2Int / Vector3Int | Integer tuple of 2 / 3 values |
| Color | RGB or RGBA numeric tuple; RGB uses alpha 1 |
| Quaternion | Numeric XYZW tuple, normalized; alternatively 3 Euler angles in degrees with the binding checkbox enabled |
| Rect / RectInt | Numeric / integer tuple: x, y, width, height |
| Bounds | Numeric tuple: center XYZ, size XYZ |
| BoundsInt | Integer tuple: position XYZ, size XYZ |
| Asset reference | String project asset path; empty string clears the reference |
| Array / List of the above | A flat tuple on one source element, with explicit Array Element Size |

The existing attribute reader uses HAPI 32-bit integer, float and string storage. A Unity double or long can receive these values, but this does not provide a 64-bit-precision Houdini transport. Unsupported HAPI storage types are skipped with a warning.

Values are assigned directly in Unity coordinates and units. This system does not flip Houdini axes or convert color spaces. HDR color values are not clamped. Euler conversion uses Unity's `Quaternion.Euler` convention.

Asset references must resolve to the destination property's type. Custom asset-field types are resolved through field reflection; built-in asset types use serialized type information. Asset paths do not specify a particular subasset among several of the same type.

## Array/list bindings

Select the array/list property itself. **Array Element Size** is the number of tuple values per item:

- Float, integer, boolean, enum, string and asset-reference lists: 1.
- Vector3 list: 3.
- RGBA color or quaternion list: 4 (Euler quaternion list: 3 with the checkbox enabled).

For example, a float tuple `(1, 2, 3, 4, 5, 6)` with Array Element Size 3 produces two Vector3 elements. The tuple length must divide evenly by the element size. The binding replaces the entire list with the converted items. A failed conversion skips the entire binding, including its staged resizing.

This uses fixed-size Houdini tuples, **not VEX variable-length array attributes** such as `f[]@...`. Author the desired tuple size with an Attribute Create SOP or another fixed-tuple authoring method. Values are taken from the first source primitive/point/detail element, not concatenated across all primitives. Empty HAPI tuples, jagged arrays, lists of arbitrary structs and polymorphic arrays are not supported.

## Order and missing values

1. Apply the component profile according to Keep Existing / Overwrite Settings.
2. Apply bindings in Inspector order. Later bindings to the same property win.
3. Continue the normal bake process; per-object bindings are resolved again when copying into bake targets.

Bindings are explicit overrides and apply even with **Keep Existing**. Missing attributes leave the profile-applied/current value intact. Invalid prefixes, property paths, tuple sizes, types and asset paths warn and skip the affected binding. Other bindings continue.

Root profile bindings run before `unity_script_root`'s optional function calls and the final `unity_layer_root` override. Those later operations can change the same settings.

Profiles are still additive. Removing a binding or attribute does not undo its previous effect on an object that survives a recook in Keep Existing mode. Overwrite Settings restores copied template settings first, then applies the remaining bindings.

## Limits and verification

Only writable serialized properties are supported. C# property setters, methods, dictionaries, managed-reference objects, AnimationCurve and Gradient construction, and arbitrary structs as whole values are not supported. Bind supported individual fields inside a serialized struct instead. Existing protected geometry-component restrictions still apply.

Source syntax and hook checks were performed. Unity compilation and live Inspector/cook/bake tests remain unverified.

Suggested check: bind Rigidbody mass, AudioSource enabled and clip, and custom fields for Vector3, Color, an enum and a float list. Test a detail profile with different primitive values on two `unity_path` outputs, two instance points, and a detail root profile. Test missing and wrong-type values. Then bake and update the prefab, including with Keep Existing enabled.
