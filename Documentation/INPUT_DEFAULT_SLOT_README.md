# Default input slot

Newly created HDA inputs start with one empty object slot, so an object can be assigned immediately without clicking **Add Slot**.

- Applies to inputs created through the shared input-node setup, including geometry inputs and node-input parameters.
- Both object and HDA input lists start with one empty slot. Transform offsets retain their normal identity defaults, including scale `(1, 1, 1)`.
- Add Slot still adds more entries. Clear still empties a list; drawing the Inspector does not recreate a cleared slot.
- Existing saved input lists and restored presets are not migrated or overwritten. A newly dropped HDA demonstrates the new default.
- This does not change the number of multiparm entries or scalar asset-path fields.

Check in Unity: drop an HDA with several inputs; confirm one empty slot per input; assign an object; switch a fresh input to HDA mode; add/remove/clear slots; recook and save/reopen. Also restore a preset and verify its exact input count. Unity compilation and live cooking remain unverified here.


## Instance Inputs — 13 October 2026

New Instance Inputs override lists now start empty. They no longer automatically add the original source object as an override. The generated instances continue using the source specified by the HDA when no overrides are present. Add an override manually when you want to substitute another object.

The default-slot feature for Input Nodes remains unchanged: newly created input nodes still start with one slot. Existing saved instance override entries are preserved; remove unwanted entries manually or use a fresh HDA instance to check the new default.

### Verification

- Create a fresh 05_baseline_prefab, assign a valid Unity Prefab and recook. Expect three instances and an empty Instance Inputs override list.
- Recook again: the list remains empty and instances are still generated.
- Add an instance override manually and recook: the replacement is used. Remove it and recook: the original HDA source is used again.
- Check packed/object instancing with no overrides and with an explicitly added override.
- Create a fresh HDA with input nodes: one input slot is still present by default.
- Save/reopen an HDA with explicit overrides: they remain intact.

Source paths were reviewed; Unity compilation and runtime verification are still required.
