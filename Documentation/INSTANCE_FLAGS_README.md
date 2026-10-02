# Instance flag compatibility fix

Set `i@unity_use_instance_flags = 1;` on instancer points to preserve each source object's tag, layer and exact static flags, including different settings on its descendants. Packed instances also support primitive values; detail values provide defaults. Packed point/primitive counts must match the instance transform count.

`unity_instance_parent`, `unity_instance_name` and `unity_instance_suffix` still apply. Packed prototypes generated with `unity_path` retain their mesh hierarchy and per-object flags. Materials, collision geometry, scripts and stored attributes continue through their existing processing.

When the value is 1, `unity_tag`, `unity_layer` and `unity_static` on the instancer do not overwrite the source flags. Use 0 (or omit the attribute) to allow those overrides. Ordinary non-instance `unity_path` mesh outputs still use their own attributes.

The fix guards both initial creation and the later attribute pass, avoids recursively resetting existing instances during output setup, and preserves individual editor static flag bits when baking.

Regression tests are included in Validation~/ScopedOutputs. Unity execution and HDA cook/bake verification are still required in the target Unity project.
