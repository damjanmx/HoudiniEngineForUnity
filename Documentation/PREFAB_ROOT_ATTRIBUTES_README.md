# Baked prefab root attributes

Add these **scalar string attributes at Detail level** to the HDA's output geometry. They apply to **Bake Prefab** and **Update Prefab**, for both cache and scene-folder destinations.

| Attribute | Effect |
| --- | --- |
| `unity_layer_root` | Sets the named Unity layer on the baked prefab root and every descendant, including inactive objects and prefab instances. |
| `unity_script_root` | Attaches scripts to the baked prefab root using the existing `unity_script` syntax and type lookup. |

In a Detail Wrangle before the output:

```c
s@unity_layer_root = "Environment";
s@unity_script_root = "MyGame.EnvironmentRoot";
```

Create the `Environment` layer in Unity first and use the actual class name of your MonoBehaviour, including its namespace when applicable. Multiple scripts use the existing semicolon-separated syntax. Existing `unity_script` function/argument syntax is supported through the same attachment routine.

```c
s@unity_script_root = "MyGame.EnvironmentRoot;MyGame.Interactable";
```

## Behavior and precedence

- Attributes are read from the output parts eligible for baking, not from arbitrary internal SOPs. Ensure the attributes reach the output.
- Only scalar detail strings are accepted. Missing or empty values leave normal bake behavior unchanged.
- If multiple output parts supply the same value, it is applied once. For different non-empty values, the first in the plugin's output traversal wins independently for each attribute and a warning is logged. Use matching values across outputs to avoid ambiguity.
- `unity_layer_root` is applied after output creation and script attachment. It overrides per-object `unity_layer` values and layers preserved by `unity_use_instance_flags = 1` throughout the baked hierarchy. An unknown layer logs a warning and does not override layers.
- `unity_script_root` attaches to the root only. Existing child scripts from `unity_script` remain unchanged. A component of the same type already on the root is reused by the existing attachment routine.
- Script lookup, optional function invocation and error handling are the same as `unity_script`. The script must already exist in the Unity project.
- Live HDA output and standalone GameObject bakes are unaffected. The root is the actual final prefab root, whether the HDA has one output part or multiple parts.
- Update Prefab applies attributes to the rebuilt root before saving. Removing an attribute returns to the normal update process; it is not an explicit component-removal command.

## Verification

Source syntax and prefab save-hook checks were performed. Unity compilation and actual baking remain to be tested. Check a multiple-output HDA with inactive children: bake once, change both attributes, and update. Verify all baked layers and root script components, and verify that the live HDA retains its previous properties.
